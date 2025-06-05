# Direct curl test for OpenAI API
Write-Host "Running direct curl test for OpenAI API..." -ForegroundColor Cyan

# Get API key from user secrets
$apiProjectDir = Join-Path $PSScriptRoot "src\HomeDecorator.Api"
Push-Location $apiProjectDir

try {
    # Get the API key from user secrets
    $secrets = dotnet user-secrets list
    Write-Host "User secrets:" -ForegroundColor Yellow
    $secrets

    $apiKey = $null
    foreach ($secret in $secrets) {
        if ($secret -match "DallE:ApiKey = (.+)") {
            $apiKey = $matches[1]
            Write-Host "Found API key in user secrets" -ForegroundColor Green
            break
        }
    }
    
    if (-not $apiKey) {
        Write-Host "ERROR: DallE:ApiKey not found in user secrets" -ForegroundColor Red
        exit
    }
    
    # Set the API key as an environment variable for this session
    $env:OPENAI_API_KEY = $apiKey
    Write-Host "Set OPENAI_API_KEY environment variable" -ForegroundColor Green

} finally {
    Pop-Location
}

# Now run basic curl command to test
Write-Host "`nTesting OpenAI API connection with curl..." -ForegroundColor Cyan
$curlCommand = "curl https://api.openai.com/v1/models -H `"Authorization: Bearer $env:OPENAI_API_KEY`""
Write-Host "Running: $curlCommand" -ForegroundColor Yellow
Invoke-Expression $curlCommand

# Now let's test a specific API call
try {
    Write-Host "`nTesting image capability with a simple request..." -ForegroundColor Cyan
    
    # Download test image if not already downloaded
    $imagePath = "./test-image.png"
    if (-not (Test-Path $imagePath)) {
        $imageUrl = "https://images.pexels.com/photos/1571460/pexels-photo-1571460.jpeg"
        Write-Host "Downloading test image from $imageUrl..." -ForegroundColor Yellow
        Invoke-WebRequest -Uri $imageUrl -OutFile $imagePath
        Write-Host "Downloaded test image to $imagePath" -ForegroundColor Green
    }
    
    $imageSize = (Get-Item -Path $imagePath).Length
    Write-Host "Image file size: $imageSize bytes" -ForegroundColor Green
    
    # Create curl command for image edit
    $prompt = "Show me the exact same image but remove the people and change the furniture color to blue"
    $curl = @"
curl https://api.openai.com/v1/images/edits `
  -H "Authorization: Bearer $env:OPENAI_API_KEY" `
  -F model="gpt-image-1" `
  -F image="@$imagePath" `
  -F prompt="$prompt" `
  -F n=1 `
  -F size="1024x1024"
"@
    
    Write-Host "Command to run:" -ForegroundColor Yellow
    Write-Host $curl -ForegroundColor Gray
    
    Write-Host "`nExecuting API call..." -ForegroundColor Cyan
    Invoke-Expression $curl
} catch {
    Write-Host "Error executing curl command: $_" -ForegroundColor Red
}
