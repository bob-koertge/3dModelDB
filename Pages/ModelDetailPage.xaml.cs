using MauiApp3.Models;
using MauiApp3.ViewModels;
using CommunityToolkit.Mvvm.Messaging;

namespace MauiApp3.Pages
{
    [QueryProperty(nameof(Model), "Model")]
    public partial class ModelDetailPage : ContentPage
    {
        private ModelDetailViewModel ViewModel => (ModelDetailViewModel)BindingContext;

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

        public ModelDetailPage()
        {
            InitializeComponent();
        }

        private void InitializeViewModel(Model3DFile model)
        {
            BindingContext = new ModelDetailViewModel(model);

            // Load the 3D model if available
            if (model.ParsedModel != null)
            {
                Model3DViewer.LoadModel(model.ParsedModel);
            }

            // Subscribe to reset view message
            WeakReferenceMessenger.Default.Register<ResetViewMessage>(this, (r, m) =>
            {
                Model3DViewer.ResetView();
            });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Reload 3D model if needed
            if (_model?.ParsedModel != null)
            {
                Model3DViewer.LoadModel(_model.ParsedModel);
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
