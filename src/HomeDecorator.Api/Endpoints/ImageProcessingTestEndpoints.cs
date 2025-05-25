using HomeDecorator.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HomeDecorator.Api.Endpoints;

/// <summary>
/// Test endpoints for testing image processing service
/// </summary>
public static class ImageProcessingTestEndpoints
{
    public static void MapImageProcessingTestEndpoints(this WebApplication app)
    {
        // Test endpoint for image processing performance
        app.MapPost("/api/test/image-processing", async (
            IFormFile file,
            ImageProcessingServiceNew imageService,
            ILogger<Program> logger) =>
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest(new { error = "No file provided" });
                }

                // Read the file into memory
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                logger.LogInformation("Testing image processing with file: {Name}, Size: {Size} bytes",
                    file.FileName, imageBytes.Length);

                // Process with image processing service
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                string? outputPath = null;
                long outputFileSize = 0;
                string? errorMessage = null;

                try
                {
                    outputPath = await imageService.EnsureImageMeetsDalleRequirements(imageBytes);

                    if (File.Exists(outputPath))
                    {
                        var fileInfo = new FileInfo(outputPath);
                        outputFileSize = fileInfo.Length;
                        logger.LogInformation("Processed image: {Path}, Size: {Size} bytes", outputPath, outputFileSize);
                    }
                    else
                    {
                        logger.LogWarning("Output path was returned but file does not exist: {Path}", outputPath);
                    }
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    logger.LogError(ex, "Error processing image");
                }

                stopwatch.Stop();
                var processingTime = stopwatch.Elapsed;

                // Return the processing result
                var result = new
                {
                    OriginalSize = imageBytes.Length,
                    ProcessedSize = outputFileSize,
                    CompressionRatio = outputFileSize > 0 ? (double)imageBytes.Length / outputFileSize : 0,
                    ProcessingTimeMs = processingTime.TotalMilliseconds,
                    Success = string.IsNullOrEmpty(errorMessage) && !string.IsNullOrEmpty(outputPath),
                    Error = errorMessage,
                    OutputPath = outputPath
                };

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error testing image processing");
                return Results.Problem(
                    title: "Error testing image processing",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .DisableAntiforgery() // Required for file uploads
        .WithName("TestImageProcessing")
        .WithTags("Testing")
        .WithOpenApi();
    }
}
