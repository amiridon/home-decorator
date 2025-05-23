using HomeDecorator.Core.Models;

namespace HomeDecorator.Core.Services;

/// <summary>
/// Repository for managing image generation requests
/// </summary>
public interface IImageRequestRepository
{
    /// <summary>
    /// Creates a new image request
    /// </summary>
    Task<ImageRequest> CreateAsync(ImageRequest request);

    /// <summary>
    /// Gets an image request by ID
    /// </summary>
    Task<ImageRequest?> GetByIdAsync(string id);

    /// <summary>
    /// Updates an existing image request
    /// </summary>
    Task<ImageRequest> UpdateAsync(ImageRequest request);

    /// <summary>
    /// Gets image requests for a user
    /// </summary>
    Task<List<ImageRequest>> GetByUserIdAsync(string userId, int limit = 10);

    /// <summary>
    /// Gets recent image requests across all users (for admin)
    /// </summary>
    Task<List<ImageRequest>> GetRecentAsync(int limit = 50);
}
