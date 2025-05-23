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
        public readonly string _baseUrl; // Made public to access from other classes

        public ApiService(HttpMessageHandler? handler = null)
        {
            _httpClient = handler != null ? new HttpClient(handler) : new HttpClient();

            // Set longer timeout to allow for DALL-E processing
            _httpClient.Timeout = TimeSpan.FromMinutes(2);

            // In a real app, this would come from configuration
            _baseUrl = "https://your-api-url.com";

#if DEBUG
            // For local development - use HTTP to match API configuration (no HTTPS redirect in dev)
            _baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:5002" // Android emulator uses this IP for localhost
                : "http://localhost:5002";

            Console.WriteLine($"API base URL set to: {_baseUrl}");

            // Enable detailed logging for network requests in debug mode
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "HomeDecorator-MauiApp/1.0");
            _httpClient.DefaultRequestHeaders.Add("X-Debug", "true");
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
                attempts++; try
                {
                    // Validate and normalize the URL before sending
                    string normalizedUrl = originalImageUrl;

                    try
                    {
                        var uri = new Uri(originalImageUrl);
                        // Make sure we have http or https scheme
                        if (uri.Scheme != "http" && uri.Scheme != "https")
                        {
                            throw new InvalidOperationException($"Invalid URL scheme: {uri.Scheme}. URL must start with http:// or https://");
                        }
                        // Use the normalized form of the URI
                        normalizedUrl = uri.AbsoluteUri;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"URL validation failed: {ex.Message}");
                        throw new ArgumentException($"Invalid image URL: {ex.Message}", nameof(originalImageUrl), ex);
                    }

                    var request = new CreateImageRequestDto
                    {
                        OriginalImageUrl = normalizedUrl,
                        Prompt = prompt
                    };

                    // Enhanced logging for troubleshooting
                    Console.WriteLine($"Sending image request - attempt {attempts}");
                    Console.WriteLine($"Original image URL: {originalImageUrl}");
                    Console.WriteLine($"Normalized URL: {normalizedUrl}");
                    Console.WriteLine($"Prompt: {prompt}");
                    Console.WriteLine($"API endpoint: {_baseUrl}/api/image-request");

                    var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/image-request", request);

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Image request failed with status {response.StatusCode}: {errorContent}");
                        throw new HttpRequestException($"Image request failed with status {response.StatusCode}: {errorContent}");
                    }

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
        }        /// <summary>
                 /// Creates an image generation request
                 /// </summary>
        public async Task<ImageRequestResponseDto> CreateImageRequestAsync(string originalImageUrl, string decorStyle, string customPrompt)
        {
            try
            {
                var request = new CreateImageRequestDto
                {
                    OriginalImageUrl = originalImageUrl,
                    Prompt = decorStyle, // This will be the decor style key like "Modern"
                    CustomPrompt = customPrompt // This will be the detailed application-controlled prompt
                };

                Console.WriteLine($"Creating image request with URL: {originalImageUrl}");
                Console.WriteLine($"Decor style: {decorStyle}");
                Console.WriteLine($"API endpoint: {_baseUrl}/api/image-request");

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/image-request", request);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Image request failed with status {response.StatusCode}: {errorContent}");
                    throw new HttpRequestException($"Image request failed with status {response.StatusCode}: {errorContent}");
                }

                var result = await response.Content.ReadFromJsonAsync<ImageRequestResponseDto>();
                if (result == null)
                    throw new InvalidOperationException("No response from API");

                // Fix image URLs if they're relative
                NormalizeImageUrls(result);

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating image request: {ex.Message}");
                throw;
            }
        }

        /// <summary>        /// Gets an image generation request by ID
        /// </summary>
        public async Task<ImageRequestResponseDto> GetImageRequestAsync(string requestId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ImageRequestResponseDto>(
                    $"{_baseUrl}/api/image-request/{requestId}");

                if (response == null)
                    throw new InvalidOperationException("Request not found");

                // Fix image URLs if they're relative
                NormalizeImageUrls(response);

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting image request: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Ensures image URLs are absolute by prepending the API base URL if needed
        /// </summary>
        private void NormalizeImageUrls(ImageRequestResponseDto response)
        {
            // Handle original image URL
            if (!string.IsNullOrEmpty(response.OriginalImageUrl) && response.OriginalImageUrl.StartsWith("/"))
            {
                response.OriginalImageUrl = $"{_baseUrl}{response.OriginalImageUrl}";
                Console.WriteLine($"Normalized original image URL: {response.OriginalImageUrl}");
            }

            // Handle generated image URL
            if (!string.IsNullOrEmpty(response.GeneratedImageUrl) && response.GeneratedImageUrl.StartsWith("/"))
            {
                response.GeneratedImageUrl = $"{_baseUrl}{response.GeneratedImageUrl}";
                Console.WriteLine($"Normalized generated image URL: {response.GeneratedImageUrl}");
            }
        }

        /// <summary>        /// Gets user's image generation history
        /// </summary>
        public async Task<List<ImageRequestResponseDto>> GetImageHistoryAsync(int limit = 10)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<ImageRequestResponseDto>>(
                    $"{_baseUrl}/api/history?limit={limit}");

                if (response == null)
                    return new List<ImageRequestResponseDto>();

                // Normalize URLs for all items in the list
                foreach (var item in response)
                {
                    NormalizeImageUrls(item);
                }

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting image history: {ex.Message}");
                return new List<ImageRequestResponseDto>();
            }
        }/// <summary>
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
                content.Add(streamContent, "file", fileName); Console.WriteLine($"Uploading image: {fileName}, size: {memoryStream.Length} bytes, to URL: {_baseUrl}/api/upload-image");
                Console.WriteLine($"Content type: {contentType}");                // Ensure stream is readable and positioned at the beginning
                memoryStream.Position = 0;

                // Add a health check request first to test connectivity
                try
                {
                    Console.WriteLine("Checking API health before upload...");
                    var healthResponse = await _httpClient.GetAsync($"{_baseUrl}/api/ping-image-service");
                    if (healthResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine("API health check successful");
                    }
                    else
                    {
                        Console.WriteLine($"API health check failed with status {healthResponse.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"API health check failed: {ex.Message}");
                    // Continue anyway, as this is just diagnostic
                }

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/upload-image", content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Upload failed with status {response.StatusCode}: {errorContent}");
                    throw new HttpRequestException($"Upload failed with status {response.StatusCode}: {errorContent}");
                }
                var result = await response.Content.ReadFromJsonAsync<UploadResponse>();
                if (result == null || string.IsNullOrEmpty(result.ImageUrl))
                    throw new InvalidOperationException("No image URL returned");

                // Normalize the URL if it's a relative path
                string imageUrl = result.ImageUrl;
                if (imageUrl.StartsWith("/"))
                {
                    imageUrl = $"{_baseUrl}{imageUrl}";
                    Console.WriteLine($"Normalized uploaded image URL: {imageUrl}");
                }

                return imageUrl;
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