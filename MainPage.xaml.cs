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
            
            // Log to verify initialization
            Console.WriteLine($"MainPage: BindingContext set to MainViewModel");
            Console.WriteLine($"MainPage: UploadModelCommand is null? {viewModel.UploadModelCommand == null}");
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

        private async void OnUploadButtonClicked(object sender, EventArgs e)
        {
            Console.WriteLine("=== UPLOAD BUTTON CLICKED ===");
            
            try
            {
                Console.WriteLine("Opening file picker directly...");
                
                // Direct FilePicker call
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select 3D Model File",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".stl", ".3mf" } }
                    })
                });
                
                Console.WriteLine($"File picker result: {result?.FileName ?? "null"}");
                
                if (result == null)
                {
                    Console.WriteLine("User cancelled file selection");
                    return;
                }
                
                // File was selected - now process it
                Console.WriteLine($"Processing file: {result.FileName}");
                Console.WriteLine($"File path: {result.FullPath}");
                
                // Validate file format
                if (!result.FileName.EndsWith(".stl", StringComparison.OrdinalIgnoreCase) && 
                    !result.FileName.EndsWith(".3mf", StringComparison.OrdinalIgnoreCase))
                {
                    await DisplayAlert("Error", "Please select an STL or 3MF file.", "OK");
                    return;
                }
                
                // Show loading
                ViewModel.IsLoading = true;
                
                try
                {
                    Console.WriteLine("Creating model object...");
                    var fileInfo = new FileInfo(result.FullPath);
                    var extension = Path.GetExtension(result.FileName).ToUpperInvariant().TrimStart('.');
                    
                    var model = new Models.Model3DFile
                    {
                        Name = result.FileName,
                        FilePath = result.FullPath,
                        FileType = extension,
                        FileSize = fileInfo.Length,
                        UploadedDate = DateTime.Now
                    };
                    
                    Console.WriteLine($"Parsing {extension} file...");
                    
                    // Use the Model3DService to load and parse
                    var model3DService = Handler?.MauiContext?.Services.GetService<Services.Model3DService>();
                    if (model3DService == null)
                    {
                        Console.WriteLine("ERROR: Could not get Model3DService from services");
                        await DisplayAlert("Error", "Service initialization failed", "OK");
                        return;
                    }
                    
                    model.ParsedModel = await model3DService.LoadModelAsync(result.FullPath);
                    
                    if (model.ParsedModel == null)
                    {
                        Console.WriteLine("ERROR: Failed to parse model");
                        await DisplayAlert("Error", $"Failed to parse {extension} file. The file may be corrupted or invalid.", "OK");
                        return;
                    }
                    
                    Console.WriteLine($"Successfully parsed {model.ParsedModel.Triangles.Count} triangles");
                    Console.WriteLine("Generating thumbnail...");
                    
                    // Generate thumbnail
                    model.ThumbnailData = await model3DService.GenerateThumbnailAsync(result.FullPath, model.ParsedModel);
                    
                    Console.WriteLine("Adding model to collection...");
                    
                    // Add to ViewModel
                    ViewModel.Models.Add(model);
                    ViewModel.SelectedModel = model;
                    
                    Console.WriteLine("Saving to database...");
                    
                    // Save to database
                    var databaseService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
                    if (databaseService != null)
                    {
                        await databaseService.SaveModelAsync(model);
                        Console.WriteLine("Saved to database successfully");
                    }
                    
                    Console.WriteLine("Upload complete!");
                    await DisplayAlert("Success", 
                        $"Model '{model.Name}' loaded successfully!\n\nTriangles: {model.ParsedModel.Triangles.Count:N0}", 
                        "OK");
                }
                finally
                {
                    ViewModel.IsLoading = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                ViewModel.IsLoading = false;
                await DisplayAlert("Error", $"Failed to upload model:\n{ex.Message}", "OK");
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
