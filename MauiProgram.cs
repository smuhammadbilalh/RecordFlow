using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Plugin.Maui.ScreenRecording;
using RecordFlow.Services; // Add this for our GoogleAnalyticsService

namespace RecordFlow;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseScreenRecording()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            }); 

         builder.UseMauiCommunityToolkit(options =>
          {
              options.SetShouldEnableSnackbarOnWindows(true);
          });

        // Register services
        builder.Services.AddSingleton<MainPage>();
        // Register the screen recording service
        builder.Services.AddSingleton<IScreenRecording>(ScreenRecording.Default);
        // Register our HTTP-based Analytics service

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}