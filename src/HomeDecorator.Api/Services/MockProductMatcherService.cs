using HomeDecorator.Core.Services;

namespace HomeDecorator.Api.Services;

/// <summary>
/// Mock implementation of IProductMatcherService for development
/// </summary>
public class MockProductMatcherService : IProductMatcherService
{
    private readonly ILogger<MockProductMatcherService> _logger;

    public MockProductMatcherService(ILogger<MockProductMatcherService> logger)
    {
        _logger = logger;
    }

    public Task<List<(string ProductId, double Score)>> DetectAndMatchProductsAsync(string imageUrl)
    {
        _logger.LogInformation("[MOCK] Detecting and matching products for image: {ImageUrl}", imageUrl);

        // Return mock product matches with realistic scores
        var products = new List<(string ProductId, double Score)>
        {
            ("mock-sofa-001", 0.92),
            ("mock-coffee-table-003", 0.87),
            ("mock-lamp-015", 0.81),
            ("mock-rug-007", 0.75),
            ("mock-wall-art-012", 0.68)
        };

        _logger.LogInformation("[MOCK] Found {ProductCount} product matches", products.Count);
        return Task.FromResult(products);
    }
}
