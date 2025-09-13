using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.ApplicationModel;
using System.Diagnostics;
using RecordFlow.Services;

namespace RecordFlow.Helpers;

public static class NotificationHelper
{
    private static CancellationTokenSource? _currentToastCts;
    private static CancellationTokenSource? _currentSnackbarCts;

    #region Toast Notifications

    /// <summary>
    /// Shows a simple toast notification
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="duration">Duration to show the toast</param>
    /// <param name="fontSize">Font size for the toast text</param>
    public static async Task ShowToastAsync(string message, ToastDuration duration = ToastDuration.Short, double fontSize = 14.0)
    {
        try
        {
            // Cancel any existing toast
            _currentToastCts?.Cancel();
            _currentToastCts = new CancellationTokenSource();

            var toast = Toast.Make(message, duration, fontSize);
            await toast.Show(_currentToastCts.Token);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error showing toast: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows a recording saved successfully toast notification
    /// </summary>
    /// <param name="recordingName">Name of the saved recording</param>
    public static async Task ShowRecordingSavedToastAsync(string recordingName = null)
    {
        var message = string.IsNullOrEmpty(recordingName)
            ? "🎬 Recording saved successfully!"
            : $"🎬 '{recordingName}' saved successfully!";

        await ShowToastAsync(message, ToastDuration.Long, 15.0);
    }

    /// <summary>
    /// Shows a recording started toast notification
    /// </summary>
    public static async Task ShowRecordingStartedToastAsync()
    {
        await ShowToastAsync("🔴 Recording started", ToastDuration.Short, 14.0);
    }

    /// <summary>
    /// Shows a recording stopped toast notification
    /// </summary>
    public static async Task ShowRecordingStoppedToastAsync()
    {
        await ShowToastAsync("⏹️ Recording stopped", ToastDuration.Short, 14.0);
    }

    #endregion

    #region Snackbar Notifications

    /// <summary>
    /// Shows a snackbar notification with action button
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="actionButtonText">Text for the action button</param>
    /// <param name="action">Action to perform when button is clicked</param>
    /// <param name="duration">Duration to show the snackbar</param>
    public static async Task ShowSnackbarAsync(
        string message,
        string actionButtonText = null,
        Action action = null,
        TimeSpan? duration = null)
    {
        try
        {
            // Cancel any existing snackbar
            _currentSnackbarCts?.Cancel();
            _currentSnackbarCts = new CancellationTokenSource();

            var snackbarOptions = new SnackbarOptions
            {
                BackgroundColor = GetSnackbarBackgroundColor(),
                TextColor = GetSnackbarTextColor(),
                ActionButtonTextColor = GetSnackbarActionColor(),
                CornerRadius = new CornerRadius(8),
                Font = Microsoft.Maui.Font.SystemFontOfSize(14),
                ActionButtonFont = Microsoft.Maui.Font.SystemFontOfSize(14, FontWeight.Medium),
                CharacterSpacing = 0.5
            };

            var snackbar = Snackbar.Make(
                message,
                action,
                actionButtonText,
                duration ?? TimeSpan.FromSeconds(4),
                snackbarOptions);

            await snackbar.Show(_currentSnackbarCts.Token);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error showing snackbar: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows a recording saved snackbar with "View Recording" action
    /// </summary>
    /// <param name="recordingPath">Path to the saved recording</param>
    /// <param name="recordingName">Name of the recording (optional)</param>
    public static async Task ShowRecordingSavedSnackbarAsync(string recordingPath, string recordingName = null)
    {
        var message = string.IsNullOrEmpty(recordingName)
            ? "🎬 Recording saved successfully!"
            : $"🎬 '{recordingName}' saved!";

        await ShowSnackbarAsync(
            message,
            "View Recording",
            async () => await OpenRecordingAsync(recordingPath),
            TimeSpan.FromSeconds(6)
        );
    }

    /// <summary>
    /// Shows a recording in progress snackbar with "Stop" and "View" actions
    /// </summary>
    /// <param name="stopAction">Action to stop recording</param>
    /// <param name="viewAction">Action to view current recording status</param>
    public static async Task ShowRecordingInProgressSnackbarAsync(Action stopAction = null, Action viewAction = null)
    {
        // For recording in progress, we'll show a persistent snackbar
        var message = "🔴 Recording in progress...";

        await ShowSnackbarAsync(
            message,
            "Stop Recording",
            stopAction,
            TimeSpan.FromSeconds(10) // Longer duration for recording notification
        );
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Opens a recording file using the default system app
    /// </summary>
    /// <param name="recordingPath">Path to the recording file</param>
    private static async Task OpenRecordingAsync(string recordingPath)
    {
        try
        {
            if (string.IsNullOrEmpty(recordingPath) || !File.Exists(recordingPath))
            {
                await ShowToastAsync("❌ Recording file not found", ToastDuration.Short);
                return;
            }

            // Use the existing SaveHelper method to open the recording
            await SaveHelper.OpenRecordingAsync(recordingPath, async (title, message, button) =>
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert(title, message, button);
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening recording: {ex.Message}");
            await ShowToastAsync("❌ Unable to open recording", ToastDuration.Short);
        }
    }

    /// <summary>
    /// Gets the appropriate background color for snackbars based on current theme
    /// </summary>
    private static Color GetSnackbarBackgroundColor()
    {
        // Use system theme-aware colors
        if (Application.Current?.RequestedTheme == AppTheme.Dark)
        {
            return Color.FromArgb("#2D2D30"); // Dark theme background
        }
        else
        {
            return Color.FromArgb("#323232"); // Light theme background (Material Design default)
        }
    }

    /// <summary>
    /// Gets the appropriate text color for snackbars based on current theme
    /// </summary>
    private static Color GetSnackbarTextColor()
    {
        return Colors.White; // White text works for both dark backgrounds
    }

    /// <summary>
    /// Gets the appropriate action button color for snackbars
    /// </summary>
    private static Color GetSnackbarActionColor()
    {
        return Color.FromArgb("#BB86FC"); // Material Design accent color
    }

    /// <summary>
    /// Dismisses any currently showing toast or snackbar
    /// </summary>
    public static void DismissAll()
    {
        try
        {
            _currentToastCts?.Cancel();
            _currentSnackbarCts?.Cancel();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error dismissing notifications: {ex.Message}");
        }
    }

    #endregion

    #region Convenience Methods

    /// <summary>
    /// Shows a success notification (green-themed)
    /// </summary>
    public static async Task ShowSuccessAsync(string message, bool useSnackbar = false, Action action = null, string actionText = null)
    {
        var successMessage = $"✅ {message}";

        if (useSnackbar)
        {
            await ShowSnackbarAsync(successMessage, actionText, action, TimeSpan.FromSeconds(4));
        }
        else
        {
            await ShowToastAsync(successMessage, ToastDuration.Short, 14.0);
        }
    }

    /// <summary>
    /// Shows an error notification (red-themed)
    /// </summary>
    public static async Task ShowErrorAsync(string message, bool useSnackbar = false)
    {
        var errorMessage = $"❌ {message}";

        if (useSnackbar)
        {
            await ShowSnackbarAsync(errorMessage, null, null, TimeSpan.FromSeconds(5));
        }
        else
        {
            await ShowToastAsync(errorMessage, ToastDuration.Long, 14.0);
        }
    }

    /// <summary>
    /// Shows an info notification (blue-themed)
    /// </summary>
    public static async Task ShowInfoAsync(string message, bool useSnackbar = false, Action action = null, string actionText = null)
    {
        var infoMessage = $"ℹ️ {message}";

        if (useSnackbar)
        {
            await ShowSnackbarAsync(infoMessage, actionText, action, TimeSpan.FromSeconds(4));
        }
        else
        {
            await ShowToastAsync(infoMessage, ToastDuration.Short, 14.0);
        }
    }

    /// <summary>
    /// Shows a warning notification (yellow-themed)
    /// </summary>
    public static async Task ShowWarningAsync(string message, bool useSnackbar = false)
    {
        var warningMessage = $"⚠️ {message}";

        if (useSnackbar)
        {
            await ShowSnackbarAsync(warningMessage, null, null, TimeSpan.FromSeconds(4));
        }
        else
        {
            await ShowToastAsync(warningMessage, ToastDuration.Short, 14.0);
        }
    }

    #endregion
}