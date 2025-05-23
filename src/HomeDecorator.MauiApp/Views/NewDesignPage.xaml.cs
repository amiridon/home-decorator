using HomeDecorator.Core.Services;
using HomeDecorator.MauiApp.Services;

namespace HomeDecorator.MauiApp.Views;

public partial class NewDesignPage : ContentPage
{
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IGenerationService _generationService;
    private readonly ApiService _apiService;
    private FileResult? _selectedPhoto;

    public NewDesignPage(IFeatureFlagService featureFlagService, IGenerationService generationService, ApiService apiService)
    {
        InitializeComponent();
        _featureFlagService = featureFlagService;
        _generationService = generationService;
        _apiService = apiService;
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
        if (string.IsNullOrWhiteSpace(PromptEditor.Text))
        {
            await DisplayAlert("Missing Information", "Please enter a design prompt.", "OK");
            return;
        }

        if (_selectedPhoto == null)
        {
            await DisplayAlert("Missing Information", "Please select or take a photo first.", "OK");
            return;
        }

        IsBusy = true;

        try
        {
            // Upload the selected image first
            string imageUrl;
            using (var stream = await _selectedPhoto.OpenReadAsync())
            {
                imageUrl = await _apiService.UploadImageAsync(stream, _selectedPhoto.FileName);
            }

            // Create the image generation request
            var response = await _apiService.CreateImageRequestAsync(imageUrl, PromptEditor.Text);

            // Show initial response
            await DisplayAlert("Generation Started",
                $"Your design generation has been started!\n\nRequest ID: {response.Id}\n\nStatus: {response.Status}",
                "OK");

            // Poll for completion
            var completedRequest = await PollForCompletion(response.Id);

            if (completedRequest != null && completedRequest.Status == "Completed")
            {
                // Display the generated image
                if (!string.IsNullOrEmpty(completedRequest.GeneratedImageUrl))
                {
                    GeneratedImage.Source = completedRequest.GeneratedImageUrl;
                    GeneratedImage.IsVisible = true;
                }

                await DisplayAlert("Design Completed",
                    "Your design is ready! Check out the generated image above.",
                    "OK");
            }
            else
            {
                await DisplayAlert("Generation Status",
                    completedRequest?.ErrorMessage ?? "Generation may still be in progress or failed.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to generate design: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<Core.Models.ImageRequestResponseDto?> PollForCompletion(string requestId, int maxAttempts = 30)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                var request = await _apiService.GetImageRequestAsync(requestId);

                if (request.Status == "Completed" || request.Status == "Failed")
                {
                    return request;
                }

                // Wait 2 seconds before next poll
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                // If polling fails, return null
                System.Diagnostics.Debug.WriteLine($"Polling failed: {ex.Message}");
                return null;
            }
        }

        return null;
    }
}