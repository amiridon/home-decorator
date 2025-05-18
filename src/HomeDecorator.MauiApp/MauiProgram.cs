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

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static void RegisterServices(IServiceCollection services)
	{       // Register pages
		services.AddSingleton<HomePage>();
		services.AddSingleton<SettingsPage>();
		services.AddTransient<NewDesignPage>();

		// Initialize feature flags with defaults
		var initialFlags = new Dictionary<string, bool>
		{
			["IsFakeDataMode"] = true // Default to ON for development
		};

		// Register core services
		services.AddSingleton<IFeatureFlagService>(new FeatureFlagService(initialFlags));

		// Register mock services for development
		services.AddSingleton<IBillingService, MockBillingService>();
		services.AddSingleton<IGenerationService, MockGenerationService>();
		services.AddSingleton<IProductMatcherService, MockProductMatcherService>();
		services.AddSingleton<IRecommendationService, MockRecommendationService>();
	}
}
