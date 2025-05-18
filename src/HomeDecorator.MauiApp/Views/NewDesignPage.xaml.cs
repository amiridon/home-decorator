using HomeDecorator.Core.Services;

namespace HomeDecorator.MauiApp.Views;

public partial class NewDesignPage : ContentPage
{
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IGenerationService _generationService;

    public NewDesignPage(IFeatureFlagService featureFlagService, IGenerationService generationService)
    {
        InitializeComponent();
        _featureFlagService = featureFlagService;
        _generationService = generationService;

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
    }

    private async void OnGenerateDesignClicked(object sender, EventArgs e)
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
            // Mock image URL for testing
            string imageUrl = "https://via.placeholder.com/400x300?text=Original+Room";

            // Call the generation service (will be the mock implementation in fake mode)
            string generatedImageUrl = await _generationService.GenerateImageAsync(imageUrl, PromptEditor.Text);

            // Navigate to a result page (we'll simulate this with an alert for now)
            await DisplayAlert("Design Generated",
                $"Your design has been generated!\n\nPrompt: {PromptEditor.Text}\n\nImage URL: {generatedImageUrl}\n\nFake Mode: {_featureFlagService.IsFakeDataMode}",
                "OK");
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
}
