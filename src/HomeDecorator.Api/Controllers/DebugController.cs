using HomeDecorator.Api.Services;
using Microsoft.AspNetCore.Mvc;
using OpenAI;
using OpenAI.Images;
using System.Reflection;
using HomeDecorator.Core.Services;

namespace HomeDecorator.Api.Controllers;

/// <summary>
/// Debug controller for diagnosing API issues
/// </summary>
[ApiController]
[Route("api/debug")]
public class DebugController : ControllerBase
{
    private readonly ILogger<DebugController> _logger;
    private readonly OpenAIClient _openAIClient;
    private readonly IStorageService _storageService;

    public DebugController(
        ILogger<DebugController> logger,
        IConfiguration configuration,
        IStorageService storageService)
    {
        _logger = logger;
        _storageService = storageService;

        // Initialize OpenAI client
        var apiKey = configuration["DallE:ApiKey"] ??
                     Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
                     configuration["OpenAI:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key not configured");
        }

        _openAIClient = new OpenAIClient(apiKey);
    }

    [HttpGet("test-dalle-response")]
    public async Task<IActionResult> TestDalleResponse()
    {
        try
        {
            _logger.LogInformation("Running DALL-E response debug test");

            // Create temporary file with a simple image
            var tempImagePath = Path.Combine(Path.GetTempPath(), $"debug_input_{Guid.NewGuid()}.png");

            // Download a sample image to use for the test
            string sampleImageUrl = "https://images.pexels.com/photos/1571460/pexels-photo-1571460.jpeg";
            using (var client = new HttpClient())
            {
                var imageBytes = await client.GetByteArrayAsync(sampleImageUrl);
                await System.IO.File.WriteAllBytesAsync(tempImagePath, imageBytes);
            }

            _logger.LogInformation("Created temporary test image: {TempPath}", tempImagePath);

            try
            {
                // Test with DALL-E 2
                var imageClient = _openAIClient.GetImageClient("dall-e-2");

                _logger.LogInformation("Calling DALL-E 2 API for image variation test");
                var response = await imageClient.GenerateImageVariationAsync(tempImagePath);

                if (response == null || response.Value == null)
                {
                    return BadRequest("DALL-E API returned null response");
                }

                // Log detailed response properties
                var generatedImage = response.Value;
                var properties = generatedImage.GetType().GetProperties();

                var responseDetails = new Dictionary<string, string>();
                foreach (var prop in properties)
                {
                    try
                    {
                        var value = prop.GetValue(generatedImage);
                        responseDetails.Add(prop.Name, value?.ToString() ?? "null");
                        _logger.LogInformation("Property: {PropertyName}, Value: {PropertyValue}", prop.Name, value?.ToString() ?? "null");
                    }
                    catch (Exception ex)
                    {
                        responseDetails.Add(prop.Name, $"Error: {ex.Message}");
                    }
                }

                // Extract URL
                string? imageUrl = null;
                var urlProperty = generatedImage.GetType().GetProperty("Url");
                if (urlProperty != null)
                {
                    var urlValue = urlProperty.GetValue(generatedImage);
                    imageUrl = urlValue?.ToString();
                }

                if (string.IsNullOrEmpty(imageUrl))
                {
                    return BadRequest("Could not find image URL in DALL-E response");
                }

                // Test URL accessibility
                bool isUrlAccessible = false;
                try
                {
                    using (var client = new HttpClient())
                    {
                        var urlResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, imageUrl));
                        isUrlAccessible = urlResponse.IsSuccessStatusCode;
                        _logger.LogInformation("URL accessibility test: {StatusCode}", urlResponse.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error testing URL accessibility");
                }

                return Ok(new
                {
                    Success = true,
                    ImageType = generatedImage.GetType().Name,
                    ResponseProperties = responseDetails,
                    ImageUrl = imageUrl,
                    IsUrlAccessible = isUrlAccessible
                });
            }
            finally
            {
                // Clean up
                if (System.IO.File.Exists(tempImagePath))
                {
                    System.IO.File.Delete(tempImagePath);
                    _logger.LogInformation("Deleted temporary file: {TempPath}", tempImagePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Debug test failed");
            return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
        }
    }
}
