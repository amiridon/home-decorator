using Amazon.S3;
using Amazon.S3.Model;
using HomeDecorator.Core.Services;

namespace HomeDecorator.Api.Services;

/// <summary>
/// S3-based implementation of IStorageService
/// </summary>
public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<S3StorageService> _logger;
    private readonly HttpClient _httpClient;

    public S3StorageService(
        IAmazonS3 s3Client,
        IConfiguration configuration,
        ILogger<S3StorageService> logger,
        HttpClient httpClient)
    {
        _s3Client = s3Client;
        _bucketName = configuration["Storage:S3:BucketName"] ??
                     throw new InvalidOperationException("Storage:S3:BucketName configuration is missing");
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<string> StoreImageFromUrlAsync(string imageUrl, string category)
    {
        try
        {
            _logger.LogInformation("Downloading image from URL: {ImageUrl}", imageUrl);

            var response = await _httpClient.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            var fileName = $"{Guid.NewGuid()}.jpg";

            return await StoreImageFromStreamAsync(stream, fileName, category);
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
            var key = $"{category}/{DateTime.UtcNow:yyyy/MM/dd}/{fileName}";

            _logger.LogInformation("Uploading image to S3: {Key}", key);

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = imageStream,
                ContentType = "image/jpeg",
                CannedACL = S3CannedACL.PublicRead
            };

            var response = await _s3Client.PutObjectAsync(request);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new InvalidOperationException($"Failed to upload image to S3. Status: {response.HttpStatusCode}");
            }

            var imageUrl = $"https://{_bucketName}.s3.amazonaws.com/{key}";
            _logger.LogInformation("Image uploaded successfully: {ImageUrl}", imageUrl);

            return imageUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image to S3");
            throw;
        }
    }

    public async Task<bool> DeleteImageAsync(string imageUrl)
    {
        try
        {
            // Extract key from URL
            var uri = new Uri(imageUrl);
            var key = uri.AbsolutePath.TrimStart('/');

            _logger.LogInformation("Deleting image from S3: {Key}", key);

            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            var response = await _s3Client.DeleteObjectAsync(request);

            _logger.LogInformation("Image deleted successfully: {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image from S3: {ImageUrl}", imageUrl);
            return false;
        }
    }
}
