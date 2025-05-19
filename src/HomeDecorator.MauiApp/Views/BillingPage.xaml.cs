using HomeDecorator.Core.Services;
using HomeDecorator.MauiApp.Models;
using HomeDecorator.MauiApp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace HomeDecorator.MauiApp.Views
{
    public partial class BillingPage : ContentPage
    {
        private readonly IBillingService _billingService;
        private readonly IFeatureFlagService _featureFlagService;
        private readonly ApiService _apiService;
        private const string DefaultUserId = "test-user-id"; // In a real app, this would come from authentication

        public BillingPage(IBillingService billingService, IFeatureFlagService featureFlagService, ApiService apiService)
        {
            InitializeComponent();
            _billingService = billingService;
            _featureFlagService = featureFlagService;
            _apiService = apiService;

            // Display fake data notice if in fake data mode
            if (_featureFlagService.IsFakeDataMode)
            {
                DisplayFakeDataNotice();
            }
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            UpdateCreditDisplay();

            // Load transaction history if not in fake data mode
            if (!_featureFlagService.IsFakeDataMode)
            {
                await LoadTransactionHistoryAsync();
            }
        }
        private void DisplayFakeDataNotice()
        {
            // Create a simple alert to notify the user we're in fake data mode
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Demo Mode",
                    "You are in FAKE DATA MODE. No actual charges will be made.",
                    "OK");
            });

            // Add a visual indicator at the top of the credits display
            var label = new Label
            {
                Text = "DEMO MODE",
                TextColor = Colors.White,
                BackgroundColor = Colors.Orange,
                HorizontalTextAlignment = TextAlignment.Center,
                Padding = new Thickness(5),
                FontAttributes = FontAttributes.Bold,
                FontSize = 12
            };

            // Find the credits section and add the notice
            var creditsLabel = this.FindByName<Label>("CreditsLabel");
            if (creditsLabel != null && creditsLabel.Parent is VerticalStackLayout stack)
            {
                stack.Insert(0, label);
            }
        }
        private async void UpdateCreditDisplay()
        {
            try
            {
                // Show loading state
                CreditsLabel.Text = "Loading...";

                // Get actual credit balance from API
                int credits = await _apiService.GetCreditBalanceAsync(DefaultUserId);

                // Update the UI
                CreditsLabel.Text = $"{credits} credits";

                // Update the last updated timestamp
                var lastUpdated = DateTime.Now;
                var lastUpdatedLabel = this.FindByName<Label>("LastUpdatedLabel");
                if (lastUpdatedLabel != null)
                {
                    lastUpdatedLabel.Text = $"Last updated: {lastUpdated:MMM d, yyyy HH:mm}";
                }
            }
            catch (Exception ex)
            {
                // In case of error, show a placeholder
                CreditsLabel.Text = "-- credits";
                await DisplayAlert("Error", $"Failed to fetch credit balance: {ex.Message}", "OK");
            }
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
                if (_featureFlagService.IsFakeDataMode)
                {
                    // In fake mode, use the mock service
                    string url = await _billingService.GetCheckoutUrlAsync(DefaultUserId, packId);

                    // Show a demo alert
                    await DisplayAlert("Demo Mode",
                        $"In production, you would be redirected to Stripe: {url}",
                        "OK");

                    // Refresh the credit display to show the mock purchase
                    UpdateCreditDisplay();
                }
                else
                {
                    // In production mode, use the real API
                    string url = await _apiService.GetCheckoutUrlAsync(DefaultUserId, packId);

                    // Open the URL in the browser if it's valid
                    if (!string.IsNullOrEmpty(url))
                    {
                        // Start the refresh timer to periodically check for balance updates
                        // after the user completes the checkout process
                        StartRefreshTimer();

                        await Browser.OpenAsync(url, BrowserLaunchMode.SystemPreferred);

                        // Show a message to the user
                        await DisplayAlert("Purchase Started",
                            "Complete the purchase in the browser. Your credits will be updated automatically once payment is processed.",
                            "OK");
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to get checkout URL from server", "OK");
                    }
                }
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
                if (_featureFlagService.IsFakeDataMode)
                {
                    // In fake mode, use the mock service
                    string url = await _billingService.GetBillingPortalUrlAsync(DefaultUserId);

                    // Show a demo alert
                    await DisplayAlert("Demo Mode",
                        $"In production, you would be redirected to Stripe portal: {url}",
                        "OK");
                }
                else
                {                    // In production mode, use the real API
                    string url = await _apiService.GetBillingPortalUrlAsync(DefaultUserId);

                    // Open the URL in the browser if it's valid
                    if (!string.IsNullOrEmpty(url))
                    {
                        await Browser.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to get billing portal URL from server", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to open billing portal: {ex.Message}", "OK");
            }
        }
        private async Task LoadTransactionHistoryAsync()
        {
            try
            {
                // Retrieve transaction history
                var transactions = await _apiService.GetCreditTransactionHistoryAsync(DefaultUserId, 5);

                // We'll add a transaction history panel in the XAML
                var transactionsList = this.FindByName<StackLayout>("TransactionsStack");
                if (transactionsList == null)
                {
                    return;
                }

                // Clear existing items
                transactionsList.Clear();

                // Add transaction items
                foreach (var transaction in transactions)
                {
                    // Create a simplified stack for the transaction details
                    var stack = new VerticalStackLayout
                    {
                        Padding = new Thickness(10),
                        Margin = new Thickness(0, 0, 0, 8)
                    };

                    // Transaction type and amount in one row
                    var headerGrid = new Grid
                    {
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Auto }
                        }
                    };

                    var typeLabel = new Label
                    {
                        Text = transaction.Type,
                        FontAttributes = FontAttributes.Bold
                    };
                    headerGrid.Add(typeLabel, 0, 0);

                    var amountLabel = new Label
                    {
                        Text = (transaction.Amount > 0 ? "+" : "") + transaction.Amount + " credits",
                        FontAttributes = FontAttributes.Bold,
                        TextColor = transaction.Amount > 0 ? Colors.Green : Colors.Red,
                        HorizontalOptions = LayoutOptions.End
                    };
                    headerGrid.Add(amountLabel, 1, 0);

                    // Description
                    var description = new Label
                    {
                        Text = transaction.Description,
                        FontSize = 12
                    };

                    // Date
                    var date = new Label
                    {
                        Text = transaction.Timestamp.ToLocalTime().ToString("MMM d, yyyy HH:mm"),
                        FontSize = 10,
                        TextColor = Colors.Gray
                    };

                    // Add all components to the stack
                    stack.Add(headerGrid);
                    stack.Add(description);
                    stack.Add(date);

                    // Add the stack to the transactions list
                    transactionsList.Add(stack);

                    // Add a separator
                    if (transactions.IndexOf(transaction) < transactions.Count - 1)
                    {
                        transactionsList.Add(new BoxView
                        {
                            Color = Colors.LightGray,
                            HeightRequest = 1,
                            HorizontalOptions = LayoutOptions.Fill,
                            Margin = new Thickness(0, 5, 0, 5)
                        });
                    }
                }

                if (transactions.Count == 0)
                {
                    // Show a message if no transactions found
                    transactionsList.Add(new Label
                    {
                        Text = "No transactions found.",
                        HorizontalOptions = LayoutOptions.Center,
                        Margin = new Thickness(0, 20, 0, 0),
                        TextColor = Colors.Gray
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load transaction history: {ex.Message}", "OK");
            }
        }
        // Timer for refreshing credit balance after checkout
        private System.Timers.Timer? _refreshTimer;
        private void StartRefreshTimer()
        {
            // Create timer that checks for balance updates
            _refreshTimer = new System.Timers.Timer(5000); // Check every 5 seconds
            _refreshTimer.Elapsed += async (s, e) =>
            {
                // Update on the UI thread
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await UpdateCreditsFromApi();
                });
            };
            _refreshTimer.AutoReset = true;
            _refreshTimer.Start();
        }

        private void StopRefreshTimer()
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            _refreshTimer = null;
        }

        private async Task UpdateCreditsFromApi()
        {
            try
            {
                int credits = await _apiService.GetCreditBalanceAsync(DefaultUserId);
                CreditsLabel.Text = $"{credits} credits";

                // Update timestamp
                var lastUpdated = DateTime.Now;
                var lastUpdatedLabel = this.FindByName<Label>("LastUpdatedLabel");
                if (lastUpdatedLabel != null)
                {
                    lastUpdatedLabel.Text = $"Last updated: {lastUpdated:MMM d, yyyy HH:mm}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update credits: {ex.Message}");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Stop any refresh timers to avoid memory leaks
            StopRefreshTimer();
        }
    }
}
