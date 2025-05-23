namespace HomeDecorator.Core.Models;

/// <summary>
/// Represents an image generation request
/// </summary>
public class ImageRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string OriginalImageUrl { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed
    public string? GeneratedImageUrl { get; set; }
    public int CreditsCharged { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Request DTO for creating an image generation
/// </summary>
public class CreateImageRequestDto
{
    public string OriginalImageUrl { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for image generation requests
/// </summary>
public class ImageRequestResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? GeneratedImageUrl { get; set; }
    public string OriginalImageUrl { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public int CreditsCharged { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
