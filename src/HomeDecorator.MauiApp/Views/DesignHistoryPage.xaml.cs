using HomeDecorator.Core.Services;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace HomeDecorator.MauiApp.Views
{
    public partial class DesignHistoryPage : ContentPage
    {
        private readonly IGenerationService _generationService;
        private readonly IFeatureFlagService _featureFlagService;

        public ObservableCollection<DesignHistoryItem> Designs { get; } = new ObservableCollection<DesignHistoryItem>();

        public DesignHistoryPage(IGenerationService generationService, IFeatureFlagService featureFlagService)
        {
            InitializeComponent();

            _generationService = generationService;
            _featureFlagService = featureFlagService;

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
        }

        private async System.Threading.Tasks.Task LoadDesignsFromService()
        {
            try
            {
                // In a real app, this would fetch designs from a service
                // For now, just display mock data
                LoadMockDesigns();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load design history: {ex.Message}", "OK");
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
    }

    public class DesignHistoryItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public DateTime DateCreated { get; set; }

        public ICommand ViewCommand => new Command(() =>
            Application.Current.MainPage.DisplayAlert("View Design", $"Viewing design: {Name}", "OK"));

        public ICommand DeleteCommand => new Command(() =>
            Application.Current.MainPage.DisplayAlert("Delete Design", $"Delete design: {Name}", "OK"));
    }
}
