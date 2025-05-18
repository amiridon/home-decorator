using System;
using HomeDecorator.Core.Services;

namespace HomeDecorator.MauiApp.Views;

public partial class SettingsPage : ContentPage
{
    private readonly IFeatureFlagService _featureFlagService;

    public SettingsPage(IFeatureFlagService featureFlagService)
    {
        InitializeComponent();
        _featureFlagService = featureFlagService;

        // Initialize the switch based on the current flag value
        FakeDataSwitch.IsToggled = _featureFlagService.IsFakeDataMode;
    }

    private async void OnFakeDataToggled(object sender, ToggledEventArgs e)
    {
        bool enabled = e.Value;

        // In a real implementation, we would persist this change
        // For now, just show a message indicating the change
        await DisplayAlert("Feature Flag Changed",
            $"Fake Data Mode is now {(enabled ? "enabled" : "disabled")}.\n\n" +
            "In a production app, this setting would be persisted.",
            "OK");
    }

    private async void OnPrivacyPolicyClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Privacy Policy",
            "This is a test harness application. No actual user data is collected.",
            "OK");
    }

    private async void OnTermsOfServiceClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Terms of Service",
            "This is a test harness application used for development purposes only.",
            "OK");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Confirm Logout",
            "Are you sure you want to log out?",
            "Yes", "No");

        if (confirm)
        {
            // In a real app, this would clear authentication state
            await DisplayAlert("Test Harness", "User logged out (simulated)", "OK");
        }
    }
}
