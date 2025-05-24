using HomeDecorator.Core.Services;
using HomeDecorator.MauiApp.Services;

namespace HomeDecorator.MauiApp.Views;

public partial class NewDesignPage : ContentPage
{
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IGenerationService _generationService;
    private readonly ApiService _apiService;
    private FileResult? _selectedPhoto;
    private readonly Dictionary<string, string> _decorStyles;

    public NewDesignPage(IFeatureFlagService featureFlagService, IGenerationService generationService, ApiService apiService)
    {
        InitializeComponent();
        _featureFlagService = featureFlagService;
        _generationService = generationService;
        _apiService = apiService;

        _decorStyles = new Dictionary<string, string>
        {
            { "Modern", "A detailed prompt for a modern look focusing on clean lines, neutral colors, and minimalist furniture." },
            { "Contemporary", "A detailed prompt for a contemporary look featuring current trends, fluid shapes, and a mix of textures." },
            { "Minimalist", "A detailed prompt for a minimalist look emphasizing simplicity, functionality, and a monochromatic color palette." },
            { "Industrial", "A detailed prompt for an industrial look showcasing raw materials like brick and metal, open spaces, and vintage-inspired furniture." },
            { "Bohemian", "A detailed prompt for a bohemian look characterized by a relaxed atmosphere, vibrant colors, and eclectic patterns." },
            { "Farmhouse", "A detailed prompt for a farmhouse look that is cozy and rustic, with natural wood, comfortable furniture, and vintage accents." }
        };

        foreach (var style in _decorStyles.Keys)
        {
            StylePicker.Items.Add(style);
        }
        // Set a default selection if desired
        if (StylePicker.Items.Count > 0)
        {
            StylePicker.SelectedIndex = 0;
        }
    }
    private async void OnTakePhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.CapturePhotoAsync();
            if (photo != null)
            {
                _selectedPhoto = photo;
                var stream = await photo.OpenReadAsync();
                SelectedImage.Source = ImageSource.FromStream(() => stream);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to take photo: {ex.Message}", "OK");
        }
    }
    private async void OnChoosePhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.PickPhotoAsync();
            if (photo != null)
            {
                _selectedPhoto = photo;
                var stream = await photo.OpenReadAsync();
                SelectedImage.Source = ImageSource.FromStream(() => stream);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to pick photo: {ex.Message}", "OK");
        }
    }
    private async void OnGenerateDesignClicked(object sender, EventArgs e)
    {
        if (StylePicker.SelectedItem == null)
        {
            await DisplayAlert("Missing Information", "Please select a design style.", "OK");
            return;
        }

        if (_selectedPhoto == null)
        {
            await DisplayAlert("Missing Information", "Please select or take a photo first.", "OK");
            return;
        }

        IsBusy = true;
        LoadingIndicator.IsVisible = true;
        GeneratedImage.IsVisible = false;

        try
        {
            // Upload the selected image first
            string imageUrl;
            try
            {
                using (var stream = await _selectedPhoto.OpenReadAsync())
                {
                    Console.WriteLine($"Uploading image: {_selectedPhoto.FileName}, size: {stream.Length} bytes");
                    imageUrl = await _apiService.UploadImageAsync(stream, _selectedPhoto.FileName);
                    Console.WriteLine($"Successfully uploaded image, received URL: {imageUrl}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Image upload failed: {ex.Message}");
                throw new Exception($"Failed to upload image: {ex.Message}", ex);
            }            // Get the selected style and its corresponding detailed prompt
            var selectedStyleKey = StylePicker.SelectedItem?.ToString() ?? "Modern";
            if (!_decorStyles.ContainsKey(selectedStyleKey))
            {
                selectedStyleKey = _decorStyles.Keys.FirstOrDefault() ?? "Modern";
                Console.WriteLine($"Selected style not found in dictionary, using default: {selectedStyleKey}");
            }
            var detailedPromptForStyle = _decorStyles[selectedStyleKey];

            // Create the image generation request
            // The 'selectedStyleKey' is sent as the 'decorStyle' (which was previously the 'prompt' field in CreateImageRequestDto)
            // The 'detailedPromptForStyle' is sent as the 'customPrompt' (the new field in CreateImageRequestDto for the actual generation instructions)
            var response = await _apiService.CreateImageRequestAsync(imageUrl, selectedStyleKey, detailedPromptForStyle);

            // Show initial response
            await DisplayAlert("Generation Started",
                $"Your design generation has been started!\n\nRequest ID: {response.Id}\n\nStatus: {response.Status}",
                "OK");

            // Poll for completion
            var completedRequest = await PollForCompletion(response.Id);

            if (completedRequest != null && completedRequest.Status == "Completed")
            {                // Display the generated image
                if (!string.IsNullOrEmpty(completedRequest.GeneratedImageUrl))
                {
                    try
                    {
                        // Properly create an ImageSource from URL
                        string fullUrl = completedRequest.GeneratedImageUrl;

                        // If the URL is relative, prepend the base API URL
                        if (completedRequest.GeneratedImageUrl.StartsWith("/"))
                        {
                            fullUrl = $"{_apiService._baseUrl}{completedRequest.GeneratedImageUrl}";
                            Console.WriteLine($"Using full URL: {fullUrl}");
                        }

                        // Log detailed URL information for debugging
                        Console.WriteLine($"Original URL: {completedRequest.GeneratedImageUrl}");
                        Console.WriteLine($"Full URL being used: {fullUrl}");

                        // Verify URL is valid
                        var uri = new Uri(fullUrl);
                        Console.WriteLine($"URI scheme: {uri.Scheme}, host: {uri.Host}, path: {uri.AbsolutePath}");

                        // Check if the URL is for a local file
                        if (uri.IsFile)
                        {
                            Console.WriteLine($"This is a file URI - check if file exists: {System.IO.File.Exists(uri.LocalPath)}");
                        }
                        // Try to ping the URL to see if it's accessible
                        try
                        {
                            using var httpClient = new HttpClient();
                            var pingResponse = httpClient.GetAsync(fullUrl).Result;
                            Console.WriteLine($"HTTP response from image URL: {pingResponse.StatusCode}");
                        }
                        catch (Exception pingEx)
                        {
                            Console.WriteLine($"Failed to ping image URL: {pingEx.Message}");
                            // Don't throw here, just log the error
                        }

                        // Update UI on main thread and use try-catch for robust error handling
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            try
                            {
                                GeneratedImage.Source = ImageSource.FromUri(new Uri(fullUrl));
                                GeneratedImage.IsVisible = true;
                                Console.WriteLine($"Successfully set image source to URL: {fullUrl}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"UI thread error setting image: {ex.Message}");

                                // Show error to the user
                                DisplayAlert("Image Display Error",
                                    $"There was a problem displaying the image.\n\nURL: {fullUrl}\n\nError: {ex.Message}",
                                    "OK");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error setting image source: {ex.Message}");
                        // Continue execution to show the completed message even if image loading fails
                    }
                }
                else
                {
                    Console.WriteLine("Generated image URL is null or empty!");
                }

                await DisplayAlert("Design Completed",
                    "Your design is ready! Check out the generated image above.",
                    "OK");
            }
            else
            {
                // Show error with more context
                if (completedRequest?.Status == "Failed")
                {
                    await DisplayAlert("Generation Failed",
                        $"Sorry, we couldn't generate your design.\n\nError: {completedRequest.ErrorMessage ?? "Unknown error"}",
                        "OK");
                }
                else
                {
                    await DisplayAlert("Generation Status",
                        "The design is taking longer than expected. You can check the history page later to see if it completed.",
                        "OK");
                }
            }
        }
        catch (Exception ex)
        {
            // Provide more detailed error information
            string errorDetails = ex.Message;

            // Provide specific error messages based on the exception type or message contents
            if (ex.Message.Contains("copying content to stream"))
            {
                errorDetails = "Error storing the generated image. This might be due to network issues or permission problems. Please try again with a different image.";
            }
            else if (ex.Message.Contains("400 Bad Request"))
            {
                if (ex.Message.Contains("File size too large"))
                {
                    errorDetails = "The image file is too large. Please choose a smaller image (less than 10MB).";
                }
                else if (ex.Message.Contains("Invalid file type"))
                {
                    errorDetails = "The image format is not supported. Please use JPEG, PNG, or WebP images.";
                }
                else
                {
                    errorDetails = "The server rejected the request. Please check your image and prompt.";
                }
            }

            Console.WriteLine($"Error generating design: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }

            await DisplayAlert("Error", $"Failed to generate design: {errorDetails}", "OK");
            System.Diagnostics.Debug.WriteLine($"Detailed error: {ex}");
        }
        finally
        {
            IsBusy = false;
            LoadingIndicator.IsVisible = false;
        }
    }
    private async Task<Core.Models.ImageRequestResponseDto?> PollForCompletion(string requestId, int maxAttempts = 30)
    {
        // Show loading indicator during the polling process
        LoadingIndicator.IsVisible = true;

        // Display polling indicator to user
        await DisplayAlert("Processing", "Your design is being generated. This may take up to a minute.", "OK");

        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                var request = await _apiService.GetImageRequestAsync(requestId);
                System.Diagnostics.Debug.WriteLine($"Poll {i + 1}: Status={request.Status}, Error={request.ErrorMessage}");

                if (request.Status == "Completed" || request.Status == "Failed")
                {
                    return request;
                }

                // Wait 2 seconds before next poll
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                // If polling fails, log the error but continue polling
                System.Diagnostics.Debug.WriteLine($"Polling failed: {ex.Message}");

                // Only return null after a few failed attempts
                if (i > 3)
                {
                    return null;
                }

                await Task.Delay(3000); // Wait longer after an error
            }
        }

        // If we've reached max attempts, return a custom response
        return new Core.Models.ImageRequestResponseDto
        {
            Status = "Failed",
            ErrorMessage = "Timed out waiting for design generation to complete. Please check the history page later."
        };
    }
}