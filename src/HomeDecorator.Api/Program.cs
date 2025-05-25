using HomeDecorator.Api.Endpoints;
using HomeDecorator.Api.Services;
using HomeDecorator.Core.Services;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// Add CORS policy for MAUI app
builder.Services.AddCors(options =>
{
    options.AddPolicy("MauiAppPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // In development, allow localhost and local IPs
            policy.WithOrigins(
                "http://localhost:*",
                "https://localhost:*",
                "http://10.0.2.2:*",
                "https://10.0.2.2:*")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .SetIsOriginAllowedToAllowWildcardSubdomains();
        }
        else
        {
            // In production, specify exact origins
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// Add feature flags
var featureFlags = new Dictionary<string, bool>();
builder.Configuration.GetSection("FeatureFlags").Bind(featureFlags);
builder.Services.AddSingleton<IFeatureFlagService>(new FeatureFlagService(featureFlags));

// Register core services
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddScoped<ICreditLedgerService, SqliteCreditLedgerService>();
builder.Services.AddScoped<IImageRequestRepository, SqliteImageRequestRepository>();
builder.Services.AddScoped<IGenerationService, DalleGenerationService>();
builder.Services.AddScoped<ImageProcessingServiceNew>();
builder.Services.AddScoped<IProductMatcherService, MockProductMatcherService>();
builder.Services.AddScoped<ImageGenerationOrchestrator>();

// Register billing service
Console.WriteLine("Using Stripe billing service");
builder.Services.AddScoped<IBillingService, StripeService>();  // Changed from singleton to scoped

// Register test service for DALL-E 2 variations
builder.Services.AddScoped<TestDalleVariationService>();  // Changed from singleton to scoped because it uses IGenerationService

// Register log service for request logs
builder.Services.AddScoped<ILogService, SqliteLogService>();  // Changed from singleton to scoped for consistency

// Register HttpClient for services that need it
builder.Services.AddHttpClient();

// Configure Azure Key Vault if enabled
if (builder.Configuration.GetValue<bool>("KeyVault:Enabled"))
{
    var keyVaultName = builder.Configuration.GetValue<string>("KeyVault:Name");
    if (!string.IsNullOrEmpty(keyVaultName))
    {
        // Note: In production, this would use DefaultAzureCredential
        // builder.Configuration.AddAzureKeyVault(new Uri($"https://{keyVaultName}.vault.azure.net/"), new DefaultAzureCredential());
        Console.WriteLine($"Azure Key Vault '{keyVaultName}' integration would be enabled in production");
    }
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS
app.UseCors("MauiAppPolicy");

// Ensure image directories exist
var imagesPath = Path.Combine(app.Environment.WebRootPath, "images");
var uploadedPath = Path.Combine(imagesPath, "uploaded");
var generatedPath = Path.Combine(imagesPath, "generated");

Directory.CreateDirectory(imagesPath);
Directory.CreateDirectory(uploadedPath);
Directory.CreateDirectory(generatedPath);

Console.WriteLine($"Ensured directories exist: {imagesPath}, {uploadedPath}, {generatedPath}");

// Serve static files (for locally stored images)
app.UseStaticFiles();

// Only use HTTPS redirection in production, not in development
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// API endpoints based on the specification in section 6
app.MapGet("/api/feature-flags", (IFeatureFlagService featureFlagService) =>
{
    return new { EnableStripeBilling = featureFlagService.GetFlag("EnableStripeBilling", true) };
})
.WithName("GetFeatureFlags");

app.MapPost("/api/feature-flags/update", async (IConfiguration configuration, HttpContext context) =>
{
    string? flagName = context.Request.Query["flag"].ToString();
    string? valueStr = context.Request.Query["value"].ToString();

    if (string.IsNullOrEmpty(flagName) || string.IsNullOrEmpty(valueStr))
    {
        return Results.BadRequest("Flag name and value are required");
    }

    if (!bool.TryParse(valueStr, out bool value))
    {
        return Results.BadRequest("Value must be a boolean (true/false)");
    }

    try
    {
        // Update the appsettings.json file
        string jsonPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        string json = await File.ReadAllTextAsync(jsonPath);

        // Create a simple way to update the value using string replacement
        string currentSetting = $"\"{flagName}\": {(!value).ToString().ToLowerInvariant()}";
        string newSetting = $"\"{flagName}\": {value.ToString().ToLowerInvariant()}";

        if (json.Contains(currentSetting))
        {
            json = json.Replace(currentSetting, newSetting);
            await File.WriteAllTextAsync(jsonPath, json);
            return Results.Ok(new { success = true, message = $"Feature flag {flagName} updated to {value}. Please restart the API for changes to take effect." });
        }
        else
        {
            return Results.BadRequest(new { success = false, message = $"Could not find feature flag {flagName} with value {!value}" });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.AllowAnonymous()
.WithName("UpdateFeatureFlag");

// Define the required API endpoints from section 6
app.MapPost("/api/auth/login", () =>
{
    return TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
})
.WithName("Login");

// Map billing endpoints from the BillingEndpoints class
app.MapBillingEndpoints();

// Image generation endpoints
app.MapImageGenerationEndpoints();
app.MapImageProcessingTestEndpoints(); // Add test endpoints

// Add diagnostic endpoint for checking API health
app.MapGet("/api/health", (HttpContext context) =>
{
    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return Results.Ok(new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow,
        clientIp = clientIp,
        environment = app.Environment.EnvironmentName,
        httpPort = 5002,
        httpsPort = 7072
    });
})
.WithName("ApiHealth");

// Add a diagnostic endpoint for checking file existence
app.MapGet("/api/check-file-exists", (HttpContext context, IWebHostEnvironment env) =>
{
    var filePath = context.Request.Query["path"].ToString();
    if (string.IsNullOrEmpty(filePath))
    {
        return Results.BadRequest("Path parameter is required");
    }

    // Remove leading slash for file path resolution
    if (filePath.StartsWith("/"))
    {
        filePath = filePath.Substring(1);
    }

    // Check if the file exists in wwwroot
    var fullPath = Path.Combine(env.WebRootPath, filePath);
    bool exists = File.Exists(fullPath);

    // List directory contents if file not found to help debugging
    var directoryContents = new List<string>();
    if (!exists)
    {
        var directory = Path.GetDirectoryName(fullPath);
        if (Directory.Exists(directory))
        {
            directoryContents = Directory.GetFiles(directory)
                .Select(f => Path.GetFileName(f) ?? string.Empty)
                .Where(f => !string.IsNullOrEmpty(f))
                .ToList();
        }
    }

    return Results.Ok(new
    {
        exists,
        requestedPath = filePath,
        fullPath,
        directoryExists = Directory.Exists(Path.GetDirectoryName(fullPath)),
        directoryContents
    });
})
.WithName("CheckFileExists");

// The file-exists endpoint is now handled by the FileSystemController

// Test DALL-E 2 image variations endpoint (for development only)
app.MapGet("/api/test-dalle-variations", async (
    TestDalleVariationService testService,
    [FromQuery] string imageUrl) =>
{
    try
    {
        // Run the test with the provided image URL
        await testService.TestVariationGenerationAsync(imageUrl);
        return Results.Ok(new { success = true, message = "Test completed successfully" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
})
.WithName("TestDalleVariations")
.WithOpenApi();

app.MapGet("/api/products/{id}", (string id) =>
{
    return TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
})
.RequireAuthorization()
.WithName("GetProductDetails");

app.Run();