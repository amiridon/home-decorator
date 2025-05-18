using System.Collections.Generic;
using System.Threading.Tasks;

namespace HomeDecorator.Core.Services
{
    /// <summary>
    /// Represents a product that can be matched or recommended
    /// </summary>
    public class Product
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public string? Currency { get; set; }
        public string? Vendor { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? DetailUrl { get; set; }
    }
}
