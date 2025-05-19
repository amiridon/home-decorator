using HomeDecorator.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HomeDecorator.Api.Services
{
    /// <summary>
    /// Mock implementation of IBillingService for development
    /// </summary>
    public class MockBillingService : IBillingService
    {
        private readonly ICreditLedgerService _creditLedgerService;

        // Credit pack definitions
        private readonly Dictionary<string, (int Credits, decimal Price)> _creditPacks = new()
        {
            ["starter-pack"] = (50, 4.99m),
            ["standard-pack"] = (200, 14.99m),
            ["premium-pack"] = (500, 29.99m),
            ["pro-pack"] = (1200, 59.99m)
        };

        public MockBillingService(ICreditLedgerService creditLedgerService)
        {
            _creditLedgerService = creditLedgerService;
        }

        public async Task<bool> HasEnoughCreditsAsync(string userId, int requiredCredits)
        {
            try
            {
                int balance = await _creditLedgerService.GetBalanceAsync(userId);
                return balance >= requiredCredits;
            }
            catch
            {
                // In case of any error in mock mode, return true
                return true;
            }
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
            catch
            {
                // In mock mode, we can fail gracefully
                return false;
            }
        }

        public Task<string> GetCheckoutUrlAsync(string userId, string packId)
        {
            // Simulate purchasing credits by immediately adding them
            if (_creditPacks.TryGetValue(packId, out var packInfo))
            {
                var (credits, price) = packInfo;

                // In a real app, this would redirect to Stripe
                // In mock mode, we'll just add the credits directly
                _ = _creditLedgerService.AddCreditsAsync(
                    userId,
                    credits,
                    "Purchase",
                    $"Purchased {credits} credits for ${price}",
                    $"mock-payment-{Guid.NewGuid()}");
            }

            // Return a fake checkout URL
            return Task.FromResult($"https://fake-stripe-checkout.example.com/checkout?session=mock-session-{packId}");
        }

        public Task<string> GetBillingPortalUrlAsync(string userId)
        {
            // Return a fake portal URL
            return Task.FromResult($"https://fake-stripe-portal.example.com/portal?customer={userId}");
        }

        public Task<bool> HandleWebhookAsync(string json, string signature)
        {
            // Simulate webhook processing
            Console.WriteLine($"[MOCK] Processing webhook: {json.Substring(0, Math.Min(json.Length, 100))}...");

            // Always succeed in mock mode
            return Task.FromResult(true);
        }
    }
}
