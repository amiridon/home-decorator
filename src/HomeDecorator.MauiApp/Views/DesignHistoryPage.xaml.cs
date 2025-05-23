using HomeDecorator.Core.Services;
using HomeDecorator.MauiApp.Services;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace HomeDecorator.MauiApp.Views
{    public partial class DesignHistoryPage : ContentPage
    {
        private readonly IGenerationService _generationService;
        private readonly IFeatureFlagService _featureFlagService;
        private readonly ApiService _apiService;

        public ObservableCollection<DesignHistoryItem> Designs { get; } = new ObservableCollection<DesignHistoryItem>();

        public DesignHistoryPage(IGenerationService generationService, IFeatureFlagService featureFlagService, ApiService apiService)
        {
            InitializeComponent();

            _generationService = generationService;
            _featureFlagService = featureFlagService;
            _apiService = apiService;

            DesignCollection.ItemsSource = Designs;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Clear existing designs
            Designs.Clear();

            if (_featureFlagService.IsFakeDataMode)
            {
                LoadMockDesigns();
            }
            else
            {
                await LoadDesignsFromService();
            }
        }        private async System.Threading.Tasks.Task LoadDesignsFromService()
        {
            try
            {
                // Load designs from API
                var imageHistory = await _apiService.GetImageHistoryAsync(20);
                
                foreach (var imageRequest in imageHistory)
                {
                    Designs.Add(new DesignHistoryItem
                    {
                        Id = imageRequest.Id,
                        Name = $"Design {imageRequest.Id[..8]}...", // Use first 8 chars of ID as name
                        Description = imageRequest.Prompt,
                        ImageUrl = imageRequest.GeneratedImageUrl ?? imageRequest.OriginalImageUrl,
                        DateCreated = imageRequest.CreatedAt.ToLocalTime()
                    });
                }
                
                if (Designs.Count == 0)
                {
                    // If no API data, fall back to mock data
                    LoadMockDesigns();
                }
            }            catch (Exception)
            {
                // If API fails, show mock data
                LoadMockDesigns();
                await DisplayAlert("Info", "Using mock data - API unavailable", "OK");
            }
        }

        private void LoadMockDesigns()
        {
            // Add some mock designs for testing
            Designs.Add(new DesignHistoryItem
            {
                Id = "design-1",
                Name = "Living Room Makeover",
                Description = "Modern living room with blue accents",
                ImageUrl = "https://via.placeholder.com/300x200?text=Living+Room",
                DateCreated = DateTime.Now.AddDays(-2)
            });

            Designs.Add(new DesignHistoryItem
            {
                Id = "design-2",
                Name = "Kitchen Redesign",
                Description = "Contemporary kitchen with island",
                ImageUrl = "https://via.placeholder.com/300x200?text=Kitchen",
                DateCreated = DateTime.Now.AddDays(-5)
            });

            Designs.Add(new DesignHistoryItem
            {
                Id = "design-3",
                Name = "Master Bedroom",
                Description = "Cozy bedroom with natural light",
                ImageUrl = "https://via.placeholder.com/300x200?text=Bedroom",
                DateCreated = DateTime.Now.AddDays(-10)
            });
        }
    }    public class DesignHistoryItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }

        public ICommand ViewCommand => new Command(async () =>
        {
            if (Application.Current?.Windows?.FirstOrDefault()?.Page is Page page)
            {
                await page.DisplayAlert("View Design", $"Viewing design: {Name}", "OK");
            }
        });

        public ICommand DeleteCommand => new Command(async () =>
        {
            if (Application.Current?.Windows?.FirstOrDefault()?.Page is Page page)
            {
                await page.DisplayAlert("Delete Design", $"Delete design: {Name}", "OK");
            }
        });
    }
}
