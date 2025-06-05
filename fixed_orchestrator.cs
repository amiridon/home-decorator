// filepath: c:\Users\ImamuHunter\dev\home-decorator\src\HomeDecorator.Api\Services\ImageGenerationOrchestrator.cs
using HomeDecorator.Core.Models;
using HomeDecorator.Core.Services;
using SkiaSharp; // For cross-platform image processing
using System.IO; // Ensure this is included

namespace HomeDecorator.Api.Services;

/// <summary>
/// High-level service that orchestrates the image generation workflow
/// </summary>
public class ImageGenerationOrchestrator
{
    private readonly IGenerationService _generationService;
    private readonly IImageRequestRepository _imageRequestRepository;
    private readonly IBillingService _billingService;
    private readonly IProductMatcherService _productMatcherService;
    private readonly ILogger<ImageGenerationOrchestrator> _logger;
    private readonly ILogService _logService; // replace ISqliteLogService with ILogService
    private readonly MaskGenerationService _maskGenerationService;

    // Cost configuration - in a real app this would come from configuration
    private const int GENERATION_COST_CREDITS = 1;

    public ImageGenerationOrchestrator(
        IGenerationService generationService,
        IImageRequestRepository imageRequestRepository,
        IBillingService billingService,
        IProductMatcherService productMatcherService,
        ILogger<ImageGenerationOrchestrator> logger,
        ILogService logService, // Inject log service
        MaskGenerationService maskGenerationService)
    {
        _generationService = generationService;
        _imageRequestRepository = imageRequestRepository;
        _billingService = billingService;
        _productMatcherService = productMatcherService;
        _logger = logger;
        _logService = logService; // Initialize log service
        _maskGenerationService = maskGenerationService;
    }

    /// <summary>
    /// Creates and processes a new image generation request
    /// </summary>
    public async Task<ImageRequest> CreateAndProcessRequestAsync(string userId, CreateImageRequestDto requestDto)
    {
        _logger.LogInformation("Starting image generation request for user: {UserId}", userId);        
        // Create the request record
        var imageRequest = new ImageRequest
        {
            UserId = userId,
            OriginalImageUrl = requestDto.OriginalImageUrl,
            Prompt = requestDto.Prompt,
            CustomPrompt = requestDto.CustomPrompt,
            Status = "Pending",
            UseMask = requestDto.UseMask, // Include mask generation flag from request
            CreditsCharged = GENERATION_COST_CREDITS
        };

        // Persist request and log creation
        await _imageRequestRepository.CreateAsync(imageRequest);
        _logger.LogInformation("Created image request: {RequestId}", imageRequest.Id);
        _logService.Log(imageRequest.Id, "Information", "Image request created.");

        // Process the request asynchronously
        _ = Task.Run(async () => await ProcessRequestAsync(imageRequest));

        return imageRequest;
    }

    /// <summary>
    /// Gets an image request by ID
    /// </summary>
    public async Task<ImageRequest?> GetRequestAsync(string requestId)
    {
        return await _imageRequestRepository.GetByIdAsync(requestId);
    }

    /// <summary>
    /// Gets image generation history for a user
    /// </summary>
    public async Task<List<ImageRequest>> GetUserHistoryAsync(string userId, int limit = 10)
    {
        return await _imageRequestRepository.GetByUserIdAsync(userId, limit);
    }

