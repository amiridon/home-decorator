using HomeDecorator.Core.Models;
using HomeDecorator.Api.Services;
using Microsoft.AspNetCore.Authorization;
using HomeDecorator.Core.Services;
using System;
using System.Linq; // for LINQ support

namespace HomeDecorator.Api.Endpoints;

/// <summary>
/// API endpoints for image generation functionality
/// </summary>
public static class ImageGenerationEndpoints
{
    public static void MapImageGenerationEndpoints(this WebApplication app)
    {
        // Upload an image
        app.MapPost("/api/upload-image", async (
            IFormFile file,
            IStorageService storageService) =>
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest(new { error = "No file provided" });
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    return Results.BadRequest(new { error = "Invalid file type. Only JPEG, PNG, and WebP images are allowed." });
                }

                // Validate file size (max 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    return Results.BadRequest(new { error = "File size too large. Maximum size is 10MB." });
                }

                using var stream = file.OpenReadStream();
                var fileName = Path.GetFileName(file.FileName);
                var imageUrl = await storageService.StoreImageFromStreamAsync(stream, fileName, "uploaded");

                return Results.Ok(new { imageUrl });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Error uploading image",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("UploadImage")
        .WithTags("Image Generation")
        .WithOpenApi()
        .DisableAntiforgery(); // Required for file uploads

