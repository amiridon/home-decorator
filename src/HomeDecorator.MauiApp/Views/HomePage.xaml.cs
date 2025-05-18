using HomeDecorator.Core.Services;

namespace HomeDecorator.MauiApp.Views;

public partial class HomePage : ContentPage
{
    private readonly IFeatureFlagService _featureFlagService;

    public HomePage(IFeatureFlagService featureFlagService)
    {
        InitializeComponent();
        _featureFlagService = featureFlagService;

        // Initialize the switch based on the current flag value
        FakeDataSwitch.IsToggled = _featureFlagService.IsFakeDataMode;

        // Bind some simple properties
        BindingContext = new { HasNoDesigns = true };
    }
    private async void OnNewDesignClicked(object sender, EventArgs e)
    {
        // Navigate to the new design page
        await Shell.Current.GoToAsync(nameof(NewDesignPage));
    }

    private void OnBuyCreditsClicked(object sender, EventArgs e)
    {
        // TODO: Implement navigation to billing page
        DisplayAlert("Test Harness", $"Purchase credits! Fake Data Mode: {_featureFlagService.IsFakeDataMode}", "OK");
    }

    private void OnHistoryClicked(object sender, EventArgs e)
    {
        // TODO: Implement navigation to history page
        DisplayAlert("Test Harness", $"View history! Fake Data Mode: {_featureFlagService.IsFakeDataMode}", "OK");
    }

    private void OnSettingsClicked(object sender, EventArgs e)
    {
        // TODO: Implement navigation to settings page
        DisplayAlert("Test Harness", $"View settings! Fake Data Mode: {_featureFlagService.IsFakeDataMode}", "OK");
    }

    private void OnFakeDataToggled(object sender, ToggledEventArgs e)
    {
        // For now, just show a message - this would be wired up to the actual feature flag service
        DisplayAlert("Fake Data Mode", $"Fake Data Mode {(e.Value ? "enabled" : "disabled")}", "OK");

        // TODO: Update the feature flag service with this toggle
        // This would be persisted and wired to the actual service
    }
}
