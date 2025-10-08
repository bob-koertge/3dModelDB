using MauiApp3.Models;
using MauiApp3.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace MauiApp3.ViewModels
{
    public class ModelDetailViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService? _databaseService;
        private Model3DFile _model;
        private string _newTagText = string.Empty;
        private Project? _parentProject;
        private AttachedImage? _viewingImage;
        private bool _isImageViewerVisible;

        public Model3DFile Model
        {
            get => _model;
            set
            {
                _model = value;
                OnPropertyChanged();
                
                // Load project info if model has a ProjectId
                if (!string.IsNullOrEmpty(_model?.ProjectId) && _databaseService != null)
                {
                    _databaseService.GetProjectByIdAsync(_model.ProjectId)
                        .ContinueWith(task =>
                        {
                            if (task.IsCompletedSuccessfully && task.Result != null)
                            {
                                ParentProject = task.Result;
                                // Update the model's ProjectName property
                                _model.ProjectName = task.Result.Name;
                                // Force UI update for ProjectDisplayText
                                OnPropertyChanged(nameof(ProjectDisplayText));
                            }
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                else
                {
                    // No project, clear ParentProject and notify
                    ParentProject = null;
                    OnPropertyChanged(nameof(ProjectDisplayText));
                }
            }
        }

        public Project? ParentProject
        {
            get => _parentProject;
            set
            {
                if (_parentProject != value)
                {
                    _parentProject = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasProject));
                    OnPropertyChanged(nameof(ProjectDisplayText));
                }
            }
        }

        public bool HasProject => ParentProject != null;
        
        public string ProjectDisplayText => ParentProject?.Name ?? "No Project";

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

        public AttachedImage? ViewingImage
        {
            get => _viewingImage;
            set
            {
                if (_viewingImage != value)
                {
                    _viewingImage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsImageViewerVisible
        {
            get => _isImageViewerVisible;
            set
            {
                if (_isImageViewerVisible != value)
                {
                    _isImageViewerVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand AddTagCommand { get; }
        public ICommand RemoveTagCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand ResetViewCommand { get; }
        public ICommand AttachImageCommand { get; }
        public ICommand RemoveImageCommand { get; }
        public ICommand ViewImageCommand { get; }
        public ICommand CloseImageViewerCommand { get; }
        public ICommand AttachGCodeCommand { get; }
        public ICommand RemoveGCodeCommand { get; }
        public ICommand OpenGCodeCommand { get; }

        public ModelDetailViewModel(Model3DFile model, DatabaseService? databaseService)
        {
            _databaseService = databaseService;
            
            AddTagCommand = new Command(AddTag);
            RemoveTagCommand = new Command<string>(RemoveTag);
            CloseCommand = new Command(async () => await Close());
            ResetViewCommand = new Command(ResetView);
            AttachImageCommand = new Command(async () => await AttachImage());
            RemoveImageCommand = new Command<AttachedImage>(async (image) => await RemoveImage(image));
            ViewImageCommand = new Command<AttachedImage>(ViewImage);
            CloseImageViewerCommand = new Command(CloseImageViewer);
            AttachGCodeCommand = new Command(async () => await AttachGCode());
            RemoveGCodeCommand = new Command<AttachedGCode>(async (gcode) => await RemoveGCode(gcode));
            OpenGCodeCommand = new Command<AttachedGCode>(async (gcode) => await OpenGCode(gcode));
            
            // Use property setter instead of field assignment to trigger project loading
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        #region Tag Management

        private void AddTag()
        {
            if (string.IsNullOrWhiteSpace(NewTagText))
                return;

            var trimmedTag = NewTagText.Trim();
            
            if (Model.Tags.Contains(trimmedTag, StringComparer.OrdinalIgnoreCase))
                return;

            Model.Tags.Add(trimmedTag);
            NewTagText = string.Empty;
        }

        private void RemoveTag(string? tag)
        {
            if (!string.IsNullOrEmpty(tag))
            {
                Model.Tags.Remove(tag);
            }
        }

        #endregion

        #region Image Management

        private async Task AttachImage()
        {
            try
            {
                var result = await PickFileAsync(
                    "Select Image",
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" } },
                        { DevicePlatform.macOS, new[] { "jpg", "jpeg", "png", "bmp", "gif" } },
                        { DevicePlatform.MacCatalyst, new[] { "jpg", "jpeg", "png", "bmp", "gif" } },
                        { DevicePlatform.iOS, new[] { "public.image" } },
                        { DevicePlatform.Android, new[] { "image/*" } }
                    });

                if (result == null)
                    return;

                using var stream = await result.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                var attachedImage = new AttachedImage
                {
                    FileName = result.FileName,
                    ImageData = memoryStream.ToArray(),
                    AttachedDate = DateTime.Now
                };

                Model.AttachedImages.Add(attachedImage);
                await SaveModelAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error attaching image: {ex.Message}");
            }
        }

        private async Task RemoveImage(AttachedImage? image)
        {
            if (image == null)
                return;

            Model.AttachedImages.Remove(image);
            await SaveModelAsync();
        }

        private void ViewImage(AttachedImage? image)
        {
            if (image == null)
                return;

            ViewingImage = image;
            IsImageViewerVisible = true;
        }

        private void CloseImageViewer()
        {
            IsImageViewerVisible = false;
            ViewingImage = null;
        }

        #endregion

        #region G-code Management

        private async Task AttachGCode()
        {
            try
            {
                var result = await PickFileAsync(
                    "Select G-code File",
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".gcode", ".gco", ".g" } },
                        { DevicePlatform.macOS, new[] { "gcode", "gco", "g" } },
                        { DevicePlatform.MacCatalyst, new[] { "gcode", "gco", "g" } },
                        { DevicePlatform.iOS, new[] { "public.data" } },
                        { DevicePlatform.Android, new[] { "*/*" } }
                    });

                if (result == null)
                    return;

                var fileInfo = new FileInfo(result.FullPath);

                var attachedGCode = new AttachedGCode
                {
                    FileName = result.FileName,
                    FilePath = result.FullPath,
                    FileSize = fileInfo.Length,
                    AttachedDate = DateTime.Now
                };

                Model.AttachedGCodeFiles.Add(attachedGCode);
                await SaveModelAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error attaching G-code: {ex.Message}");
            }
        }

        private async Task RemoveGCode(AttachedGCode? gcode)
        {
            if (gcode == null)
                return;

            Model.AttachedGCodeFiles.Remove(gcode);
            await SaveModelAsync();
        }

        private async Task OpenGCode(AttachedGCode? gcode)
        {
            if (gcode == null || string.IsNullOrEmpty(gcode.FilePath))
                return;

            try
            {
                if (!File.Exists(gcode.FilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"G-code file not found: {gcode.FilePath}");
                    return;
                }

                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(gcode.FilePath)
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening G-code: {ex.Message}");
            }
        }

        #endregion

        #region Helpers

        private async Task<FileResult?> PickFileAsync(string title, Dictionary<DevicePlatform, IEnumerable<string>> fileTypes)
        {
            var customFileType = new FilePickerFileType(fileTypes);
            
            return await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = title,
                FileTypes = customFileType
            });
        }

        private async Task SaveModelAsync()
        {
            if (_databaseService != null)
            {
                await _databaseService.SaveModelAsync(Model);
            }
        }

        private async Task Close()
        {
            await Shell.Current.GoToAsync("..");
        }

        private void ResetView()
        {
            WeakReferenceMessenger.Default.Send(new ResetViewMessage());
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public class ResetViewMessage { }
}
