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
            set
            {
                if (_isDrawerOpen != value)
                {
                    _isDrawerOpen = value;
                    OnPropertyChanged();
                    DrawerWidth = value ? 250 : 0;
                }
            }
        }

        public double DrawerWidth
        {
            get => _drawerWidth;
            set
            {
                if (_drawerWidth != value)
                {
                    _drawerWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        public Model3DFile? SelectedModel
        {
            get => _selectedModel;
            set
            {
                if (_selectedModel != value)
                {
                    _selectedModel = value;
                    OnPropertyChanged();
                    NewTagText = string.Empty; // Reset tag input when model changes
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public string NewTagText
        {
            get => _newTagText;
            set
            {
                if (_newTagText != value)
                {
                    _newTagText = value;
                    OnPropertyChanged();
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

        public MainViewModel(Model3DService model3DService)
        {
            _model3DService = model3DService;
            
            ToggleDrawerCommand = new Command(ToggleDrawer);
            UploadModelCommand = new Command(async () => await UploadModel());
            SelectModelCommand = new Command<Model3DFile>(SelectModel);
            DeleteModelCommand = new Command<Model3DFile>(DeleteModel);
            AddTagCommand = new Command(AddTag);
            RemoveTagCommand = new Command<string>(RemoveTag);
            OpenDetailCommand = new Command<Model3DFile>(async (model) => await OpenDetailView(model));

            // Add sample data
            LoadSampleData();
        }

        private void ToggleDrawer()
        {
            IsDrawerOpen = !IsDrawerOpen;
        }

        private async Task UploadModel()
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select 3D Model File",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".stl", ".3mf" } },
                        { DevicePlatform.macOS, new[] { "stl", "3mf" } },
                        { DevicePlatform.MacCatalyst, new[] { "stl", "3mf" } },
                        { DevicePlatform.iOS, new[] { "public.item" } },
                        { DevicePlatform.Android, new[] { "*/*" } }
                    })
                });

                if (result != null)
                {
                    // Validate file format
                    if (!_model3DService.IsSupportedFormat(result.FileName))
                    {
                        await ShowAlert("Error", "Unsupported file format. Please select an STL or 3MF file.");
                        return;
                    }

                    IsLoading = true;

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

                    // Load and parse the 3D model (supports both STL and 3MF)
                    model.ParsedModel = await _model3DService.LoadModelAsync(result.FullPath);
                    
                    if (model.ParsedModel == null)
                    {
                        await ShowAlert("Error", $"Failed to parse {extension} file. The file may be corrupted or invalid.");
                        IsLoading = false;
                        return;
                    }

                    // Generate thumbnail from the parsed model
                    model.ThumbnailData = await _model3DService.GenerateThumbnailAsync(result.FullPath, model.ParsedModel);

                    Models.Add(model);
                    SelectedModel = model;

                    IsLoading = false;
                    await ShowAlert("Success", $"Model '{model.Name}' loaded successfully!\nTriangles: {model.ParsedModel?.Triangles.Count ?? 0}");
                }
            }
            catch (Exception ex)
            {
                IsLoading = false;
                await ShowAlert("Error", $"Failed to upload model: {ex.Message}");
            }
        }

        private async Task ShowAlert(string title, string message)
        {
            if (Application.Current?.Windows.Count > 0)
            {
                var window = Application.Current.Windows[0];
                if (window.Page != null)
                {
                    await window.Page.DisplayAlert(title, message, "OK");
                }
            }
        }

        private void SelectModel(Model3DFile model)
        {
            SelectedModel = model;
        }

        private void DeleteModel(Model3DFile model)
        {
            if (SelectedModel == model)
            {
                SelectedModel = null;
            }
            Models.Remove(model);
        }

        private void AddTag()
        {
            if (SelectedModel == null || string.IsNullOrWhiteSpace(NewTagText))
                return;

            var trimmedTag = NewTagText.Trim();
            
            // Check if tag already exists on this model
            if (SelectedModel.Tags.Contains(trimmedTag, StringComparer.OrdinalIgnoreCase))
                return;

            // Add tag to model
            SelectedModel.Tags.Add(trimmedTag);

            // Add to global tags list if not already there
            if (!AllTags.Contains(trimmedTag, StringComparer.OrdinalIgnoreCase))
            {
                AllTags.Add(trimmedTag);
            }

            // Clear input
            NewTagText = string.Empty;
        }

        private void RemoveTag(string tag)
        {
            if (SelectedModel == null || string.IsNullOrEmpty(tag))
                return;

            SelectedModel.Tags.Remove(tag);

            // Remove from global tags if no model uses it anymore
            if (!Models.Any(m => m.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
            {
                AllTags.Remove(tag);
            }
        }

        private async Task OpenDetailView(Model3DFile? model)
        {
            if (model == null)
                return;

            var navigationParameter = new Dictionary<string, object>
            {
                { "Model", model }
            };

            await Shell.Current.GoToAsync($"{nameof(MauiApp3.Pages.ModelDetailPage)}", navigationParameter);
        }

        private void LoadSampleData()
        {
            // Add sample models for demonstration (with placeholder thumbnails)
            var cubeModel = new Model3DFile
            {
                Name = "sample_cube.stl",
                FileType = "STL",
                FileSize = 102400,
                UploadedDate = DateTime.Now.AddDays(-2),
                ThumbnailData = _model3DService.GenerateThumbnailAsync("", null).Result
            };
            cubeModel.Tags.Add("geometric");
            cubeModel.Tags.Add("simple");
            Models.Add(cubeModel);

            var sphereModel = new Model3DFile
            {
                Name = "sphere_model.3mf",
                FileType = "3MF",
                FileSize = 256000,
                UploadedDate = DateTime.Now.AddDays(-1),
                ThumbnailData = _model3DService.GenerateThumbnailAsync("", null).Result
            };
            sphereModel.Tags.Add("geometric");
            sphereModel.Tags.Add("round");
            Models.Add(sphereModel);

            var complexModel = new Model3DFile
            {
                Name = "complex_part.stl",
                FileType = "STL",
                FileSize = 512000,
                UploadedDate = DateTime.Now.AddHours(-3),
                ThumbnailData = _model3DService.GenerateThumbnailAsync("", null).Result
            };
            complexModel.Tags.Add("mechanical");
            complexModel.Tags.Add("complex");
            Models.Add(complexModel);

            // Build initial tag list
            foreach (var model in Models)
            {
                foreach (var tag in model.Tags)
                {
                    if (!AllTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    {
                        AllTags.Add(tag);
                    }
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
