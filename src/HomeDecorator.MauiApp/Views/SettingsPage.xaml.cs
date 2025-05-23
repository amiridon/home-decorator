using System;
using System.Net.Http;
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

        try
        {
            // Call the API to update the feature flag
            var httpClient = new HttpClient();
            string baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                ? "https://10.0.2.2:5184" // Android emulator uses this IP for localhost
                : "https://localhost:5184";

            // We need to handle SSL certificate issues in development
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true // Accept all certificates
            };
            var client = new HttpClient(handler);

            var response = await client.PostAsync(
                $"{baseUrl}/api/feature-flags/update?flag=IsFakeDataMode&value={enabled}",
                null);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Feature Flag Changed",
                    $"Fake Data Mode is now {(enabled ? "enabled" : "disabled")}. " +
                    $"Please restart the application for changes to take effect.",
                    "OK");
            }
            else
            {
                // If the API call fails, revert the toggle
                FakeDataSwitch.IsToggled = !enabled;
                string errorMessage = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error",
                    $"Failed to update feature flag: {errorMessage}. " +
                    "Please ensure the API is running and try again.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            // If an exception occurs, revert the toggle
            FakeDataSwitch.IsToggled = !enabled;
            await DisplayAlert("Error",
                $"An error occurred: {ex.Message}. " +
                "Please ensure the API is running and try again.",
                "OK");
        }
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
