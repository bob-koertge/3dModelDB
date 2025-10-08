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
        private string? _selectedFileFilter; // null = All, "STL", "3MF"

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
            _model3DService = model3DService;
            _databaseService = databaseService;
            
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

        private async Task SelectModelAsync(Model3DFile? model)
        {
            if (model == null)
                return;

            Console.WriteLine($"SelectModelAsync: Selecting model {model.Name}");
            
            // If ParsedModel is null (loaded from database), load it now
            if (model.ParsedModel == null && !string.IsNullOrEmpty(model.FilePath) && File.Exists(model.FilePath))
            {
                Console.WriteLine($"SelectModelAsync: ParsedModel is null, loading from file: {model.FilePath}");
                try
                {
                    // Load the ParsedModel BEFORE setting SelectedModel
                    model.ParsedModel = await _model3DService.LoadModelAsync(model.FilePath);
                    Console.WriteLine($"SelectModelAsync: Loaded ParsedModel with {model.ParsedModel?.Triangles.Count ?? 0} triangles");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SelectModelAsync: Failed to load ParsedModel: {ex.Message}");
                    await ShowAlertAsync("Error", $"Failed to load 3D model: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"SelectModelAsync: ParsedModel already loaded with {model.ParsedModel?.Triangles.Count ?? 0} triangles");
            }
            
            // Now set SelectedModel - this will trigger PropertyChanged with ParsedModel already loaded
            Console.WriteLine($"SelectModelAsync: Setting SelectedModel");
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

                // Load projects first
                var savedProjects = await _databaseService.GetAllProjectsAsync();
                foreach (var project in savedProjects)
                {
                    Projects.Add(project);
                }

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

                // Start with empty library - no sample data
                Console.WriteLine($"Loaded {Models.Count} models from database");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data from database: {ex.Message}");
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
                Description = "",
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

            // Refresh models view
            await FilterModelsByProjectAsync();
        }

        private void SelectProject(Project? project)
        {
            SelectedProject = project;
        }

        private async Task AddModelToProject(Model3DFile? model)
        {
            if (model == null || Projects.Count == 0)
                return;

            var projectNames = Projects.Select(p => p.Name).ToArray();
            var selectedProjectName = await Shell.Current?.CurrentPage?.DisplayActionSheet(
                "Add to Project",
                "Cancel",
                null,
                projectNames
            )!;

            if (string.IsNullOrEmpty(selectedProjectName) || selectedProjectName == "Cancel")
                return;

            var project = Projects.FirstOrDefault(p => p.Name == selectedProjectName);
            if (project == null)
                return;

            // Store old ProjectId for comparison
            var oldProjectId = model.ProjectId;
            
            model.ProjectId = project.Id;
            await SaveModelToDatabaseAsync(model);
            await _databaseService.AssignModelToProjectAsync(model.Id, project.Id);

            if (!project.ModelIds.Contains(model.Id))
            {
                project.ModelIds.Add(model.Id);
                await _databaseService.SaveProjectAsync(project);
            }

            // Refresh the view to show updated project badge
            await RefreshCurrentViewAsync(oldProjectId, project.Id);
            
            // Trigger UI update for right panel buttons
            if (model == SelectedModel)
            {
                OnPropertyChanged(nameof(SelectedModel));
            }

            await ShowAlertAsync("Success", $"Added '{model.Name}' to project '{project.Name}'");
        }

        private async Task RemoveModelFromProject(Model3DFile? model)
        {
            if (model == null || string.IsNullOrEmpty(model.ProjectId))
                return;

            var oldProjectId = model.ProjectId;
            var project = Projects.FirstOrDefault(p => p.Id == model.ProjectId);
            
            Console.WriteLine($"=== REMOVE FROM PROJECT START ===");
            Console.WriteLine($"RemoveModelFromProject: Model to remove: {model.Name} (ID: {model.Id})");
            Console.WriteLine($"RemoveModelFromProject: Project: {project?.Name ?? "Unknown"}");
            Console.WriteLine($"RemoveModelFromProject: Current SelectedProject: {SelectedProject?.Name ?? "None"} (ID: {SelectedProject?.Id ?? "null"})");
            Console.WriteLine($"RemoveModelFromProject: Models.Count before removal: {Models.Count}");
            
            var confirm = await Shell.Current?.CurrentPage?.DisplayAlert(
                "Remove from Project",
                $"Remove '{model.Name}' from project '{project?.Name ?? "Unknown"}'?",
                "Remove",
                "Cancel"
            )!;

            if (!confirm)
            {
                Console.WriteLine($"RemoveModelFromProject: User cancelled");
                return;
            }

            // Store whether we're viewing this project
            bool isViewingAffectedProject = SelectedProject?.Id == oldProjectId;
            Console.WriteLine($"RemoveModelFromProject: isViewingAffectedProject = {isViewingAffectedProject}");

            // Find the actual model instance in the collection
            var modelInCollection = Models.FirstOrDefault(m => m.Id == model.Id);
            Console.WriteLine($"RemoveModelFromProject: Model found in collection: {modelInCollection != null}");
            Console.WriteLine($"RemoveModelFromProject: Same instance: {ReferenceEquals(model, modelInCollection)}");

            // If this model is currently selected and we're viewing the project it's being removed from,
            // clear the selection to avoid issues
            if (SelectedModel?.Id == model.Id && isViewingAffectedProject)
            {
                Console.WriteLine($"RemoveModelFromProject: Clearing selection as model is being removed from current view");
                SelectedModel = null;
            }

            // Update the model's ProjectId
            model.ProjectId = null;
            if (modelInCollection != null)
            {
                modelInCollection.ProjectId = null;
            }
            
            await SaveModelToDatabaseAsync(model);
            await _databaseService.RemoveModelFromProjectAsync(model.Id);

            if (project != null && project.ModelIds.Contains(model.Id))
            {
                project.ModelIds.Remove(model.Id);
                await _databaseService.SaveProjectAsync(project);
            }

            Console.WriteLine($"RemoveModelFromProject: Database updates complete");
            
            // If we're viewing the project the model was removed from, remove it directly from the collection
            if (isViewingAffectedProject && modelInCollection != null)
            {
                Console.WriteLine($"RemoveModelFromProject: Attempting to remove model from Models collection...");
                Console.WriteLine($"RemoveModelFromProject: Collection contains model: {Models.Contains(modelInCollection)}");
                
                bool removed = Models.Remove(modelInCollection);
                Console.WriteLine($"RemoveModelFromProject: Models.Remove() returned: {removed}");
                Console.WriteLine($"RemoveModelFromProject: Models.Count after removal: {Models.Count}");
                
                // Force a collection changed notification just in case
                OnPropertyChanged(nameof(Models));
            }
            else if (!isViewingAffectedProject)
            {
                // If viewing all models, just update the ProjectId in the collection
                Console.WriteLine($"RemoveModelFromProject: Not viewing affected project, updating ProjectId only");
                await RefreshCurrentViewAsync(oldProjectId, null);
            }
            else
            {
                Console.WriteLine($"RemoveModelFromProject: WARNING - modelInCollection is null!");
            }
            
            // Trigger UI update for right panel buttons if this was the selected model
            if (model == SelectedModel)
            {
                OnPropertyChanged(nameof(SelectedModel));
            }

            Console.WriteLine($"=== REMOVE FROM PROJECT END ===");
            await ShowAlertAsync("Success", "Model removed from project");
        }

        private void ToggleProjectView()
        {
            ShowProjectView = !ShowProjectView;
        }

        private async Task FilterModelsByProjectAsync()
        {
            if (SelectedProject == null)
            {
                // Show all models
                var allModels = await _databaseService.GetAllModelsAsync();
                Models.Clear();
                foreach (var model in allModels)
                {
                    Models.Add(model);
                }
            }
            else
            {
                // Show only models in selected project
                var projectModels = await _databaseService.GetModelsByProjectIdAsync(SelectedProject.Id);
                Models.Clear();
                foreach (var model in projectModels)
                {
                    Models.Add(model);
                }
            }
            
            // Apply file type filter if one is selected
            await ApplyFileFilterToCurrentCollectionAsync();
        }

        /// <summary>
        /// Apply both project and file type filters
        /// </summary>
        private async Task ApplyFiltersAsync()
        {
            Console.WriteLine($"ApplyFiltersAsync: FileFilter={SelectedFileFilter}, Project={SelectedProject?.Name ?? "All"}");
            
            // Start with project filter (or all models)
            await FilterModelsByProjectAsync();
        }

        /// <summary>
        /// Apply file type filter to the current Models collection
        /// </summary>
        private async Task ApplyFileFilterToCurrentCollectionAsync()
        {
            if (string.IsNullOrEmpty(SelectedFileFilter))
            {
                // No file filter, already showing all files from project filter
                return;
            }
            
            Console.WriteLine($"ApplyFileFilterToCurrentCollectionAsync: Filtering by {SelectedFileFilter}");
            
            // Get the current models (could be from a project or all models)
            var currentModels = Models.ToList();
            
            // Filter by file type
            var filteredModels = currentModels.Where(m => 
                string.Equals(m.FileType, SelectedFileFilter, StringComparison.OrdinalIgnoreCase)
            ).ToList();
            
            // Update collection
            Models.Clear();
            foreach (var model in filteredModels)
            {
                Models.Add(model);
            }
            
            Console.WriteLine($"ApplyFileFilterToCurrentCollectionAsync: Showing {Models.Count} models");
        }

        /// <summary>
        /// Refreshes the current view to reflect changes in model-project assignments
        /// </summary>
        private async Task RefreshCurrentViewAsync(string? oldProjectId, string? newProjectId)
        {
            Console.WriteLine($"RefreshCurrentViewAsync: oldProjectId={oldProjectId}, newProjectId={newProjectId}");
            Console.WriteLine($"RefreshCurrentViewAsync: SelectedProject={(SelectedProject?.Id ?? "null")}");
            
            // If we're viewing a specific project that was affected, refresh the filter
            if (SelectedProject != null)
            {
                Console.WriteLine($"RefreshCurrentViewAsync: Checking if project is affected...");
                // If the change affects the currently selected project, re-filter
                if (SelectedProject.Id == oldProjectId || SelectedProject.Id == newProjectId)
                {
                    Console.WriteLine($"RefreshCurrentViewAsync: Project IS affected, re-filtering...");
                    Console.WriteLine($"RefreshCurrentViewAsync: Models.Count before filter: {Models.Count}");
                    await FilterModelsByProjectAsync();
                    Console.WriteLine($"RefreshCurrentViewAsync: Models.Count after filter: {Models.Count}");
                }
                else
                {
                    Console.WriteLine($"RefreshCurrentViewAsync: Project NOT affected, no refresh needed");
                }
            }
            else
            {
                Console.WriteLine($"RefreshCurrentViewAsync: Viewing all models, updating ProjectId values...");
                // If viewing all models, we need to update the specific model's ProjectId
                // without losing its ParsedModel and ThumbnailData
                
                var allModels = await _databaseService.GetAllModelsAsync();
                
                // Update each model in the collection with fresh database data
                foreach (var dbModel in allModels)
                {
                    var existingModel = Models.FirstOrDefault(m => m.Id == dbModel.Id);
                    if (existingModel != null)
                    {
                        // Update only the ProjectId, preserve other in-memory data
                        existingModel.ProjectId = dbModel.ProjectId;
                        
                        // Force UI update by removing and re-adding
                        var index = Models.IndexOf(existingModel);
                        Models.RemoveAt(index);
                        Models.Insert(index, existingModel);
                    }
                }
                Console.WriteLine($"RefreshCurrentViewAsync: Updated ProjectId values for all models");
            }
        }

        #endregion

        /// <summary>
        /// Refresh all data from the database - useful when returning from detail views
        /// </summary>
        public async Task RefreshDataAsync()
        {
            Console.WriteLine("MainViewModel: RefreshDataAsync called");
            
            try
            {
                // Store current selections
                var selectedModelId = SelectedModel?.Id;
                var selectedProjectId = SelectedProject?.Id;
                
                // Reload projects
                var freshProjects = await _databaseService.GetAllProjectsAsync();
                
                // Update existing projects or add new ones
                foreach (var freshProject in freshProjects)
                {
                    var existing = Projects.FirstOrDefault(p => p.Id == freshProject.Id);
                    if (existing != null)
                    {
                        // Update existing project properties
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
                
                // Remove deleted projects
                var projectsToRemove = Projects.Where(p => !freshProjects.Any(fp => fp.Id == p.Id)).ToList();
                foreach (var project in projectsToRemove)
                {
                    Projects.Remove(project);
                }
                
                // Reload models
                var freshModels = await _databaseService.GetAllModelsAsync();
                
                // Update existing models or add new ones
                foreach (var freshModel in freshModels)
                {
                    var existing = Models.FirstOrDefault(m => m.Id == freshModel.Id);
                    if (existing != null)
                    {
                        // Update existing model properties (preserve ParsedModel)
                        existing.Name = freshModel.Name;
                        existing.FilePath = freshModel.FilePath;
                        existing.FileType = freshModel.FileType;
                        existing.UploadedDate = freshModel.UploadedDate;
                        existing.FileSize = freshModel.FileSize;
                        existing.ThumbnailData = freshModel.ThumbnailData;
                        existing.ProjectId = freshModel.ProjectId;
                        
                        // Update tags
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
                
                // Remove deleted models
                var modelsToRemove = Models.Where(m => !freshModels.Any(fm => fm.Id == m.Id)).ToList();
                foreach (var model in modelsToRemove)
                {
                    Models.Remove(model);
                }
                
                // Rebuild tags
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
                
                // Restore selections if they still exist
                if (selectedModelId != null)
                {
                    SelectedModel = Models.FirstOrDefault(m => m.Id == selectedModelId);
                }
                
                if (selectedProjectId != null)
                {
                    SelectedProject = Projects.FirstOrDefault(p => p.Id == selectedProjectId);
                }
                
                Console.WriteLine($"MainViewModel: RefreshDataAsync complete - {Models.Count} models, {Projects.Count} projects");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MainViewModel: Error refreshing data: {ex.Message}");
            }
        }

        /// <summary>
        /// Reset the database - clear all data and reload
        /// </summary>
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

            // Double confirmation for safety
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
                
                Console.WriteLine("=== RESET DATABASE START ===");
                
                // Clear selections
                SelectedModel = null;
                SelectedProject = null;
                
                // Clear collections
                Models.Clear();
                Projects.Clear();
                AllTags.Clear();
                
                // Reset database
                await _databaseService.ResetDatabaseAsync();
                
                Console.WriteLine("=== RESET DATABASE COMPLETE ===");
                
                await ShowAlertAsync("Success", "Database has been reset. All data has been cleared.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR resetting database: {ex.Message}");
                await ShowAlertAsync("Error", $"Failed to reset database: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
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
