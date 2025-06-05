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

    // NOTE: For image-to-image (restyle) with GPT-Image-1, you must use /v1/images/edits and multipart/form-data.
    // The /v1/images/generations endpoint only supports text-to-image (no image input allowed).
    // See GenerateImageEditAsync for the correct implementation.
    // If you want to support text-to-image, keep the generations logic below, but do NOT include an 'image' field.
    public Task<string> GenerateImageAsync(string originalImageUrl, string prompt, string decorStyle)
    {
        throw new NotSupportedException("For image-to-image restyling, use GenerateImageEditAsync with a PNG stream. /v1/images/generations does not support image input for GPT-Image-1.");
    }

    Task<string> IGenerationService.GenerateImageAsync(string originalImageUrl, string prompt)
    {
        throw new NotSupportedException("For image-to-image restyling, use GenerateImageEditAsync with a PNG stream. /v1/images/generations does not support image input for GPT-Image-1. Update your API endpoint to use the correct method.");
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
    public async Task<string> GenerateImageEditAsync(
        System.IO.Stream imageStream, // PNG image stream
        System.IO.Stream? maskStream, // optional PNG mask
        string prompt,
        string decorStyle)
    {
        if (imageStream == null)
        {
            _logger.LogError("Input image stream is null");
            throw new ArgumentNullException(nameof(imageStream), "Input image stream cannot be null");
        }

        // Check if stream is readable and has content
        if (!imageStream.CanRead)
        {
            _logger.LogError("Input image stream is not readable");
            throw new ArgumentException("Input image stream is not readable", nameof(imageStream));
        }

        // Check if mask stream is readable when provided
        if (maskStream != null && !maskStream.CanRead)
        {
            _logger.LogError("Mask stream is not readable");
            throw new ArgumentException("Mask stream is not readable", nameof(maskStream));
        }

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
            _configuration["DallE:ApiKey"] ??
            _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("API key is not configured.");
            throw new InvalidOperationException("API key is not configured.");
        }

        // Log that we're about to start the generation process
        string maskInfo = maskStream != null ? " with mask" : " without mask";
        _logger.LogInformation("Starting GPT-Image-1 edit generation with {Style} style{MaskInfo}", decorStyle, maskInfo); var fullPrompt = BuildEnhancedPrompt(prompt, "", decorStyle);

        // Reset stream position to beginning if possible
        if (imageStream.CanSeek)
        {
            imageStream.Position = 0;
            _logger.LogInformation("Reset image stream position to beginning");
        }

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("gpt-image-1"), "model");
        content.Add(new StringContent(fullPrompt), "prompt");
        content.Add(new StringContent("1024x1024"), "size");
        content.Add(new StringContent("medium"), "quality");
        content.Add(new StringContent("1"), "n");// Add the image as a file - ensure it's properly formatted as PNG
        var imageContent = new StreamContent(imageStream);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "image", "input.png");

        _logger.LogInformation("Added image to request with Content-Type: image/png");

        // Add the mask if provided
        if (maskStream != null)
        {
            _logger.LogInformation("Adding mask to GPT-Image-1 request");
            var maskContent = new StreamContent(maskStream);
            maskContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            content.Add(maskContent, "mask", "mask.png");
        }
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/edits");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = content;

        // Add detailed logging of the request
        _logger.LogInformation("Sending request to GPT-Image-1 API with model: gpt-image-1");
        _logger.LogInformation("Image stream position: {Position}, Can read: {CanRead}, Length: {Length}",
            imageStream.Position, imageStream.CanRead, imageStream.CanSeek ? imageStream.Length.ToString() : "unknown");

        using var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        // Log detailed response info
        _logger.LogInformation("API Response Status: {Status} {ReasonPhrase}",
            (int)response.StatusCode, response.ReasonPhrase);

        // Log the entire JSON response structure regardless of status code
        _logger.LogInformation("OpenAI API Response: {Json}", json);

        if (!response.IsSuccessStatusCode)
        {
            try
            {
                // Parse error details if available
                var errorJson = System.Text.Json.JsonDocument.Parse(json).RootElement;

                if (errorJson.TryGetProperty("error", out var errorElement))
                {
                    string errorMessage = "Unknown error";
                    string errorType = "unknown";

                    if (errorElement.TryGetProperty("message", out var messageElement))
                        errorMessage = messageElement.GetString() ?? errorMessage;

                    if (errorElement.TryGetProperty("type", out var typeElement))
                        errorType = typeElement.GetString() ?? errorType;

                    _logger.LogError("OpenAI API error: {ErrorType} - {ErrorMessage}", errorType, errorMessage);
                    throw new InvalidOperationException($"OpenAI API error: {errorType} - {errorMessage}");
                }
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                _logger.LogError(ex, "Error parsing OpenAI error response: {Message}", ex.Message);
            }

            // If we couldn't parse the error details, fall back to the generic error
            throw new InvalidOperationException($"OpenAI error {response.StatusCode}: {json}");
        }

        var root = System.Text.Json.JsonDocument.Parse(json).RootElement;        // Safely navigate the JSON response structure
        string? url = null;
        try
        {
            // Log all available top-level properties for debugging
            _logger.LogInformation("Response JSON structure:");
            foreach (var prop in root.EnumerateObject())
            {
                _logger.LogInformation("  - {PropertyName}: {ValueKind}", prop.Name, prop.Value.ValueKind);
            }

            if (root.TryGetProperty("data", out var dataElement) &&
                dataElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                int arrayLength = dataElement.GetArrayLength();
                _logger.LogInformation("Found 'data' array with {Count} elements", arrayLength);

                if (arrayLength > 0 &&
                    dataElement[0].TryGetProperty("url", out var urlElement) &&
                    urlElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    url = urlElement.GetString();
                    _logger.LogInformation("Found URL in data[0].url");
                }
            }
            else if (root.TryGetProperty("results", out var resultsElement) &&
                    resultsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                int arrayLength = resultsElement.GetArrayLength();
                _logger.LogInformation("Found 'results' array with {Count} elements", arrayLength);

                if (arrayLength > 0 &&
                    resultsElement[0].TryGetProperty("url", out var urlElement) &&
                    urlElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    url = urlElement.GetString();
                    _logger.LogInformation("Found URL in results[0].url");
                }
            }
            else
            {
                // Try to find any property that might contain a URL
                foreach (var prop in root.EnumerateObject())
                {
                    _logger.LogInformation("Found top-level property: {Property}", prop.Name);                    // If we find an array property, check if it contains objects with URL properties
                    if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Array && prop.Value.GetArrayLength() > 0)
                    {
                        var firstItem = prop.Value[0];
                        if (firstItem.ValueKind == System.Text.Json.JsonValueKind.Object &&
                            firstItem.TryGetProperty("url", out var propUrlElement))
                        {
                            url = propUrlElement.GetString();
                            _logger.LogInformation("Found URL in {Property}[0].url", prop.Name);
                            break;
                        }
                    }
                    // If we find a direct URL property at the top level
                    else if (prop.Name.Equals("url", StringComparison.OrdinalIgnoreCase) &&
                             prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        url = prop.Value.GetString();
                        _logger.LogInformation("Found URL at root.url");
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing JSON response: {Message}", ex.Message);
            throw new InvalidOperationException($"Failed to parse OpenAI API response: {ex.Message}. Response: {json}");
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            // Check if response contains base64 image data instead of a URL
            bool hasB64Json = false;
            try
            {
                var jsonDoc = System.Text.Json.JsonDocument.Parse(json);
                var rootElement = jsonDoc.RootElement;
                // Check data array for b64_json
                if (rootElement.TryGetProperty("data", out var dataElement) &&
                    dataElement.ValueKind == System.Text.Json.JsonValueKind.Array &&
                    dataElement.GetArrayLength() > 0 &&
                    dataElement[0].TryGetProperty("b64_json", out var b64Element))
                {
                    var b64Data = b64Element.GetString();
                    if (!string.IsNullOrEmpty(b64Data))
                    {
                        _logger.LogInformation("Found base64 encoded image data instead of URL");

                        // Convert base64 to image and save it directly
                        var imageBytes = Convert.FromBase64String(b64Data);
                        var fileName = $"gpt_image_{Guid.NewGuid()}.png";
                        _logger.LogInformation("Storing base64 image with filename: {FileName}", fileName);
                        using var memStream = new MemoryStream(imageBytes);
                        var storedUrl = await _storageService.StoreImageFromStreamAsync(
                            memStream, fileName, "generated");

                        return storedUrl;
                    }
                    hasB64Json = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for base64 image data: {Message}", ex.Message);
            }

            // If we couldn't find a URL or base64 data, throw exception
            throw new InvalidOperationException(
                hasB64Json
                    ? "Found b64_json data but failed to process it"
                    : $"Could not find image URL in GPT-Image-1 response: {json}");
        }

        try
        {
            // Download and store the image
            _logger.LogInformation("Downloading image from URL: {Url}", url);
            using var imgResp = await _httpClient.GetAsync(url);

            // Log response details for debugging
            _logger.LogInformation("Image download response: Status: {Status}, Content-Type: {ContentType}, Content-Length: {ContentLength}",
                imgResp.StatusCode,
                imgResp.Content.Headers.ContentType?.MediaType ?? "unknown",
                imgResp.Content.Headers.ContentLength?.ToString() ?? "unknown");

            if (!imgResp.IsSuccessStatusCode)
            {
                var errorContent = await imgResp.Content.ReadAsStringAsync();
                _logger.LogError("Failed to download image from GPT-Image-1 URL: {StatusCode}. Response: {ErrorContent}",
                    imgResp.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to download image from GPT-Image-1 URL: {imgResp.StatusCode}. Response: {errorContent}");
            }

            await using var imgStream = await imgResp.Content.ReadAsStreamAsync();
            var fileName = $"gpt_image_{Guid.NewGuid()}.png";

            _logger.LogInformation("Storing image with filename: {FileName}", fileName);
            var storedUrl = await _storageService.StoreImageFromStreamAsync(
                imgStream, fileName, "generated");

            if (string.IsNullOrEmpty(storedUrl))
            {
                _logger.LogError("Storage service returned empty URL");
                throw new InvalidOperationException("Storage service returned empty URL");
            }

            _logger.LogInformation("Successfully stored GPT-Image-1 result at {Url}", storedUrl);
            return storedUrl;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error downloading or storing the generated image: {Message}", ex.Message);
            throw new InvalidOperationException($"Error downloading or storing the generated image: {ex.Message}", ex);
        }
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

    // Helper method for debugging the image URL access
    private async Task<bool> CanAccessUrl(string url)
    {
        try
        {
            _logger.LogInformation("Testing accessibility of URL: {Url}", url);

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            // Use HEAD request to minimize data transfer
            var request = new HttpRequestMessage(HttpMethod.Head, url);
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
        // Enhance the user's prompt with context for home decoration optimized for GPT-Image-1
        // Include information about maintaining structure and only changing decoration style

        _logger.LogInformation("Building enhanced prompt with style: {Style} and user prompt: {UserPrompt}",
            decorStyle, userPrompt);

        var enhancedPrompt = $"Show me the exact same image but remove the people and animals and change the color of the furniture. " +
               $"Transform this interior/exterior home space into a PHOTOREALISTIC, professionally designed version in a '{decorStyle}' style. " +
               $"The user's specific request is: '{userPrompt}'. " +
               $"CRITICALLY IMPORTANT: " +
               $"Focus style changes ONLY on elements like wall decor, lighting fixtures, furniture pieces, textiles, wall colors, and decorative accessories. " +
               $"The final image should look like a professional architectural/interior design photograph with realistic lighting, natural shadows, proper perspective, and high-end finishes for the '{decorStyle}' style. " +
               $"Use the provided input image as a precise reference for the existing space and architectural elements, updating only the decor and aesthetic elements as requested.";

        _logger.LogInformation("Final prompt (length: {Length}): {Prompt}", enhancedPrompt.Length, enhancedPrompt);
        return enhancedPrompt;
    }
}
