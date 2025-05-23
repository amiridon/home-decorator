using HomeDecorator.Core.Services;
using HomeDecorator.MauiApp.Services;

namespace HomeDecorator.MauiApp.Views;

public partial class NewDesignPage : ContentPage
{
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IGenerationService _generationService;
    private readonly ApiService _apiService;

    public NewDesignPage(IFeatureFlagService featureFlagService, IGenerationService generationService, ApiService apiService)
    {
        InitializeComponent();
        _featureFlagService = featureFlagService;
        _generationService = generationService;
        _apiService = apiService;

        // Set the binding context for the fake data mode indicator
        BindingContext = new
        {
            IsFakeDataMode = _featureFlagService.IsFakeDataMode
        };
    }

    private async void OnTakePhotoClicked(object sender, EventArgs e)
    {
        if (_featureFlagService.IsFakeDataMode)
        {
            // In fake data mode, just show a message
            await DisplayAlert("Fake Data Mode", "In fake data mode, we'd normally open the camera here.", "OK");
            return;
        }

        // In a real implementation, we would use MediaPicker to take a photo
        try
        {
            var photo = await MediaPicker.CapturePhotoAsync();
            if (photo != null)
            {
                // Handle the photo
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
        if (_featureFlagService.IsFakeDataMode)
        {
            // In fake data mode, just show a message
            await DisplayAlert("Fake Data Mode", "In fake data mode, we'd normally open the gallery here.", "OK");
            return;
        }

        // In a real implementation, we would use MediaPicker to pick a photo
        try
        {
            var photo = await MediaPicker.PickPhotoAsync();
            if (photo != null)
            {
                // Handle the photo
                var stream = await photo.OpenReadAsync();
                SelectedImage.Source = ImageSource.FromStream(() => stream);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to pick photo: {ex.Message}", "OK");
        }
    }    private async void OnGenerateDesignClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PromptEditor.Text))
        {
            await DisplayAlert("Missing Information", "Please enter a design prompt.", "OK");
            return;
        }

        // Show a loading indicator
        IsBusy = true;

        try
        {
            if (_featureFlagService.IsFakeDataMode)
            {
                // In fake mode, use the local generation service
                string imageUrl = "https://via.placeholder.com/400x300?text=Original+Room";
                string generatedImageUrl = await _generationService.GenerateImageAsync(imageUrl, PromptEditor.Text);

                await DisplayAlert("Design Generated (Fake Mode)",
                    $"Your design has been generated!\n\nPrompt: {PromptEditor.Text}\n\nImage URL: {generatedImageUrl}",
                    "OK");
            }
            else
            {
                // In real mode, use the API service
                string imageUrl = "https://via.placeholder.com/400x300?text=Original+Room";
                
                // Create the image generation request
                var response = await _apiService.CreateImageRequestAsync(imageUrl, PromptEditor.Text);
                
                // Show initial response
                await DisplayAlert("Generation Started",
                    $"Your design generation has been started!\n\nRequest ID: {response.Id}\n\nStatus: {response.Status}",
                    "OK");
                
                // Poll for completion (in a real app, you might use SignalR or WebSockets)
                var completedRequest = await PollForCompletion(response.Id);
                
                if (completedRequest != null && completedRequest.Status == "Completed")
                {
                    await DisplayAlert("Design Completed",
                        $"Your design is ready!\n\nGenerated Image: {completedRequest.GeneratedImageUrl}",
                        "OK");
                }
                else
                {
                    await DisplayAlert("Generation Status",
                        $"Status: {completedRequest?.Status ?? "Unknown"}\n\nError: {completedRequest?.ErrorMessage ?? "None"}",
                        "OK");
                }
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

    private async Task<HomeDecorator.Core.Models.ImageRequestResponseDto?> PollForCompletion(string requestId)
    {
        // Simple polling mechanism - in production, use SignalR or WebSockets
        for (int i = 0; i < 30; i++) // Poll for up to 30 seconds
        {
            try
            {
                var request = await _apiService.GetImageRequestAsync(requestId);
                if (request.Status == "Completed" || request.Status == "Failed")
                {
                    return request;
                }
                await Task.Delay(1000); // Wait 1 second before next poll
            }
            catch
            {
                // Continue polling even if individual requests fail
                await Task.Delay(1000);
            }
        }
        return null; // Timeout
    }
}
