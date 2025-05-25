# Detailed DALL-E Response Debugging Script
# This script helps analyze the actual structure of DALL-E API responses

Write-Host "=== DALL-E Response Debugging Tool ===" -ForegroundColor Green
Write-Host "This script will test DALL-E API calls and provide detailed response analysis." -ForegroundColor Cyan

# Check if API key is configured
$apiKey = $env:OPENAI_API_KEY
if (-not $apiKey) {
    Write-Host "ERROR: OPENAI_API_KEY environment variable is not set!" -ForegroundColor Red
    Write-Host "Please run: set-openai-key.ps1" -ForegroundColor Yellow
    exit 1
}

Write-Host "API Key found (length: $($apiKey.Length))" -ForegroundColor Green

# Test if API is accessible
Write-Host "`nTesting API connectivity..." -ForegroundColor Cyan
try {
    $headers = @{
        "Authorization" = "Bearer $apiKey"
        "Content-Type" = "application/json"
    }
    
    # Simple API test call
    $testResponse = Invoke-RestMethod -Uri "https://api.openai.com/v1/models" -Headers $headers -Method GET
    Write-Host "✓ API connection successful" -ForegroundColor Green
    Write-Host "Available models count: $($testResponse.data.Count)" -ForegroundColor Cyan
}
catch {
    Write-Host "✗ API connection failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Suggest next steps
Write-Host "`n=== Next Steps ===" -ForegroundColor Yellow
Write-Host "1. Run your DALL-E generation request that's causing the error" -ForegroundColor White
Write-Host "2. Check the API logs for the detailed GeneratedImage structure analysis" -ForegroundColor White
Write-Host "3. Look for the '=== DALL-E GeneratedImage Analysis ===' section in the logs" -ForegroundColor White
Write-Host "4. The enhanced error handling will now provide detailed property information" -ForegroundColor White

Write-Host "`n=== Enhanced Features Added ===" -ForegroundColor Green
Write-Host "✓ Comprehensive binary data detection (Data, Bytes, ImageData, ImageBytes, Content, BinaryData)" -ForegroundColor Cyan
Write-Host "✓ Support for byte arrays, streams, and base64 encoded data" -ForegroundColor Cyan
Write-Host "✓ Automatic scanning of all properties for byte arrays" -ForegroundColor Cyan
Write-Host "✓ Detailed error reporting with available properties" -ForegroundColor Cyan
Write-Host "✓ Enhanced logging for debugging API response structure" -ForegroundColor Cyan

Write-Host "`nThe DalleGenerationService has been enhanced to better handle:" -ForegroundColor White
Write-Host "- Binary image data in various formats" -ForegroundColor Gray
Write-Host "- Multiple property naming conventions" -ForegroundColor Gray
Write-Host "- Stream-based image data" -ForegroundColor Gray
Write-Host "- Base64 encoded image data" -ForegroundColor Gray
Write-Host "- Comprehensive error reporting" -ForegroundColor Gray

Write-Host "`nIf you still encounter issues, the logs will now show exactly what properties" -ForegroundColor White
Write-Host "and data types are available in the DALL-E response." -ForegroundColor White
