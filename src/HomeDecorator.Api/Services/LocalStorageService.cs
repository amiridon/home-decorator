using HomeDecorator.Core.Services;

namespace HomeDecorator.Api.Services;

/// <summary>
/// Local file storage implementation for development/testing
/// </summary>
public class LocalStorageService : IStorageService
{
    private readonly string _storageRoot;
    private readonly ILogger<LocalStorageService> _logger;
    private readonly HttpClient _httpClient;

    public LocalStorageService(IConfiguration configuration, ILogger<LocalStorageService> logger)
    {
        _storageRoot = configuration["Storage:LocalPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        _logger = logger;
        _httpClient = new HttpClient();

        // Ensure the storage directory exists
        Directory.CreateDirectory(_storageRoot);
        Directory.CreateDirectory(Path.Combine(_storageRoot, "generated"));
        Directory.CreateDirectory(Path.Combine(_storageRoot, "uploaded"));
    }
    public async Task<string> StoreImageFromUrlAsync(string imageUrl, string category)
    {
        try
        {
            _logger.LogInformation("Downloading image from URL: {ImageUrl}", imageUrl);

            // Log if the URL is valid
            if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri? uri))
            {
                _logger.LogError("Invalid URL provided: {ImageUrl}", imageUrl);
                throw new ArgumentException($"Invalid URL format: {imageUrl}");
            }

            _logger.LogInformation("URL scheme: {Scheme}, Host: {Host}, Path: {Path}",
                uri.Scheme, uri.Host, uri.AbsolutePath);

            // Ensure the category directory exists
            var categoryPath = Path.Combine(_storageRoot, category);
            if (!Directory.Exists(categoryPath))
            {
                _logger.LogInformation("Creating directory: {DirectoryPath}", categoryPath);
                Directory.CreateDirectory(categoryPath);
            }

            // Generate a unique filename
            var fileName = $"{Guid.NewGuid()}.png";
            var filePath = Path.Combine(categoryPath, fileName);

            // Download the image with retry logic
            int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Download attempt {Attempt} of {MaxRetries}", attempt, maxRetries);
                    // Use longer timeout for remote requests
                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(30);

                    // Download image
                    _logger.LogInformation("Sending HTTP request to: {ImageUrl}", imageUrl);
                    var response = await client.GetAsync(imageUrl);

                    _logger.LogInformation("Received HTTP response: Status {StatusCode} - {ReasonPhrase}",
                        (int)response.StatusCode, response.ReasonPhrase);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("HTTP request failed with status code {StatusCode}: {ReasonPhrase}",
                            (int)response.StatusCode, response.ReasonPhrase);

                        // Try to get response content if available
                        try
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            _logger.LogError("Error response content: {ErrorContent}",
                                errorContent.Length > 1000 ? errorContent.Substring(0, 1000) + "..." : errorContent);
                        }
                        catch (Exception contentEx)
                        {
                            _logger.LogWarning("Could not read error content: {Error}", contentEx.Message);
                        }
                    }

                    response.EnsureSuccessStatusCode();

                    // Read as byte array to avoid stream issues
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    _logger.LogInformation("Successfully downloaded {ByteCount} bytes", imageBytes.Length);

                    // Save to local storage
                    await File.WriteAllBytesAsync(filePath, imageBytes);
                    _logger.LogInformation("Successfully wrote file to: {FilePath}", filePath);            // Return the local URL (relative to wwwroot) with base URL for direct client access
                    var relativeUrl = $"/images/{category}/{fileName}";
                    var baseUrl = "http://localhost:5002"; // Base URL for development
                    var fullUrl = $"{baseUrl}{relativeUrl}";
                    _logger.LogInformation("Image stored locally at: {FilePath}, accessible at: {RelativeUrl}, Full URL: {FullUrl}", filePath, relativeUrl, fullUrl);

                    // Check if file exists before returning
                    if (!File.Exists(filePath))
                    {
                        _logger.LogError("File was not created successfully at path: {FilePath}", filePath);
                    }

                    return relativeUrl;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "Download attempt {Attempt} failed, retrying in 1 second", attempt);
                    await Task.Delay(1000); // Wait 1 second before retry
                }
            }

            // If we get here, all retries failed
            throw new InvalidOperationException($"Failed to download image after {maxRetries} attempts");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing image from URL: {ImageUrl}", imageUrl);
            throw;
        }
    }
    public async Task<string> StoreImageFromStreamAsync(Stream imageStream, string fileName, string category)
    {
        try
        {
            // Ensure the category directory exists
            var categoryPath = Path.Combine(_storageRoot, category);
            if (!Directory.Exists(categoryPath))
            {
                _logger.LogInformation("Creating directory: {DirectoryPath}", categoryPath);
                Directory.CreateDirectory(categoryPath);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(categoryPath, uniqueFileName);

            _logger.LogInformation("Creating file stream at: {FilePath}", filePath);

            // First read the entire stream into memory to avoid issues with stream being closed prematurely
            byte[] imageBytes;
            using (var memoryStream = new MemoryStream())
            {
                await imageStream.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }

            // Then write to disk
            await File.WriteAllBytesAsync(filePath, imageBytes); var relativeUrl = $"/images/{category}/{uniqueFileName}";
            var baseUrl = "http://localhost:5002"; // Base URL for development
            var fullUrl = $"{baseUrl}{relativeUrl}";

            _logger.LogInformation("Image stored locally at: {FilePath}, accessible at: {RelativeUrl}, Full URL: {FullUrl}", filePath, relativeUrl, fullUrl);

            // Check if file exists before returning
            if (!File.Exists(filePath))
            {
                _logger.LogError("File was not created successfully at path: {FilePath}", filePath);
            }

            return relativeUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing image stream: {FileName}. Error: {ErrorMessage}",
                fileName, ex.Message);
            throw;
        }
    }

    public Task<bool> DeleteImageAsync(string imageUrl)
    {
        try
        {
            // Convert relative URL back to file path
            if (imageUrl.StartsWith("/images/"))
            {
                var relativePath = imageUrl.Substring("/images/".Length);
                var filePath = Path.Combine(_storageRoot, relativePath);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Deleted image: {FilePath}", filePath);
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image: {ImageUrl}", imageUrl);
            return Task.FromResult(false);
        }
    }
}
