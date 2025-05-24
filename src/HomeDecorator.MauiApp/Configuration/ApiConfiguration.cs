using Microsoft.Extensions.Configuration;

namespace HomeDecorator.MauiApp.Configuration
{
    /// <summary>
    /// Configuration helper for API endpoints and other settings
    /// </summary>
    public static class ApiConfiguration
    {
        /// <summary>
        /// Gets the base API URL for the current environment
        /// </summary>
        public static string GetBaseApiUrl()
        {
#if DEBUG
            // For local development - use HTTP to match API configuration
            return DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:5002" // Android emulator uses this IP for localhost
                : "http://localhost:5002";
#else
            // Production URL would be read from configuration
            return "https://your-production-api-url.com";
#endif
        }

        /// <summary>
        /// Gets the timeout for HTTP requests
        /// </summary>
        public static TimeSpan GetHttpTimeout()
        {
            return TimeSpan.FromMinutes(2); // Generous timeout for DALL-E processing
        }

        /// <summary>
        /// Gets whether to ignore SSL certificate errors (development only)
        /// </summary>
        public static bool IgnoreSslErrors()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
}
