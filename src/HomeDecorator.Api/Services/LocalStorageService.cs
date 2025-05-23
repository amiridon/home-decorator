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
            _logger.LogInformation("Downloading image from: {ImageUrl}", imageUrl);

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
                    var response = await client.GetAsync(imageUrl);
                    response.EnsureSuccessStatusCode();

                    // Read as byte array to avoid stream issues
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    _logger.LogInformation("Successfully downloaded {ByteCount} bytes", imageBytes.Length);

                    // Save to local storage
                    await File.WriteAllBytesAsync(filePath, imageBytes);
                    _logger.LogInformation("Successfully wrote file to: {FilePath}", filePath);

                    // Return the local URL (relative to wwwroot)
                    var relativeUrl = $"/images/{category}/{fileName}";
                    _logger.LogInformation("Image stored locally at: {FilePath}, accessible at: {RelativeUrl}", filePath, relativeUrl);
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
            await File.WriteAllBytesAsync(filePath, imageBytes);

            var relativeUrl = $"/images/{category}/{uniqueFileName}";

            _logger.LogInformation("Image stored locally at: {FilePath}, accessible at: {RelativeUrl}", filePath, relativeUrl);

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
