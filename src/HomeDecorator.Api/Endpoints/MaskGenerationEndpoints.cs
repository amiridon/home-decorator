using HomeDecorator.Api.Services;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace HomeDecorator.Api.Endpoints;

/// <summary>
/// Endpoints for mask generation testing and development
/// </summary>
public static class MaskGenerationEndpoints
{
    public static void MapMaskGenerationEndpoints(this WebApplication app)
    {
        // Endpoint to check if SAM API is available
        app.MapGet("/api/mask/status", (
            IConfiguration config,
            ILogger<Program> logger) =>
        {
            var samApiEndpoint = config["SAM:ApiEndpoint"];
            bool samEnabled = !string.IsNullOrEmpty(samApiEndpoint) &&
                             config.GetValue<bool>("SAM:Enabled", false);

            logger.LogInformation("Checking SAM API status. Enabled: {Enabled}", samEnabled);

            return Results.Ok(new
            {
                samEnabled = samEnabled,
                samApiEndpoint = samEnabled ? samApiEndpoint : null
            });
        })
        .WithName("MaskStatus")
        .WithTags("Mask Generation")
        .WithOpenApi();

        // Generate a mask for an uploaded image
        app.MapPost("/api/mask/generate", async (
            HttpContext context,
            MaskGenerationService maskService,
            IConfiguration config,
            ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation("Received request to generate mask");

                if (!context.Request.HasFormContentType)
                {
                    return Results.BadRequest(new { error = "Request must be multipart/form-data" });
                }

                var form = await context.Request.ReadFormAsync();
                var file = form.Files["image"];

                if (file == null)
                {
                    logger.LogError("No image file was provided");
                    return Results.BadRequest(new { error = "No image file was provided" });
                }

                if (!file.ContentType.StartsWith("image/"))
                {
                    logger.LogError("Uploaded file is not an image. Content-Type: {ContentType}", file.ContentType);
                    return Results.BadRequest(new { error = "Uploaded file must be an image" });
                }

                logger.LogInformation("Processing image with size: {Size} bytes", file.Length);

                // Get mask options from form
                var maskType = form["maskType"].ToString();
                var preserveWalls = form["preserveWalls"].ToString().ToLower() == "true";
                var preserveWindows = form["preserveWindows"].ToString().ToLower() == "true";
                var preserveFloors = form["preserveFloors"].ToString().ToLower() == "true";
                var preserveElements = form["preserveElements"].ToString();

                logger.LogInformation("Mask options - Type: {MaskType}, PreserveWalls: {PreserveWalls}, " +
                    "PreserveWindows: {PreserveWindows}, PreserveFloors: {PreserveFloors}, PreserveElements: {PreserveElements}",
                    maskType, preserveWalls, preserveWindows, preserveFloors, preserveElements);

                // Override configuration for this request if needed
                var configOverrides = new Dictionary<string, string>();

                // Set SAM API usage based on mask type
                if (!string.IsNullOrEmpty(maskType))
                {
                    if (maskType == "sam")
                    {
                        configOverrides["SAM:Enabled"] = "true";
                    }
                    else if (maskType == "demo")
                    {
                        configOverrides["SAM:Enabled"] = "false";
                    }
                    // "automatic" uses the default configuration
                }

                // Generate the mask
                using var inputStream = file.OpenReadStream();
                using var maskStream = await maskService.GenerateMaskAsync(inputStream, configOverrides);

                // Set appropriate headers for image download
                context.Response.Headers.ContentType = "image/png";
                context.Response.Headers.ContentDisposition = "attachment; filename=\"mask.png\"";

                // Copy the mask stream directly to the response
                await maskStream.CopyToAsync(context.Response.Body);
                return Results.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating mask: {Message}", ex.Message);
                return Results.Problem(
                    title: "Error generating mask",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .DisableAntiforgery()
        .WithName("GenerateMask")
        .WithTags("Mask Generation")
        .WithOpenApi();

        // Add a route for the mask test page
        app.MapGet("/mask-test", () => Results.Redirect("/mask-test.html"));
    }
}
