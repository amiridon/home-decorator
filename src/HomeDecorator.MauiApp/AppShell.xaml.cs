using HomeDecorator.MauiApp.Views;

namespace HomeDecorator.MauiApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register routes for navigation
		Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
		Routing.RegisterRoute(nameof(NewDesignPage), typeof(NewDesignPage));
	}
}
