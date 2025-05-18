using System;

namespace HomeDecorator.Shared.Models
{
    public class DesignRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string OriginalImageUrl { get; set; }
        public string Prompt { get; set; }
        public string Status { get; set; } = "Pending";
        public string GeneratedImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class Product
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; }
        public string Vendor { get; set; }
        public string ThumbnailUrl { get; set; }
        public string DetailUrl { get; set; }
    }
}
