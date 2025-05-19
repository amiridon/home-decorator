using HomeDecorator.Core.Services;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HomeDecorator.Api.Services
{
    /// <summary>
    /// Implementation of IBillingService using Stripe
    /// </summary>
    public class StripeService : IBillingService
    {
        private readonly string _secretKey;
        private readonly string _publishableKey;
        private readonly string _webhookSecret;
        private readonly ICreditLedgerService _creditLedgerService;

        // Credit pack definitions
        private readonly Dictionary<string, (int Credits, decimal Price)> _creditPacks = new()
        {
            ["starter-pack"] = (50, 4.99m),
            ["standard-pack"] = (200, 14.99m),
            ["premium-pack"] = (500, 29.99m),
            ["pro-pack"] = (1200, 59.99m)
        };

        public StripeService(IConfiguration configuration, ICreditLedgerService creditLedgerService)
        {
            _secretKey = configuration["Stripe:SecretKey"] ??
                throw new InvalidOperationException("Stripe:SecretKey not found in configuration");
            _publishableKey = configuration["Stripe:PublishableKey"] ??
                throw new InvalidOperationException("Stripe:PublishableKey not found in configuration");
            _webhookSecret = configuration["Stripe:WebhookSecret"] ??
                throw new InvalidOperationException("Stripe:WebhookSecret not found in configuration");

            _creditLedgerService = creditLedgerService ??
                throw new ArgumentNullException(nameof(creditLedgerService));

            // Initialize Stripe
            StripeConfiguration.ApiKey = _secretKey;
        }

        public async Task<bool> HasEnoughCreditsAsync(string userId, int requiredCredits)
        {
            var balance = await _creditLedgerService.GetBalanceAsync(userId);
            return balance >= requiredCredits;
        }

        public async Task<bool> DeductCreditsAsync(string userId, int credits)
        {
            try
            {
                await _creditLedgerService.DeductCreditsAsync(
                    userId,
                    credits,
                    "Usage",
                    $"Used {credits} credits for image generation",
                    null);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to deduct credits: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetCheckoutUrlAsync(string userId, string packId)
        {
            if (!_creditPacks.TryGetValue(packId, out var pack))
            {
                throw new ArgumentException($"Invalid credit pack ID: {packId}");
            }

            // Convert decimal price to long (cents)
            long priceCents = (long)(pack.Price * 100);

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = priceCents,
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"{packId} - {pack.Credits} Credits",
                                Description = $"Purchase {pack.Credits} credits for Home Decorator"
                            }
                        },
                        Quantity = 1
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    { "userId", userId },
                    { "packId", packId },
                    { "credits", pack.Credits.ToString() }
                },
                Mode = "payment",
                SuccessUrl = "https://yourdomain.com/billing/success?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = "https://yourdomain.com/billing/cancel"
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return session.Url;
        }

        public async Task<string> GetBillingPortalUrlAsync(string userId)
        {
            // First, get the customer ID for this user
            // In a real app, you'd look this up in your database
            string customerId = await GetOrCreateStripeCustomerAsync(userId);

            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = "https://yourdomain.com/billing"
            };

            var service = new Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(options);

            return session.Url;
        }

        public async Task<bool> HandleWebhookAsync(string json, string signature)
        {
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    signature,
                    _webhookSecret
                );                // Handle specific event types
                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;

                    if (session != null && session.PaymentStatus == "paid")
                    {
                        // Get metadata from the session
                        string userId = session.Metadata["userId"];
                        string packId = session.Metadata["packId"];
                        int credits = int.Parse(session.Metadata["credits"]);

                        // Add credits to the user's account
                        await _creditLedgerService.AddCreditsAsync(
                            userId,
                            credits,
                            "Purchase",
                            $"Purchased {credits} credits",
                            session.Id);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to handle webhook: {ex.Message}");
                return false;
            }
        }

        private Task<string> GetOrCreateStripeCustomerAsync(string userId)
        {
            // In a real implementation, you would:
            // 1. Check if the user already has a Stripe customer ID in your database
            // 2. If not, create a new customer in Stripe and save the ID
            // 3. Return the customer ID

            // For simplicity in this example, we'll return a fake ID
            return Task.FromResult($"cus_mock_{userId}");
        }
    }
}
