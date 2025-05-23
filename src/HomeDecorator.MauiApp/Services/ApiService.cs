using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HomeDecorator.Core.Models;
using HomeDecorator.MauiApp.Models;

namespace HomeDecorator.MauiApp.Services
{
    /// <summary>
    /// Service for communicating with the API
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl; public ApiService(HttpMessageHandler? handler = null)
        {
            _httpClient = handler != null ? new HttpClient(handler) : new HttpClient();

            // In a real app, this would come from configuration
            _baseUrl = "https://your-api-url.com";

#if DEBUG
            // For local development use localhost - using HTTP since API runs on HTTP in development
            _baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:5184" // Android emulator uses this IP for localhost
                : "http://localhost:5184";
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

                return response?.Url ?? throw new InvalidOperationException("No checkout URL returned");
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

                return response?.Url ?? throw new InvalidOperationException("No portal URL returned");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting portal URL: {ex.Message}");
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
                return 0; // Return 0 in case of error
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
        }        /// <summary>
                 /// Creates a new image generation request
                 /// </summary>
        public async Task<ImageRequestResponseDto> CreateImageRequestAsync(string originalImageUrl, string prompt)
        {
            const int maxRetries = 2;
            int attempts = 0;

            while (attempts < maxRetries)
            {
                attempts++;
                try
                {
                    var request = new CreateImageRequestDto
                    {
                        OriginalImageUrl = originalImageUrl,
                        Prompt = prompt
                    };

                    Console.WriteLine($"Sending image request - attempt {attempts}, URL: {originalImageUrl}, Prompt: {prompt}");
                    var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/image-request", request);
                    response.EnsureSuccessStatusCode();

                    var result = await response.Content.ReadFromJsonAsync<ImageRequestResponseDto>();
                    if (result == null)
                        throw new InvalidOperationException("Null response from API");

                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating image request (attempt {attempts}): {ex.Message}");

                    if (attempts >= maxRetries)
                        throw;

                    await Task.Delay(1000); // Wait 1 second before retry
                }
            }

            // This will never be reached, but compiler needs it
            throw new InvalidOperationException("Failed to create image request after multiple attempts");
        }

        /// <summary>
        /// Gets an image generation request by ID
        /// </summary>
        public async Task<ImageRequestResponseDto> GetImageRequestAsync(string requestId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ImageRequestResponseDto>(
                    $"{_baseUrl}/api/image-request/{requestId}");

                return response ?? throw new InvalidOperationException("Request not found");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting image request: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets user's image generation history
        /// </summary>
        public async Task<List<ImageRequestResponseDto>> GetImageHistoryAsync(int limit = 10)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<ImageRequestResponseDto>>(
                    $"{_baseUrl}/api/history?limit={limit}");

                return response ?? new List<ImageRequestResponseDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting image history: {ex.Message}");
                return new List<ImageRequestResponseDto>();
            }
        }        /// <summary>
                 /// Uploads an image and returns the URL
                 /// </summary>
        public async Task<string> UploadImageAsync(Stream imageStream, string fileName)
        {
            try
            {
                // First, verify if the stream can be read
                if (!imageStream.CanRead)
                {
                    throw new InvalidOperationException("Cannot read from the image stream");
                }

                // Copy stream to memory to avoid issues with stream being consumed
                var memoryStream = new MemoryStream();
                await imageStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                using var content = new MultipartFormDataContent();
                using var streamContent = new StreamContent(memoryStream);

                // Set appropriate content type based on file extension
                string contentType = "image/jpeg";
                if (fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    contentType = "image/png";
                else if (fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                    contentType = "image/webp";

                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                content.Add(streamContent, "file", fileName);

                Console.WriteLine($"Uploading image: {fileName}, size: {memoryStream.Length} bytes");
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/upload-image", content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Upload failed with status {response.StatusCode}: {errorContent}");
                    throw new HttpRequestException($"Upload failed with status {response.StatusCode}: {errorContent}");
                }

                var result = await response.Content.ReadFromJsonAsync<UploadResponse>();
                return result?.ImageUrl ?? throw new InvalidOperationException("No image URL returned");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading image: {ex.Message}");
                throw;
            }
        }

        // Response types
        private class CheckoutResponse
        {
            public string Url { get; set; } = string.Empty;
        }

        private class PortalResponse
        {
            public string Url { get; set; } = string.Empty;
        }

        private class CreditBalanceResponse
        {
            public int Credits { get; set; }
        }

        private class UploadResponse
        {
            public string ImageUrl { get; set; } = string.Empty;
        }
    }
}