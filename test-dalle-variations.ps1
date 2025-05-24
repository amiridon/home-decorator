
# Test script for DALL-E 2 image variations

# Configuration
$apiUrl = "https://localhost:7075/api/test-dalle-variations"
$testImageUrl = "https://images.pexels.com/photos/1571460/pexels-photo-1571460.jpeg?auto=compress&cs=tinysrgb&w=1260&h=750&dpr=1" # Sample living room image

# Make the API call
Write-Host "Testing DALL-E 2 image variations with image: $testImageUrl"
Write-Host "Calling API: $apiUrl"

try {
    $response = Invoke-RestMethod -Uri "$apiUrl`?imageUrl=$([uri]::EscapeDataString($testImageUrl))" -Method Get
    
    Write-Host "Response received:"
    $response | ConvertTo-Json
    
    if ($response.success) {
        Write-Host "✅ Test completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "❌ Test failed: $($response.error)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error calling API: $_" -ForegroundColor Red
}
