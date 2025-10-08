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
                Console.WriteLine($"MainPage: SelectedModel changed");
                UpdateViewer();
            }
        }

        private void UpdateViewer()
        {
            try
            {
                Console.WriteLine($"MainPage: UpdateViewer called");
                Console.WriteLine($"MainPage: SelectedModel is null? {ViewModel.SelectedModel == null}");
                Console.WriteLine($"MainPage: SelectedModel.ParsedModel is null? {ViewModel.SelectedModel?.ParsedModel == null}");
                
                // Always update the viewer when selection changes
                if (ViewModel.SelectedModel?.ParsedModel != null)
                {
                    Console.WriteLine($"MainPage: Loading model with {ViewModel.SelectedModel.ParsedModel.Triangles.Count} triangles");
                    Console.WriteLine($"MainPage: Model3DViewer is null? {Model3DViewer == null}");
                    
                    if (Model3DViewer != null)
                    {
                        try
                        {
                            // Load the 3D model
                            Model3DViewer.LoadModel(ViewModel.SelectedModel.ParsedModel);
                            Console.WriteLine($"MainPage: Model loaded into viewer successfully");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"MainPage: ERROR loading model into viewer: {ex.Message}");
                            Console.WriteLine($"MainPage: Stack trace: {ex.StackTrace}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"MainPage: ERROR - Model3DViewer control is null!");
                    }
                }
                else
                {
                    Console.WriteLine($"MainPage: Cannot load model - ParsedModel is null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MainPage: FATAL ERROR in UpdateViewer: {ex.Message}");
                Console.WriteLine($"MainPage: Stack trace: {ex.StackTrace}");
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
                    var extension = Path.GetExtension(result.FileName).ToUpperInvariant().TrimStart('.');
                    
                    // Special handling for 3MF files - check if multi-object
                    if (extension == "3MF")
                    {
                        var model3DService = Handler?.MauiContext?.Services.GetService<Services.Model3DService>();
                        if (model3DService == null)
                        {
                            await DisplayAlert("Error", "Service initialization failed", "OK");
                            return;
                        }

                        var threeMfParser = new Services.ThreeMfParser();
                        
                        // Check object count (no diagnostics window)
                        var objectCount = await threeMfParser.GetObjectCountAsync(result.FullPath);
                        
                        System.Diagnostics.Debug.WriteLine($"3MF file contains {objectCount} object(s) with meshes");
                        
                        if (objectCount == 0)
                        {
                            await DisplayAlert("No Geometry Found",
                                $"This 3MF file contains no mesh geometry to display.\n\n" +
                                $"File: {Path.GetFileName(result.FullPath)}",
                                "OK");
                            return;
                        }
                        
                        if (objectCount > 1)
                        {
                            // Multi-object 3MF file - ask user what to do
                            var action = await DisplayActionSheet(
                                $"This 3MF file contains {objectCount} objects. How would you like to import them?",
                                "Cancel",
                                null,
                                "Import as single combined model",
                                "Import each object separately"
                            );
                            
                            if (action == "Cancel" || string.IsNullOrEmpty(action))
                            {
                                Console.WriteLine("User cancelled multi-object import");
                                return;
                            }
                            
                            if (action == "Import each object separately")
                            {
                                await ImportMultiObject3MfAsync(result, model3DService);
                                return;
                            }
                            
                            // Otherwise, continue with normal combined import
                            Console.WriteLine("User chose to import as single combined model");
                        }
                    }
                    
                    // Standard single-model import
                    await ImportSingleModelAsync(result);
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

        private async Task ImportSingleModelAsync(FileResult result)
        {
            System.Diagnostics.Debug.WriteLine("=== IMPORT SINGLE MODEL START ===");
            
            try
            {
                // Show loading indicator
                ViewModel.IsLoading = true;
                
                var fileInfo = new FileInfo(result.FullPath);
                var extension = Path.GetExtension(result.FileName).ToUpperInvariant().TrimStart('.');
                
                System.Diagnostics.Debug.WriteLine($"File: {result.FileName}");
                System.Diagnostics.Debug.WriteLine($"Extension: {extension}");
                System.Diagnostics.Debug.WriteLine($"File size: {fileInfo.Length:N0} bytes");
                
                var model = new Models.Model3DFile
                {
                    Name = result.FileName,
                    FilePath = result.FullPath,
                    FileType = extension,
                    FileSize = fileInfo.Length,
                    UploadedDate = DateTime.Now
                };
                
                System.Diagnostics.Debug.WriteLine($"Parsing {extension} file...");
                
                var model3DService = Handler?.MauiContext?.Services.GetService<Services.Model3DService>();
                if (model3DService == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Could not get Model3DService");
                    await DisplayAlert("Error", "Service initialization failed", "OK");
                    return;
                }
                
                model.ParsedModel = await model3DService.LoadModelAsync(result.FullPath);
                
                if (model.ParsedModel == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Failed to parse model (returned null)");
                    await DisplayAlert("Error", 
                        $"Failed to parse {extension} file.\n\n" +
                        $"The file may be corrupted, invalid, or use an unsupported format.", 
                        "OK");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"✓ Successfully parsed {model.ParsedModel.Triangles.Count:N0} triangles");
                
                if (model.ParsedModel.Triangles.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("WARNING: Model has no triangles");
                    await DisplayAlert("Warning", 
                        $"The file was parsed but contains no geometry (0 triangles).", 
                        "OK");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("Generating thumbnail...");
                
                try
                {
                    model.ThumbnailData = await model3DService.GenerateThumbnailAsync(result.FullPath, model.ParsedModel);
                    System.Diagnostics.Debug.WriteLine($"✓ Thumbnail generated: {model.ThumbnailData?.Length ?? 0} bytes");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠ WARNING: Failed to generate thumbnail: {ex.Message}");
                }
                
                System.Diagnostics.Debug.WriteLine("Adding model to collection...");
                
                ViewModel.Models.Add(model);
                System.Diagnostics.Debug.WriteLine($"✓ Model added. Total models: {ViewModel.Models.Count}");
                
                System.Diagnostics.Debug.WriteLine("Setting as selected model...");
                ViewModel.SelectedModel = model;
                System.Diagnostics.Debug.WriteLine($"✓ SelectedModel set");
                System.Diagnostics.Debug.WriteLine($"  - Has ParsedModel: {ViewModel.SelectedModel?.ParsedModel != null}");
                System.Diagnostics.Debug.WriteLine($"  - Triangle count: {ViewModel.SelectedModel?.ParsedModel?.Triangles.Count ?? 0:N0}");
                
                System.Diagnostics.Debug.WriteLine("Saving to database...");
                
                var databaseService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
                if (databaseService != null)
                {
                    await databaseService.SaveModelAsync(model);
                    System.Diagnostics.Debug.WriteLine("✓ Saved to database successfully");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠ WARNING: Could not get DatabaseService");
                }
                
                System.Diagnostics.Debug.WriteLine("Forcing viewer update...");
                
                // Force viewer update on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        UpdateViewer();
                        System.Diagnostics.Debug.WriteLine("✓ Viewer update triggered");
                    }
                    catch (Exception updateEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠ ERROR in viewer update: {updateEx.Message}");
                    }
                });
                
                System.Diagnostics.Debug.WriteLine("=== IMPORT COMPLETE ===");
                
                await DisplayAlert("Success", 
                    $"Model '{model.Name}' loaded successfully!\n\nTriangles: {model.ParsedModel.Triangles.Count:N0}", 
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ImportSingleModel: FATAL ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ImportSingleModel: Stack trace: {ex.StackTrace}");
                
                await DisplayAlert("Error", $"Failed to import model:\n{ex.Message}", "OK");
                throw;
            }
            finally
            {
                ViewModel.IsLoading = false;
            }
        }

        private async Task ImportMultiObject3MfAsync(FileResult result, Services.Model3DService model3DService)
        {
            Console.WriteLine("Importing 3MF as multiple separate objects...");
            
            var threeMfParser = new Services.ThreeMfParser();
            var objects = await threeMfParser.ParseMultipleObjectsAsync(result.FullPath);
            
            if (objects.Count == 0)
            {
                await DisplayAlert("Error", "No valid objects found in 3MF file.", "OK");
                return;
            }
            
            Console.WriteLine($"Found {objects.Count} object(s) to import");
            
            var fileInfo = new FileInfo(result.FullPath);
            var baseFileName = Path.GetFileNameWithoutExtension(result.FileName);
            var databaseService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
            
            int successCount = 0;
            Models.Model3DFile? lastModel = null;
            
            foreach (var (objectName, parsedModel) in objects)
            {
                try
                {
                    var modelName = $"{baseFileName} - {objectName}";
                    Console.WriteLine($"Creating model entry for: {modelName}");
                    
                    var model = new Models.Model3DFile
                    {
                        Name = modelName,
                        FilePath = result.FullPath,
                        FileType = "3MF",
                        FileSize = fileInfo.Length / objects.Count, // Approximate size per object
                        UploadedDate = DateTime.Now,
                        ParsedModel = parsedModel
                    };
                    
                    // Generate thumbnail
                    model.ThumbnailData = await model3DService.GenerateThumbnailAsync(result.FullPath, parsedModel);
                    
                    // Add to collection
                    ViewModel.Models.Add(model);
                    lastModel = model;
                    
                    // Save to database
                    if (databaseService != null)
                    {
                        await databaseService.SaveModelAsync(model);
                    }
                    
                    successCount++;
                    Console.WriteLine($"Successfully imported: {modelName} ({parsedModel.Triangles.Count} triangles)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR importing object {objectName}: {ex.Message}");
                }
            }
            
            // Select the last imported model
            if (lastModel != null)
            {
                ViewModel.SelectedModel = lastModel;
            }
            
            Console.WriteLine($"Multi-object import complete: {successCount}/{objects.Count} objects imported");
            await DisplayAlert("Success", 
                $"Imported {successCount} object(s) from 3MF file!\n\nEach object is now a separate model in your library.", 
                "OK");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            Console.WriteLine("MainPage: OnAppearing - Refreshing data");
            
            // Re-subscribe to property changes in case subscription was lost
            if (BindingContext is MainViewModel vm && !ReferenceEquals(vm, ViewModel))
            {
                vm.PropertyChanged += OnViewModelPropertyChanged;
            }
            
            // Refresh the viewer if a model is selected
            if (ViewModel.SelectedModel?.ParsedModel != null)
            {
                Model3DViewer.LoadModel(ViewModel.SelectedModel.ParsedModel);
            }
            
            // Refresh data from database to pick up any changes
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100); // Small delay to ensure page is fully loaded
                Console.WriteLine("MainPage: Calling RefreshDataAsync");
                await ViewModel.RefreshDataAsync();
            });
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
