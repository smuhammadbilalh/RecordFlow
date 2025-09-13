using RecordFlow;
using Microsoft.Maui.ApplicationModel;
using RecordFlow.Services;
using System.Diagnostics;

namespace RecordFlow;

public partial class AppShell : Shell
{

    public AppShell()
    {
        InitializeComponent();

        StartRecording.BackgroundColor = Colors.LightGray;

        // Register routes for navigation - MUST be done before any navigation
        Routing.RegisterRoute("MainPage", typeof(MainPage));
        Routing.RegisterRoute("DeveloperPage", typeof(DeveloperPage));

        MyRecordings.BackgroundColor = Colors.Transparent;
        appVersion.Text = $"v{AppInfo.Current.VersionString}"; // Display app version in the UI
    }

    // Navigation with duplicate prevention
    private async void OnStartRecordingTapped(object sender, EventArgs e)
    {
        MyRecordings.BackgroundColor = Colors.Transparent;
        StartRecording.BackgroundColor = Colors.LightSkyBlue;
        DeveloperMenu.BackgroundColor = Colors.Transparent;

        try
        {
            // Close flyout first for smooth experience
            FlyoutIsPresented = false;

            // Check if we're already on the Record Screen page
            if (Current.CurrentState.Location.OriginalString.Contains("MainPage"))
            {
                return; // Already on this page, don't navigate
            }

            // Navigate to MainPage (Record Screen functionality)
            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation Error", "Unable to navigate to Record Screen page.", "OK");
        }
    }

    public async void OnMyRecordingsTapped(object sender, EventArgs e)
    {


        try
        {
            // Close flyout first for smooth experience
            FlyoutIsPresented = false;

            // Open RecordFlow gallery album/folder directly instead of navigating to MyRecordings page
            await OpenRecordFlowGalleryAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Unable to open RecordFlow recordings. Please check your gallery or file manager.", "OK");
        }
    }

