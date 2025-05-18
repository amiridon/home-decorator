namespace HomeDecorator.Core.Services;

/// <summary>
/// Service for ranking and filtering product lists
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Gets recommended products for an image request
    /// </summary>
    /// <param name="imageRequestId">The ID of the image request</param>
    /// <returns>List of recommended product IDs</returns>
    Task<List<string>> GetRecommendationsAsync(string imageRequestId);

    /// <summary>
    /// Ranks and filters a list of products
    /// </summary>
    /// <param name="productIds">The list of product IDs with scores</param>
    /// <returns>Ranked and filtered list of product IDs</returns>
    Task<List<string>> RankAndFilterProductsAsync(List<(string ProductId, double Score)> productIds);
}
