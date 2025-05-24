namespace HomeDecorator.Core.Models;

/// <summary>
/// Represents a log entry for image generation requests.
/// </summary>
public class LogEntry
{
    public long Id { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
