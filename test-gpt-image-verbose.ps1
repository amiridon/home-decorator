# Simple GPT-Image-1 Test Script with User Secrets support and enhanced error handling
Write-Host "Testing GPT-Image-1 API with user secrets key..." -ForegroundColor Cyan

# First check if API key is set via environment variable
$apiKey = $env:OPENAI_API_KEY

# If not, try to get it from user secrets
if (-not $apiKey) {
    Write-Host "API key not found in environment variables, checking user secrets..." -ForegroundColor Yellow
    
    # Change to the API project directory
    $apiProjectDir = Join-Path $PSScriptRoot "src\HomeDecorator.Api"
    Push-Location $apiProjectDir
    
    try {
        # Get the API key from user secrets
        $secrets = dotnet user-secrets list 2>$null
        
        foreach ($secret in $secrets) {
            if ($secret -match "DallE:ApiKey = (.+)") {
                $apiKey = $matches[1]
                Write-Host "Found API key in user secrets (length: $($apiKey.Length))" -ForegroundColor Green
                Write-Host "First few characters: $($apiKey.Substring(0, [Math]::Min(5, $apiKey.Length)))..." -ForegroundColor Green
                break
            }
        }
    } catch {
        Write-Host "Error reading user secrets: $_" -ForegroundColor Red
    } finally {
        Pop-Location
    }
}

if (-not $apiKey) {
    Write-Host "ERROR: No API key found in environment variables or user secrets" -ForegroundColor Red
    Write-Host "Please either:" -ForegroundColor Yellow
    Write-Host "  1. Set your OpenAI API key: `$env:OPENAI_API_KEY = 'your-key-here'" -ForegroundColor Yellow
    Write-Host "  2. Add it to user secrets: dotnet user-secrets set 'DallE:ApiKey' 'your-key-here' --project src/HomeDecorator.Api" -ForegroundColor Yellow
    exit
}

# Download test image
$imageUrl = "https://images.pexels.com/photos/1571460/pexels-photo-1571460.jpeg"
$imagePath = "./test-image.png"
Write-Host "Downloading test image from $imageUrl..." -ForegroundColor Cyan
Invoke-WebRequest -Uri $imageUrl -OutFile $imagePath
Write-Host "Downloaded test image to $imagePath" -ForegroundColor Green

# Verify image was downloaded properly
if (Test-Path $imagePath) {
    $fileInfo = Get-Item -Path $imagePath
    Write-Host "Image file size: $($fileInfo.Length) bytes" -ForegroundColor Green
} else {
    Write-Host "ERROR: Failed to download test image" -ForegroundColor Red
    exit
}

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

Write-Host "Preparing to send request to $uri" -ForegroundColor Cyan
Write-Host "Using model: $($form.model)" -ForegroundColor Cyan
Write-Host "Prompt: $($form.prompt)" -ForegroundColor Cyan

# Build request with image file
try {
    Write-Host "Sending API request..." -ForegroundColor Cyan
    
    # Use verbose option to see full request/response details
    $progressPreference = 'Continue'
    $verbosePreference = 'Continue'
    
    $response = Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -Form $form -InFile $imagePath -Verbose
    
    # Display response
    Write-Host "API Response received!" -ForegroundColor Green
    $responseJson = $response | ConvertTo-Json -Depth 5
    Write-Host $responseJson -ForegroundColor Cyan
    
    # Download result if available
    if ($response.data -and $response.data[0].url) {
        $resultUrl = $response.data[0].url
        $resultPath = "./gpt-image1-result.png"
        Write-Host "Downloading result from $resultUrl..." -ForegroundColor Cyan
        Invoke-WebRequest -Uri $resultUrl -OutFile $resultPath
        Write-Host "Downloaded result to $resultPath" -ForegroundColor Green
        
        # Compare file sizes
        $originalSize = (Get-Item -Path $imagePath).Length
        $resultSize = (Get-Item -Path $resultPath).Length
        Write-Host "Original image size: $originalSize bytes" -ForegroundColor Green
        Write-Host "Result image size: $resultSize bytes" -ForegroundColor Green
        
        # Open the result
        Write-Host "Opening result image..." -ForegroundColor Green
        Start-Process $resultPath
    }
    elseif ($response.data -and $response.data[0].b64_json) {
        Write-Host "Got base64 encoded image instead of URL" -ForegroundColor Green
        $b64Data = $response.data[0].b64_json
        $resultPath = "./gpt-image1-result.png"
        
        # Convert base64 to binary and save
        $bytes = [Convert]::FromBase64String($b64Data)
        [IO.File]::WriteAllBytes($resultPath, $bytes)
        
        # Compare file sizes
        $originalSize = (Get-Item -Path $imagePath).Length
        $resultSize = (Get-Item -Path $resultPath).Length
        Write-Host "Original image size: $originalSize bytes" -ForegroundColor Green
        Write-Host "Result image size: $resultSize bytes" -ForegroundColor Green
        
        Write-Host "Saved result to $resultPath" -ForegroundColor Green
        
        # Open the result
        Start-Process $resultPath
    }
    else {
        Write-Host "Response doesn't contain an expected image format" -ForegroundColor Red
        Write-Host "Full response:" -ForegroundColor Yellow
        $response | ConvertTo-Json -Depth 5
    }
}
catch {
    Write-Host "Error calling GPT-Image-1 API:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.ErrorDetails) {
        Write-Host "API Error Details:" -ForegroundColor Red
        try {
            $errorJson = $_.ErrorDetails.Message | ConvertFrom-Json
            $errorJson | ConvertTo-Json -Depth 5
        } catch {
            Write-Host "Raw error details: $($_.ErrorDetails.Message)" -ForegroundColor Red
        }
    }
    
    Write-Host "Full exception:" -ForegroundColor Red
    $_ | Format-List -Force
}
