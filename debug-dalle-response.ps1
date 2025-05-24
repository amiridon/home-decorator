
# Debug script for testing DALL-E 2 response handling

# Configuration
$apiUrl = "https://localhost:7075/api/debug/test-dalle-response"

# Make the API call
Write-Host "Testing DALL-E 2 response handling"
Write-Host "Calling API: $apiUrl"

try {
    $response = Invoke-RestMethod -Uri $apiUrl -Method Get
    
    Write-Host "Response received:"
    $response | ConvertTo-Json -Depth 10
    
    Write-Host "`n==== DALL-E Response Properties ===="
    foreach ($prop in $response.ResponseProperties.PSObject.Properties) {
        Write-Host "$($prop.Name): $($prop.Value)"
    }
    
    Write-Host "`n==== URL Accessibility Test ===="
    Write-Host "Image URL: $($response.ImageUrl)"
    Write-Host "Is Accessible: $($response.IsUrlAccessible)"
    
    if ($response.Success) {
        Write-Host "`n✅ Debug test completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "`n❌ Debug test failed!" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error calling API: $_" -ForegroundColor Red
}

Write-Host "`nTo test again, run this script after starting the API."
