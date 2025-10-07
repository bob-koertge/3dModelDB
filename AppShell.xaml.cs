using MauiApp3.Pages;

namespace MauiApp3
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            
            // Register routes for navigation
            Routing.RegisterRoute(nameof(ModelDetailPage), typeof(ModelDetailPage));
        }
    }
}
