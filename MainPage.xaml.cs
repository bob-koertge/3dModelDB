using MauiApp3.ViewModels;

namespace MauiApp3
{
    public partial class MainPage : ContentPage
    {
        private MainViewModel ViewModel => (MainViewModel)BindingContext;

        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            
            // Subscribe to property changes to update the 3D viewer
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedModel))
            {
                UpdateViewer();
            }
        }

        private void UpdateViewer()
        {
            // Always update the viewer when selection changes
            if (ViewModel.SelectedModel?.ParsedModel != null)
            {
                // Load the 3D model
                Model3DViewer.LoadModel(ViewModel.SelectedModel.ParsedModel);
            }
        }

        private void OnResetViewClicked(object sender, EventArgs e)
        {
            if (ViewModel.SelectedModel?.ParsedModel != null)
            {
                Model3DViewer.ResetView();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Refresh the viewer if a model is selected
            if (ViewModel.SelectedModel?.ParsedModel != null)
            {
                Model3DViewer.LoadModel(ViewModel.SelectedModel.ParsedModel);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (BindingContext is MainViewModel viewModel)
            {
                viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
        }
    }
}
