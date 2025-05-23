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

            // Download the image
            var response = await _httpClient.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();

            var imageBytes = await response.Content.ReadAsByteArrayAsync();

            // Generate a unique filename
            var fileName = $"{Guid.NewGuid()}.png";
            var categoryPath = Path.Combine(_storageRoot, category);
            var filePath = Path.Combine(categoryPath, fileName);

            // Save to local storage
            await File.WriteAllBytesAsync(filePath, imageBytes);

            // Return the local URL (relative to wwwroot)
            var relativeUrl = $"/images/{category}/{fileName}";

            _logger.LogInformation("Image stored locally at: {FilePath}, accessible at: {RelativeUrl}", filePath, relativeUrl);

            return relativeUrl;
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
            var categoryPath = Path.Combine(_storageRoot, category);
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(categoryPath, uniqueFileName);

            using var fileStream = new FileStream(filePath, FileMode.Create);
            await imageStream.CopyToAsync(fileStream);

            var relativeUrl = $"/images/{category}/{uniqueFileName}";

            _logger.LogInformation("Image stored locally at: {FilePath}, accessible at: {RelativeUrl}", filePath, relativeUrl);

            return relativeUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing image stream: {FileName}", fileName);
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
