using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeDecorator.Core.Services;

namespace HomeDecorator.MauiApp.Services
{
    /// <summary>
    /// Mock implementation of IRecommendationService for development
    /// </summary>
    public class MockRecommendationService : IRecommendationService
    {
        // Mock product IDs for testing
        private readonly List<string> _mockProductIds = new List<string>
        {
            "mock-product-1",
            "mock-product-2",
            "mock-product-3"
        };

        public Task<List<string>> GetRecommendationsAsync(string imageRequestId)
        {
            // Return mock product IDs
            return Task.FromResult(_mockProductIds);
        }

        public Task<List<string>> RankAndFilterProductsAsync(List<(string ProductId, double Score)> productMatches)
        {
            // Extract product IDs, sort by score, and return
            var rankedProducts = productMatches
                .OrderByDescending(p => p.Score)
                .Select(p => p.ProductId)
                .ToList();

            return Task.FromResult(rankedProducts);
        }
    }
}
