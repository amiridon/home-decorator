using HomeDecorator.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HomeDecorator.MauiApp;

public partial class App : Application
{
	private readonly IFeatureFlagService _featureFlagService;
	private readonly IBillingService _billingService;

	public App(IFeatureFlagService featureFlagService, IBillingService billingService)
	{
		InitializeComponent();
		_featureFlagService = featureFlagService;
		_billingService = billingService;
		MainPage = new AppShell();
	}

	protected override Window CreateWindow(IActivationState activationState)
	{
		var window = base.CreateWindow(activationState);

		// Set window title
		window.Title = "Home Decorator";

		// For desktop platforms, set a reasonable default size
		window.Width = 1024;
		window.Height = 768;

		return window;
	}
}