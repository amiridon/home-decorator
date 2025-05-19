using HomeDecorator.Api.Endpoints;
using HomeDecorator.Api.Services;
using HomeDecorator.Core.Services;
using Microsoft.AspNetCore.Http.Json;
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

// Add feature flags
var featureFlags = new Dictionary<string, bool>();
builder.Configuration.GetSection("FeatureFlags").Bind(featureFlags);
builder.Services.AddSingleton<IFeatureFlagService>(new FeatureFlagService(featureFlags));

// Register core services
bool isFakeDataMode = builder.Configuration.GetValue<bool>("FeatureFlags:IsFakeDataMode");

// Register credit ledger service
builder.Services.AddSingleton<ICreditLedgerService, SqliteCreditLedgerService>();

// Register billing service based on fake data mode
if (isFakeDataMode)
{
    Console.WriteLine("Using mock billing service (fake data mode)");
    builder.Services.AddSingleton<IBillingService, MockBillingService>();
}
else
{
    Console.WriteLine("Using Stripe billing service");
    builder.Services.AddSingleton<IBillingService, StripeService>();
}

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

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// API endpoints based on the specification in section 6
app.MapGet("/api/feature-flags", (IFeatureFlagService featureFlagService) =>
{
    return new { IsFakeDataMode = featureFlagService.IsFakeDataMode };
})
.WithName("GetFeatureFlags");

// Define the required API endpoints from section 6
app.MapPost("/api/auth/login", () =>
{
    return TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
})
.WithName("Login");

// Map billing endpoints from the BillingEndpoints class
app.MapBillingEndpoints();

app.MapPost("/api/image-request", () =>
{
    return TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
})
.RequireAuthorization()
.WithName("CreateImageRequest");

app.MapGet("/api/image-request/{id}", (string id) =>
{
    return TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
})
.RequireAuthorization()
.WithName("GetImageRequest");

app.MapGet("/api/history", () =>
{
    return TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
})
.RequireAuthorization()
.WithName("GetHistory");

app.MapGet("/api/products/{id}", (string id) =>
{
    return TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
})
.RequireAuthorization()
.WithName("GetProductDetails");

app.Run();