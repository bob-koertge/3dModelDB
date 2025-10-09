using MauiApp3.Models;
using MauiApp3.ViewModels;
using MauiApp3.Services;
using CommunityToolkit.Mvvm.Messaging;

namespace MauiApp3.Pages
{
    [QueryProperty(nameof(Model), "Model")]
    public partial class ModelDetailPage : ContentPage
    {
        private readonly DatabaseService? _databaseService;
        private readonly Model3DService? _modelService;
        private ModelDetailViewModel? ViewModel => BindingContext as ModelDetailViewModel;

        private Model3DFile? _model;
        public Model3DFile? Model
        {
            get => _model;
            set
            {
                _model = value;
                if (value != null && BindingContext == null)
                {
                    InitializeViewModel(value);
                }
            }
        }

        // Constructor with DI
        public ModelDetailPage(DatabaseService databaseService, Model3DService modelService)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _modelService = modelService;
            
            System.Diagnostics.Debug.WriteLine($"ModelDetailPage: Constructor called with services - DB: {_databaseService != null}, Model3D: {_modelService != null}");
        }

        // Parameterless constructor fallback
        public ModelDetailPage()
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine($"ModelDetailPage: Parameterless constructor called");
        }

        private async void InitializeViewModel(Model3DFile model)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"InitializeViewModel: Starting for model '{model.Name}'");
                
                // Use injected services first, fallback to Handler if needed
                var databaseService = _databaseService ?? Handler?.MauiContext?.Services.GetService<DatabaseService>();
                var model3DService = _modelService ?? Handler?.MauiContext?.Services.GetService<Model3DService>();
                
                if (databaseService == null)
                {
                    System.Diagnostics.Debug.WriteLine("WARNING: DatabaseService not available in ModelDetailPage");
                }
                
                if (model3DService == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Model3DService not available - 3D rendering will not work!");
                }
                
                BindingContext = new ModelDetailViewModel(model, databaseService);

                // Load the 3D model if not already loaded
                if (model.ParsedModel == null && !string.IsNullOrEmpty(model.FilePath) && File.Exists(model.FilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"InitializeViewModel: ParsedModel is null, loading from file: {model.FilePath}");
                    
                    if (model3DService != null)
                    {
                        try
                        {
                            model.ParsedModel = await model3DService.LoadModelAsync(model.FilePath);
                            System.Diagnostics.Debug.WriteLine($"InitializeViewModel: Loaded ParsedModel with {model.ParsedModel?.Triangles.Count ?? 0} triangles");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"InitializeViewModel: Failed to load model: {ex.Message}");
                            await DisplayAlert("Error", $"Failed to load 3D model: {ex.Message}", "OK");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"InitializeViewModel: Cannot load model - Model3DService is null");
                        await DisplayAlert("Error", "Model3DService is not available. 3D rendering will not work.", "OK");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"InitializeViewModel: ParsedModel already loaded with {model.ParsedModel?.Triangles.Count ?? 0} triangles");
                }

                // Load into viewer if available
                if (model.ParsedModel != null && Model3DViewer != null)
                {
                    System.Diagnostics.Debug.WriteLine($"InitializeViewModel: Loading model into 3D viewer");
                    Model3DViewer.LoadModel(model.ParsedModel);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"InitializeViewModel: Cannot load into viewer - ParsedModel: {model.ParsedModel != null}, Viewer: {Model3DViewer != null}");
                }

                // Subscribe to reset view message
                WeakReferenceMessenger.Default.Register<ResetViewMessage>(this, (r, m) =>
                {
                    if (Model3DViewer != null)
                    {
                        Model3DViewer.ResetView();
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing ViewModel: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            System.Diagnostics.Debug.WriteLine($"ModelDetailPage: OnAppearing - Model: {_model?.Name}");
            
            // Reload 3D model if needed
            if (_model?.ParsedModel != null && Model3DViewer != null)
            {
                System.Diagnostics.Debug.WriteLine($"ModelDetailPage: OnAppearing - Reloading model into viewer");
                Model3DViewer.LoadModel(_model.ParsedModel);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ModelDetailPage: OnAppearing - Cannot reload - ParsedModel: {_model?.ParsedModel != null}, Viewer: {Model3DViewer != null}");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            WeakReferenceMessenger.Default.Unregister<ResetViewMessage>(this);
            
            // Notify that we're leaving - main page should refresh
            Console.WriteLine("ModelDetailPage: OnDisappearing - changes may have been made");
        }
    }
}
