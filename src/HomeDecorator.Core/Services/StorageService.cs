namespace HomeDecorator.Core.Services;

/// <summary>
/// Service for storing and managing images
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Stores an image from a URL and returns the permanent storage URL
    /// </summary>
    /// <param name="imageUrl">The URL of the image to store</param>
    /// <param name="category">Category for organizing images (e.g., "original", "generated")</param>
    /// <returns>The permanent storage URL</returns>
    Task<string> StoreImageFromUrlAsync(string imageUrl, string category);

    /// <summary>
    /// Stores an image from a stream and returns the permanent storage URL
    /// </summary>
    /// <param name="imageStream">The image stream</param>
    /// <param name="fileName">The desired file name</param>
    /// <param name="category">Category for organizing images</param>
    /// <returns>The permanent storage URL</returns>
    Task<string> StoreImageFromStreamAsync(Stream imageStream, string fileName, string category);

    /// <summary>
    /// Deletes an image from storage
    /// </summary>
    /// <param name="imageUrl">The URL of the image to delete</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteImageAsync(string imageUrl);
}
