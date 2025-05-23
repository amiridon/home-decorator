using HomeDecorator.Core.Services;
using OpenAI;
using OpenAI.Images;

namespace HomeDecorator.Api.Services;

/// <summary>
/// Real implementation of IGenerationService using DALL-E API
/// </summary>
public class DalleGenerationService : IGenerationService
{
    private readonly OpenAIClient _openAIClient;
    private readonly IStorageService _storageService;
    private readonly ILogger<DalleGenerationService> _logger;

    public DalleGenerationService(
        IConfiguration configuration,
        IStorageService storageService,
        ILogger<DalleGenerationService> logger)
    {
        var apiKey = configuration["DallE:ApiKey"] ??
                    throw new InvalidOperationException("DallE:ApiKey configuration is missing");

        _openAIClient = new OpenAIClient(apiKey);
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<string> GenerateImageAsync(string originalImageUrl, string prompt)
    {
        try
        {
            _logger.LogInformation("Starting DALL-E image generation for prompt: {Prompt}", prompt);

            // Create a comprehensive prompt for home decoration
            var enhancedPrompt = BuildEnhancedPrompt(prompt, originalImageUrl);

            var client = _openAIClient.GetImageClient("dall-e-3");
            var options = new ImageGenerationOptions
            {
                Size = GeneratedImageSize.W1024xH1024,
                Quality = GeneratedImageQuality.Standard,
                Style = GeneratedImageStyle.Natural
                // Url is the default format
            };

            _logger.LogInformation("Calling DALL-E API with enhanced prompt");
            var response = await client.GenerateImageAsync(enhancedPrompt, options);

            if (response?.Value?.ImageUri == null)
            {
                throw new InvalidOperationException("DALL-E API returned null response");
            }

            _logger.LogInformation("DALL-E generation successful, storing image");

            // Download and store the image permanently
            var storedImageUrl = await _storageService.StoreImageFromUrlAsync(
                response.Value.ImageUri.ToString(),
                "generated");

            _logger.LogInformation("Image stored successfully at: {StoredUrl}", storedImageUrl);
            return storedImageUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image with DALL-E");
            throw;
        }
    }
    public Task<string> GetGenerationStatusAsync(string requestId)
    {
        // DALL-E is synchronous, so we always return completed
        // In a real implementation, this might track async jobs
        return Task.FromResult("Completed");
    }

    public Task<List<string>> GetRecentGenerationsAsync(string userId, int count = 5)
    {
        _logger.LogInformation("Getting recent generations for user: {UserId}", userId);

        // This would typically query the database for recent generations
        // For now, return empty list since we don't have user context here
        // The proper implementation will be through the ImageGenerationOrchestrator
        return Task.FromResult(new List<string>());
    }

    private string BuildEnhancedPrompt(string userPrompt, string originalImageUrl)
    {
        // Enhance the user's prompt with context for home decoration
        return $"Create a beautiful, professionally designed interior/exterior home space. " +
               $"Focus on: {userPrompt}. " +
               $"Style: modern, elegant, well-lit, magazine-quality photography. " +
               $"Ensure the space looks realistic, inviting, and professionally decorated. " +
               $"Include proper lighting, color coordination, and high-end finishes.";
    }
}
