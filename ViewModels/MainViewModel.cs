using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiApp3.Models;
using MauiApp3.Services;

namespace MauiApp3.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly Model3DService _model3DService;
        private readonly DatabaseService _databaseService;
        private bool _isDrawerOpen = true;
        private Model3DFile? _selectedModel;
        private double _drawerWidth = 250;
        private bool _isLoading;
        private string _newTagText = string.Empty;

        public ObservableCollection<Model3DFile> Models { get; } = new();
        public ObservableCollection<string> AllTags { get; } = new();

        public bool IsDrawerOpen
        {
            get => _isDrawerOpen;
            set => SetProperty(ref _isDrawerOpen, value, () => DrawerWidth = value ? 250 : 0);
        }

        public double DrawerWidth
        {
            get => _drawerWidth;
            set => SetProperty(ref _drawerWidth, value);
        }

        public Model3DFile? SelectedModel
        {
            get => _selectedModel;
            set
            {
                if (SetProperty(ref _selectedModel, value))
                {
                    NewTagText = string.Empty;
                    // Update CanExecute when selected model changes
                    ((Command)AddTagCommand).ChangeCanExecute();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string NewTagText
        {
            get => _newTagText;
            set
            {
                if (SetProperty(ref _newTagText, value))
                {
                    // Update CanExecute when text changes
                    ((Command)AddTagCommand).ChangeCanExecute();
                }
            }
        }

        public ICommand ToggleDrawerCommand { get; }
        public ICommand UploadModelCommand { get; }
        public ICommand SelectModelCommand { get; }
        public ICommand DeleteModelCommand { get; }
        public ICommand AddTagCommand { get; }
        public ICommand RemoveTagCommand { get; }
        public ICommand OpenDetailCommand { get; }

        public MainViewModel(Model3DService model3DService, DatabaseService databaseService)
        {
            _model3DService = model3DService;
            _databaseService = databaseService;
            
            ToggleDrawerCommand = new Command(ToggleDrawer);
            UploadModelCommand = new Command(OnUploadModel, () => !IsLoading);
            SelectModelCommand = new Command<Model3DFile>(SelectModel);
            DeleteModelCommand = new Command<Model3DFile>(DeleteModel);
            AddTagCommand = new Command(AddTag, () => SelectedModel != null && !string.IsNullOrWhiteSpace(NewTagText));
            RemoveTagCommand = new Command<string>(RemoveTag);
            OpenDetailCommand = new Command<Model3DFile>(async (model) => await OpenDetailView(model));

            // Load data from database instead of sample data
            _ = LoadDataFromDatabaseAsync();
        }

        private void OnUploadModel()
        {
            Console.WriteLine("OnUploadModel: Method called!");
            
            // Ensure we're on the main thread
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                Console.WriteLine("OnUploadModel: Executing on MainThread");
                await UploadModelAsync();
            });
        }

        private void ToggleDrawer()
        {
            IsDrawerOpen = !IsDrawerOpen;
        }

        private async Task UploadModelAsync()
        {
            try
            {
                Console.WriteLine("UploadModel: Starting file picker...");
                
                var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".stl", ".3mf" } },
                    { DevicePlatform.macOS, new[] { "stl", "3mf" } },
                    { DevicePlatform.MacCatalyst, new[] { "stl", "3mf" } },
                    { DevicePlatform.iOS, new[] { "public.item" } },
                    { DevicePlatform.Android, new[] { "*/*" } }
                });

                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select 3D Model File",
                    FileTypes = customFileType
                });

                Console.WriteLine($"UploadModel: File picker result: {(result != null ? result.FileName : "null")}");

                if (result == null)
                {
                    Console.WriteLine("UploadModel: User cancelled or no file selected");
                    return;
                }

                Console.WriteLine($"UploadModel: Selected file: {result.FullPath}");

                // Validate file format
                if (!_model3DService.IsSupportedFormat(result.FileName))
                {
                    Console.WriteLine($"UploadModel: Unsupported format: {result.FileName}");
                    await ShowAlertAsync("Error", "Unsupported file format. Please select an STL or 3MF file.");
                    return;
                }

                IsLoading = true;
                ((Command)UploadModelCommand).ChangeCanExecute();

                try
                {
                    Console.WriteLine("UploadModel: Creating file info...");
                    var fileInfo = new FileInfo(result.FullPath);
                    var extension = Path.GetExtension(result.FileName).ToUpperInvariant().TrimStart('.');

                    var model = new Model3DFile
                    {
                        Name = result.FileName,
                        FilePath = result.FullPath,
                        FileType = extension,
                        FileSize = fileInfo.Length,
                        UploadedDate = DateTime.Now
                    };

                    Console.WriteLine($"UploadModel: Parsing {extension} file...");
                    // Load and parse the 3D model
                    model.ParsedModel = await _model3DService.LoadModelAsync(result.FullPath);
                    
                    if (model.ParsedModel == null)
                    {
                        Console.WriteLine("UploadModel: Failed to parse file");
                        await ShowAlertAsync("Error", $"Failed to parse {extension} file. The file may be corrupted or invalid.");
                        return;
                    }

                    Console.WriteLine($"UploadModel: Successfully parsed {model.ParsedModel.Triangles.Count} triangles");
                    Console.WriteLine("UploadModel: Generating thumbnail...");
                    
                    // Generate thumbnail asynchronously
                    model.ThumbnailData = await _model3DService.GenerateThumbnailAsync(result.FullPath, model.ParsedModel);

                    Console.WriteLine("UploadModel: Adding model to collection...");
                    Models.Add(model);
                    SelectedModel = model;

                    Console.WriteLine("UploadModel: Saving to database...");
                    // Save to database
                    await SaveModelToDatabaseAsync(model);

                    Console.WriteLine("UploadModel: Upload complete!");
                    await ShowAlertAsync("Success", $"Model '{model.Name}' loaded successfully!\nTriangles: {model.ParsedModel.Triangles.Count:N0}");
                }
                finally
                {
                    IsLoading = false;
                    ((Command)UploadModelCommand).ChangeCanExecute();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UploadModel: Exception occurred: {ex.Message}");
                Console.WriteLine($"UploadModel: Stack trace: {ex.StackTrace}");
                IsLoading = false;
                ((Command)UploadModelCommand).ChangeCanExecute();
                await ShowAlertAsync("Error", $"Failed to upload model: {ex.Message}\n\nDetails: {ex.GetType().Name}");
            }
        }

        private Task ShowAlertAsync(string title, string message)
        {
            // Use Shell.Current instead of deprecated Application.MainPage
            if (Shell.Current?.CurrentPage != null)
            {
                return Shell.Current.CurrentPage.DisplayAlert(title, message, "OK");
            }
            return Task.CompletedTask;
        }

        private void SelectModel(Model3DFile? model)
        {
            if (model != null)
            {
                SelectedModel = model;
            }
        }

        private async void DeleteModel(Model3DFile? model)
        {
            if (model == null)
                return;

            if (SelectedModel == model)
            {
                SelectedModel = null;
            }
            
            Models.Remove(model);
            
            // Delete from database
            await DeleteModelFromDatabaseAsync(model.Id);
        }

        private async void AddTag()
        {
            if (SelectedModel == null || string.IsNullOrWhiteSpace(NewTagText))
                return;

            var trimmedTag = NewTagText.Trim();
            
            // Check if tag already exists (case-insensitive)
            if (SelectedModel.Tags.Any(t => string.Equals(t, trimmedTag, StringComparison.OrdinalIgnoreCase)))
                return;

            // Add tag to model
            SelectedModel.Tags.Add(trimmedTag);

            // Add to global tags list if not already there
            if (!AllTags.Any(t => string.Equals(t, trimmedTag, StringComparison.OrdinalIgnoreCase)))
            {
                AllTags.Add(trimmedTag);
            }

            // Save to database
            await SaveModelToDatabaseAsync(SelectedModel);

            // Clear input
            NewTagText = string.Empty;
        }

        private async void RemoveTag(string? tag)
        {
            if (SelectedModel == null || string.IsNullOrEmpty(tag))
                return;

            SelectedModel.Tags.Remove(tag);

            // Remove from global tags if no model uses it anymore
            if (!Models.SelectMany(m => m.Tags).Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
            {
                AllTags.Remove(tag);
            }

            // Save to database
            await SaveModelToDatabaseAsync(SelectedModel);
        }

        private async Task OpenDetailView(Model3DFile? model)
        {
            if (model == null)
                return;

            await Shell.Current.GoToAsync(nameof(Pages.ModelDetailPage), new Dictionary<string, object>
            {
                { "Model", model }
            });
        }

        private async Task LoadDataFromDatabaseAsync()
        {
            try
            {
                IsLoading = true;

                // Load models from database
                var savedModels = await _databaseService.GetAllModelsAsync();

                foreach (var model in savedModels)
                {
                    Models.Add(model);

                    // Rebuild tag list
                    foreach (var tag in model.Tags)
                    {
                        if (!AllTags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                        {
                            AllTags.Add(tag);
                        }
                    }
                }

                // If no models, load sample data for first run
                if (Models.Count == 0)
                {
                    await LoadSampleDataAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data from database: {ex.Message}");
                // Fallback to sample data on error
                await LoadSampleDataAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveModelToDatabaseAsync(Model3DFile model)
        {
            try
            {
                await _databaseService.SaveModelAsync(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving model to database: {ex.Message}");
            }
        }

        private async Task DeleteModelFromDatabaseAsync(string modelId)
        {
            try
            {
                await _databaseService.DeleteModelAsync(modelId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting model from database: {ex.Message}");
            }
        }

        private async Task LoadSampleDataAsync()
        {
            // Generate sample data without blocking
            var sampleModels = new[]
            {
                new { Name = "sample_cube.stl", Type = "STL", Size = 102400L, Days = -2, Tags = new[] { "geometric", "simple" } },
                new { Name = "sphere_model.3mf", Type = "3MF", Size = 256000L, Days = -1, Tags = new[] { "geometric", "round" } },
                new { Name = "complex_part.stl", Type = "STL", Size = 512000L, Days = 0, Tags = new[] { "mechanical", "complex" } }
            };

            foreach (var sample in sampleModels)
            {
                var model = new Model3DFile
                {
                    Name = sample.Name,
                    FileType = sample.Type,
                    FileSize = sample.Size,
                    UploadedDate = DateTime.Now.AddDays(sample.Days)
                };

                foreach (var tag in sample.Tags)
                {
                    model.Tags.Add(tag);
                    if (!AllTags.Contains(tag))
                    {
                        AllTags.Add(tag);
                    }
                }

                // Generate placeholder thumbnail asynchronously
                model.ThumbnailData = await _model3DService.GenerateThumbnailAsync("", null);
                
                Models.Add(model);

                // Save sample model to database
                await SaveModelToDatabaseAsync(model);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, Action? onChanged = null, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
