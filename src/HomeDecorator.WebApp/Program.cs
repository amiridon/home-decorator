using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using HomeDecorator.WebApp;
using HomeDecorator.Core.Services;
using System.Collections.Generic;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Initialize feature flags with defaults
var initialFlags = new Dictionary<string, bool>
{
    ["EnableStripeBilling"] = true // Enable billing features
};

// Register core services
builder.Services.AddSingleton<IFeatureFlagService>(new FeatureFlagService(initialFlags));

await builder.Build().RunAsync();
