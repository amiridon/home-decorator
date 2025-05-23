using HomeDecorator.Core.Models;
using HomeDecorator.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace HomeDecorator.Api.Endpoints;

/// <summary>
/// API endpoints for image generation functionality
/// </summary>
public static class ImageGenerationEndpoints
{
    public static void MapImageGenerationEndpoints(this WebApplication app)
    {
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
        .RequireAuthorization()
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
        .RequireAuthorization()
        .WithName("GetImageRequest")
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
        })
        .RequireAuthorization()
        .WithName("GetHistory")
        .WithTags("Image Generation")
        .WithOpenApi();
    }
}
