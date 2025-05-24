using SkiaSharp;

namespace HomeDecorator.Api.Services;

/// <summary>
/// Service for processing images to meet DALL-E API requirements
/// </summary>
public class ImageProcessingService
{
    private readonly ILogger<ImageProcessingService> _logger;

    public ImageProcessingService(ILogger<ImageProcessingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Ensures an image meets DALL-E API requirements:
    /// - Converts the image to PNG format
    /// - Ensures the image is under 4MB in size
    /// - Resizes the image if needed to meet size requirements
    /// </summary>
    /// <param name="imageBytes">The original image bytes</param>
    /// <returns>A path to a temporary file that meets all DALL-E API requirements</returns>
    public async Task<string> EnsureImageMeetsDalleRequirements(byte[] imageBytes)
    {
        _logger.LogInformation("Processing image to meet DALL-E API requirements");

        // Create a temporary file path
        string tempImagePath = Path.Combine(Path.GetTempPath(), $"dalle_input_{Guid.NewGuid()}.png");

        try
        {
            // Check if the image needs to be resized (if it's larger than 4MB)
            bool needsResize = imageBytes.Length > 4 * 1024 * 1024; // 4MB limit

            using (var ms = new MemoryStream(imageBytes))
            {
                // Load the image with SkiaSharp
                using (var originalBitmap = SKBitmap.Decode(ms))
                {
                    if (originalBitmap == null)
                    {
                        throw new InvalidOperationException("Failed to decode the image data");
                    }

                    _logger.LogInformation("Original image size: {Width}x{Height}, Data size: {Size} bytes",
                        originalBitmap.Width, originalBitmap.Height, imageBytes.Length);

                    if (needsResize)
                    {
                        // Calculate resize factor to get under 4MB (with some safety margin)
                        double resizeFactor = Math.Sqrt(3.5 * 1024 * 1024 / (double)imageBytes.Length);
                        int newWidth = (int)(originalBitmap.Width * resizeFactor);
                        int newHeight = (int)(originalBitmap.Height * resizeFactor);

                        _logger.LogInformation("Resizing image. New dimensions: {Width}x{Height}", newWidth, newHeight);

                        // Create a resized bitmap
                        using (var resizedBitmap = originalBitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High))
                        {
                            if (resizedBitmap == null)
                            {
                                throw new InvalidOperationException("Failed to resize the image");
                            }

                            // Encode as PNG and save
                            using (var image = SKImage.FromBitmap(resizedBitmap))
                            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                            using (var stream = File.OpenWrite(tempImagePath))
                            {
                                data.SaveTo(stream);
                            }

                            _logger.LogInformation("Image resized and saved as PNG to: {TempPath}", tempImagePath);
                        }
                    }
                    else
                    {
                        // Just convert to PNG without resizing
                        using (var image = SKImage.FromBitmap(originalBitmap))
                        using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                        using (var stream = File.OpenWrite(tempImagePath))
                        {
                            data.SaveTo(stream);
                        }

                        _logger.LogInformation("Image converted to PNG and saved to: {TempPath}", tempImagePath);
                    }
                }
            }

            // Verify the file size after processing
            var fileInfo = new FileInfo(tempImagePath);
            _logger.LogInformation("Final image size: {Size} bytes", fileInfo.Length);

            if (fileInfo.Length > 4 * 1024 * 1024)
            {
                _logger.LogError("Image is still larger than 4MB after processing: {Size} bytes", fileInfo.Length);
                throw new InvalidOperationException("Unable to reduce image size below 4MB as required by DALL-E API");
            }

            return tempImagePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image for DALL-E API: {Message}", ex.Message);

            // Clean up temp file if it exists
            if (File.Exists(tempImagePath))
            {
                File.Delete(tempImagePath);
            }

            throw new InvalidOperationException($"Failed to process image for DALL-E API: {ex.Message}", ex);
        }
    }
}