    /// <summary>
    /// Opens the RecordFlow gallery album on Android or RecordFlow folder on Windows/other platforms
    /// </summary>
    private async Task OpenRecordFlowGalleryAsync()
    {
        try
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                // Try to open RecordFlow album in gallery
                await OpenAndroidRecordFlowAlbumAsync();
            }
            else if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                // Open RecordFlow folder in File Explorer
                await OpenWindowsRecordFlowFolderAsync();
            }
            else if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                // Open iOS Files app with RecordFlow folder
                await OpeniOSRecordFlowFolderAsync();
            }
            else
            {
                // Fallback for other platforms
                await OpenDefaultRecordFlowFolderAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening RecordFlow gallery: {ex.Message}");

            // Fallback: Show user a helpful message with manual instructions
            await ShowRecordFlowLocationInfoAsync();
        }
    }

    /// <summary>
    /// Opens RecordFlow album in Android gallery
    /// </summary>
    private async Task OpenAndroidRecordFlowAlbumAsync()
    {
        try
        {
#if ANDROID
            // Try to open gallery with RecordFlow album
            var intent = new Android.Content.Intent(Android.Content.Intent.ActionView);
            intent.SetType("video/*");

            // Try to open specific folder if possible
            var recordFlowPath = "/storage/emulated/0/Movies/RecordFlow";
            if (Directory.Exists(recordFlowPath))
            {
                var uri = Android.Net.Uri.FromFile(new Java.IO.File(recordFlowPath));
                intent.SetDataAndType(uri, "video/*");
            }

            // Add chooser for better user experience
            var chooser = Android.Content.Intent.CreateChooser(intent, "Open RecordFlow Recordings");

            var currentActivity = Platform.CurrentActivity ?? Android.App.Application.Context;
            if (currentActivity is Android.App.Activity activity)
            {
                activity.StartActivity(chooser);
            }
            else
            {
                // Fallback to context
                chooser.AddFlags(Android.Content.ActivityFlags.NewTask);
                currentActivity.StartActivity(chooser);
            }
#else
            // If not Android build, show fallback message
            await DisplayAlert("📱 RecordFlow Recordings",
                "Please open your Gallery app and look for the 'RecordFlow' album to view your recordings.",
                "✅ OK");
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening Android gallery: {ex.Message}");

            // Try alternative method - open gallery app directly
            try
            {
#if ANDROID
                await DisplayAlert("📱 RecordFlow Album",
                    "Gallery opening! Look for the 'RecordFlow' album to find your recordings.",
                    "✅ Got it");
                var galleryIntent = new Android.Content.Intent(Android.Content.Intent.ActionMain);
                galleryIntent.AddCategory(Android.Content.Intent.CategoryAppGallery);
                galleryIntent.AddFlags(Android.Content.ActivityFlags.NewTask);

                var currentActivity = Platform.CurrentActivity ?? Android.App.Application.Context;
                currentActivity.StartActivity(galleryIntent);
#endif
            }
            catch
            {
                // Final fallback
                await ShowRecordFlowLocationInfoAsync();
            }
        }
    }

    /// <summary>
    /// Opens RecordFlow folder in Windows File Explorer
    /// </summary>
    private async Task OpenWindowsRecordFlowFolderAsync()
    {
        try
        {
            var videosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            var recordFlowPath = Path.Combine(videosPath, "RecordFlow");

            // Ensure the folder exists
            if (!Directory.Exists(recordFlowPath))
            {
                Directory.CreateDirectory(recordFlowPath);
            }

            // Open folder in File Explorer
            var startInfo = new ProcessStartInfo
            {
                Arguments = recordFlowPath,
                FileName = "explorer.exe",
                UseShellExecute = true
            };

            Process.Start(startInfo);

            Debug.WriteLine($"Opened RecordFlow folder: {recordFlowPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening Windows folder: {ex.Message}");

            // Fallback: Try to open Videos folder
            try
            {
                var videosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                var startInfo = new ProcessStartInfo
                {
                    Arguments = videosPath,
                    FileName = "explorer.exe",
                    UseShellExecute = true
                };
                Process.Start(startInfo);

                await DisplayAlert("📁 RecordFlow Folder",
                    "Videos folder opened! Look for the 'RecordFlow' folder to find your recordings.",
                    "✅ OK");
            }
            catch
            {
                await ShowRecordFlowLocationInfoAsync();
            }
        }
    }

    /// <summary>
    /// Opens RecordFlow folder in iOS Files app
    /// </summary>
    private async Task OpeniOSRecordFlowFolderAsync()
    {
        try
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var recordFlowPath = Path.Combine(documentsPath, "RecordFlow");

            // Ensure the folder exists
            if (!Directory.Exists(recordFlowPath))
            {
                Directory.CreateDirectory(recordFlowPath);
            }

            // Try to open Files app (iOS doesn't have direct folder opening like Windows)
            var uri = new Uri("shareddocuments://");
            await Launcher.Default.OpenAsync(uri);

            await DisplayAlert("📁 RecordFlow Recordings",
                "Files app opened! Navigate to 'On My iPhone/iPad' → Your App → RecordFlow to find your recordings.",
                "✅ Got it");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening iOS Files app: {ex.Message}");

            await ShowRecordFlowLocationInfoAsync();
        }
    }

    /// <summary>
    /// Opens RecordFlow folder for other platforms
    /// </summary>
    private async Task OpenDefaultRecordFlowFolderAsync()
    {
        try
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var recordFlowPath = Path.Combine(documentsPath, "RecordFlow");

            // Ensure the folder exists
            if (!Directory.Exists(recordFlowPath))
            {
                Directory.CreateDirectory(recordFlowPath);
            }

            // Try to open with default file manager
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(recordFlowPath),
                Title = "📁 Open RecordFlow Folder"
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening default folder: {ex.Message}");

            await ShowRecordFlowLocationInfoAsync();
        }
    }

    /// <summary>
    /// Shows information about where to find RecordFlow recordings as a fallback
    /// </summary>
    private async Task ShowRecordFlowLocationInfoAsync()
    {
        string message;

        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            message = "📱 Find your RecordFlow recordings:\n\n" +
                     "1. Open your Gallery app\n" +
                     "2. Look for 'RecordFlow' album\n" +
                     "3. Or check: Internal Storage → Movies → RecordFlow\n\n" +
                     "📁 All your screen recordings are organized there!";
        }
        else if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            var videosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            message = "💻 Find your RecordFlow recordings:\n\n" +
                     $"📁 Location: {videosPath}\\RecordFlow\n\n" +
                     "💡 Tip: You can also search 'RecordFlow' in File Explorer to find the folder quickly!";
        }
        else if (DeviceInfo.Platform == DevicePlatform.iOS)
        {
            message = "📱 Find your RecordFlow recordings:\n\n" +
                     "1. Open Files app\n" +
                     "2. Go to 'On My iPhone/iPad'\n" +
                     "3. Find your app folder\n" +
                     "4. Look for 'RecordFlow' folder\n\n" +
                     "📁 All your recordings are saved there!";
        }
        else
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            message = "📁 Find your RecordFlow recordings:\n\n" +
                     $"Location: {documentsPath}/RecordFlow\n\n" +
                     "💡 Open your file manager and navigate to this location.";
        }

        await DisplayAlert("📁 RecordFlow Recordings Location", message, "✅ Got it");
    }

    private async void OnDeveloperTapped(object sender, EventArgs e)
    {
        StartRecording.BackgroundColor = Colors.Transparent;
        MyRecordings.BackgroundColor = Colors.Transparent;
        DeveloperMenu.BackgroundColor = Colors.LightSkyBlue;

        try
        {
            // Close flyout first for smooth experience
            FlyoutIsPresented = false;

            // Check if we're already on the Developer page
            if (Current.CurrentState.Location.OriginalString.Contains("DeveloperPage"))
            {
                return; // Already on this page, don't navigate
            }

            // Navigate to DeveloperPage
            await Shell.Current.GoToAsync("//DeveloperPage");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation Error", "Unable to navigate to Developer tools.", "OK");
        }
    }

    private async void OnDemoClicked(object sender, EventArgs e)
    {
        try
        {
            // Replace with your actual demo video URL for RecordFlow
            string demoVideoUrl = "https://drive.google.com/file/d/1DA-Eqn1lC2Ua0IMkdsBuV5z9G2SnlyJq/view?usp=drive_link";
            await Browser.Default.OpenAsync(demoVideoUrl, BrowserLaunchMode.SystemPreferred);

            // Close flyout smoothly
            FlyoutIsPresented = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Unable to open demo video. Please check your internet connection.", "OK");
        }
    }

    private async void OnUpdateClicked(object sender, EventArgs e)
    {
        try
        {
            // Close flyout first for smooth experience
            FlyoutIsPresented = false;

            // Get the App instance and call manual update check
            if (Application.Current is App app)
            {
                await app.CheckForUpdatesManually();
            }
            else
            {
                await DisplayAlert("Error", "Unable to check for updates at this time.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Update Error", "Failed to check for updates. Please try again later.", "OK");
        }
    }

    private async void OnAboutClicked(object sender, EventArgs e)
    {
        FlyoutIsPresented = false;

        var aboutInfo = new
        {
            Name = AppInfo.Current.Name,
            Version = AppInfo.Current.VersionString,
            Build = AppInfo.Current.BuildString,
            Developer = "Muhammad Bilal",
            Company = "RecordFlow Solutions",
            Email = "bilal@recordflow.com"
        };

        string message = $"📱 {aboutInfo.Name}\n" +
                        $"🏷️ Version {aboutInfo.Version} (Build {aboutInfo.Build})\n\n" +
                        $"👨‍💻 Developed by {aboutInfo.Developer}\n" +
                        $"🏢 {aboutInfo.Company}\n" +
                        $"📧 smuhammadbilalh@gmail.com\n\n" +
                        $"🎥 Professional Screen Recording Made Simple\n" +
                        $"© 2025 All Rights Reserved";

        await DisplayAlert("About RecordFlow", message, "Close");
    }
}