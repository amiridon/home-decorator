using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HomeDecorator.MauiApp.Models;

namespace HomeDecorator.MauiApp.Services
{
    /// <summary>
    /// Service for communicating with the API
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ApiService()
        {
            _httpClient = new HttpClient();

            // In a real app, this would come from configuration
            _baseUrl = "https://your-api-url.com";

#if DEBUG
            // For local development use localhost
            _baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:5000" // Android emulator uses this IP for localhost
                : "http://localhost:5000";
#endif
        }

        /// <summary>
        /// Gets checkout URL for a credit pack
        /// </summary>
        public async Task<string> GetCheckoutUrlAsync(string userId, string packId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<CheckoutResponse>(
                    $"{_baseUrl}/api/billing/checkout/{packId}?userId={userId}");

                return response?.Url;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting checkout URL: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets billing portal URL
        /// </summary>
        public async Task<string> GetBillingPortalUrlAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<PortalResponse>(
                    $"{_baseUrl}/api/billing/portal?userId={userId}");

                return response?.Url;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting billing portal URL: {ex.Message}");
                throw;
            }
        }
        /// <summary>
        /// Gets credit balance
        /// </summary>
        public async Task<int> GetCreditBalanceAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<CreditBalanceResponse>(
                    $"{_baseUrl}/api/billing/credits?userId={userId}");

                return response?.Credits ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting credit balance: {ex.Message}");
                return 0; // Default to 0 in case of error
            }
        }

        /// <summary>
        /// Gets credit transaction history
        /// </summary>
        public async Task<List<CreditTransactionDto>> GetCreditTransactionHistoryAsync(string userId, int count = 10)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<CreditTransactionDto>>(
                    $"{_baseUrl}/api/billing/transactions?userId={userId}&count={count}");

                return response ?? new List<CreditTransactionDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting transaction history: {ex.Message}");
                return new List<CreditTransactionDto>(); // Return empty list in case of error
            }
        }

        // Response types
        private class CheckoutResponse
        {
            public string Url { get; set; }
        }

        private class PortalResponse
        {
            public string Url { get; set; }
        }

        private class CreditBalanceResponse
        {
            public int Credits { get; set; }
        }
    }
}
