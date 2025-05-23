# PowerShell script to set the OpenAI API key as an environment variable
# Pass your API key as a parameter when running this script

param (
    [Parameter(Mandatory=$true)]
    [string]$ApiKey
)

# Set for the current process
$env:OPENAI_API_KEY = $ApiKey
Write-Host "OPENAI_API_KEY environment variable set for the current process."
Write-Host "Key starts with: $($ApiKey.Substring(0, 5))..."

# Build and run the API with the environment variable set
dotnet run --project .\src\HomeDecorator.Api\HomeDecorator.Api.csproj
