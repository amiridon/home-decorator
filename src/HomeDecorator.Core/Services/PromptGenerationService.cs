using System;
using System.Collections.Generic;
using System.Linq;

namespace HomeDecorator.Core.Services
{
    public static class PromptGenerationService
    {
        private static readonly Random _random = new Random();
        private static readonly string _promptPrefix = "Clear the existing space of existing furniture and treatments and create a";

        private static readonly Dictionary<string, List<string>> _stylePrompts = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "Modern", new List<string>
                {
                    "with clean lines, a neutral color palette with a single bold accent wall, and pops of color from abstract art.",
                    "with sleek surfaces, high-gloss finishes on cabinets, and metallic accents. Consider a feature wall with geometric wood paneling.",
                    "with geometric patterns on a feature wall, minimalist furniture, and an abundance of natural light.",
                    "with an open-concept feel, polished concrete floors, and large, unadorned windows. Walls are a crisp white to maximize light.",
                    "with a focus on simplicity, using natural materials like a light wood slat wall, metal fixtures, and glass partitions."
                }
            },
            {
                "Minimalist", new List<string>
                {
                    "with only essential items, a monochromatic color scheme of whites and grays, and a focus on texture through a lime-washed accent wall.",
                    "with uncluttered surfaces, handleless cabinetry, and integrated appliances for a seamless look. Walls are a soft, uniform off-white.",
                    "with a simple, clean aesthetic, using a limited color palette and high-quality materials. A single wall might feature subtle, textured wallpaper.",
                    "with a focus on functionality and form, where every object has a purpose and a place. Walls are kept bare to emphasize space.",
                    "that is calm and serene, with a simple layout, large empty spaces, and a single piece of statement art on a stark white wall."
                }
            },
            {
                "Industrial", new List<string>
                {
                    "with exposed original brick walls on one side, and dark, moody paint on the others. Steel beams and concrete floors complete the look.",
                    "with a mix of raw materials, including a reclaimed wood accent wall, metal accents, and vintage-inspired furniture.",
                    "with an urban, lofty feel, featuring high ceilings, large, bare windows, and walls painted in a deep charcoal gray.",
                    "featuring a large wooden statement piece with a metal base, surrounded by functional, sturdy furniture, against a backdrop of a painted or exposed brick wall.",
                    "with a raw and edgy aesthetic, using a color palette of grays, blacks, and browns. One wall could be clad in corrugated metal sheeting."
                }
            },
            {
                "Scandinavian", new List<string>
                {
                    "that is bright and airy, with light wood floors, cozy textiles, and a focus on simplicity. Walls are painted a soft white or very light grey.",
                    "that is functional and beautiful, with white walls, clean lines, and pops of color. A single wall might have a subtle, light-colored wallpaper pattern.",
                    "with a neutral color palette, plush textiles, and natural wood elements like a slatted wood accent wall to create a cozy 'hygge' atmosphere.",
                    "with simple, elegant furniture, iconic design pieces, and a clutter-free environment against a backdrop of soft, muted wall colors.",
                    "that is filled with natural light, indoor plants, and a connection to the outdoors, with walls painted in a calming, nature-inspired hue like a soft sage green."
                }
            },
            {
                "Mid-Century Modern", new List<string>
                {
                    "with iconic furniture pieces, organic shapes, and a warm, earthy color palette on the walls, like terracotta or olive green.",
                    "with a retro feel, featuring flat-panel cabinetry, colorful accents, and a feature wall with bold, geometric wallpaper.",
                    "that is stylish and functional, with tapered-leg furniture, and a statement mirror against a wall painted in a deep teal or mustard yellow.",
                    "with a mix of natural and man-made materials, a feature wall with wood paneling, bold patterns, and a seamless flow between indoors and outdoors.",
                    "with a chic and timeless look, using geometric patterned wallpaper, a floating vanity or console, and brass or gold fixtures."
                }
            },
            {
                "Coastal", new List<string>
                {
                    "that is breezy and relaxed, with white shiplap walls, light-colored furniture, blue and sandy accents, and natural fiber rugs.",
                    "that is light-filled, with white cabinetry, a sea-glass-colored backsplash, and walls painted in a soft, airy blue.",
                    "that is relaxing and serene, with whitewashed wood paneling on the walls, crisp white linens, and ocean-themed artwork.",
                    "with a rustic wooden table or centerpiece, wicker or rattan furniture, against a backdrop of walls painted a sandy beige.",
                    "with white shiplap on an accent wall, a pebble-tile floor or accents, and a color palette of blues, whites, and sandy beiges on the remaining walls."
                }
            },
            { "Contemporary", new List<string>
                {
                    "with sleek surfaces, neutral colors, and bold accent pieces. Walls are painted in soft grays or whites.",
                    "featuring open space, minimal clutter, and a mix of textures. Consider a feature wall with modern art.",
                    "with large windows, natural light, and a blend of metal, glass, and wood finishes."
                }
            },
            { "Traditional", new List<string>
                {
                    "with classic furniture, rich wood tones, and elegant moldings. Walls are painted in warm neutrals.",
                    "featuring symmetrical layouts, antique accents, and detailed trim work.",
                    "with plush textiles, ornate rugs, and timeless decor pieces."
                }
            },
            { "Transitional", new List<string>
                {
                    "blending modern and traditional elements, with neutral walls and a mix of classic and contemporary furniture.",
                    "featuring clean lines, soft color palettes, and subtle patterns.",
                    "with a balance of comfort and sophistication, using both old and new decor."
                }
            },
            { "Bohemian", new List<string>
                {
                    "with vibrant colors, eclectic patterns, and layered textiles. Walls can be painted in bold hues or left white.",
                    "featuring a mix of vintage and handmade decor, plants, and global-inspired accents.",
                    "with relaxed seating, tapestries, and an abundance of decorative pillows."
                }
            }
        };

        // Room-specific fallback prompts
        private static readonly Dictionary<string, string> _roomFallbacks = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Garage", "with organized storage, clean floors, and modern lighting. Add functional workspaces and sleek cabinetry." },
            { "Basement", "with cozy seating, bright lighting, and moisture-resistant finishes. Consider a feature wall with art or shelving." },
            { "Bathroom", "with spa-like features, modern fixtures, and calming colors. Use tile accents and minimalist decor." },
            // Add more as needed...
        };

        public static string GetRandomPrompt(string style, string roomType)
        {
            // Try to get a style-specific prompt
            if (_stylePrompts.TryGetValue(style, out var prompts))
            {
                int index = _random.Next(prompts.Count);
                string description = prompts[index];
                // If the room type has a fallback, append it for extra context
                if (_roomFallbacks.TryGetValue(roomType, out var roomDesc))
                {
                    return $"{_promptPrefix} {roomType} in a {style} style {description} {roomDesc}";
                }
                return $"{_promptPrefix} {roomType} in a {style} style {description}";
            }
            // If style not found, fallback to room-specific prompt
            if (_roomFallbacks.TryGetValue(roomType, out var fallback))
            {
                return $"{_promptPrefix} {roomType} in a {style} style. {fallback}";
            }
            // Generic fallback
            return $"{_promptPrefix} {roomType} in a {style} style.";
        }

        public static List<string> GetAvailableStyles()
        {
            return _stylePrompts.Keys.ToList();
        }
    }
}
