using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeDecorator.Core.Services;

namespace HomeDecorator.MauiApp.Services
{
    /// <summary>
    /// Mock implementation of IBillingService for development
    /// </summary>
    public class MockBillingService : IBillingService
    {
        public Task<bool> HasEnoughCreditsAsync(string userId, int requiredCredits)
        {
            // Always return true in mock mode
            return Task.FromResult(true);
        }

        public Task<bool> DeductCreditsAsync(string userId, int credits)
        {
            // Always succeed in mock mode
            return Task.FromResult(true);
        }

        public Task<string> GetCheckoutUrlAsync(string userId, string packId)
        {
            // Return a fake checkout URL
            return Task.FromResult("https://fake-stripe-checkout.example.com/checkout?session=mock-session-id");
        }

        public Task<string> GetBillingPortalUrlAsync(string userId)
        {
            // Return a fake portal URL
            return Task.FromResult("https://fake-stripe-portal.example.com/portal?customer=mock-customer-id");
        }

        public Task<bool> HandleWebhookAsync(string json, string signature)
        {
            // Always succeed in mock mode
            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// Mock implementation of IGenerationService for development
    /// </summary>
    public class MockGenerationService : IGenerationService
    {
        public Task<string> GenerateImageAsync(string originalImageUrl, string prompt)
        {
            // Return a placeholder image URL
            return Task.FromResult("https://via.placeholder.com/400x300?text=Generated+Image+(Mock)");
        }

        public Task<string> GenerateImageAsync(string originalImageUrl, string prompt, string decorStyle)
        {
            // Return a placeholder image URL that includes the decor style for mock purposes
            return Task.FromResult($"https://via.placeholder.com/400x300?text=Generated+Image+(Mock)+Style:{decorStyle}");
        }

        public Task<string> GetGenerationStatusAsync(string requestId)
        {
            // Always return completed status
            return Task.FromResult("Completed");
        }

        public Task<List<string>> GetRecentGenerationsAsync(string userId, int count = 5)
        {
            // Return some placeholder images
            var images = new List<string>
            {
                "https://via.placeholder.com/400x300?text=Mock+Image+1",
                "https://via.placeholder.com/400x300?text=Mock+Image+2",
                "https://via.placeholder.com/400x300?text=Mock+Image+3"
            };

            return Task.FromResult(images);
        }
    }

    /// <summary>
    /// Mock implementation of IProductMatcherService for development
    /// </summary>
    public class MockProductMatcherService : IProductMatcherService
    {
        public Task<List<(string ProductId, double Score)>> DetectAndMatchProductsAsync(string imageUrl)
        {
            // Return fake product matches with scores
            var products = new List<(string ProductId, double Score)>
            {
                ("mock-product-1", 0.95),
                ("mock-product-2", 0.85),
                ("mock-product-3", 0.75)
            };

            return Task.FromResult(products);
        }
    }    /// <summary>
         /// Mock implementation of IRecommendationService for development
         /// This is kept for backward compatibility, implementation moved to separate file
         /// </summary>
    [Obsolete("Use the separate MockRecommendationService class instead")]
    public class MockRecommendationService_Obsolete { }
}
