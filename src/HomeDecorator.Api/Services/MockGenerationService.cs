using HomeDecorator.Core.Services;

namespace HomeDecorator.Api.Services;

/// <summary>
/// Mock implementation of IGenerationService for development
/// </summary>
public class MockGenerationService : IGenerationService
{
    private readonly ILogger<MockGenerationService> _logger;

    public MockGenerationService(ILogger<MockGenerationService> logger)
    {
        _logger = logger;
    }

    public async Task<string> GenerateImageAsync(string originalImageUrl, string prompt)
    {
        _logger.LogInformation("[MOCK] Generating image for prompt: {Prompt}", prompt);
        
        // Simulate generation delay
        await Task.Delay(2000);
        
        // Return a mock generated image URL
        var mockImageUrl = $"https://via.placeholder.com/1024x1024?text=AI+Generated+Design+(Mock)";
        
        _logger.LogInformation("[MOCK] Generated image: {ImageUrl}", mockImageUrl);
        return mockImageUrl;
    }    public Task<string> GetGenerationStatusAsync(string requestId)
    {
        _logger.LogInformation("[MOCK] Getting status for request: {RequestId}", requestId);
        
        // Always return completed status in mock mode
        return Task.FromResult("Completed");
    }

    public Task<List<string>> GetRecentGenerationsAsync(string userId, int count = 5)
    {
        _logger.LogInformation("[MOCK] Getting recent generations for user: {UserId}", userId);
        
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
