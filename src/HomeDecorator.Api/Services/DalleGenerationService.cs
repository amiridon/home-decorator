using HomeDecorator.Core.Services;
using OpenAI;
using OpenAI.Images;
using System.Net.Http;
using System.Reflection;

namespace HomeDecorator.Api.Services;

/// <summary>
/// Real implementation of IGenerationService using DALL-E API
/// </summary>
public class DalleGenerationService : IGenerationService
{
    private readonly OpenAIClient _openAIClient;
    private readonly IStorageService _storageService;
    private readonly ILogger<DalleGenerationService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public DalleGenerationService(
        IConfiguration configuration,
        IStorageService storageService,
        ILogger<DalleGenerationService> logger)
    {
        _configuration = configuration;

        try
        {
            // Look for API key in various locations
            var apiKey = configuration["DallE:ApiKey"] ??
                          Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
                          configuration["OpenAI:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                // Log available configuration keys to help troubleshoot
                LogAvailableConfigurationKeys(configuration, logger);

                logger.LogError("DallE:ApiKey configuration is missing. Check user secrets, environment variables, or appsettings.json");
                throw new InvalidOperationException("DallE:ApiKey configuration is missing");
            }

            // If we found it, log where we found it
            if (configuration["DallE:ApiKey"] != null)
                logger.LogInformation("DALL-E API key found in configuration");
            else if (Environment.GetEnvironmentVariable("OPENAI_API_KEY") != null)
                logger.LogInformation("DALL-E API key found in environment variable");
            else
                logger.LogInformation("DALL-E API key found in OpenAI configuration");

            logger.LogInformation("DALL-E API key found with length: {KeyLength}", apiKey.Length);
            _openAIClient = new OpenAIClient(apiKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize OpenAI client: {Message}", ex.Message);
            throw new InvalidOperationException($"Failed to initialize OpenAI client: {ex.Message}", ex);
        }

        _storageService = storageService;
        _logger = logger;
        _httpClient = new HttpClient(new HttpClientHandler
        {
            // For development, accept all certificates
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
    }

    public async Task<string> GenerateImageAsync(string originalImageUrl, string prompt, string decorStyle)
    {
        try
        {
            _logger.LogInformation("Starting DALL-E 2 image variation generation for image: {OriginalImage}, decor style: {DecorStyle}", originalImageUrl, decorStyle);

            // Log the prompt for context, even though we won't use it directly with the image variation API
            if (!string.IsNullOrEmpty(prompt))
            {
                _logger.LogInformation("User prompt (for context only): {Prompt}", prompt);
            }

            // Validate API key first
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
                _configuration["DallE:ApiKey"] ??
                _configuration["OpenAI:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("API key is not configured.");
                throw new InvalidOperationException("API key is not configured.");
            }

            _logger.LogInformation("Using API key that starts with: {ApiKeyStart}",
                apiKey.Length > 5 ? apiKey.Substring(0, 5) + "..." : "invalid");            // Download the original image if it's a URL
            byte[]? originalImageBytes = null;
            Uri? imageUri = null;

            if (Uri.TryCreate(originalImageUrl, UriKind.Absolute, out imageUri))
            {
                try
                {
                    _logger.LogInformation("Downloading original image from URL");
                    originalImageBytes = await _httpClient.GetByteArrayAsync(imageUri);
                    _logger.LogInformation("Successfully downloaded original image: {Size} bytes", originalImageBytes.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to download original image from URL: {URL}", originalImageUrl);
                    throw new InvalidOperationException($"Unable to download image from {originalImageUrl}: {ex.Message}", ex);
                }
            }
            else
            {
                _logger.LogWarning("Invalid original image URL provided: {Url}", originalImageUrl);
                throw new ArgumentException($"Invalid image URL format: {originalImageUrl}");
            }

            if (originalImageBytes == null || originalImageBytes.Length == 0)
            {
                throw new InvalidOperationException("Failed to obtain image data from the provided URL");
            }

            try
            {
                // Save image to a temporary file since the SDK requires a file path for variations
                string tempImagePath = Path.Combine(Path.GetTempPath(), $"dalle_input_{Guid.NewGuid()}.png");

                try
                {
                    // Save the downloaded image to the temporary file
                    await File.WriteAllBytesAsync(tempImagePath, originalImageBytes);
                    _logger.LogInformation("Saved input image to temporary file: {TempPath}", tempImagePath);

                    // Initialize the ImageClient with DALL-E 2 model
                    var imageClient = _openAIClient.GetImageClient("dall-e-2");

                    _logger.LogInformation("Calling DALL-E 2 API with image variation request");

                    try
                    {                        // Create image variation using the file path
                        var response = await imageClient.GenerateImageVariationAsync(tempImagePath);

                        _logger.LogInformation("Successfully received DALL-E 2 API response");

                        // Get the generated image from the response
                        if (response == null || response.Value == null)
                        {
                            _logger.LogError("DALL-E API returned null response");
                            throw new InvalidOperationException("DALL-E API returned null response");
                        }                        // Inspect the generated image object to find the URL
                        var generatedImage = response.Value;

                        // Log all available properties for debugging
                        _logger.LogInformation("Generated image type: {Type}", generatedImage.GetType().Name);

                        // Try to extract URL from different potential properties
                        string? generatedImageUrl = null;

                        // Try to use reflection to find the URL property
                        var urlProperty = generatedImage.GetType().GetProperty("Url") ??
                                          generatedImage.GetType().GetProperty("Uri") ??
                                          generatedImage.GetType().GetProperty("ImageUrl");

                        if (urlProperty != null)
                        {
                            var urlValue = urlProperty.GetValue(generatedImage);
                            generatedImageUrl = urlValue?.ToString();
                        }

                        // If we couldn't find a URL property, try to use ToString() on the image itself
                        if (string.IsNullOrEmpty(generatedImageUrl))
                        {
                            generatedImageUrl = generatedImage.ToString();
                        }

                        if (string.IsNullOrEmpty(generatedImageUrl))
                        {
                            _logger.LogError("DALL-E API returned null or empty image URL");
                            throw new InvalidOperationException("DALL-E API returned null or empty image URL");
                        }
                        _logger.LogInformation("DALL-E 2 generated image URL: {ImageUrl}", generatedImageUrl);

                        // Download and store the image permanently
                        var storedImageUrl = await _storageService.StoreImageFromUrlAsync(
                            generatedImageUrl,
                            "generated");

                        _logger.LogInformation("Image stored successfully at: {StoredUrl}", storedImageUrl);
                        return storedImageUrl;
                    }
                    catch (HttpRequestException ex) when (ex.Message.Contains("401"))
                    {
                        _logger.LogError(ex, "DALL-E API authentication failed (401 Unauthorized): {Message}", ex.Message);
                        throw new InvalidOperationException("DALL-E API authentication failed. Please check your API key.", ex);
                    }
                    catch (HttpRequestException ex) when (ex.Message.Contains("429"))
                    {
                        _logger.LogError(ex, "DALL-E API rate limit exceeded (429 Too Many Requests): {Message}", ex.Message);
                        throw new InvalidOperationException("DALL-E API rate limit exceeded or insufficient quota. Please check your billing status.", ex);
                    }
                    catch (HttpRequestException ex) when (ex.Message.Contains("400"))
                    {
                        _logger.LogError(ex, "DALL-E API rejected the request (400 Bad Request): {Message}", ex.Message);
                        throw new InvalidOperationException($"DALL-E API rejected the request: {ex.Message}", ex);
                    }
                    catch (HttpRequestException ex) when (ex.Message.Contains("500"))
                    {
                        _logger.LogError(ex, "DALL-E API server error (500 Internal Server Error): {Message}", ex.Message);
                        throw new InvalidOperationException($"DALL-E API server error: {ex.Message}", ex);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unknown error calling DALL-E API: {Message}", ex.Message);
                        throw new InvalidOperationException($"Error calling DALL-E API: {ex.Message}", ex);
                    }
                }
                finally
                {
                    // Clean up the temporary file regardless of success or failure
                    try
                    {
                        if (File.Exists(tempImagePath))
                        {
                            File.Delete(tempImagePath);
                            _logger.LogInformation("Deleted temporary file: {TempPath}", tempImagePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary file: {TempPath}", tempImagePath);
                        // Don't throw here as this is just cleanup
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DALL-E API call failed with error: {Message}", ex.Message);
                throw new InvalidOperationException($"DALL-E API call failed: {ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image with DALL-E: {ExceptionType} - {Message}",
                ex.GetType().Name, ex.Message);
            throw;
        }
    }

    // This explicit implementation is required if you want to keep the original signature for IGenerationService
    async Task<string> IGenerationService.GenerateImageAsync(string originalImageUrl, string prompt)
    {
        // Forward to the new method with a default/placeholder decorStyle
        return await GenerateImageAsync(originalImageUrl, prompt, "UserDefined"); // Or some other default
    }

    public Task<string> GetGenerationStatusAsync(string requestId)
    {
        // DALL-E is synchronous, so we always return completed
        return Task.FromResult("Completed");
    }

    public Task<List<string>> GetRecentGenerationsAsync(string userId, int count = 5)
    {
        _logger.LogInformation("Getting recent generations for user: {UserId}", userId);
        // This would typically query the database for recent generations
        return Task.FromResult(new List<string>());
    }

    // Helper method to log available configuration keys for troubleshooting
    private void LogAvailableConfigurationKeys(IConfiguration configuration, ILogger logger)
    {
        logger.LogInformation("Available configuration keys:");
        foreach (var kvp in configuration.AsEnumerable().OrderBy(c => c.Key))
        {
            logger.LogInformation($"  {kvp.Key}: {kvp.Value?.Substring(0, Math.Min(kvp.Value.Length, 50))}...");
        }

        // Log User Secrets ID if available
        try
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            var attribute = assembly?.GetCustomAttributes(typeof(Microsoft.Extensions.Configuration.UserSecrets.UserSecretsIdAttribute), false).FirstOrDefault();
            if (attribute != null)
            {
                var userSecretsId = attribute.GetType().GetProperty("UserSecretsId")?.GetValue(attribute);
                logger.LogInformation("User Secrets ID: {UserSecretsId}", userSecretsId);
                logger.LogInformation("User secrets should be stored in: %APPDATA%\\Microsoft\\UserSecrets\\{UserSecretsId}\\secrets.json", userSecretsId);
            }
            else
            {
                logger.LogWarning("No UserSecretsId attribute found on assembly");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while logging configuration keys");
        }
    }

    private string BuildEnhancedPrompt(string userPrompt, string originalImageUrl, string decorStyle)
    {
        // Enhance the user's prompt with context for home decoration
        // Include information about using the original image as a base
        // Focus on maintaining structural elements and changing decor.
        return $"Transform this interior/exterior home space into a beautiful, professionally designed version in a '{decorStyle}' style. " +
               $"The user's specific request is: '{userPrompt}'. " +
               $"IMPORTANT: Maintain the original room shape, window positions, ceiling, and floor. " +
               $"Focus the style changes on elements like wall decor, lighting, furniture, textiles, and accessories. " +
               $"The final image should look like a realistic, inviting, and professionally decorated space with appropriate lighting, color coordination, and high-end finishes for the '{decorStyle}' style. " +
               $"Use the provided image as a strong reference for the existing layout and architectural elements, updating only the decor and style as requested.";
    }
}
