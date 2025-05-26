# SAM Mask Generation with Automatic Class Filtering

## Overview

The enhanced `MaskGenerationService` now provides fully automatic class filtering with ultra-clean masks. This system automatically identifies structural elements that should be preserved and decorative elements that can be edited.

## Key Features

### 1. Automatic Class Filtering
- **High Confidence Threshold (0.8+)**: Objects detected with high confidence are automatically categorized
- **Medium Confidence Threshold (0.6+)**: Uses stricter categorization rules
- **Low Confidence (<0.6)**: Conservative approach, defaults to preserving unless obviously furniture

### 2. Ultra-Clean Masks
- **Feathering**: Applies 2-3px Gaussian blur followed by binary thresholding
- **Edge Smoothing**: Eliminates hard cut lines in final renders
- **Professional Quality**: Production-ready mask output

### 3. Performance Optimization
- **Down-sizing**: Automatically processes at 1024px for speed
- **Up-sizing**: Scales masks back to original resolution
- **Caching**: 30-minute cache for processed masks

### 4. Multi-Pass Editing
- **Furniture Pass**: Identifies sofas, chairs, tables, lamps, etc.
- **Wall Art Pass**: Separate detection for paintings, artwork, mirrors
- **Intelligent Combining**: Merges masks using multiply blend mode

## Configuration

Add to `appsettings.json`:

```json
{
  "SAM": {
    "Enabled": true,
    "ApiEndpoint": "https://your-sam-api-endpoint.com/segment",
    "ApiKey": "your-api-key",
    "MultiPass": true,
    "OptimizationEnabled": true,
    "FeatheringEnabled": true,
    "ConfidenceThresholds": {
      "High": 0.8,
      "Medium": 0.6,
      "Low": 0.3
    }
  }
}
```

## Object Categories

### Automatically Preserved (Structural)
- Walls, ceiling, floor
- Windows, doors, doorways
- Beams, columns, molding
- Electrical outlets, switches
- HVAC vents, radiators
- Stairs, railings

### Automatically Editable (Decorative)
- Furniture: sofas, chairs, tables, beds
- Lighting: lamps, chandeliers
- Storage: cabinets, shelves, dressers
- Textiles: rugs, curtains, pillows
- Decor: artwork, plants, vases
- Electronics: TVs, appliances

### Wall Art (Separate Pass)
- Paintings and artwork
- Photographs and prints
- Mirrors and frames
- Wall-mounted decorations

## Usage Examples

### Basic Usage
```csharp
var maskStream = await maskService.GenerateMaskAsync(imageStream);
```

### With Configuration Overrides
```csharp
var configOverrides = new Dictionary<string, string>
{
    ["SAM:MultiPass"] = "true",
    ["SAM:FeatheringEnabled"] = "true"
};

var maskStream = await maskService.GenerateMaskAsync(imageStream, configOverrides);
```

## API Response Format

Expected SAM API response format:

```json
{
  "segments": [
    {
      "class": "sofa",
      "confidence": 0.95,
      "mask": "base64-encoded-mask-image",
      // or alternatively:
      "points": [
        {"x": 100, "y": 150},
        {"x": 200, "y": 150},
        {"x": 200, "y": 250},
        {"x": 100, "y": 250}
      ]
    }
  ]
}
```

## Filtering Logic

### High Confidence (0.8+)
- Structural elements → Always preserved
- Editable elements → Always editable

### Medium Confidence (0.6-0.8)
- Uses strict categorization
- "Definitely structural" → Preserved
- "Definitely editable" → Editable
- Unknown → Defaults to editable

### Low Confidence (<0.6)
- Conservative approach
- Only "obvious furniture" → Editable
- Everything else → Preserved

## Performance Tips

1. **Image Size**: The service automatically optimizes large images to 1024px for SAM processing
2. **Caching**: Identical images with same config are cached for 30 minutes
3. **Multi-pass**: Use for complex scenes with mixed furniture and wall art
4. **Single-pass**: Use for simple scenes or when speed is critical

## Troubleshooting

### SAM API Not Available
- Falls back to demo mask (center 60% transparent)
- Logs warning about fallback usage

### Poor Segmentation Quality
- Check confidence thresholds in configuration
- Verify image quality and lighting
- Consider adjusting `OptimalSegmentationSize` for your use case

### Performance Issues
- Disable multi-pass for faster processing
- Reduce `OptimalSegmentationSize` for speed
- Check cache hit rates in logs

## Best Practices

1. **Test with Representative Images**: Validate filtering logic with your typical interior photos
2. **Monitor Confidence Scores**: Adjust thresholds based on your SAM API's performance
3. **Use Multi-pass Strategically**: Enable for complex scenes, disable for simple ones
4. **Cache Warming**: Pre-generate masks for common room types
5. **Fallback Strategy**: Always handle SAM API failures gracefully

## Future Enhancements

- **Room Type Detection**: Automatic adjustment of filtering based on room type
- **User Preference Learning**: Adaptive filtering based on user corrections
- **Batch Processing**: Process multiple images simultaneously
- **Custom Categories**: User-defined object categories
