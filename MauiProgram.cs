using Microsoft.Extensions.Logging;
using MauiApp3.Services;
using MauiApp3.ViewModels;
using MauiApp3.Pages;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace MauiApp3
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register services
            builder.Services.AddSingleton<Model3DService>();
            builder.Services.AddSingleton<DatabaseService>();
            
            // Register ViewModels
            builder.Services.AddTransient<MainViewModel>();
            
            // Register Pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<ModelDetailPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
