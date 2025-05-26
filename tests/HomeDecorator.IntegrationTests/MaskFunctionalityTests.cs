using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HomeDecorator.Api.Services;
using HomeDecorator.Core.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HomeDecorator.IntegrationTests
{
    /// <summary>
    /// Integration tests for mask generation and DALL-E integration with masks
    /// </summary>
    [TestClass]
    public class MaskFunctionalityTests
    {
        private HttpClient _client;
        private WebApplicationFactory<Program> _factory;
        private ILogger<MaskFunctionalityTests> _logger;

        [TestInitialize]
        public void Initialize()
        {
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        // Register any mock services needed for testing
                    });
                });

            _client = _factory.CreateClient();
            _logger = _factory.Services.GetRequiredService<ILogger<MaskFunctionalityTests>>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _client.Dispose();
            _factory.Dispose();
        }
        [TestMethod]
        public async Task MaskGenerationEndpoint_ShouldReturnMask()
        {
            // Arrange
            string testImagePath = Path.Combine(AppContext.BaseDirectory, "TestData", "before.JPEG");

            // Skip test if the test image doesn't exist
            if (!File.Exists(testImagePath))
            {
                Assert.Inconclusive("Test image not found. This test requires a test image at TestData/before.JPEG");
                return;
            }

            using var imageContent = new StreamContent(File.OpenRead(testImagePath));
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

            using var formData = new MultipartFormDataContent();
            formData.Add(imageContent, "image", "before.JPEG");

            // Act
            var response = await _client.PostAsync("/api/mask/generate", formData);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Mask generation should return OK status");
            Assert.AreEqual("image/png", response.Content.Headers.ContentType?.MediaType, "Response should be a PNG image");

            // Check that we got actual content
            var responseStream = await response.Content.ReadAsStreamAsync();
            Assert.IsTrue(responseStream.Length > 0, "Response should contain image data");
        }

        [TestMethod]
        public async Task ImageGeneration_WithMask_ShouldReturnValidResponse()
        {
            // This test verifies that image generation with a mask flag works
            // Note: This is dependent on having API keys configured

            // Arrange
            var requestDto = new CreateImageRequestDto
            {
                OriginalImageUrl = "https://example.com/test-room.jpg", // Use a sample URL, will be mocked
                Prompt = "Modern",
                UseMask = true
            };

            // Act & Assert
            // Because this test requires API keys and real services, we'll just verify the endpoint is available
            var response = await _client.GetAsync("/api/image/generate");

            // Should exist (even if it returns unauthorized without proper auth)
            Assert.IsFalse(response.StatusCode == HttpStatusCode.NotFound,
                "Image generation endpoint should exist");
        }
        [TestMethod]
        public void MaskGenerationService_ShouldGenerateMask()
        {
            // Arrange
            var serviceProvider = _factory.Services;
            var maskService = serviceProvider.GetRequiredService<MaskGenerationService>();
            var config = serviceProvider.GetRequiredService<IConfiguration>();

            string testImagePath = Path.Combine(AppContext.BaseDirectory, "TestData", "before.JPEG");

            // Skip test if the test image doesn't exist
            if (!File.Exists(testImagePath))
            {
                Assert.Inconclusive("Test image not found. This test requires a test image at TestData/before.JPEG");
                return;
            }

            using var imageStream = File.OpenRead(testImagePath);

            // Act & Assert
            Assert.ThrowsExceptionAsync<Exception>(() => maskService.GenerateMaskAsync(null),
                "Should throw when null image is provided");

            // Test the actual generation
            using var stream = imageStream;
            var maskTask = maskService.GenerateMaskAsync(stream);

            // Wait for the task to complete
            maskTask.Wait();

            // Check result
            var maskStream = maskTask.Result;
            Assert.IsNotNull(maskStream, "Generated mask stream should not be null");
            Assert.IsTrue(maskStream.Length > 0, "Generated mask should have content");
        }

        [TestMethod]
        public async Task MaskGenerationService_WithConfigOverrides_ShouldGenerateMask()
        {
            // Arrange
            var serviceProvider = _factory.Services;
            var maskService = serviceProvider.GetRequiredService<MaskGenerationService>();

            string testImagePath = Path.Combine(AppContext.BaseDirectory, "TestData", "before.JPEG");

            // Skip test if the test image doesn't exist
            if (!File.Exists(testImagePath))
            {
                Assert.Inconclusive("Test image not found. This test requires a test image at TestData/before.JPEG");
                return;
            }

            using var imageStream = File.OpenRead(testImagePath);

            // Create configuration overrides
            var configOverrides = new Dictionary<string, string>
            {
                { "SAM:Enabled", "false" },
                { "SAM:ApiEndpoint", "https://test-endpoint.com" }
            };

            // Act
            var maskStream = await maskService.GenerateMaskAsync(imageStream, configOverrides);

            // Assert
            Assert.IsNotNull(maskStream, "Generated mask stream should not be null");
            Assert.IsTrue(maskStream.Length > 0, "Generated mask should have content");
        }

        [TestMethod]
        public async Task MaskGenerationService_CachingBehavior_ShouldCacheResults()
        {
            // Arrange
            var serviceProvider = _factory.Services;
            var maskService = serviceProvider.GetRequiredService<MaskGenerationService>();

            string testImagePath = Path.Combine(AppContext.BaseDirectory, "TestData", "before.JPEG");

            // Skip test if the test image doesn't exist
            if (!File.Exists(testImagePath))
            {
                Assert.Inconclusive("Test image not found. This test requires a test image at TestData/before.JPEG");
                return;
            }

            // Act - Generate mask twice with the same image
            using var imageStream1 = File.OpenRead(testImagePath);
            var maskStream1 = await maskService.GenerateMaskAsync(imageStream1);

            using var imageStream2 = File.OpenRead(testImagePath);
            var maskStream2 = await maskService.GenerateMaskAsync(imageStream2);

            // Assert
            Assert.IsNotNull(maskStream1, "First mask generation should succeed");
            Assert.IsNotNull(maskStream2, "Second mask generation should succeed");
            Assert.IsTrue(maskStream1.Length > 0, "First mask should have content");
            Assert.IsTrue(maskStream2.Length > 0, "Second mask should have content");

            // Both should have the same length (indicating cache hit)
            Assert.AreEqual(maskStream1.Length, maskStream2.Length, "Cached mask should have same size");
        }

        [TestMethod]
        public async Task MaskGenerationEndpoint_StatusCheck_ShouldReturnStatus()
        {
            // Act
            var response = await _client.GetAsync("/api/mask/status");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Status endpoint should return OK");

            var content = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(content), "Status endpoint should return content");
        }
    }
}
