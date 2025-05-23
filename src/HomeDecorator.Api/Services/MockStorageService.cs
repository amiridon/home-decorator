using HomeDecorator.Core.Services;

namespace HomeDecorator.Api.Services;

/// <summary>
/// Mock storage service for development/testing
/// </summary>
public class MockStorageService : IStorageService
{
    private readonly ILogger<MockStorageService> _logger;

    public MockStorageService(ILogger<MockStorageService> logger)
    {
        _logger = logger;
    }

    public Task<string> StoreImageFromUrlAsync(string imageUrl, string category)
    {
        _logger.LogInformation("Mock: Would store image from URL: {ImageUrl} in category: {Category}", imageUrl, category);

        // Return a mock URL that looks realistic
        var mockUrl = $"/images/{category}/mock-{Guid.NewGuid()}.png";
        return Task.FromResult(mockUrl);
    }
    public Task<string> StoreImageFromStreamAsync(Stream imageStream, string fileName, string category)
    {
        _logger.LogInformation("Mock: Would store image: {FileName} in category: {Category}", fileName, category);

        // Return a mock URL that looks realistic
        var mockUrl = $"/images/{category}/mock-{fileName}";
        return Task.FromResult(mockUrl);
    }

    public Task<bool> DeleteImageAsync(string imageUrl)
    {
        _logger.LogInformation("Mock: Would delete image: {ImageUrl}", imageUrl);
        return Task.FromResult(true);
    }
}
