using HomeDecorator.Core.Models;
using HomeDecorator.Core.Services; // ensure ILogService is included

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

    // Cost configuration - in a real app this would come from configuration
    private const int GENERATION_COST_CREDITS = 1;

    public ImageGenerationOrchestrator(
        IGenerationService generationService,
        IImageRequestRepository imageRequestRepository,
        IBillingService billingService,
        IProductMatcherService productMatcherService,
        ILogger<ImageGenerationOrchestrator> logger,
        ILogService logService) // Inject log service
    {
        _generationService = generationService;
        _imageRequestRepository = imageRequestRepository;
        _billingService = billingService;
        _productMatcherService = productMatcherService;
        _logger = logger;
        _logService = logService; // Initialize log service
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
            var generatedImageUrl = await _generationService.GenerateImageAsync(request.OriginalImageUrl, generationPrompt, request.Prompt);
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
