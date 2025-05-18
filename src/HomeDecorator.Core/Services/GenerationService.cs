namespace HomeDecorator.Core.Services;

/// <summary>
/// Service for handling image generation operations
/// Calls DALL·E and manages image storage
/// </summary>
public interface IGenerationService
{
    /// <summary>
    /// Generates an image from a prompt
    /// </summary>
    /// <param name="originalImageUrl">URL of the original image</param>
    /// <param name="prompt">The text prompt for generation</param>
    /// <returns>URL of the generated image</returns>
    Task<string> GenerateImageAsync(string originalImageUrl, string prompt);

    /// <summary>
    /// Gets the status of a generation request
    /// </summary>
    /// <param name="requestId">The ID of the generation request</param>
    /// <returns>The status of the generation</returns>
    Task<string> GetGenerationStatusAsync(string requestId);
}
