using SkiaSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HomeDecorator.Api.Services;

/// <summary>
/// Enhanced service for processing images to meet DALL-E API requirements
/// with multiple strategies for size reduction and format handling
/// </summary>
public class ImageProcessingServiceNew
{
    private readonly ILogger<ImageProcessingServiceNew> _logger;
    // DALL-E API requirements
    private const int MAX_FILE_SIZE = 4 * 1024 * 1024; // 4MB
    private const int MAX_IMAGE_DIMENSION = 4096; // 4096px is a common limit

    public ImageProcessingServiceNew(ILogger<ImageProcessingServiceNew> logger)
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
    public Task<string> EnsureImageMeetsDalleRequirements(byte[] imageBytes)
    {
        try
        {
            _logger.LogInformation("Processing image to meet DALL-E API requirements, original size: {Size} bytes", imageBytes.Length);

            // Create a temporary file path
            string tempImagePath = Path.Combine(Path.GetTempPath(), $"dalle_input_{Guid.NewGuid()}.png");

            // Initial check if we need to process the image
            bool needsProcessing = imageBytes.Length > MAX_FILE_SIZE; using (var ms = new MemoryStream(imageBytes))
            {
                // Load the image with SkiaSharp
                using (var originalBitmap = SKBitmap.Decode(ms))
                {
                    if (originalBitmap == null)
                    {
                        throw new InvalidOperationException("Failed to decode the image data");
                    }

                    int originalWidth = originalBitmap.Width;
                    int originalHeight = originalBitmap.Height;
                    _logger.LogInformation("Original image dimensions: {Width}x{Height}, Size: {Size} bytes",
                        originalWidth, originalHeight, imageBytes.Length);

                    // Also check if dimensions exceed limits
                    if (originalWidth > MAX_IMAGE_DIMENSION || originalHeight > MAX_IMAGE_DIMENSION)
                    {
                        needsProcessing = true;
                    }

                    // If we need to process, try multiple approaches in order of preference
                    if (needsProcessing)
                    {
                        // Strategy 1: Basic resize if significantly over the size limit
                        if (imageBytes.Length > MAX_FILE_SIZE * 1.5)
                        {
                            ProcessImageWithResizing(originalBitmap, tempImagePath, 0.7); // 70% of original size
                        }
                        else
                        {
                            ProcessImageWithResizing(originalBitmap, tempImagePath, 0.85); // 85% of original size
                        }

                        // Check if we need further compression
                        var fileInfo = new FileInfo(tempImagePath);
                        if (fileInfo.Length > MAX_FILE_SIZE)
                        {
                            _logger.LogInformation("First pass compression insufficient, trying with more aggressive settings");

                            // Strategy 2: More aggressive resize and quality reduction
                            using (var bitmap = SKBitmap.Decode(tempImagePath))
                            {
                                if (bitmap != null)
                                {
                                    // More aggressive resize
                                    double scaleFactor = Math.Sqrt(MAX_FILE_SIZE * 0.8 / (double)fileInfo.Length);
                                    int newWidth = (int)(bitmap.Width * scaleFactor);
                                    int newHeight = (int)(bitmap.Height * scaleFactor);

                                    _logger.LogInformation("Second pass resizing to: {Width}x{Height}", newWidth, newHeight);

                                    // Apply more aggressive compression
                                    ProcessImageWithResizing(bitmap, tempImagePath, 1.0, newWidth, newHeight, 80);
                                }
                            }
                        }

                        // Final check
                        fileInfo = new FileInfo(tempImagePath);
                        if (fileInfo.Length > MAX_FILE_SIZE)
                        {
                            _logger.LogInformation("Second pass compression insufficient, using maximum compression");

                            // Strategy 3: Maximum compression
                            using (var bitmap = SKBitmap.Decode(tempImagePath))
                            {
                                if (bitmap != null)
                                {
                                    // Scale to reduce size drastically if needed
                                    double scaleFactor = Math.Sqrt(MAX_FILE_SIZE * 0.7 / (double)fileInfo.Length);
                                    int newWidth = (int)(bitmap.Width * scaleFactor);
                                    int newHeight = (int)(bitmap.Height * scaleFactor);

                                    // Ensure dimensions don't go below reasonable quality
                                    newWidth = Math.Max(newWidth, 512);
                                    newHeight = Math.Max(newHeight, 512);

                                    _logger.LogInformation("Final pass resizing to: {Width}x{Height} with maximum compression",
                                        newWidth, newHeight);

                                    // Apply maximum compression
                                    ProcessImageWithResizing(bitmap, tempImagePath, 1.0, newWidth, newHeight, 60);
                                }
                            }
                        }
                    }
                    else
                    {
                        // No processing needed, just convert to PNG
                        _logger.LogInformation("Image doesn't need resizing, just converting to PNG format");
                        ProcessImageWithResizing(originalBitmap, tempImagePath, 1.0);
                    }
                }
            }

            // Final verification
            var finalFileInfo = new FileInfo(tempImagePath);
            _logger.LogInformation("Final image size: {Size} bytes", finalFileInfo.Length);

            if (finalFileInfo.Length > MAX_FILE_SIZE)
            {
                _logger.LogWarning("Image still exceeds 4MB limit after multiple compression attempts: {Size} bytes", finalFileInfo.Length);

                // Last resort: Delete the file and create a drastically downscaled version
                File.Delete(tempImagePath);

                using (var ms = new MemoryStream(imageBytes))
                using (var bitmap = SKBitmap.Decode(ms))
                {
                    if (bitmap != null)
                    {
                        // Force to a modest size that will definitely be under 4MB
                        int newWidth = 1024;
                        int newHeight = 1024;

                        if (bitmap.Width > bitmap.Height)
                        {
                            newHeight = (int)(1024.0 * bitmap.Height / bitmap.Width);
                        }
                        else
                        {
                            newWidth = (int)(1024.0 * bitmap.Width / bitmap.Height);
                        }

                        _logger.LogInformation("Last resort resizing to: {Width}x{Height}", newWidth, newHeight);
                        ProcessImageWithResizing(bitmap, tempImagePath, 1.0, newWidth, newHeight, 50);
                    }
                }

                // One final check
                finalFileInfo = new FileInfo(tempImagePath);
                if (finalFileInfo.Length > MAX_FILE_SIZE)
                {
                    _logger.LogError("Failed to reduce image below 4MB even after drastic measures: {Size} bytes", finalFileInfo.Length);
                    throw new InvalidOperationException($"Image still exceeds 4MB limit ({finalFileInfo.Length / 1024 / 1024:F1}MB) despite multiple compression attempts");
                }
            }

            return Task.FromResult(tempImagePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image for DALL-E API: {Message}", ex.Message);
            return Task.FromException<string>(new InvalidOperationException($"Failed to process image for DALL-E API: {ex.Message}", ex));
        }
    }

    /// <summary>
    /// Helper method to process an image with specified parameters
    /// </summary>
    private void ProcessImageWithResizing(SKBitmap originalBitmap, string outputPath, double scaleFactor, int? targetWidth = null, int? targetHeight = null, int quality = 100)
    {
        int newWidth = targetWidth ?? (int)(originalBitmap.Width * scaleFactor);
        int newHeight = targetHeight ?? (int)(originalBitmap.Height * scaleFactor);

        // Ensure dimensions don't exceed maximum
        if (newWidth > MAX_IMAGE_DIMENSION)
        {
            double aspectRatio = (double)originalBitmap.Height / originalBitmap.Width;
            newWidth = MAX_IMAGE_DIMENSION;
            newHeight = (int)(newWidth * aspectRatio);
        }

        if (newHeight > MAX_IMAGE_DIMENSION)
        {
            double aspectRatio = (double)originalBitmap.Width / originalBitmap.Height;
            newHeight = MAX_IMAGE_DIMENSION;
            newWidth = (int)(newHeight * aspectRatio);
        }

        // Use modern SKSamplingOptions instead of obsolete SKFilterQuality
        var samplingOptions = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Nearest);

        using (var resizedBitmap = originalBitmap.Resize(new SKImageInfo(newWidth, newHeight), samplingOptions))
        {
            if (resizedBitmap == null)
            {
                throw new InvalidOperationException("Failed to resize the image");
            }

            // Encode as PNG with specified quality
            using (var image = SKImage.FromBitmap(resizedBitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, quality))
            using (var stream = File.OpenWrite(outputPath))
            {
                data.SaveTo(stream);
            }
        }

        _logger.LogInformation("Image processed: dimensions {Width}x{Height}, quality {Quality}%, file size: {Size} bytes",
            newWidth, newHeight, quality, new FileInfo(outputPath).Length);
    }
}