    /// <summary>
    /// Processes an image generation request (background task)
    /// </summary>
    private async Task ProcessRequestAsync(ImageRequest request)
    {
        try
        {
            // Log that processing has started in DB
            _logService.Log(request.Id, "Information", "Processing of image request started.");
            _logger.LogInformation("Processing image request: {RequestId}", request.Id);

            // Update status to processing
            request.Status = "Processing";
            await _imageRequestRepository.UpdateAsync(request);

            // Deduct credits first
            var creditsDeducted = await _billingService.DeductCreditsAsync(request.UserId, request.CreditsCharged);
            if (!creditsDeducted)
            {
                throw new InvalidOperationException("Failed to deduct credits");
            }

            // Determine the prompt to use for image generation
            string generationPrompt = !string.IsNullOrEmpty(request.CustomPrompt)
                ? request.CustomPrompt
                : $"Update the decor style to {request.Prompt}. Maintain the room's structural integrity, including walls, windows, ceiling, and floor. Focus on changing decor elements like furniture, wall art, and lighting."; // Fallback if CustomPrompt is missing            
                
            // Generate the image using the determined prompt and the decor style (request.Prompt)
            _logger.LogInformation("Calling generation service with originalImageUrl: {OriginalImageUrl}", request.OriginalImageUrl);
            _logService.Log(request.Id, "Information", $"Starting image generation with prompt: {generationPrompt}");

            string generatedImageUrl;
            try
            {
                // Download the original image as a stream (PNG)
                _logger.LogInformation("Downloading original image from URL: {OriginalImageUrl}", request.OriginalImageUrl);
                using var httpClient = new HttpClient();

                // Add timeout to avoid hanging if the image server is slow
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                using var imageResponse = await httpClient.GetAsync(request.OriginalImageUrl);

                // Log response details
                _logger.LogInformation("Original image download response: Status: {Status}, Content-Type: {ContentType}, Content-Length: {ContentLength}",
                    imageResponse.StatusCode,
                    imageResponse.Content.Headers.ContentType?.MediaType ?? "unknown",
                    imageResponse.Content.Headers.ContentLength?.ToString() ?? "unknown");

                if (!imageResponse.IsSuccessStatusCode)
                {
                    var errorContent = await imageResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to download original image for edit: {StatusCode}, Error: {Error}",
                        imageResponse.StatusCode, errorContent);
                    _logService.Log(request.Id, "Error", $"Failed to download original image: {imageResponse.StatusCode}");
                    throw new InvalidOperationException($"Failed to download original image for edit: {imageResponse.StatusCode}");
                }

                // Validate content type - should be an image
                var contentType = imageResponse.Content.Headers.ContentType?.MediaType;
                if (contentType == null || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("Downloaded content is not an image. Content-Type: {ContentType}", contentType ?? "null");
                    _logService.Log(request.Id, "Error", $"Downloaded content is not an image. Content-Type: {contentType ?? "null"}");
                    throw new InvalidOperationException($"Downloaded content is not an image. Content-Type: {contentType ?? "null"}");
                }

                await using var imageStream = await imageResponse.Content.ReadAsStreamAsync();

                // Verify stream has content
                if (imageResponse.Content.Headers.ContentLength == 0 ||
                    (imageStream.CanSeek && imageStream.Length == 0))
                {
                    _logger.LogError("Downloaded image is empty");
                    _logService.Log(request.Id, "Error", "Downloaded image is empty");
                    throw new InvalidOperationException("Downloaded image is empty");
                }

                _logger.LogInformation("Successfully downloaded original image, size: {Size} bytes",
                    imageResponse.Content.Headers.ContentLength ?? -1);
                    
                // Convert image to PNG if necessary
                MemoryStream pngMs;
                if (contentType != null && !contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Converting {ContentType} image to PNG format", contentType);

                    // Create a memory stream for the PNG output
                    pngMs = new MemoryStream();

                    try
                    {
                        // Load the image data using SkiaSharp
                        using (var skBitmap = SKBitmap.Decode(imageStream))
                        {
                            if (skBitmap != null)
                            {
                                // Create an image and encode as PNG
                                using var skImage = SKImage.FromBitmap(skBitmap);
                                using var skData = skImage.Encode(SKEncodedImageFormat.Png, 100);

                                // Write the PNG data to the memory stream
                                skData.SaveTo(pngMs);
                            }
                            else
                            {
                                _logger.LogError("Failed to decode image with SkiaSharp");
                                throw new InvalidOperationException("Failed to decode image for PNG conversion");
                            }
                        }

                        // Rewind the stream for reading
                        pngMs.Position = 0;
                        _logger.LogInformation("Successfully converted image to PNG format, size: {Size} bytes", pngMs.Length);
                    }
                    catch (Exception ex)
                    {
                        pngMs.Dispose();
                        _logger.LogError(ex, "Error converting image to PNG: {Message}", ex.Message);
                        throw new InvalidOperationException($"Error converting image to PNG: {ex.Message}", ex);
                    }
                }
                else
                {
                    // Already PNG, just copy the stream to ensure we can read it from the beginning
                    pngMs = new MemoryStream();
                    await imageStream.CopyToAsync(pngMs);
                    pngMs.Position = 0;
                    _logger.LogInformation("Image is already PNG format, copied to memory stream, size: {Size} bytes", pngMs.Length);
                }
                
                // Call the new image-to-image edit method with the PNG stream
                try
                {
                    Stream? maskStream = null;

                    // Check if mask should be generated
                    if (request.UseMask)
                    {
                        _logger.LogInformation("Generating mask for image generation request {RequestId}", request.Id);
                        _logService.Log(request.Id, "Information", "Generating mask to focus changes on furniture items");

                        // Reset PNG stream position for reading
                        pngMs.Position = 0;

                        try
                        {
                            // Create a copy of the image stream for mask generation
                            var pngMsCopy = new MemoryStream();
                            await pngMs.CopyToAsync(pngMsCopy);
                            pngMsCopy.Position = 0;

                            // Generate mask - transparent areas (alpha=0) will be edited, white opaque areas will be preserved
                            maskStream = await _maskGenerationService.GenerateMaskAsync(pngMsCopy);

                            _logger.LogInformation("Successfully generated mask for request {RequestId}", request.Id);
                            _logService.Log(request.Id, "Information", "Mask generated successfully");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error generating mask for request {RequestId}: {Error}", request.Id, ex.Message);
                            _logService.Log(request.Id, "Warning", $"Mask generation failed, proceeding without mask: {ex.Message}");
                            // Continue without a mask if generation fails
                            maskStream = null;
                        }
                    }

                    // Reset PNG stream position for reading
                    pngMs.Position = 0;

                    // Call DALL-E with or without the mask
                    generatedImageUrl = await ((DalleGenerationService)_generationService)
                        .GenerateImageEditAsync(pngMs, maskStream, generationPrompt, request.Prompt);

                    // Log whether a mask was used
                    if (maskStream != null)
                    {
                        _logService.Log(request.Id, "Information", "Generated image with mask to preserve structural elements");
                    }
                }
                finally
                {
                    // Make sure we dispose the memory stream
                    pngMs.Dispose();
                }

                if (string.IsNullOrEmpty(generatedImageUrl))
                {
                    _logger.LogError("Generation service returned empty URL");
                    _logService.Log(request.Id, "Error", "Generation service returned empty URL");
                    throw new InvalidOperationException("Generation service returned empty URL");
                }

                // Verify the URL format
                _logger.LogInformation("Received generated image URL: {GeneratedImageUrl}", generatedImageUrl);
                _logService.Log(request.Id, "Information", $"Image generated successfully. URL: {generatedImageUrl}");

                // Check if the URL is relative and log it
                if (generatedImageUrl.StartsWith("/"))
                {
                    _logger.LogInformation("Generated URL is relative. This is expected for local storage.");

                    // Try to check if the file exists
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", generatedImageUrl.TrimStart('/'));
                    bool fileExists = File.Exists(filePath);
                    _logger.LogInformation("File exists at path {FilePath}: {Exists}", filePath, fileExists);
                    _logService.Log(request.Id, "Information", $"Generated image file exists: {fileExists}. Path: {filePath}");
                }

                request.GeneratedImageUrl = generatedImageUrl;
                _logger.LogInformation("Generated image for request {RequestId}: {GeneratedImageUrl}", request.Id, generatedImageUrl);

                // Update status to completed
                request.Status = "Completed";
                request.CompletedAt = DateTime.UtcNow;
                await _imageRequestRepository.UpdateAsync(request);

                _logger.LogInformation("Completed image request: {RequestId}", request.Id);

                // Optionally, start product matching in the background
                _ = Task.Run(async () => await MatchProductsAsync(request.Id, generatedImageUrl));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating image: {Message}", ex.Message);
                _logService.Log(request.Id, "Error", $"Image generation failed: {ex.Message}");
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image request: {RequestId}", request.Id);

            // Update request with error
            request.Status = "Failed";
            request.ErrorMessage = ex.Message;
            request.CompletedAt = DateTime.UtcNow;
            await _imageRequestRepository.UpdateAsync(request);

            // Log error to database
            _logService.Log(request.Id, "Error", ex.Message);

            // Note: In a production system, you might want to refund credits on failure
        }
    }

    /// <summary>
    /// Matches products to a generated image (background task)
    /// </summary>
    private async Task MatchProductsAsync(string requestId, string imageUrl)
    {
        try
        {
            _logger.LogInformation("Starting product matching for request: {RequestId}", requestId);

            var products = await _productMatcherService.DetectAndMatchProductsAsync(imageUrl);

            _logger.LogInformation("Found {ProductCount} product matches for request: {RequestId}",
                products.Count, requestId);

            // TODO: Store product matches in database
            // This will be implemented in Week 4
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching products for request: {RequestId}", requestId);
            // Product matching failure shouldn't affect the main generation
        }
    }
}
