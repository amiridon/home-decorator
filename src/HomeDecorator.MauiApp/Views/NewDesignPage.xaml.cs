using HomeDecorator.Core.Services;
using HomeDecorator.MauiApp.Services;

namespace HomeDecorator.MauiApp.Views;

public partial class NewDesignPage : ContentPage
{
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IGenerationService _generationService;
    private readonly ApiService _apiService;
    private List<FileResult> _selectedPhotos = new List<FileResult>();

    public NewDesignPage(IFeatureFlagService featureFlagService, IGenerationService generationService, ApiService apiService)
    {
        InitializeComponent();
        _featureFlagService = featureFlagService;
        _generationService = generationService;
        _apiService = apiService;

        var availableStyles = PromptGenerationService.GetAvailableStyles();
        foreach (var style in availableStyles)
        {
            StylePicker.Items.Add(style);
        }
        // Set a default selection if desired
        if (StylePicker.Items.Count > 0)
        {
            StylePicker.SelectedIndex = 0;
        }

        var roomTypes = new List<string> { "Living Room", "Bedroom", "Bathroom", "Kitchen", "Dining Room", "Office", "Basement", "Backyard", "Garage", "Foyer" };
        foreach (var roomType in roomTypes)
        {
            RoomTypePicker.Items.Add(roomType);
        }
        if (RoomTypePicker.Items.Count > 0)
        {
            RoomTypePicker.SelectedIndex = 0;
        }
    }
    private async void OnTakePhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.CapturePhotoAsync();
            if (photo != null)
            {
                _selectedPhotos.Add(photo);
                await UpdateImageDisplay();
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
            // Use FilePicker for multiple photo selection
            var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.iOS, new[] { "public.image" } },
                { DevicePlatform.Android, new[] { "image/*" } },
                { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp" } },
                { DevicePlatform.MacCatalyst, new[] { "public.image" } }
            });

            var options = new PickOptions
            {
                PickerTitle = "Select Photos",
                FileTypes = fileTypes
            };

            var photos = await FilePicker.PickMultipleAsync(options);
            if (photos != null && photos.Any())
            {
                // Clear previous selections and add new ones
                _selectedPhotos.Clear();
                _selectedPhotos.AddRange(photos);
                await UpdateImageDisplay();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to pick photos: {ex.Message}", "OK");
        }
    }
    private async Task UpdateImageDisplay()
    {
        if (_selectedPhotos.Any())
        {
            // Display the first selected photo
            var firstPhoto = _selectedPhotos.First();
            var stream = await firstPhoto.OpenReadAsync();
            SelectedImage.Source = ImageSource.FromStream(() => stream);

            // Update the count label
            PhotoCountLabel.Text = _selectedPhotos.Count == 1
                ? $"1 photo selected: {firstPhoto.FileName}"
                : $"{_selectedPhotos.Count} photos selected (showing: {firstPhoto.FileName})";
            PhotoCountLabel.TextColor = Colors.Green;

            // Show clear button
            ClearButton.IsVisible = true;
        }
        else
        {
            PhotoCountLabel.Text = "No photos selected";
            PhotoCountLabel.TextColor = Colors.Gray;

            // Hide clear button and reset image
            ClearButton.IsVisible = false;
            SelectedImage.Source = "dotnet_bot.png";
        }
    }
    private async void OnGenerateDesignClicked(object sender, EventArgs e)
    {
        if (StylePicker.SelectedItem == null)
        {
            await DisplayAlert("Missing Information", "Please select a design style.", "OK");
            return;
        }
        if (!_selectedPhotos.Any())
        {
            await DisplayAlert("Missing Information", "Please select or take a photo first.", "OK");
            return;
        }

        if (RoomTypePicker.SelectedItem == null)
        {
            await DisplayAlert("Missing Information", "Please select a room type.", "OK");
            return;
        }

        IsBusy = true;
        LoadingIndicator.IsVisible = true;
        GeneratedImage.IsVisible = false;

        try
        {            // Upload the selected image first (use the first selected photo)
            var selectedPhoto = _selectedPhotos.First();
            string imageUrl;
            try
            {
                using (var stream = await selectedPhoto.OpenReadAsync())
                {
                    Console.WriteLine($"Uploading image: {selectedPhoto.FileName}, size: {stream.Length} bytes");
                    imageUrl = await _apiService.UploadImageAsync(stream, selectedPhoto.FileName);
                    Console.WriteLine($"Successfully uploaded image, received URL: {imageUrl}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Image upload failed: {ex.Message}");
                throw new Exception($"Failed to upload image: {ex.Message}", ex);
            }            // Get the selected style and its corresponding detailed prompt
            var selectedStyleKey = StylePicker.SelectedItem?.ToString() ?? "Modern";
            var selectedRoomType = RoomTypePicker.SelectedItem?.ToString() ?? "Living Room";
            var detailedPromptForStyle = PromptGenerationService.GetRandomPrompt(selectedStyleKey, selectedRoomType);

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

    private async void OnClearSelectionClicked(object sender, EventArgs e)
    {
        _selectedPhotos.Clear();
        await UpdateImageDisplay();
    }
}