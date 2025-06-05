# Simple GPT-Image-1 Test Script
$apiKey = $env:OPENAI_API_KEY
if (-not $apiKey) {
    Write-Host "ERROR: No API key found" -ForegroundColor Red
    Write-Host "Please set your OpenAI API key: $env:OPENAI_API_KEY = 'your-key-here'" -ForegroundColor Yellow
    exit
}

Write-Host "Testing GPT-Image-1 API..." -ForegroundColor Cyan

# Download test image
$imageUrl = "https://images.pexels.com/photos/1571460/pexels-photo-1571460.jpeg"
$imagePath = "./test-image.png"
Invoke-WebRequest -Uri $imageUrl -OutFile $imagePath
Write-Host "Downloaded test image to $imagePath" -ForegroundColor Green

# Create API request
$uri = "https://api.openai.com/v1/images/edits"
$headers = @{
    "Authorization" = "Bearer $apiKey"
}

# Create form data
$form = @{
    model = "gpt-image-1"
    prompt = "Show me the exact same image but remove all people and change the furniture color to blue."
    n = 1
    size = "1024x1024"
}

# Add image file
$fileBinary = Get-Item -Path $imagePath
$fileStream = $fileBinary.OpenRead()
$fileName = $fileBinary.Name

# Build request with image file
try {
    Write-Host "Sending API request..." -ForegroundColor Cyan
    $response = Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -Form $form -InFile $imagePath 
    
    # Display response
    Write-Host "API Response:" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 5
    
    # Download result if available
    if ($response.data -and $response.data[0].url) {
        $resultUrl = $response.data[0].url
        $resultPath = "./gpt-image1-result.png"
        Invoke-WebRequest -Uri $resultUrl -OutFile $resultPath
        Write-Host "Downloaded result to $resultPath" -ForegroundColor Green
    }
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}
finally {
    if ($null -ne $fileStream) { $fileStream.Close() }
}
