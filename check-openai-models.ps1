# Check OpenAI API models access
Write-Host "Checking OpenAI API models access..." -ForegroundColor Cyan

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
    exit
}

# Set up request
$uri = "https://api.openai.com/v1/models"
$headers = @{
    "Authorization" = "Bearer $apiKey"
}

try {
    Write-Host "Fetching available models..." -ForegroundColor Cyan
    $response = Invoke-RestMethod -Uri $uri -Method Get -Headers $headers

    # Extract model IDs and filter for relevant ones
    $modelIds = $response.data | ForEach-Object { $_.id }
    
    Write-Host "All available models:" -ForegroundColor Green
    $modelIds | ForEach-Object { Write-Host "  - $_" }
    
    # Check for specific models
    Write-Host "`nChecking for specific models:" -ForegroundColor Cyan
    
    $models = @(
        "gpt-image-1",
        "dall-e-2",
        "dall-e-3"
    )
    
    foreach ($model in $models) {
        if ($modelIds -contains $model) {
            Write-Host "✅ $model is AVAILABLE" -ForegroundColor Green
        } else {
            Write-Host "❌ $model is NOT AVAILABLE" -ForegroundColor Red
        }
    }
}
catch {
    Write-Host "Error checking models:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}
