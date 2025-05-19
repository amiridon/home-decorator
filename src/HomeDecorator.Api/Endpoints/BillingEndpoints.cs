using HomeDecorator.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HomeDecorator.Api.Endpoints
{
    public static class BillingEndpoints
    {
        public static void MapBillingEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/billing/checkout/{packId}", async (string packId, IBillingService billingService, HttpContext context) =>
            {
                try
                {
                    // In a real app, get the userId from the authenticated user
                    string userId = context.User.Identity?.Name ?? "test-user";

                    // Get checkout URL from the billing service
                    string checkoutUrl = await billingService.GetCheckoutUrlAsync(userId, packId);

                    return Results.Ok(new { url = checkoutUrl });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .RequireAuthorization()
            .WithName("BillingCheckout");

            app.MapGet("/api/billing/portal", async (IBillingService billingService, HttpContext context) =>
            {
                try
                {
                    // In a real app, get the userId from the authenticated user
                    string userId = context.User.Identity?.Name ?? "test-user";

                    // Get billing portal URL from the service
                    string portalUrl = await billingService.GetBillingPortalUrlAsync(userId);

                    return Results.Ok(new { url = portalUrl });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .RequireAuthorization()
            .WithName("BillingPortal");

            app.MapGet("/api/billing/credits", async (ICreditLedgerService creditLedgerService, HttpContext context) =>
            {
                try
                {
                    // In a real app, get the userId from the authenticated user
                    string userId = context.User.Identity?.Name ?? "test-user";

                    // Get credit balance
                    int balance = await creditLedgerService.GetBalanceAsync(userId);

                    return Results.Ok(new { credits = balance });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .RequireAuthorization()
            .WithName("GetCredits");

            app.MapGet("/api/billing/transactions", async (ICreditLedgerService creditLedgerService, int count, HttpContext context) =>
            {
                try
                {
                    // In a real app, get the userId from the authenticated user
                    string userId = context.User.Identity?.Name ?? "test-user";

                    // Get transaction history (default to 20 if not specified)
                    var transactions = await creditLedgerService.GetTransactionHistoryAsync(userId, count > 0 ? count : 20);

                    return Results.Ok(transactions);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .RequireAuthorization()
            .WithName("GetTransactions");

            app.MapPost("/api/stripe/webhook", async (HttpContext context, IBillingService billingService) =>
            {
                try
                {
                    // Read the request body
                    using var reader = new StreamReader(context.Request.Body);
                    var json = await reader.ReadToEndAsync();

                    // Get the Stripe signature header
                    if (!context.Request.Headers.TryGetValue("Stripe-Signature", out var signatureValues) || signatureValues.Count == 0)
                    {
                        return Results.BadRequest(new { error = "Missing Stripe-Signature header" });
                    }

                    string signature = signatureValues.First();

                    // Handle the webhook
                    bool handled = await billingService.HandleWebhookAsync(json, signature);

                    if (handled)
                    {
                        return Results.Ok(new { received = true });
                    }
                    else
                    {
                        return Results.BadRequest(new { error = "Failed to process webhook" });
                    }
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .AllowAnonymous()
            .WithName("StripeWebhook");
        }
    }
}
