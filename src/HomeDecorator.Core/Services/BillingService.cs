namespace HomeDecorator.Core.Services;

/// <summary>
/// Service for handling billing operations
/// Wraps Stripe SDK, manages ledger and webhooks
/// </summary>
public interface IBillingService
{
    /// <summary>
    /// Checks if the user has enough credits for a generation
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="requiredCredits">Number of credits required</param>
    /// <returns>True if user has enough credits, false otherwise</returns>
    Task<bool> HasEnoughCreditsAsync(string userId, int requiredCredits);

    /// <summary>
    /// Deducts credits from a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="credits">Number of credits to deduct</param>
    /// <returns>True if deduction was successful, false otherwise</returns>
    Task<bool> DeductCreditsAsync(string userId, int credits);

    /// <summary>
    /// Gets a checkout URL for purchasing credit packs
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="packId">The ID of the credit pack</param>
    /// <returns>URL for Stripe checkout</returns>
    Task<string> GetCheckoutUrlAsync(string userId, string packId);

    /// <summary>
    /// Gets a URL for the Stripe billing portal
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>URL for Stripe billing portal</returns>
    Task<string> GetBillingPortalUrlAsync(string userId);

    /// <summary>
    /// Handles Stripe webhook events
    /// </summary>
    /// <param name="json">The JSON payload from Stripe</param>
    /// <param name="signature">The Stripe signature header</param>
    /// <returns>True if handled successfully, false otherwise</returns>
    Task<bool> HandleWebhookAsync(string json, string signature);
}
