using HomeDecorator.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using HomeDecorator.MauiApp.Views;
using HomeDecorator.MauiApp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using System.Collections.Generic;

namespace HomeDecorator.MauiApp;

public static class MauiProgram
{
	public static Microsoft.Maui.Hosting.MauiApp CreateMauiApp()
	{
		var builder = Microsoft.Maui.Hosting.MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});
		// Register services
		RegisterServices(builder.Services);

		// Configure HTTP client handler for development
		builder.Services.AddTransient<HttpMessageHandler>(_ =>
		{
#if DEBUG
			// In debug mode, allow self-signed certificates for local development
			return new HttpClientHandler
			{
				ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
			};
#else
			return new HttpClientHandler();
#endif
		});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
	private static void RegisterServices(IServiceCollection services)
	{       // Register pages
		services.AddSingleton<HomePage>();
		services.AddSingleton<SettingsPage>();
		services.AddSingleton<BillingPage>();
		services.AddSingleton<DesignHistoryPage>();
		services.AddTransient<NewDesignPage>();

		// Initialize feature flags with defaults
		var initialFlags = new Dictionary<string, bool>
		{
			["IsFakeDataMode"] = true, // Default to ON for development
			["EnableStripeBilling"] = true, // Enable billing features
			["EnableCreditLedger"] = true // Enable credit tracking
		};

		// Register core services
		services.AddSingleton<IFeatureFlagService>(new FeatureFlagService(initialFlags));
		// Register API service for connecting to backend
		services.AddSingleton<ApiService>(sp =>
		{
			var handler = sp.GetRequiredService<HttpMessageHandler>();
			return new ApiService(handler);
		});

		// Register credit ledger service
		services.AddSingleton<ICreditLedgerService, MockCreditLedgerService>();
		// Register enhanced mock services for development
		services.AddSingleton<IBillingService, EnhancedMockBillingService>();
		services.AddSingleton<IGenerationService, MockGenerationService>();
		services.AddSingleton<IProductMatcherService, MockProductMatcherService>();
		services.AddSingleton<IRecommendationService, MockRecommendationService>();
	}
}
