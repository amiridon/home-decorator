using HomeDecorator.Api.Services;
using HomeDecorator.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomeDecorator.Api.Controllers;

[ApiController]
[Route("api/test")]
public class TestGenerationController : ControllerBase
{
    private readonly ILogger<TestGenerationController> _logger;
    private readonly IGenerationService _generationService;
    private readonly IConfiguration _configuration;
    private readonly IStorageService _storageService;

    public TestGenerationController(
        ILogger<TestGenerationController> logger,
        IGenerationService generationService,
        IConfiguration configuration,
        IStorageService storageService)
    {
        _logger = logger;
        _generationService = generationService;
        _configuration = configuration;
        _storageService = storageService;
    }

    [HttpGet("generate")]
    public async Task<IActionResult> TestGenerate([FromQuery] string imageUrl, [FromQuery] string prompt = "Modern", [FromQuery] string roomType = "Living Room")
    {
        try
        {
            _logger.LogInformation("Testing image generation with URL: {Url} and prompt: {Prompt}", imageUrl, prompt);

            var generatedPrompt = PromptGenerationService.GetRandomPrompt(prompt, roomType);

            // Generate the image
            var generatedUrl = await _generationService.GenerateImageAsync(imageUrl, generatedPrompt);

            // Check if the URL is relative and build full URL for client
            if (generatedUrl.StartsWith("/"))
            {
                var request = HttpContext.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}";
                var fullUrl = $"{baseUrl}{generatedUrl}";

                _logger.LogInformation("Generated image URL: {Url}", fullUrl);

                // Check if the file exists
                var rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var filePath = Path.Combine(rootPath, generatedUrl.TrimStart('/'));
                bool fileExists = System.IO.File.Exists(filePath);

                return Ok(new
                {
                    success = true,
                    generatedUrl,
                    fullUrl,
                    fileExists,
                    filePath,
                    message = "Image generated successfully"
                });
            }

            return Ok(new
            {
                success = true,
                generatedUrl,
                message = "Image generated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test generation");
            return BadRequest(new
            {
                success = false,
                error = ex.Message,
                innerError = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
    }
}
