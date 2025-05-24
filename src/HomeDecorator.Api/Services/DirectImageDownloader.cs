using OpenAI;
using OpenAI.Images;
using System.Reflection;
using HomeDecorator.Core.Services;

namespace HomeDecorator.Api.Services;

/// <summary>
/// Service for directly downloading images from DALL-E API responses
/// This helps solve issues with temporary URLs that may expire quickly
/// </summary>
public class DirectImageDownloader
{
    private readonly ILogger<DirectImageDownloader> _logger;
    private readonly IStorageService _storageService;

    public DirectImageDownloader(
        ILogger<DirectImageDownloader> logger,
        IStorageService storageService)
    {
        _logger = logger;
        _storageService = storageService;
    }

    /// <summary>
    /// Attempts to download and store an image directly from the DALL-E response
    /// </summary>
    public async Task<string> DownloadAndStoreImageAsync(OpenAIClient openAIClient, string imageFilePath)
    {
        _logger.LogInformation("Attempting direct image download for file: {FilePath}", imageFilePath);

        try
        {
            // Use the OpenAI client to directly get the image bytes
            var imageClient = openAIClient.GetImageClient("dall-e-2");

            // Generate variation and process the result immediately
            using var fileStream = File.OpenRead(imageFilePath);            // Use default options for image variation
            var options = new ImageVariationOptions();

            _logger.LogInformation("Calling DALL-E 2 API with direct image download");
            var response = await imageClient.GenerateImageVariationAsync(imageFilePath, options);

            if (response?.Value == null)
            {
                _logger.LogError("DALL-E API returned null response in direct download attempt");
                throw new InvalidOperationException("DALL-E API returned null response");
            }

            // Try to access the image data directly using reflection
            var generatedImage = response.Value;
            _logger.LogInformation("Response received, image type: {Type}", generatedImage.GetType().Name);

            // Examine response properties
            string? imageUrl = null;

            // First, try to get URL
            var urlProperty = generatedImage.GetType().GetProperty("Url");
            if (urlProperty != null)
            {
                var urlValue = urlProperty.GetValue(generatedImage);
                imageUrl = urlValue?.ToString();
                _logger.LogInformation("Found image URL: {Url}", imageUrl);

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    // Download image directly from URL to local file
                    using var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(30);

                    var tempOutputPath = Path.Combine(Path.GetTempPath(), $"dalle_direct_{Guid.NewGuid()}.png");

                    try
                    {
                        // Download the image
                        var imageData = await httpClient.GetByteArrayAsync(imageUrl);
                        await File.WriteAllBytesAsync(tempOutputPath, imageData);

                        // Store the downloaded image
                        using var tempFileStream = File.OpenRead(tempOutputPath);
                        return await _storageService.StoreImageFromStreamAsync(
                            tempFileStream,
                            $"dalle_variation_{Guid.NewGuid()}.png",
                            "generated");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to download from URL: {Url}", imageUrl);
                        // Continue to other methods
                    }
                    finally
                    {
                        if (File.Exists(tempOutputPath))
                        {
                            File.Delete(tempOutputPath);
                        }
                    }
                }
            }

            // If URL approach failed, try to see if we have raw data
            _logger.LogInformation("URL approach failed, attempting to access raw data");

            // Get all properties that might contain image data
            var properties = generatedImage.GetType().GetProperties();
            foreach (var prop in properties)
            {
                try
                {
                    _logger.LogInformation("Checking property: {PropertyName}", prop.Name);
                    var value = prop.GetValue(generatedImage);

                    // Check for byte array
                    if (value is byte[] bytes && bytes.Length > 0)
                    {
                        _logger.LogInformation("Found image bytes in property {PropertyName}, length: {Length}",
                            prop.Name, bytes.Length);

                        // Save bytes directly
                        return await SaveBytesToStorage(bytes);
                    }

                    // Check for BinaryData
                    if (value != null && value.GetType().Name == "BinaryData")
                    {
                        _logger.LogInformation("Found BinaryData in property {PropertyName}", prop.Name);

                        // Try to convert to bytes using reflection
                        var toBytesMethod = value.GetType().GetMethod("ToArray");
                        if (toBytesMethod != null)
                        {
                            var binaryBytes = toBytesMethod.Invoke(value, null) as byte[];
                            if (binaryBytes != null && binaryBytes.Length > 0)
                            {
                                _logger.LogInformation("Converted BinaryData to bytes, length: {Length}", binaryBytes.Length);
                                return await SaveBytesToStorage(binaryBytes);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error accessing property {PropertyName}", prop.Name);
                }
            }

            _logger.LogError("Could not extract image data from DALL-E response");
            throw new InvalidOperationException("Could not extract image data from DALL-E response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Direct image download failed");
            throw;
        }
    }

    private async Task<string> SaveBytesToStorage(byte[] imageBytes)
    {
        var tempOutputPath = Path.Combine(Path.GetTempPath(), $"dalle_bytes_{Guid.NewGuid()}.png");
        try
        {
            await File.WriteAllBytesAsync(tempOutputPath, imageBytes);
            using var tempFileStream = File.OpenRead(tempOutputPath);
            return await _storageService.StoreImageFromStreamAsync(
                tempFileStream,
                $"dalle_variation_{Guid.NewGuid()}.png",
                "generated");
        }
        finally
        {
            if (File.Exists(tempOutputPath))
            {
                File.Delete(tempOutputPath);
            }
        }
    }
}
