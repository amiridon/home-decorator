using HomeDecorator.Core.Services;
using OpenAI;
using OpenAI.Images;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.Logging.Console;
using SkiaSharp;
using System.IO;

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
    private readonly DirectImageDownloader _directImageDownloader;
    private readonly ImageProcessingServiceNew _imageProcessingService;

    public DalleGenerationService(
        IConfiguration configuration,
        IStorageService storageService,
        ILogger<DalleGenerationService> logger,
        ImageProcessingServiceNew imageProcessingService)
    {
        _configuration = configuration;
        _imageProcessingService = imageProcessingService;

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
        // Create a logger factory to get the right type of logger for DirectImageDownloader
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var downloaderLogger = loggerFactory.CreateLogger<DirectImageDownloader>();
        _directImageDownloader = new DirectImageDownloader(downloaderLogger, storageService);
    }

    public async Task<string> GenerateImageAsync(string originalImageUrl, string prompt, string decorStyle)
    {
        try
        {
            _logger.LogInformation("Starting GPT-4o image variation generation for image: {OriginalImage}, decor style: {DecorStyle}", originalImageUrl, decorStyle);

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
                // Process the image to ensure it meets API requirements (PNG format, < 4MB)
                string tempImagePath = await _imageProcessingService.EnsureImageMeetsDalleRequirements(originalImageBytes);
                _logger.LogInformation("Image processed and ready for GPT-4o Image API: {TempPath}", tempImagePath);

                try
                {
                    // Instead of using the OpenAI SDK, call the OpenAI Images API directly for GPT-4o image variation
                    var openAiApiKey = apiKey;
                    var endpoint = "https://api.openai.com/v1/images/variations";

                    using (var multipartContent = new MultipartFormDataContent())
                    {
                        multipartContent.Add(new StreamContent(File.OpenRead(tempImagePath)), "image", Path.GetFileName(tempImagePath));
                        // Optionally add prompt if needed for GPT-4o (for variations, prompt may not be required)
                        // multipartContent.Add(new StringContent(prompt), "prompt");

                        using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                        {
                            request.Headers.Add("Authorization", $"Bearer {openAiApiKey}");
                            request.Content = multipartContent;

                            using (var response = await _httpClient.SendAsync(request))
                            {
                                if (!response.IsSuccessStatusCode)
                                {
                                    var errorContent = await response.Content.ReadAsStringAsync();
                                    _logger.LogError("OpenAI Images API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                                    throw new InvalidOperationException($"OpenAI Images API error: {response.StatusCode} - {errorContent}");
                                }
                                var json = await response.Content.ReadAsStringAsync();
                                // Parse the response to extract the image URL
                                var imageUrl = System.Text.Json.JsonDocument.Parse(json)
                                    .RootElement.GetProperty("data")[0].GetProperty("url").GetString();
                                if (string.IsNullOrEmpty(imageUrl))
                                {
                                    _logger.LogError("No image URL found in OpenAI Images API response: {Json}", json);
                                    throw new InvalidOperationException("No image URL found in OpenAI Images API response.");
                                }
                                _logger.LogInformation("GPT-4o generated image URL: {ImageUrl}", imageUrl);
                                // Download and store the image as before
                                var isUrlAccessible = await CanAccessUrl(imageUrl);
                                if (!isUrlAccessible)
                                {
                                    _logger.LogError("GPT-4o generated image URL is not accessible: {ImageUrl}", imageUrl);
                                    throw new InvalidOperationException($"Generated image URL is not accessible: {imageUrl}");
                                }
                                using var httpClient = new HttpClient();
                                httpClient.Timeout = TimeSpan.FromSeconds(30);
                                _logger.LogInformation("Downloading image from URL: {ImageUrl}", imageUrl);
                                byte[] imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                                _logger.LogInformation("Successfully downloaded {BytesLength} bytes", imageBytes.Length);
                                string tempFile = Path.Combine(Path.GetTempPath(), $"gpt4o_download_{Guid.NewGuid()}.png");
                                await File.WriteAllBytesAsync(tempFile, imageBytes);
                                using var fileStream = File.OpenRead(tempFile);
                                var finalStoredImageUrl = await _storageService.StoreImageFromStreamAsync(
                                    fileStream,
                                    $"gpt4o_generated_{Guid.NewGuid()}.png",
                                    "generated");
                                _logger.LogInformation("Image stored successfully at: {StoredUrl}", finalStoredImageUrl);
                                try { File.Delete(tempFile); } catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete temp file: {TempFile}", tempFile); }
                                return finalStoredImageUrl;
                            }
                        }
                    }
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "GPT-4o Image API call failed with error: {Message}", ex.Message);
                throw new InvalidOperationException($"GPT-4o Image API call failed: {ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image with GPT-4o: {ExceptionType} - {Message}",
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
    }    // Helper method for debugging the image URL access
    private async Task<bool> CanAccessUrl(string url)
    {
        try
        {
            // Validate the URL first
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogWarning("Cannot test accessibility: URL is null or empty");
                return false;
            }

            // Check if the URL is a valid absolute URI
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? validUri))
            {
                _logger.LogWarning("Cannot test accessibility: Invalid URL format: {Url}", url);
                return false;
            }

            // Check if the scheme is HTTP or HTTPS
            if (validUri.Scheme != Uri.UriSchemeHttp && validUri.Scheme != Uri.UriSchemeHttps)
            {
                _logger.LogWarning("Cannot test accessibility: URL must use HTTP or HTTPS scheme: {Url}", url);
                return false;
            }

            _logger.LogInformation("Testing accessibility of URL: {Url}", url);

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            // Use HEAD request to minimize data transfer
            var request = new HttpRequestMessage(HttpMethod.Head, validUri);
            var response = await client.SendAsync(request);

            _logger.LogInformation("URL test result: {StatusCode} - {ReasonPhrase}",
                (int)response.StatusCode, response.ReasonPhrase);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accessing URL: {Url}, Error: {Message}", url, ex.Message);
            return false;
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

    // Helper method to comprehensively analyze GeneratedImage object structure
    private void LogGeneratedImageStructure(object generatedImage)
    {
        try
        {
            var objectType = generatedImage.GetType();
            _logger.LogInformation("=== DALL-E GeneratedImage Analysis ===");
            _logger.LogInformation("Type: {TypeName}", objectType.FullName);

            var properties = objectType.GetProperties();
            _logger.LogInformation("Found {PropertyCount} properties:", properties.Length);

            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(generatedImage);
                    var valueType = value?.GetType()?.Name ?? "null";
                    var valueDescription = GetValueDescription(value);

                    _logger.LogInformation("  - {PropertyName} ({PropertyType}): {ValueType} = {ValueDescription}",
                        prop.Name, prop.PropertyType.Name, valueType, valueDescription);

                    // If this is a byte array, log its size
                    if (value is byte[] bytes)
                    {
                        _logger.LogInformation("    -> Byte array with {ByteCount} bytes", bytes.Length);
                    }
                    // If this looks like a URL, log that
                    else if (value is string str && (str.StartsWith("http://") || str.StartsWith("https://")))
                    {
                        _logger.LogInformation("    -> Detected as URL: {Url}", str);
                    }
                }
                catch (Exception propEx)
                {
                    _logger.LogWarning("  - {PropertyName}: Error reading property: {Error}", prop.Name, propEx.Message);
                }
            }

            // Also check for any fields (in case properties don't expose everything)
            var fields = objectType.GetFields();
            if (fields.Length > 0)
            {
                _logger.LogInformation("Found {FieldCount} fields:", fields.Length);
                foreach (var field in fields)
                {
                    try
                    {
                        var value = field.GetValue(generatedImage);
                        var valueDescription = GetValueDescription(value);
                        _logger.LogInformation("  - {FieldName} ({FieldType}): {ValueDescription}",
                            field.Name, field.FieldType.Name, valueDescription);
                    }
                    catch (Exception fieldEx)
                    {
                        _logger.LogWarning("  - {FieldName}: Error reading field: {Error}", field.Name, fieldEx.Message);
                    }
                }
            }

            _logger.LogInformation("=== End DALL-E GeneratedImage Analysis ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing GeneratedImage structure: {Message}", ex.Message);
        }
    }

    private string GetValueDescription(object? value)
    {
        if (value == null) return "null";

        if (value is byte[] bytes)
            return $"byte[{bytes.Length}]";
        if (value is string str)
            return str.Length > 100 ? $"\"{str.Substring(0, 100)}...\"" : $"\"{str}\"";
        if (value is Stream stream)
            return $"Stream (CanRead: {stream.CanRead}, Length: {(stream.CanSeek ? stream.Length.ToString() : "unknown")})";

        var stringValue = value.ToString();
        return stringValue?.Length > 100 ? $"{stringValue.Substring(0, 100)}..." : stringValue ?? "null";
    }
}