        // Create a new image generation request
        app.MapPost("/api/image-request", async (
            CreateImageRequestDto request,
            ImageGenerationOrchestrator orchestrator,
            HttpContext context) =>
        {
            try
            {
                // In a real app, get userId from authenticated user
                var userId = context.User?.Identity?.Name ?? "test-user";

                var imageRequest = await orchestrator.CreateAndProcessRequestAsync(userId, request);

                var response = new ImageRequestResponseDto
                {
                    Id = imageRequest.Id,
                    Status = imageRequest.Status,
                    OriginalImageUrl = imageRequest.OriginalImageUrl,
                    Prompt = imageRequest.Prompt,
                    CreditsCharged = imageRequest.CreditsCharged,
                    CreatedAt = imageRequest.CreatedAt
                };

                return Results.Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("credits"))
            {
                return Results.BadRequest(new { error = "Insufficient credits" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Error creating image request",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        // .RequireAuthorization() // Temporarily disabled for development testing
        .WithName("CreateImageRequest")
        .WithTags("Image Generation")
        .WithOpenApi();

        // Get an image generation request by ID
        app.MapGet("/api/image-request/{id}", async (
            string id,
            ImageGenerationOrchestrator orchestrator) =>
        {
            try
            {
                var imageRequest = await orchestrator.GetRequestAsync(id);

                if (imageRequest == null)
                {
                    return Results.NotFound();
                }

                var response = new ImageRequestResponseDto
                {
                    Id = imageRequest.Id,
                    Status = imageRequest.Status,
                    GeneratedImageUrl = imageRequest.GeneratedImageUrl,
                    OriginalImageUrl = imageRequest.OriginalImageUrl,
                    Prompt = imageRequest.Prompt,
                    CreditsCharged = imageRequest.CreditsCharged,
                    CreatedAt = imageRequest.CreatedAt,
                    CompletedAt = imageRequest.CompletedAt,
                    ErrorMessage = imageRequest.ErrorMessage
                };

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Error retrieving image request",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        // .RequireAuthorization() // Temporarily disabled for development testing
        .WithName("GetImageRequest")
        .WithTags("Image Generation")
        .WithOpenApi();

        // Get logs for an image generation request
        app.MapGet("/api/image-request/{id}/logs", (
            string id,
            ILogService logService) =>
        {
            var logs = logService.GetLogs(id);
            return Results.Ok(logs);
        })
        .WithName("GetImageRequestLogs")
        .WithTags("Image Generation")
        .WithOpenApi();

        // Get logs for the most recent image request (no ID needed)
        app.MapGet("/api/image-request/logs/latest", async (
            IImageRequestRepository repo,
            ILogService logService) =>
        {
            var recentList = await repo.GetRecentAsync(1);
            if (recentList == null || !recentList.Any())
            {
                return Results.NotFound(new { error = "No image requests found." });
            }
            var latest = recentList.First();
            var logs = logService.GetLogs(latest.Id);
            return Results.Ok(new { requestId = latest.Id, logs });
        })
        .WithName("GetLatestImageRequestLogs")
        .WithTags("Image Generation")
        .WithOpenApi();

        // Get user's image generation history
        app.MapGet("/api/history", async (
            ImageGenerationOrchestrator orchestrator,
            HttpContext context,
            int limit = 10) =>
        {
            try
            {
                // In a real app, get userId from authenticated user
                var userId = context.User?.Identity?.Name ?? "test-user";

                var history = await orchestrator.GetUserHistoryAsync(userId, limit);

                var response = history.Select(r => new ImageRequestResponseDto
                {
                    Id = r.Id,
                    Status = r.Status,
                    GeneratedImageUrl = r.GeneratedImageUrl,
                    OriginalImageUrl = r.OriginalImageUrl,
                    Prompt = r.Prompt,
                    CreditsCharged = r.CreditsCharged,
                    CreatedAt = r.CreatedAt,
                    CompletedAt = r.CompletedAt,
                    ErrorMessage = r.ErrorMessage
                }).ToList();

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Error retrieving history",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })        // .RequireAuthorization() // Temporarily disabled for development testing
        .WithName("GetHistory")
        .WithTags("Image Generation")
        .WithOpenApi();        // Test endpoint for debugging DALL-E generation
        app.MapGet("/api/test-dalle", async (
            IGenerationService generationService,
            ILogger<Program> logger,
            IConfiguration configuration,
            HttpContext context) =>
        {
            try
            {
                logger.LogInformation("Testing DALL-E generation...");

                // Check if we can read the API key for diagnostics
                var apiKeyExists = !string.IsNullOrEmpty(configuration["DallE:ApiKey"]);
                logger.LogInformation("DALL-E API key in configuration: {Exists}", apiKeyExists ? "Yes" : "No");

                if (apiKeyExists)
                {
                    var keyLength = configuration["DallE:ApiKey"]!.Length;
                    var firstChars = configuration["DallE:ApiKey"]!.Substring(0, Math.Min(5, keyLength));
                    logger.LogInformation("API key length: {Length}, starts with: {Start}...", keyLength, firstChars);
                }

                // Use a public sample image that definitely exists
                var sampleImageUrl = "https://images.pexels.com/photos/1571460/pexels-photo-1571460.jpeg";
                var testPrompt = "Add blue accent wall";

                logger.LogInformation("Using sample image: {Url} with prompt: {Prompt}",
                    sampleImageUrl, testPrompt);

                var generatedImageUrl = await generationService.GenerateImageAsync(
                    sampleImageUrl, testPrompt);

                return Results.Ok(new
                {
                    success = true,
                    message = "DALL-E generation successful",
                    generatedImageUrl
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error testing DALL-E generation");

                // Detailed diagnostics
                var diagnosticInfo = new Dictionary<string, object>
                {
                    ["Exception"] = new
                    {
                        Message = ex.Message,
                        Type = ex.GetType().Name,
                        StackTrace = ex.StackTrace,
                        InnerException = ex.InnerException?.Message
                    },
                    ["ApiKeyConfigured"] = !string.IsNullOrEmpty(configuration["DallE:ApiKey"]),
                    ["EnvironmentName"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
                };

                // Return detailed error information for debugging
                return Results.Problem(
                    title: "DALL-E Test Failed",
                    detail: System.Text.Json.JsonSerializer.Serialize(diagnosticInfo),
                    statusCode: 500);
            }
        })
        .WithName("TestDallE")
        .WithTags("Image Generation")
        .WithOpenApi();
    }
}
