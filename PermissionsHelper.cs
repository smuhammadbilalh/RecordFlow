using Microsoft.Maui.ApplicationModel;

namespace RecordFlow;

/// <summary>
/// Handles permission management for screen recording functionality
/// </summary>
public static class PermissionsHelper
{
    /// <summary>
    /// Requests all necessary permissions for screen recording including notifications
    /// </summary>
    /// <returns>True if all permissions are granted, false otherwise</returns>
    public static async Task<bool> RequestScreenRecordingPermissionsAsync()
    {
        try
        {
            var results = new List<PermissionStatus>();

            // Request camera permission (required for some platforms)
            var cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
            results.Add(cameraStatus);

            // Request microphone permission (for audio recording)
            var microphoneStatus = await Permissions.RequestAsync<Permissions.Microphone>();
            results.Add(microphoneStatus);

            // Request notification permission (Android 13+) using platform-specific code
            if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.Version.Major >= 13)
            {
                var notificationGranted = await RequestNotificationPermissionPlatformSpecificAsync();
                if (!notificationGranted)
                {
                    results.Add(PermissionStatus.Denied);
                }
            }

            // Request storage permissions based on platform
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                // For Android 13+ (API 33+), we need different permissions
                if (DeviceInfo.Version.Major >= 13)
                {
                    // Request media permissions for Android 13+
                    var mediaStatus = await Permissions.RequestAsync<Permissions.Media>();
                    results.Add(mediaStatus);
                }
                else
                {
                    // For older Android versions
                    var storageStatus = await Permissions.RequestAsync<Permissions.StorageRead>();
                    results.Add(storageStatus);

                    var storageWriteStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();
                    results.Add(storageWriteStatus);
                }
            }
            else
            {
                // For other platforms
                var storageStatus = await Permissions.RequestAsync<Permissions.StorageRead>();
                results.Add(storageStatus);

                var photosStatus = await Permissions.RequestAsync<Permissions.Photos>();
                results.Add(photosStatus);
            }

            // Check if all permissions are granted
            return results.All(status => status == PermissionStatus.Granted);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Permission request failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks the current status of screen recording permissions including notifications
    /// </summary>
    /// <returns>True if all permissions are granted, false otherwise</returns>
    public static async Task<bool> CheckScreenRecordingPermissionsAsync()
    {
        try
        {
            var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
            var microphoneStatus = await Permissions.CheckStatusAsync<Permissions.Microphone>();

            bool storageGranted = false;
            bool notificationGranted = true; // Default to true for older Android versions

            // Check notification permission for Android 13+
            if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.Version.Major >= 13)
            {
                notificationGranted = await CheckNotificationPermissionPlatformSpecificAsync();
            }

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                if (DeviceInfo.Version.Major >= 13)
                {
                    var mediaStatus = await Permissions.CheckStatusAsync<Permissions.Media>();
                    storageGranted = mediaStatus == PermissionStatus.Granted;
                }
                else
                {
                    var storageReadStatus = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
                    var storageWriteStatus = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
                    storageGranted = storageReadStatus == PermissionStatus.Granted &&
                                   storageWriteStatus == PermissionStatus.Granted;
                }
            }
            else
            {
                var storageStatus = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
                var photosStatus = await Permissions.CheckStatusAsync<Permissions.Photos>();
                storageGranted = storageStatus == PermissionStatus.Granted &&
                               photosStatus == PermissionStatus.Granted;
            }

