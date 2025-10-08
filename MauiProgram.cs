using Microsoft.Extensions.Logging;
using MauiApp3.Services;
using MauiApp3.ViewModels;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.Diagnostics;

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

#if DEBUG
            builder.Logging.AddDebug();
            
            // Enable all trace output
            Debug.WriteLine("=== MAUI APP STARTING ===");
            Debug.WriteLine($"Debug logging enabled");
            Trace.Listeners.Add(new TextWriterTraceListener(System.Console.Out));
            Trace.AutoFlush = true;
#endif

            return builder.Build();
        }
    }
}
