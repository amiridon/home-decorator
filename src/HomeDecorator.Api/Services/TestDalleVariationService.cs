using HomeDecorator.Core.Services;
using Microsoft.Extensions.Logging;

namespace HomeDecorator.Api.Services;

/// <summary>
/// Test class for DALL-E 2 image variation implementation
/// </summary>
public class TestDalleVariationService
{
    private readonly IGenerationService _generationService;
    private readonly ILogger<TestDalleVariationService> _logger;

    public TestDalleVariationService(
        IGenerationService generationService,
        ILogger<TestDalleVariationService> logger)
    {
        _generationService = generationService;
        _logger = logger;
    }

    /// <summary>
    /// Tests the DALL-E 2 image variation functionality
    /// </summary>
    public async Task TestVariationGenerationAsync(string testImageUrl)
    {
        _logger.LogInformation("Testing DALL-E 2 image variation with image: {ImageUrl}", testImageUrl);

        try
        {
            // Call the image variation API with a test prompt
            string resultUrl = await _generationService.GenerateImageAsync(
                testImageUrl,
                "Test prompt - please generate a variation of this image",
                "Modern");

            _logger.LogInformation("DALL-E 2 test successful! Generated image URL: {ResultUrl}", resultUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test of DALL-E 2 image variation failed: {Message}", ex.Message);
            throw;
        }
    }
}
