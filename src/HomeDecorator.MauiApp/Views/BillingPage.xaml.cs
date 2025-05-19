using HomeDecorator.Core.Services;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace HomeDecorator.MauiApp.Views
{
    public partial class BillingPage : ContentPage
    {
        private readonly IBillingService _billingService;
        private readonly IFeatureFlagService _featureFlagService;
        private const string DefaultUserId = "test-user-id"; // In a real app, this would come from authentication

        public BillingPage(IBillingService billingService, IFeatureFlagService featureFlagService)
        {
            InitializeComponent();
            _billingService = billingService;
            _featureFlagService = featureFlagService;

            // Display fake data notice if in fake data mode
            if (_featureFlagService.IsFakeDataMode)
            {
                DisplayFakeDataNotice();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateCreditDisplay();
        }

        private void DisplayFakeDataNotice()
        {
            // Add a banner at the top to indicate we're in fake data mode
            var banner = new Frame
            {
                BackgroundColor = Colors.Orange,
                Padding = new Thickness(10),
                HorizontalOptions = LayoutOptions.Fill,
                Content = new Label
                {
                    Text = "FAKE DATA MODE - No actual charges will be made",
                    TextColor = Colors.White,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalOptions = LayoutOptions.Center
                }
            };

            // Insert at the beginning of the layout
            var grid = (Grid)Content;
            grid.RowDefinitions.Insert(0, new RowDefinition { Height = GridLength.Auto });

            // Push all other rows down
            for (int i = grid.Children.Count - 1; i >= 0; i--)
            {
                var element = grid.Children[i];
                var row = Grid.GetRow(element);
                Grid.SetRow(element, row + 1);
            }

            // Add the banner
            grid.Add(banner, 0, 0);
        }

        private async void UpdateCreditDisplay()
        {
            // In a real app, this would fetch the actual credit balance
            // For now, just display a placeholder
            CreditsLabel.Text = "150 credits";
        }

        private async void OnStandardPackClicked(object sender, EventArgs e)
        {
            await PurchaseCreditPack("starter-pack");
        }

        private async void OnPremiumPackClicked(object sender, EventArgs e)
        {
            await PurchaseCreditPack("premium-pack");
        }

        private async void OnProPackClicked(object sender, EventArgs e)
        {
            await PurchaseCreditPack("pro-pack");
        }

        private async Task PurchaseCreditPack(string packId)
        {
            try
            {
                // Get checkout URL from billing service
                string url = await _billingService.GetCheckoutUrlAsync(DefaultUserId, packId);

                // In a real app, we would open this URL in a WebView or browser
                await DisplayAlert("Checkout",
                    $"In a production app, you would be redirected to: {url}",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to start checkout process: {ex.Message}", "OK");
            }
        }

        private async void OnBillingPortalClicked(object sender, EventArgs e)
        {
            try
            {
                // Get billing portal URL from the service
                string url = await _billingService.GetBillingPortalUrlAsync(DefaultUserId);

                // In a real app, we would open this URL in a WebView or browser
                await DisplayAlert("Billing Portal",
                    $"In a production app, you would be redirected to: {url}",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to open billing portal: {ex.Message}", "OK");
            }
        }
    }
}
