using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Plugin.Maui.ScreenRecording;
using RecordFlow.Helpers;
using RecordFlow.Services;
using System.Diagnostics;

namespace RecordFlow;

public partial class MainPage : ContentPage
{
    private string _desiredRecordingName; // Add this field
    private readonly IScreenRecording _screenRecording;
    private bool _isRecording = false;
    private DateTime _recordingStartTime;
    private System.Timers.Timer _timer;
    private ImageSource _originalImageSource;

    public MainPage()
    {
        InitializeComponent();
        _screenRecording = ScreenRecording.Default;
        InitializeUI();
        SetupTimer();
    }




    private async void InitializeUI()
    {
        // Show Android audio limitations on first load (only for Android 10+)
        if (DeviceInfo.Platform == DevicePlatform.Android && IsAndroidVersionAffected())
        {
            await ShowAndroidAudioInfoAsync();
        }

        // Update UI based on current state
        UpdateUI();
    }

    private void SetupTimer()
    {
        _timer = new System.Timers.Timer(1000); // Update every second
        _timer.Elapsed += OnTimerElapsed;
    }

    private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        if (_isRecording)
        {
            var elapsed = DateTime.Now - _recordingStartTime;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TimerLabel.Text = elapsed.ToString(@"hh\:mm\:ss");
            });
        }
    }

    #region Event Handlers

    private async void OnStartRecordingClicked(object sender, EventArgs e)
    {
        try
        {
            // Check and request permissions first
            var permissionsGranted = await PermissionsHelper.CheckScreenRecordingPermissionsAsync();
            if (!permissionsGranted)
            {
                var shouldRequestPermissions = await PermissionsHelper.ShowPermissionRationaleAsync(this);
                if (shouldRequestPermissions)
                {
                    permissionsGranted = await PermissionsHelper.RequestScreenRecordingPermissionsAsync();
                }

                if (!permissionsGranted)
                {
                    await DisplayAlert("Permission Required",
                        "Screen recording permissions are required to use this feature. Please grant permissions in the app settings.",
                        "OK");
                    return;
                }
            }

            // Check if screen recording is available on this platform
            if (!_screenRecording.IsSupported)
            {
                await DisplayAlert("Not Supported",
                    "Screen recording is not supported on this device or platform.",
                    "OK");
                return;
            }

            // Get recording name from user
            var recordingName = await GetRecordingNameFromUserAsync();
            if (string.IsNullOrEmpty(recordingName))
            {
                // User cancelled or didn't provide a name
                return;
            }

            // Store the desired name for later use
            _desiredRecordingName = recordingName;

            // Show countdown in UI
            StatusLabel.Text = "Preparing to record...";
            await ShowUICountdownAsync();

            // Start actual recording without custom file path
            await StartRecordingAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error starting recording: {ex.Message}");
            StatusLabel.Text = "Error occurred";
            await DisplayAlert("Error",
                $"An error occurred while starting recording: {ex.Message}",
                "OK");
        }
    }


    private async Task<string> GetRecordingNameFromUserAsync()
    {
        string recordingName = null;
        bool isValidName = false;

        while (!isValidName)
        {
            // Show input dialog to get recording name
            recordingName = await DisplayPromptAsync(
                "🎬 Recording Name",
                "Enter a name for your screen recording:",
                "Start Recording",
                "Cancel",
                placeholder: $"Recording_{DateTime.Now:yyyyMMdd_HHmmss}",
                maxLength: 100);

            // User cancelled
            if (string.IsNullOrEmpty(recordingName))
            {
                return null;
            }

            // Validate the name
            var validationResult = SaveHelper.ValidateRecordingName(recordingName);
            if (validationResult.IsValid)
            {
                isValidName = true;
            }
            else
            {
                // Show error and ask again
                var tryAgain = await DisplayAlert("❌ Invalid Name",
                    validationResult.ErrorMessage + "\n\nWould you like to try again?",
                    "✅ Try Again",
                    "❌ Cancel");

                if (!tryAgain)
                {
                    return null;
                }
            }
        }

        return recordingName.Trim();
    }

    private async void OnStopRecordingClicked(object sender, EventArgs e)
    {
        await StopRecordingAsync();
    }

    private void OnSettingsClicked(object sender, EventArgs e)
    {
        try
        {
            PermissionsHelper.OpenAppSettings();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening settings: {ex.Message}");
            DisplayAlert("Error",
                "Unable to open app settings. Please open settings manually from your device.",
                "OK");
        }
    }

    private async void OnMoreOptionsClicked(object sender, EventArgs e)
    {
        try
        {
            var action = await DisplayActionSheet(
                "More Options",
                "Cancel",
                null,
                "🔑 Check Permissions",
                "🎵 Audio Info",
                "💡 Quick Tips",
                "📊 App Info"
                );

            switch (action)
            {
             
                case "🔑 Check Permissions":
                    await OnCheckPermissionsClickedAsync();
                    break;
                case "🎵 Audio Info":
                    await OnAudioInfoClickedAsync();
                    break;
                case "💡 Quick Tips":
                    await OnQuickTipsClickedAsync();
                    break;
                case "📊 App Info":
                    await OnAppInfoClickedAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in more options: {ex.Message}");
            await DisplayAlert("Error", "An error occurred while opening options.", "OK");
        }
    }

    private async Task OnQuickTipsClickedAsync()
    {
        await DisplayAlert("💡 Quick Tips",
            "🎬 Professional Recording Tips:\n\n" +
            "• 💾 Ensure sufficient storage space before recording\n\n" +
            "• 🚀 Close unnecessary apps for optimal performance\n\n" +
            "• 📱 Recordings are automatically saved to your gallery in 'RecordFlow' album\n\n" +
            "• 🎯 Video quality matches your device's screen resolution\n\n" +
            "• 🔋 Keep your device charged for longer recordings",
            "✅ Got it!");
    }

    #endregion

    #region Recording Methods

    private async Task ShowUICountdownAsync()
    {
        for (int i = 3; i > 0; i--)
        {
            StatusLabel.Text = $"Recording starting in {i}...";
            await Task.Delay(1000);
        }
    }


    private async Task StartRecordingAsync()
    {
        try
        {
            // Configure recording options
            var options = new ScreenRecordingOptions
            {
                EnableMicrophone = AudioSwitch.IsToggled,
                SaveToGallery = true
                // Don't set SavePath - let the plugin use its default naming
            };

            // Add Android-specific configuration
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                // Force microphone only for Android
                options.EnableMicrophone = AudioSwitch.IsToggled;
                // Note: System audio will not work regardless of settings
                // Gallery album organization will be handled after recording completion
            }

            // Start recording
            StatusLabel.Text = "Starting recording...";

            _screenRecording.StartRecording(options);

            _isRecording = true;
            _recordingStartTime = DateTime.Now;

            _timer.Start();
            UpdateUI();

            StatusLabel.Text = "Recording in progress...";

            // Show recording started notification
            await NotificationHelper.ShowRecordingStartedToastAsync();

            // Show recording in progress snackbar with stop action
            await NotificationHelper.ShowRecordingInProgressSnackbarAsync(
                stopAction: async () => await StopRecordingAsync(),
                viewAction: () => {
                    // Bring app to foreground if minimized
                    MainThread.BeginInvokeOnMainThread(() => {
                        if (Application.Current?.MainPage != null)
                        {
                            // Focus on the main page
                            Application.Current.MainPage.Focus();
                        }
                    });
                }
            );

            var recordingType = AudioSwitch.IsToggled ? "with microphone audio" : "video only";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in StartRecordingAsync: {ex.Message}");
            _isRecording = false;
            _timer?.Stop();
            UpdateUI();

            // Show error notification
            await NotificationHelper.ShowErrorAsync("Failed to start recording");
            throw;
        }
    }

    private async Task StopRecordingAsync()
    {
        try
        {
            if (!_isRecording)
            {
                await NotificationHelper.ShowWarningAsync("No recording is currently active");
                return;
            }

            StatusLabel.Text = "Stopping recording...";

            var result = await _screenRecording.StopRecording();

            _isRecording = false;
            _timer.Stop();

            UpdateUI();
            TimerLabel.Text = "00:00:00";

            // Show recording stopped notification
            await NotificationHelper.ShowRecordingStoppedToastAsync();

            var originalFilePath = SaveHelper.GetFilePathFromResult(result);
            string finalFilePath = originalFilePath;
            string finalFileName = null;

            // Rename the file if we have a desired name
            if (!string.IsNullOrEmpty(_desiredRecordingName) && !string.IsNullOrEmpty(originalFilePath))
            {
                try
                {
                    var directory = Path.GetDirectoryName(originalFilePath);
                    var extension = Path.GetExtension(originalFilePath);
                    var newFileName = $"{_desiredRecordingName}{extension}";
                    var newFilePath = Path.Combine(directory, newFileName);

                    // Handle duplicate names
                    int counter = 1;
                    while (File.Exists(newFilePath))
                    {
                        newFileName = $"{_desiredRecordingName}_{counter}{extension}";
                        newFilePath = Path.Combine(directory, newFileName);
                        counter++;
                    }

                    // Rename the file
                    File.Move(originalFilePath, newFilePath);
                    finalFilePath = newFilePath;
                    finalFileName = Path.GetFileNameWithoutExtension(newFileName);

                    StatusLabel.Text = $"Recording saved as '{finalFileName}'";
                }
                catch (Exception renameEx)
                {
                    Debug.WriteLine($"Failed to rename file: {renameEx.Message}");
                    StatusLabel.Text = "Recording saved (rename failed)";
                    finalFilePath = originalFilePath; // Use original path if rename fails
                }
            }
            else
            {
                StatusLabel.Text = "Recording saved successfully";
                if (!string.IsNullOrEmpty(finalFilePath))
                {
                    finalFileName = Path.GetFileNameWithoutExtension(finalFilePath);
                }
            }

            // Clear the desired name
            _desiredRecordingName = null;

            // Platform-specific organization
            if (DeviceInfo.Platform == DevicePlatform.Android && result != null)
            {
                await SaveHelper.OrganizeRecordingInGalleryAsync(finalFilePath);
            }
            else if (DeviceInfo.Platform == DevicePlatform.WinUI && !string.IsNullOrEmpty(finalFilePath))
            {
                // Move to RecordFlow folder if not already there
                finalFilePath = await SaveHelper.OrganizeRecordingForWindowsAsync(finalFilePath);
            }

            // Show recording saved snackbar with "View Recording" action
            if (!string.IsNullOrEmpty(finalFilePath))
            {
                await NotificationHelper.ShowRecordingSavedSnackbarAsync(finalFilePath, finalFileName);
            }
            else
            {
                // Fallback to simple success notification
                await NotificationHelper.ShowSuccessAsync("Recording saved successfully!");
            }

            // Remove the old completion dialog since we're using snackbar now
            // The snackbar provides the "View Recording" action
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error stopping recording: {ex.Message}");
            StatusLabel.Text = "Error occurred";

            // Show error notification
            await NotificationHelper.ShowErrorAsync("Failed to stop recording");

            await DisplayAlert("Error",
                $"An error occurred while stopping recording: {ex.Message}",
                "OK");
        }
    }

    #endregion

    #region Menu Action Methods

    private async Task OnViewRecordingsClickedAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//MyRecordings");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error navigating to recordings: {ex.Message}");
            await DisplayAlert("Navigation Error",
                "Unable to open recordings page. Please try again.",
                "OK");
        }
    }

    private async Task OnCheckPermissionsClickedAsync()
    {
        try
        {
            var permissionsGranted = await PermissionsHelper.CheckScreenRecordingPermissionsAsync();

            if (permissionsGranted)
            {
                string message = "✅ All required permissions are granted.\n\n🎬 You can start recording your screen!";

                if (DeviceInfo.Platform == DevicePlatform.Android && IsAndroidVersionAffected())
                {
                    message += "\n\n📱 Android Note: Only microphone audio can be recorded due to security restrictions (Android 10+).";
                }

                await DisplayAlert("🔑 Permissions Status", message, "✅ Great!");
            }
            else
            {
                var requiredPerms = "Required permissions:\n• 📹 Camera/Screen Recording\n• 🎤 Microphone\n• 💾 Storage Access";
                if (DeviceInfo.Platform == DevicePlatform.Android && IsAndroidVersionAffected())
                {
                    requiredPerms += "\n\n📱 Android Note: System audio recording is not available due to security restrictions (Android 10+).";
                }

                var requestPermissions = await DisplayAlert("🔑 Permissions Required",
                    $"❌ Some permissions are missing.\n\n{requiredPerms}\n\nWould you like to grant these permissions now?",
                    "✅ Grant Permissions",
                    "❌ Cancel");

                if (requestPermissions)
                {
                    var granted = await PermissionsHelper.RequestScreenRecordingPermissionsAsync();
                    if (granted)
                    {
                        await DisplayAlert("Success! 🎉",
                            "✅ All permissions granted!\n\n🎬 You can now start recording your screen with professional quality.",
                            "🚀 Start Recording!");
                    }
                    else
                    {
                        var openSettings = await DisplayAlert("❌ Permissions Denied",
                            "Some permissions were denied. For the best experience, please grant them in the app settings.\n\n💡 This ensures smooth recording features.",
                            "⚙️ Open Settings",
                            "❌ Cancel");

                        if (openSettings)
                        {
                            PermissionsHelper.OpenAppSettings();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking permissions: {ex.Message}");
            await DisplayAlert("Error",
                $"An error occurred while checking permissions: {ex.Message}",
                "OK");
        }
    }

    private async Task OnAudioInfoClickedAsync()
    {
        await PermissionsHelper.ShowAndroidAudioLimitationsAsync(this);
    }

    private async Task OnAppInfoClickedAsync()
    {
        var appInfo = $"🎬 RecordFlow - Professional Screen Recording\n\n" +
                     $"📱 Version: {AppInfo.VersionString}\n" +
                     $"🏗️ Build: {AppInfo.BuildString}\n" +
                     $"📦 Package: {AppInfo.PackageName}\n" +
                     $"🔧 Platform: {DeviceInfo.Platform}\n" +
                     $"📟 Device: {DeviceInfo.Model}\n" +
                     $"🎯 OS Version: {DeviceInfo.VersionString}\n\n" +
                     $"✨ Features:\n" +
                     $"• Professional screen recording\n" +
                     $"• High-quality video output\n" +
                     $"• Microphone audio capture\n" +
                     $"• Automatic gallery saving with album organization\n" +
                     $"• Cross-platform compatibility";

        await DisplayAlert("📊 App Information", appInfo, "✅ OK");
    }

    #endregion

    #region Helper Methods

    private void UpdateUI()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_isRecording)
            {
                StartRecordingButton.IsEnabled = false;
                StopRecordingButton.IsEnabled = true;
                StatusLabel.Text = "🔴 Recording in progress.";

                // Update button appearance for recording state
                StartRecordingButton.Text = "Recording...";
                _originalImageSource = StartRecordingButton.ImageSource;
                StartRecordingButton.ImageSource = null;
                StopRecordingButton.Text = "Stop";
            }
            else
            {
                StartRecordingButton.IsEnabled = true;
                StopRecordingButton.IsEnabled = false;
                StatusLabel.Text = "Ready to Record";

                // Reset button appearance
                StartRecordingButton.ImageSource = _originalImageSource;
                StartRecordingButton.Text = "Start";
                StopRecordingButton.Text = "Stop";
            }
        });
    }

    private bool IsAndroidVersionAffected()
    {
        // Check if Android version is 10 (API 29) or higher
        // Only these versions have the system audio recording restriction
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            // DeviceInfo.Version returns the OS version
            // For Android, we need to check if it's API 29+ (Android 10+)
            var version = DeviceInfo.Version;

            // Android 10 = API 29, Android 11 = API 30, etc.
            // We can check the major version number
            return version.Major >= 10;
        }

        return false;
    }

    private async Task ShowAndroidAudioInfoAsync()
    {
        // Show this info only once per session
        var hasShownInfo = Preferences.Get("HasShownAndroidAudioInfo", false);
        if (!hasShownInfo)
        {
            await DisplayAlert("📱 Android Audio Information",
                "🎤 Important: On Android devices (10+), screen recording can only capture microphone audio.\n\n" +
                "❌ System audio (music, games, apps) cannot be recorded due to Android security restrictions.\n\n" +
                "✅ This is normal behavior and affects all screen recording apps on Android.\n\n" +
                "📁 Your recordings will be saved in a 'RecordFlow' album in your gallery for easy organization!",
                "✅ Understood");

            Preferences.Set("HasShownAndroidAudioInfo", true);
        }
    }

    #endregion

    #region Lifecycle Methods
    
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();

        // Clean up any active notifications when page disappears
        NotificationHelper.DismissAll();
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_isRecording)
        {
            _timer?.Start();
        }
    }


    #endregion
}