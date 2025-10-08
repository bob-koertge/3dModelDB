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
            try
            {
                if (ViewModel.SelectedModel?.ParsedModel != null && Model3DViewer != null)
                {
                    Model3DViewer.LoadModel(ViewModel.SelectedModel.ParsedModel);
                    System.Diagnostics.Debug.WriteLine($"Loaded model with {ViewModel.SelectedModel.ParsedModel.Triangles.Count:N0} triangles");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in UpdateViewer: {ex.Message}");
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
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select 3D Model File",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".stl", ".3mf" } }
                    })
                });
                
                if (result == null) return;
                
                // Validate file format
                if (!result.FileName.EndsWith(".stl", StringComparison.OrdinalIgnoreCase) && 
                    !result.FileName.EndsWith(".3mf", StringComparison.OrdinalIgnoreCase))
                {
                    await DisplayAlert("Error", "Please select an STL or 3MF file.", "OK");
                    return;
                }
                
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
                        var objectCount = await threeMfParser.GetObjectCountAsync(result.FullPath);
                        
                        if (objectCount == 0)
                        {
                            await DisplayAlert("No Geometry Found",
                                $"This 3MF file contains no mesh geometry to display.",
                                "OK");
                            return;
                        }
                        
                        if (objectCount > 1)
                        {
                            var action = await DisplayActionSheet(
                                $"This 3MF file contains {objectCount} objects. How would you like to import them?",
                                "Cancel",
                                null,
                                "Import as single combined model",
                                "Import each object separately"
                            );
                            
                            if (string.IsNullOrEmpty(action) || action == "Cancel")
                                return;
                            
                            if (action == "Import each object separately")
                            {
                                await ImportMultiObject3MfAsync(result, model3DService);
                                return;
                            }
                        }
                    }
                    
                    await ImportSingleModelAsync(result);
                }
                finally
                {
                    ViewModel.IsLoading = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Upload error: {ex.Message}");
                ViewModel.IsLoading = false;
                await DisplayAlert("Error", $"Failed to upload model: {ex.Message}", "OK");
            }
        }

        private async Task ImportSingleModelAsync(FileResult result)
        {
            try
            {
                ViewModel.IsLoading = true;
                
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
                
                var model3DService = Handler?.MauiContext?.Services.GetService<Services.Model3DService>();
                if (model3DService == null)
                {
                    await DisplayAlert("Error", "Service initialization failed", "OK");
                    return;
                }
                
                model.ParsedModel = await model3DService.LoadModelAsync(result.FullPath);
                
                if (model.ParsedModel == null)
                {
                    await DisplayAlert("Error", $"Failed to parse {extension} file.", "OK");
                    return;
                }
                
                if (model.ParsedModel.Triangles.Count == 0)
                {
                    await DisplayAlert("Warning", "The file contains no geometry.", "OK");
                    return;
                }
                
                // Generate thumbnail
                try
                {
                    model.ThumbnailData = await model3DService.GenerateThumbnailAsync(result.FullPath, model.ParsedModel);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Thumbnail generation failed: {ex.Message}");
                }
                
                // Add to collection and database
                ViewModel.Models.Add(model);
                ViewModel.SelectedModel = model;
                
                var databaseService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
                if (databaseService != null)
                {
                    await databaseService.SaveModelAsync(model);
                }
                
                // Force viewer update
                await MainThread.InvokeOnMainThreadAsync(() => UpdateViewer());
                
                await DisplayAlert("Success", 
                    $"Model loaded successfully!\n\nTriangles: {model.ParsedModel.Triangles.Count:N0}", 
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Import error: {ex.Message}");
                await DisplayAlert("Error", $"Failed to import model: {ex.Message}", "OK");
            }
            finally
            {
                ViewModel.IsLoading = false;
            }
        }

        private async Task ImportMultiObject3MfAsync(FileResult result, Services.Model3DService model3DService)
        {
            try
            {
                var threeMfParser = new Services.ThreeMfParser();
                var objects = await threeMfParser.ParseMultipleObjectsAsync(result.FullPath);
                
                if (objects.Count == 0)
                {
                    await DisplayAlert("Error", "No valid objects found in 3MF file.", "OK");
                    return;
                }
                
                var baseFileName = Path.GetFileNameWithoutExtension(result.FileName);
                
                // Prompt user for project name
                var projectName = await DisplayPromptAsync(
                    "Create Project",
                    $"Import {objects.Count} objects as a project?\n\nEnter project name:",
                    "Create & Import",
                    "Cancel",
                    placeholder: baseFileName,
                    initialValue: baseFileName
                );
                
                if (string.IsNullOrWhiteSpace(projectName))
                {
                    return; // User cancelled
                }
                
                // Create the project
                var project = new Models.Project
                {
                    Name = projectName,
                    Description = $"Multi-object import from {result.FileName} ({objects.Count} objects)",
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    Color = GenerateProjectColor()
                };
                
                ViewModel.Projects.Add(project);
                
                var databaseService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
                if (databaseService != null)
                {
                    await databaseService.SaveProjectAsync(project);
                }
                
                // Now import all objects and link them to the project
                var fileInfo = new FileInfo(result.FullPath);
                int successCount = 0;
                Models.Model3DFile? lastModel = null;
                
                foreach (var (objectName, parsedModel) in objects)
                {
                    try
                    {
                        var model = new Models.Model3DFile
                        {
                            Name = $"{baseFileName} - {objectName}",
                            FilePath = result.FullPath,
                            FileType = "3MF",
                            FileSize = fileInfo.Length / objects.Count,
                            UploadedDate = DateTime.Now,
                            ParsedModel = parsedModel,
                            ProjectId = project.Id // Link to project
                        };
                        
                        model.ThumbnailData = await model3DService.GenerateThumbnailAsync(result.FullPath, parsedModel);
                        
                        ViewModel.Models.Add(model);
                        lastModel = model;
                        
                        if (databaseService != null)
                        {
                            await databaseService.SaveModelAsync(model);
                            await databaseService.AssignModelToProjectAsync(model.Id, project.Id);
                        }
                        
                        // Add model to project's ModelIds collection
                        if (!project.ModelIds.Contains(model.Id))
                        {
                            project.ModelIds.Add(model.Id);
                        }
                        
                        successCount++;
                        System.Diagnostics.Debug.WriteLine($"Imported '{model.Name}' and linked to project '{project.Name}'");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to import object {objectName}: {ex.Message}");
                    }
                }
                
                // Update project with all model IDs
                if (databaseService != null)
                {
                    await databaseService.SaveProjectAsync(project);
                }
                
                if (lastModel != null)
                {
                    ViewModel.SelectedModel = lastModel;
                }
                
                // Select the newly created project to show all imported models
                ViewModel.SelectedProject = project;
                
                await DisplayAlert("Success", 
                    $"Created project '{projectName}' with {successCount} object(s)!\n\n" +
                    $"All objects have been linked to the project.", 
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Multi-object import error: {ex.Message}");
                await DisplayAlert("Error", $"Failed to import: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Generate a random color for the project
        /// </summary>
        private string GenerateProjectColor()
        {
            var colors = new[]
            {
                "#2196F3", // Blue
                "#4CAF50", // Green
                "#FF9800", // Orange
                "#9C27B0", // Purple
                "#F44336", // Red
                "#00BCD4", // Cyan
                "#FF5722", // Deep Orange
                "#3F51B5", // Indigo
                "#009688", // Teal
                "#795548"  // Brown
            };
            
            var random = new Random();
            return colors[random.Next(colors.Length)];
        }

        private bool _isFirstAppearing = true;

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Re-subscribe to property changes
            if (BindingContext is MainViewModel vm && !ReferenceEquals(vm, ViewModel))
            {
                vm.PropertyChanged += OnViewModelPropertyChanged;
            }
            
            // Refresh viewer if model selected
            if (ViewModel.SelectedModel?.ParsedModel != null)
            {
                Model3DViewer.LoadModel(ViewModel.SelectedModel.ParsedModel);
            }
            
            // Only refresh data if we're returning from another page (like detail view)
            // Don't refresh on initial page load to avoid duplicates
            if (!_isFirstAppearing)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(100);
                    await ViewModel.RefreshDataAsync();
                });
            }
            _isFirstAppearing = false;
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
