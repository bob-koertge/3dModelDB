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
        private Project? _selectedProject;
        private bool _showProjectView = false;
        private string? _selectedFileFilter;

        public ObservableCollection<Model3DFile> Models { get; } = new();
        public ObservableCollection<string> AllTags { get; } = new();
        public ObservableCollection<Project> Projects { get; } = new();

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
                    ((Command)AddTagCommand).ChangeCanExecute();
                }
            }
        }

        public Project? SelectedProject
        {
            get => _selectedProject;
            set
            {
                if (SetProperty(ref _selectedProject, value))
                {
                    _ = FilterModelsByProjectAsync();
                }
            }
        }

        public bool ShowProjectView
        {
            get => _showProjectView;
            set => SetProperty(ref _showProjectView, value);
        }

        public string? SelectedFileFilter
        {
            get => _selectedFileFilter;
            set
            {
                if (SetProperty(ref _selectedFileFilter, value))
                {
                    _ = ApplyFiltersAsync();
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
        public ICommand CreateProjectCommand { get; }
        public ICommand DeleteProjectCommand { get; }
        public ICommand SelectProjectCommand { get; }
        public ICommand AddModelToProjectCommand { get; }
        public ICommand RemoveModelFromProjectCommand { get; }
        public ICommand ToggleProjectViewCommand { get; }
        public ICommand ResetDatabaseCommand { get; }
        public ICommand SelectFileFilterCommand { get; }

        public MainViewModel(Model3DService model3DService, DatabaseService databaseService)
        {
            _model3DService = model3DService ?? throw new ArgumentNullException(nameof(model3DService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            
            ToggleDrawerCommand = new Command(ToggleDrawer);
            UploadModelCommand = new Command(OnUploadModel, () => !IsLoading);
            SelectModelCommand = new Command<Model3DFile>(model => _ = SelectModelAsync(model));
            DeleteModelCommand = new Command<Model3DFile>(DeleteModel);
            AddTagCommand = new Command(AddTag, () => SelectedModel != null && !string.IsNullOrWhiteSpace(NewTagText));
            RemoveTagCommand = new Command<string>(RemoveTag);
            OpenDetailCommand = new Command<Model3DFile>(async (model) => await OpenDetailView(model));
            CreateProjectCommand = new Command(async () => await CreateProject());
            DeleteProjectCommand = new Command<Project>(async (project) => await DeleteProject(project));
            SelectProjectCommand = new Command<Project>(SelectProject);
            AddModelToProjectCommand = new Command<Model3DFile>(async (model) => await AddModelToProject(model));
            RemoveModelFromProjectCommand = new Command<Model3DFile>(async (model) => await RemoveModelFromProject(model));
            ToggleProjectViewCommand = new Command(ToggleProjectView);
            ResetDatabaseCommand = new Command(async () => await ResetDatabaseAsync());
            SelectFileFilterCommand = new Command<string>(fileType => SelectedFileFilter = fileType);

            _ = LoadDataFromDatabaseAsync();
        }

        #region Command Handlers

        private void OnUploadModel()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
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

                if (result == null)
                    return;

                if (!_model3DService.IsSupportedFormat(result.FileName))
                {
                    await ShowAlertAsync("Error", "Unsupported file format. Please select an STL or 3MF file.");
                    return;
                }

                IsLoading = true;
                ((Command)UploadModelCommand).ChangeCanExecute();

                try
                {
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

                    model.ParsedModel = await _model3DService.LoadModelAsync(result.FullPath);
                    
                    if (model.ParsedModel == null)
                    {
                        await ShowAlertAsync("Error", $"Failed to parse {extension} file. The file may be corrupted or invalid.");
                        return;
                    }

                    model.ThumbnailData = await _model3DService.GenerateThumbnailAsync(result.FullPath, model.ParsedModel);

                    Models.Add(model);
                    SelectedModel = model;

                    await SaveModelToDatabaseAsync(model);

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
                IsLoading = false;
                ((Command)UploadModelCommand).ChangeCanExecute();
                await ShowAlertAsync("Error", $"Failed to upload model: {ex.Message}");
            }
        }

        private async Task SelectModelAsync(Model3DFile? model)
        {
            if (model == null)
                return;

            if (model.ParsedModel == null && !string.IsNullOrEmpty(model.FilePath) && File.Exists(model.FilePath))
            {
                try
                {
                    model.ParsedModel = await _model3DService.LoadModelAsync(model.FilePath);
                }
                catch (Exception ex)
                {
                    await ShowAlertAsync("Error", $"Failed to load 3D model: {ex.Message}");
                }
            }
            
            SelectedModel = model;
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
            await DeleteModelFromDatabaseAsync(model.Id);
        }

        private async void AddTag()
        {
            if (SelectedModel == null || string.IsNullOrWhiteSpace(NewTagText))
                return;

            var trimmedTag = NewTagText.Trim();
            
            if (SelectedModel.Tags.Any(t => string.Equals(t, trimmedTag, StringComparison.OrdinalIgnoreCase)))
                return;

            SelectedModel.Tags.Add(trimmedTag);

            if (!AllTags.Any(t => string.Equals(t, trimmedTag, StringComparison.OrdinalIgnoreCase)))
            {
                AllTags.Add(trimmedTag);
            }

            await SaveModelToDatabaseAsync(SelectedModel);
            NewTagText = string.Empty;
        }

        private async void RemoveTag(string? tag)
        {
            if (SelectedModel == null || string.IsNullOrEmpty(tag))
                return;

            SelectedModel.Tags.Remove(tag);

            if (!Models.SelectMany(m => m.Tags).Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
            {
                AllTags.Remove(tag);
            }

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

        #endregion

        #region Project Management

        private async Task CreateProject()
        {
            var projectName = await Shell.Current?.CurrentPage?.DisplayPromptAsync(
                "New Project",
                "Enter project name:",
                "Create",
                "Cancel",
                placeholder: "My Project"
            )!;

            if (string.IsNullOrWhiteSpace(projectName))
                return;

            var project = new Project
            {
                Name = projectName,
                Description = string.Empty,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            Projects.Add(project);
            await _databaseService.SaveProjectAsync(project);
            
            await ShowAlertAsync("Success", $"Project '{projectName}' created!");
        }

        private async Task DeleteProject(Project? project)
        {
            if (project == null)
                return;

            var confirm = await Shell.Current?.CurrentPage?.DisplayAlert(
                "Delete Project",
                $"Are you sure you want to delete '{project.Name}'? Models will not be deleted.",
                "Delete",
                "Cancel"
            )!;

            if (!confirm)
                return;

            Projects.Remove(project);
            await _databaseService.DeleteProjectAsync(project.Id);

            if (SelectedProject == project)
            {
                SelectedProject = null;
            }

            await FilterModelsByProjectAsync();
        }

        private void SelectProject(Project? project)
        {
            SelectedProject = project;
        }

        private async Task AddModelToProject(Model3DFile? model)
        {
            if (model == null || Projects.Count == 0)
            {
                await ShowAlertAsync("No Projects", "Create a project first before assigning models.");
                return;
            }

            // Use custom ProjectSelectorDialog instead of simple action sheet
            var dialog = new Pages.ProjectSelectorDialog(Projects.ToList(), model.Name);
            await Shell.Current.Navigation.PushModalAsync(dialog);
            
            var selectedProject = await dialog.GetResultAsync();
            await Shell.Current.Navigation.PopModalAsync();
            
            if (selectedProject == null)
                return; // User cancelled

            var oldProjectId = model.ProjectId;
            
            model.ProjectId = selectedProject.Id;
            model.ProjectName = selectedProject.Name;
            
            await SaveModelToDatabaseAsync(model);
            await _databaseService.AssignModelToProjectAsync(model.Id, selectedProject.Id);

            if (!selectedProject.ModelIds.Contains(model.Id))
            {
                selectedProject.ModelIds.Add(model.Id);
                await _databaseService.SaveProjectAsync(selectedProject);
            }

            await RefreshCurrentViewAsync(oldProjectId, selectedProject.Id);
            
            if (model == SelectedModel)
            {
                OnPropertyChanged(nameof(SelectedModel));
            }

            await ShowAlertAsync("Success", $"Added '{model.Name}' to project '{selectedProject.Name}'");
        }

        private async Task RemoveModelFromProject(Model3DFile? model)
        {
            if (model == null || string.IsNullOrEmpty(model.ProjectId))
                return;

            var oldProjectId = model.ProjectId;
            var project = Projects.FirstOrDefault(p => p.Id == model.ProjectId);
            
            var confirm = await Shell.Current?.CurrentPage?.DisplayAlert(
                "Remove from Project",
                $"Remove '{model.Name}' from project '{project?.Name ?? "Unknown"}'?",
                "Remove",
                "Cancel"
            )!;

            if (!confirm)
                return;

            bool isViewingAffectedProject = SelectedProject?.Id == oldProjectId;
            var modelInCollection = Models.FirstOrDefault(m => m.Id == model.Id);

            if (SelectedModel?.Id == model.Id && isViewingAffectedProject)
            {
                SelectedModel = null;
            }

            model.ProjectId = null;
            model.ProjectName = null;
            if (modelInCollection != null)
            {
                modelInCollection.ProjectId = null;
                modelInCollection.ProjectName = null;
            }
            
            await SaveModelToDatabaseAsync(model);
            await _databaseService.RemoveModelFromProjectAsync(model.Id);

            if (project != null && project.ModelIds.Contains(model.Id))
            {
                project.ModelIds.Remove(model.Id);
                await _databaseService.SaveProjectAsync(project);
            }
            
            if (isViewingAffectedProject && modelInCollection != null)
            {
                Models.Remove(modelInCollection);
                OnPropertyChanged(nameof(Models));
            }
            else if (!isViewingAffectedProject)
            {
                await RefreshCurrentViewAsync(oldProjectId, null);
            }
            
            if (model == SelectedModel)
            {
                OnPropertyChanged(nameof(SelectedModel));
            }

            await ShowAlertAsync("Success", "Model removed from project");
        }

        private void ToggleProjectView()
        {
            ShowProjectView = !ShowProjectView;
        }

        #endregion

        #region Data Management

        private async Task LoadDataFromDatabaseAsync()
        {
            try
            {
                IsLoading = true;

                var savedProjects = await _databaseService.GetAllProjectsAsync();
                foreach (var project in savedProjects)
                {
                    Projects.Add(project);
                }

                var savedModels = await _databaseService.GetAllModelsAsync();

                foreach (var model in savedModels)
                {
                    Models.Add(model);

                    foreach (var tag in model.Tags)
                    {
                        if (!AllTags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                        {
                            AllTags.Add(tag);
                        }
                    }
                }

                UpdateAllProjectNames();
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Error", $"Failed to load data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateAllProjectNames()
        {
            foreach (var model in Models)
            {
                if (!string.IsNullOrEmpty(model.ProjectId))
                {
                    var project = Projects.FirstOrDefault(p => p.Id == model.ProjectId);
                    model.ProjectName = project?.Name;
                }
                else
                {
                    model.ProjectName = null;
                }
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
                await ShowAlertAsync("Error", $"Failed to save model: {ex.Message}");
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
                await ShowAlertAsync("Error", $"Failed to delete model: {ex.Message}");
            }
        }

        private async Task FilterModelsByProjectAsync()
        {
            Models.Clear();
            
            var models = SelectedProject == null
                ? await _databaseService.GetAllModelsAsync()
                : await _databaseService.GetModelsByProjectIdAsync(SelectedProject.Id);
            
            foreach (var model in models)
            {
                // Update project name before adding to collection
                if (!string.IsNullOrEmpty(model.ProjectId))
                {
                    var project = Projects.FirstOrDefault(p => p.Id == model.ProjectId);
                    model.ProjectName = project?.Name;
                }
                
                Models.Add(model);
            }
            
            await ApplyFileFilterToCurrentCollectionAsync();
        }

        private async Task ApplyFiltersAsync()
        {
            await FilterModelsByProjectAsync();
        }

        private async Task ApplyFileFilterToCurrentCollectionAsync()
        {
            if (string.IsNullOrEmpty(SelectedFileFilter))
                return;
            
            var currentModels = Models.ToList();
            var filteredModels = currentModels.Where(m => 
                string.Equals(m.FileType, SelectedFileFilter, StringComparison.OrdinalIgnoreCase)
            ).ToList();
            
            Models.Clear();
            foreach (var model in filteredModels)
            {
                Models.Add(model);
            }
        }

        private async Task RefreshCurrentViewAsync(string? oldProjectId, string? newProjectId)
        {
            if (SelectedProject != null)
            {
                if (SelectedProject.Id == oldProjectId || SelectedProject.Id == newProjectId)
                {
                    await FilterModelsByProjectAsync();
                }
            }
            else
            {
                var allModels = await _databaseService.GetAllModelsAsync();
                
                foreach (var dbModel in allModels)
                {
                    var existingModel = Models.FirstOrDefault(m => m.Id == dbModel.Id);
                    if (existingModel != null)
                    {
                        existingModel.ProjectId = dbModel.ProjectId;
                        
                        // Update project name
                        if (!string.IsNullOrEmpty(existingModel.ProjectId))
                        {
                            var project = Projects.FirstOrDefault(p => p.Id == existingModel.ProjectId);
                            existingModel.ProjectName = project?.Name;
                        }
                        else
                        {
                            existingModel.ProjectName = null;
                        }
                        
                        var index = Models.IndexOf(existingModel);
                        Models.RemoveAt(index);
                        Models.Insert(index, existingModel);
                    }
                }
            }
        }

        public async Task RefreshDataAsync()
        {
            try
            {
                var selectedModelId = SelectedModel?.Id;
                var selectedProjectId = SelectedProject?.Id;
                
                var freshProjects = await _databaseService.GetAllProjectsAsync();
                
                foreach (var freshProject in freshProjects)
                {
                    var existing = Projects.FirstOrDefault(p => p.Id == freshProject.Id);
                    if (existing != null)
                    {
                        existing.Name = freshProject.Name;
                        existing.Description = freshProject.Description;
                        existing.ModifiedDate = freshProject.ModifiedDate;
                        existing.Color = freshProject.Color;
                        existing.ModelIds.Clear();
                        foreach (var id in freshProject.ModelIds)
                        {
                            existing.ModelIds.Add(id);
                        }
                    }
                    else
                    {
                        Projects.Add(freshProject);
                    }
                }
                
                var projectsToRemove = Projects.Where(p => !freshProjects.Any(fp => fp.Id == p.Id)).ToList();
                foreach (var project in projectsToRemove)
                {
                    Projects.Remove(project);
                }
                
                var freshModels = await _databaseService.GetAllModelsAsync();
                
                foreach (var freshModel in freshModels)
                {
                    var existing = Models.FirstOrDefault(m => m.Id == freshModel.Id);
                    if (existing != null)
                    {
                        existing.Name = freshModel.Name;
                        existing.FilePath = freshModel.FilePath;
                        existing.FileType = freshModel.FileType;
                        existing.UploadedDate = freshModel.UploadedDate;
                        existing.FileSize = freshModel.FileSize;
                        existing.ThumbnailData = freshModel.ThumbnailData;
                        existing.ProjectId = freshModel.ProjectId;
                        
                        existing.Tags.Clear();
                        foreach (var tag in freshModel.Tags)
                        {
                            existing.Tags.Add(tag);
                        }
                    }
                    else
                    {
                        Models.Add(freshModel);
                    }
                }
                
                var modelsToRemove = Models.Where(m => !freshModels.Any(fm => fm.Id == m.Id)).ToList();
                foreach (var model in modelsToRemove)
                {
                    Models.Remove(model);
                }
                
                // Update all project names after refreshing
                UpdateAllProjectNames();
                
                AllTags.Clear();
                foreach (var model in Models)
                {
                    foreach (var tag in model.Tags)
                    {
                        if (!AllTags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                        {
                            AllTags.Add(tag);
                        }
                    }
                }
                
                if (selectedModelId != null)
                {
                    SelectedModel = Models.FirstOrDefault(m => m.Id == selectedModelId);
                }
                
                if (selectedProjectId != null)
                {
                    SelectedProject = Projects.FirstOrDefault(p => p.Id == selectedProjectId);
                }
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Error", $"Failed to refresh data: {ex.Message}");
            }
        }

        private async Task ResetDatabaseAsync()
        {
            var confirm = await Shell.Current?.CurrentPage?.DisplayAlert(
                "Reset Database",
                "This will DELETE ALL models and projects. This action cannot be undone.\n\nAre you absolutely sure?",
                "Yes, Reset Everything",
                "Cancel"
            )!;

            if (!confirm)
                return;

            var doubleConfirm = await Shell.Current?.CurrentPage?.DisplayAlert(
                "Final Confirmation",
                "This is your last chance. All data will be permanently deleted.",
                "DELETE ALL DATA",
                "Cancel"
            )!;

            if (!doubleConfirm)
                return;

            try
            {
                IsLoading = true;
                
                SelectedModel = null;
                SelectedProject = null;
                
                Models.Clear();
                Projects.Clear();
                AllTags.Clear();
                
                await _databaseService.ResetDatabaseAsync();
                
                await ShowAlertAsync("Success", "Database has been reset. All data has been cleared.");
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Error", $"Failed to reset database: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Helpers

        private Task ShowAlertAsync(string title, string message)
        {
            if (Shell.Current?.CurrentPage != null)
            {
                return Shell.Current.CurrentPage.DisplayAlert(title, message, "OK");
            }
            return Task.CompletedTask;
        }

        #endregion

        #region INotifyPropertyChanged

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

        #endregion
    }
}
