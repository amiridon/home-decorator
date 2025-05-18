namespace HomeDecorator.Core.Services;

/// <summary>
/// Service for matching products to generated images
/// Detects objects and fetches matching SKUs
/// </summary>
public interface IProductMatcherService
{
    /// <summary>
    /// Detects objects in an image and matches them to products
    /// </summary>
    /// <param name="imageUrl">URL of the image to analyze</param>
    /// <returns>List of product IDs with confidence scores</returns>
    Task<List<(string ProductId, double Score)>> DetectAndMatchProductsAsync(string imageUrl);
}