            return cameraStatus == PermissionStatus.Granted &&
                   microphoneStatus == PermissionStatus.Granted &&
                   notificationGranted &&
                   storageGranted;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Permission check failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Platform-specific method to request notification permission
    /// </summary>
    /// <returns>True if notification permission is granted</returns>
    private static async Task<bool> RequestNotificationPermissionPlatformSpecificAsync()
    {
        try
        {
#if ANDROID
            if (DeviceInfo.Version.Major >= 13)
            {
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                if (context != null)
                {
                    // Check if permission is already granted
                    var result = AndroidX.Core.Content.ContextCompat.CheckSelfPermission(
                        context, 
                        Android.Manifest.Permission.PostNotifications);
                    
                    if (result == Android.Content.PM.Permission.Granted)
                        return true;

                    // For requesting permission, we'll use a simple approach
                    // In a real app, you might want to handle this with proper callbacks
                    if (context is AndroidX.AppCompat.App.AppCompatActivity activity)
                    {
                        AndroidX.Core.App.ActivityCompat.RequestPermissions(
                            activity,
                            new[] { Android.Manifest.Permission.PostNotifications },
                            1001);
                        
                        // Wait a moment for the permission dialog
                        await Task.Delay(100);
                        
                        // Check again
                        result = AndroidX.Core.Content.ContextCompat.CheckSelfPermission(
                            context, 
                            Android.Manifest.Permission.PostNotifications);
                        
                        return result == Android.Content.PM.Permission.Granted;
                    }
                }
            }
#endif
            return true; // Default to true for non-Android or older versions
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Notification permission request failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Platform-specific method to check notification permission
    /// </summary>
    /// <returns>True if notification permission is granted</returns>
    private static async Task<bool> CheckNotificationPermissionPlatformSpecificAsync()
    {
        try
        {
#if ANDROID
            if (DeviceInfo.Version.Major >= 13)
            {
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                if (context != null)
                {
                    var result = AndroidX.Core.Content.ContextCompat.CheckSelfPermission(
                        context, 
                        Android.Manifest.Permission.PostNotifications);
                    
                    return result == Android.Content.PM.Permission.Granted;
                }
            }
#endif
            return true; // Default to true for non-Android or older versions
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Notification permission check failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Specifically requests notification permission
    /// </summary>
    /// <returns>True if notification permission is granted</returns>
    public static async Task<bool> RequestNotificationPermissionAsync()
    {
        try
        {
            if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.Version.Major >= 13)
            {
                return await RequestNotificationPermissionPlatformSpecificAsync();
            }

            // For platforms/versions that don't require explicit notification permission
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Notification permission request failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks if notification permission is granted
    /// </summary>
    /// <returns>True if notification permission is granted</returns>
    public static async Task<bool> CheckNotificationPermissionAsync()
    {
        try
        {
            if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.Version.Major >= 13)
            {
                return await CheckNotificationPermissionPlatformSpecificAsync();
            }

            // For platforms/versions that don't require explicit notification permission
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Notification permission check failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the status of a specific permission
    /// </summary>
    /// <typeparam name="T">The permission type</typeparam>
    /// <returns>The permission status</returns>
    public static async Task<PermissionStatus> GetPermissionStatusAsync<T>() where T : Permissions.BasePermission, new()
    {
        try
        {
            return await Permissions.CheckStatusAsync<T>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get permission status: {ex.Message}");
            return PermissionStatus.Unknown;
        }
    }

    /// <summary>
    /// Requests a specific permission
    /// </summary>
    /// <typeparam name="T">The permission type</typeparam>
    /// <returns>The permission status after request</returns>
    public static async Task<PermissionStatus> RequestPermissionAsync<T>() where T : Permissions.BasePermission, new()
    {
        try
        {
            return await Permissions.RequestAsync<T>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to request permission: {ex.Message}");
            return PermissionStatus.Unknown;
        }
    }

    /// <summary>
    /// Shows a dialog explaining why permissions are needed, with platform-specific audio info
    /// </summary>
    /// <param name="page">The current page to display the dialog on</param>
    /// <returns>True if user wants to retry, false otherwise</returns>
    public static async Task<bool> ShowPermissionRationaleAsync(Page page)
    {
        try
        {
            string message = "RecordFlow needs access to your camera, microphone, storage, and notifications to record your screen and save the recordings.";

            // Add platform-specific audio information
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                message += "\n\n⚠️ Note: On Android devices, only microphone audio can be recorded during screen recording due to system security restrictions. System audio recording is not available.";

                if (DeviceInfo.Version.Major >= 13)
                {
                    message += "\n\n📱 Notification permission is required to show recording status and controls in the notification area.";
                }
            }

            message += "\n\nPlease grant these permissions to continue.";

            var result = await page.DisplayAlert(
                "Permissions Required",
                message,
                "Grant Permissions",
                "Cancel");

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to show permission rationale: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Shows notification-specific permission rationale
    /// </summary>
    /// <param name="page">The current page to display the dialog on</param>
    /// <returns>True if user wants to grant permission, false otherwise</returns>
    public static async Task<bool> ShowNotificationPermissionRationaleAsync(Page page)
    {
        try
        {
            var result = await page.DisplayAlert(
                "Notification Permission",
                "RecordFlow needs notification permission to:\n\n" +
                "• Show recording status\n" +
                "• Provide recording controls\n" +
                "• Display recording completion alerts\n" +
                "• Keep you informed during background operations\n\n" +
                "This helps you manage your recordings effectively.",
                "Grant Permission",
                "Skip");

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to show notification permission rationale: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Opens the app settings page for manual permission management
    /// </summary>
    public static void OpenAppSettings()
    {
        try
        {
            AppInfo.ShowSettingsUI();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open app settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets detailed permission status information including notifications
    /// </summary>
    /// <returns>Dictionary containing permission names and their status</returns>
    public static async Task<Dictionary<string, PermissionStatus>> GetDetailedPermissionStatusAsync()
    {
        var permissions = new Dictionary<string, PermissionStatus>();

        try
        {
            permissions["Camera"] = await Permissions.CheckStatusAsync<Permissions.Camera>();
            permissions["Microphone"] = await Permissions.CheckStatusAsync<Permissions.Microphone>();

            // Add notification permission for Android 13+
            if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.Version.Major >= 13)
            {
                var notificationGranted = await CheckNotificationPermissionPlatformSpecificAsync();
                permissions["Notifications"] = notificationGranted ? PermissionStatus.Granted : PermissionStatus.Denied;
            }

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                if (DeviceInfo.Version.Major >= 13)
                {
                    permissions["Media"] = await Permissions.CheckStatusAsync<Permissions.Media>();
                }
                else
                {
                    permissions["StorageRead"] = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
                    permissions["StorageWrite"] = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
                }
            }
            else
            {
                permissions["StorageRead"] = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
                permissions["Photos"] = await Permissions.CheckStatusAsync<Permissions.Photos>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get detailed permission status: {ex.Message}");
        }

        return permissions;
    }

    /// <summary>
    /// Checks if we should show permission rationale for any required permission
    /// </summary>
    /// <returns>True if rationale should be shown</returns>
    public static async Task<bool> ShouldShowPermissionRationaleAsync()
    {
        try
        {
            // In MAUI, we can check if permissions were previously denied
            var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
            var microphoneStatus = await Permissions.CheckStatusAsync<Permissions.Microphone>();

            bool storageNeedsRationale = false;
            bool notificationNeedsRationale = false;

            // Check notification permission rationale for Android 13+
            if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.Version.Major >= 13)
            {
                var notificationGranted = await CheckNotificationPermissionPlatformSpecificAsync();
                notificationNeedsRationale = !notificationGranted;
            }

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                if (DeviceInfo.Version.Major >= 13)
                {
                    var mediaStatus = await Permissions.CheckStatusAsync<Permissions.Media>();
                    storageNeedsRationale = mediaStatus == PermissionStatus.Denied;
                }
                else
                {
                    var storageStatus = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
                    storageNeedsRationale = storageStatus == PermissionStatus.Denied;
                }
            }
            else
            {
                var storageStatus = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
                storageNeedsRationale = storageStatus == PermissionStatus.Denied;
            }

            // If any permission is denied, we should show rationale
            return cameraStatus == PermissionStatus.Denied ||
                   microphoneStatus == PermissionStatus.Denied ||
                   notificationNeedsRationale ||
                   storageNeedsRationale;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to check if rationale should be shown: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Shows Android-specific audio recording limitations
    /// </summary>
    /// <param name="page">The current page to display the dialog on</param>
    public static async Task ShowAndroidAudioLimitationsAsync(Page page)
    {
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            await page.DisplayAlert(
                "Audio Recording Info",
                "📱 Android Audio Recording Limitations:\n\n" +
                "✅ Microphone audio: Available\n" +
                "❌ System/Internal audio: Blocked by Android security\n" +
                "❌ App audio: Not accessible\n\n" +
                "This is a security feature in Android 14+ and cannot be bypassed by regular apps.\n\n" +
                "Your recordings will include microphone audio when enabled.",
                "OK");
        }
    }
}