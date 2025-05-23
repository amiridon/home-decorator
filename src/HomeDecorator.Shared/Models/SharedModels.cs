using System;

namespace HomeDecorator.Shared.Models
{
    public class DesignRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string OriginalImageUrl { get; set; }
        public required string Prompt { get; set; }
        public string Status { get; set; } = "Pending";
        public string? GeneratedImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class Product
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public decimal Price { get; set; }
        public required string Currency { get; set; }
        public required string Vendor { get; set; }
        public required string ThumbnailUrl { get; set; }
        public required string DetailUrl { get; set; }
    }
}
