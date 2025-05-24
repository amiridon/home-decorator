# Script to verify if user secrets are properly configured for the HomeDecorator API

Write-Host "HomeDecorator API - User Secrets Check" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Get the user secrets ID from the project file
$projectFile = ".\src\HomeDecorator.Api\HomeDecorator.Api.csproj"
$projectContent = Get-Content $projectFile -Raw
$userSecretsIdMatch = [regex]::Match($projectContent, '<UserSecretsId>(.*?)</UserSecretsId>')

if (!$userSecretsIdMatch.Success) {
    Write-Host "❌ No UserSecretsId found in the project file." -ForegroundColor Red
    exit 1
}

$userSecretsId = $userSecretsIdMatch.Groups[1].Value
Write-Host "✅ UserSecretsId found: $userSecretsId" -ForegroundColor Green

# Check if the user secrets file exists
$secretsPath = "$env:APPDATA\Microsoft\UserSecrets\$userSecretsId\secrets.json"

if (!(Test-Path $secretsPath)) {
    Write-Host "❌ User secrets file not found at: $secretsPath" -ForegroundColor Red
    
    Write-Host ""
    Write-Host "To create user secrets, run:" -ForegroundColor Yellow
    Write-Host "cd src\HomeDecorator.Api" -ForegroundColor Yellow
    Write-Host "dotnet user-secrets init" -ForegroundColor Yellow
    Write-Host "dotnet user-secrets set ""DallE:ApiKey"" ""your-openai-api-key-here""" -ForegroundColor Yellow
    
    exit 1
}

Write-Host "✅ User secrets file exists at: $secretsPath" -ForegroundColor Green

# Check if the secrets file contains the DallE:ApiKey
$secretsContent = Get-Content $secretsPath -Raw

if (!$secretsContent.Contains("DallE:ApiKey")) {
    Write-Host "❌ DallE:ApiKey not found in user secrets file" -ForegroundColor Red
    
    Write-Host ""
    Write-Host "To set the DALL-E API key, run:" -ForegroundColor Yellow
    Write-Host "cd src\HomeDecorator.Api" -ForegroundColor Yellow
    Write-Host "dotnet user-secrets set ""DallE:ApiKey"" ""your-openai-api-key-here""" -ForegroundColor Yellow
    
    exit 1
}

Write-Host "✅ DallE:ApiKey found in user secrets file" -ForegroundColor Green

Write-Host ""
Write-Host "User secrets appear to be properly configured." -ForegroundColor Green
Write-Host "You can now run the application to test DALL-E image generation."
Write-Host ""
Write-Host "To test directly, run:" -ForegroundColor Cyan
Write-Host "dotnet run --project src\HomeDecorator.Api\HomeDecorator.Api.csproj" -ForegroundColor Cyan
Write-Host "Then access: http://localhost:5002/api/test-dalle" -ForegroundColor Cyan
Write-Host ""
