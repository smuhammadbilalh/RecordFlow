using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System.Diagnostics;

namespace RecordFlow.Services;

public static class SaveHelper
{
    /// <summary>
    /// Gets the custom save path for recordings based on the current platform
    /// </summary>
    /// <param name="customFileName">Optional custom filename (without extension)</param>
    /// <returns>Full path where recordings should be saved</returns>
    public static string GetCustomSavePath(string customFileName = null)
    {
        string recordingsPath;

        // Create RecordFlow album/folder in appropriate location for each platform
        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            // Windows - Videos folder
            var videosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            recordingsPath = Path.Combine(videosPath, "RecordFlow");
        }
        else if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            // Android - Movies/RecordFlow folder for local storage
            // We'll handle gallery album organization through MediaStore after recording
#if ANDROID
        var moviesPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).AbsolutePath;
#else
            var moviesPath = "/storage/emulated/0/Movies"; // Fallback path
#endif
            recordingsPath = Path.Combine(moviesPath, "RecordFlow");
        }
        else if (DeviceInfo.Platform == DevicePlatform.iOS)
        {
            // iOS - Documents folder (will sync to Files app)
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            recordingsPath = Path.Combine(documentsPath, "RecordFlow");
        }
        else
        {
            // Other platforms - Documents folder
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            recordingsPath = Path.Combine(documentsPath, "RecordFlow");
        }

        // Create directory if it doesn't exist
        try
        {
            if (!Directory.Exists(recordingsPath))
            {
                Directory.CreateDirectory(recordingsPath);
                Debug.WriteLine($"Created RecordFlow directory: {recordingsPath}");
            }

            // For Windows, ensure directory exists and has proper permissions
            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                // Verify directory is writable
                var testFile = Path.Combine(recordingsPath, "test_write.tmp");
                try
                {
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                    Debug.WriteLine($"Windows RecordFlow directory verified: {recordingsPath}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Windows directory write test failed: {ex.Message}");
                    // Fallback to Videos root if RecordFlow folder has permission issues
                    recordingsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating RecordFlow directory: {ex.Message}");
            // Fallback to platform default if directory creation fails
            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                recordingsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            }
        }

        // Use custom filename if provided, otherwise generate timestamp-based name
        var fileName = !string.IsNullOrEmpty(customFileName)
            ? $"{customFileName}.mp4"
            : $"RecordFlow_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";

        return Path.Combine(recordingsPath, fileName);
    }

    /// <summary>
    /// Opens a recording file using the default application
    /// </summary>
    /// <param name="filePath">Path to the recording file</param>
    /// <param name="displayAlert">Action to display alert messages</param>
    public static async Task OpenRecordingAsync(string filePath, Func<string, string, string, Task> displayAlert)
    {
        try
        {
            if (File.Exists(filePath))
            {
                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(filePath),
                    Title = "🎥 Open Recording"
                });
            }
            else
            {
                await displayAlert("📁 File Not Found",
                    "The recording file could not be found. It may have been moved or deleted.\n\n💡 Try checking your device's gallery in the 'RecordFlow' album or file manager.",
                    "✅ OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening recording: {ex.Message}");
            await displayAlert("❌ Error",
                "Unable to open the recording file.\n\n📱 Please check it in your device's gallery under 'RecordFlow' album or video player app.",
                "✅ OK");
        }
    }

    /// <summary>
    /// Organizes the recording in the Android gallery under the RecordFlow album
    /// </summary>
    /// <param name="filePath">Path to the recording file</param>
    public static async Task OrganizeRecordingInGalleryAsync(string filePath)
    {
        if (DeviceInfo.Platform != DevicePlatform.Android)
            return;

        try
        {
#if ANDROID
            // Use Android MediaStore to add the file to a specific album/collection
            await Task.Run(() =>
            {
                try
                {
                    var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                    var contentResolver = context.ContentResolver;
                    
                    // Create content values for MediaStore
                    var contentValues = new Android.Content.ContentValues();
                    contentValues.Put(Android.Provider.MediaStore.Video.Media.InterfaceConsts.DisplayName, Path.GetFileName(filePath));
                    contentValues.Put(Android.Provider.MediaStore.Video.Media.InterfaceConsts.MimeType, "video/mp4");
                    contentValues.Put(Android.Provider.MediaStore.Video.Media.InterfaceConsts.RelativePath, "Movies/RecordFlow");
                    contentValues.Put(Android.Provider.MediaStore.Video.Media.InterfaceConsts.IsPrivate, 0);
                    
                    // Insert into MediaStore
                    var uri = contentResolver.Insert(Android.Provider.MediaStore.Video.Media.ExternalContentUri, contentValues);
                    
                    if (uri != null && File.Exists(filePath))
                    {
                        // Copy file content to MediaStore location
                        using var inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                        using var outputStream = contentResolver.OpenOutputStream(uri);
                        if (outputStream != null)
                        {
                            inputStream.CopyTo(outputStream);
                            Debug.WriteLine("Successfully organized recording in RecordFlow album");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error organizing recording in gallery: {ex.Message}");
                }
            });
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in OrganizeRecordingInGalleryAsync: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows the recording completion dialog and handles user's choice
    /// </summary>
    /// <param name="result">Recording result containing file path</param>
    /// <param name="displayAlert">Function to display alert with choice</param>
    /// <param name="displaySimpleAlert">Function to display simple alert</param>
    /// <param name="isAndroidVersionAffected">Whether Android version is affected by audio limitations</param>
    /// <returns>True if user wants to view the recording</returns>
    public static async Task<bool> ShowRecordingCompletionDialogAsync(
        object result,
        Func<string, string, string, string, Task<bool>> displayAlert,
        Func<string, string, string, Task> displaySimpleAlert,
        bool isAndroidVersionAffected)
    {
        // Use reflection to get FullPath property since we don't know the exact type
        var fullPathProperty = result?.GetType().GetProperty("FullPath");
        var filePath = fullPathProperty?.GetValue(result) as string;

        if (result != null && !string.IsNullOrEmpty(filePath))
        {
            var viewRecording = await displayAlert("Recording Completed! ✅\n",
                $"📁 File: {Path.GetFileName(filePath)}\n" +
                $"💾 Location: Gallery → RecordFlow album\n\n" +
                "Would you like to view the recording now?",
                "🎥 View Recording",
                "✅ Done");

            return viewRecording;
        }
        else
        {
            await displaySimpleAlert("Recording Completed! ✅",
                "🎬 Your screen recording has been saved to the 'RecordFlow' album in your gallery!" +
                (DeviceInfo.Platform == DevicePlatform.Android && isAndroidVersionAffected ?
                    "\n\n📱 Note: Only microphone audio was recorded due to Android 10+ limitations" : ""),
                "✅ Done");

            return false;
        }
    }

    /// <summary>
    /// Moves the recording to the RecordFlow folder on Windows if it wasn't saved there initially
    /// </summary>
    /// <param name="originalFilePath">Original file path from recording result</param>
    /// <returns>Final file path where the recording is located</returns>
    public static async Task<string> OrganizeRecordingForWindowsAsync(string originalFilePath)
    {
        if (DeviceInfo.Platform != DevicePlatform.WinUI || string.IsNullOrEmpty(originalFilePath))
            return originalFilePath;

        try
        {
            var videosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            var recordFlowPath = Path.Combine(videosPath, "RecordFlow");

            // Ensure RecordFlow directory exists
            if (!Directory.Exists(recordFlowPath))
            {
                Directory.CreateDirectory(recordFlowPath);
            }

            // Check if file is already in RecordFlow folder
            if (originalFilePath.Contains("RecordFlow"))
            {
                return originalFilePath;
            }

            // Move file to RecordFlow folder if it exists
            if (File.Exists(originalFilePath))
            {
                var fileName = Path.GetFileName(originalFilePath);
                var newFilePath = Path.Combine(recordFlowPath, fileName);

                // Ensure unique filename
                var counter = 1;
                while (File.Exists(newFilePath))
                {
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    var extension = Path.GetExtension(fileName);
                    newFilePath = Path.Combine(recordFlowPath, $"{nameWithoutExt}_{counter}{extension}");
                    counter++;
                }

                File.Move(originalFilePath, newFilePath);
                Debug.WriteLine($"Moved Windows recording to RecordFlow folder: {newFilePath}");
                return newFilePath;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error organizing Windows recording: {ex.Message}");
        }

        return originalFilePath;
    }

    /// <summary>
    /// Gets the file path from a recording result object
    /// </summary>
    /// <param name="result">Recording result object</param>
    /// <returns>File path string or null if not found</returns>
    public static string GetFilePathFromResult(object result)
    {
        if (result == null) return null;

        // Use reflection to get FullPath property since we don't know the exact type
        var fullPathProperty = result.GetType().GetProperty("FullPath");
        return fullPathProperty?.GetValue(result) as string;
    }

    /// <summary>
    /// Validates a recording name for invalid characters and length
    /// </summary>
    /// <param name="recordingName">The name to validate</param>
    /// <returns>Validation result with success status and error message</returns>
    public static (bool IsValid, string ErrorMessage) ValidateRecordingName(string recordingName)
    {
        if (string.IsNullOrWhiteSpace(recordingName))
        {
            return (false, "❌ Recording name cannot be empty.");
        }

        if (recordingName.Length > 100)
        {
            return (false, "❌ Recording name is too long. Please use 100 characters or less.");
        }

        // Check for invalid file name characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var hasInvalidChars = recordingName.Any(c => invalidChars.Contains(c));

        if (hasInvalidChars)
        {
            return (false, $"❌ Recording name contains invalid characters.\n\n🚫 Avoid: {string.Join(" ", invalidChars.Where(c => !char.IsControl(c)))}");
        }

        // Check for reserved names on Windows
        var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
        if (reservedNames.Contains(recordingName.ToUpperInvariant()))
        {
            return (false, "❌ This name is reserved by the system. Please choose a different name.");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Gets a unique file path for the recording, checking for duplicates and handling them
    /// </summary>
    /// <param name="recordingName">The desired recording name</param>
    /// <returns>Full path with unique filename</returns>
    public static async Task<string> GetUniqueFilePathAsync(string recordingName)
    {
        try
        {
            var recordingsPath = GetRecordingsDirectory();

            if (string.IsNullOrEmpty(recordingsPath))
            {
                Debug.WriteLine("Error: Unable to get recordings directory");
                return null;
            }

            // Clean the recording name and add extension
            var cleanName = recordingName.Trim();
            var fileName = $"{cleanName}.mp4";
            var fullPath = Path.Combine(recordingsPath, fileName);

            // Check if file already exists
            if (!File.Exists(fullPath))
            {
                return fullPath;
            }

            // File exists, ask user what to do
            var userChoice = await ShowDuplicateHandlingDialogAsync(cleanName);

            switch (userChoice)
            {
                case DuplicateHandlingChoice.Replace:
                    // Delete existing file
                    try
                    {
                        File.Delete(fullPath);
                        return fullPath;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error deleting existing file: {ex.Message}");
                        // Fall back to creating unique name
                        return GetUniqueFileName(recordingsPath, cleanName);
                    }

                case DuplicateHandlingChoice.CreateNew:
                    return GetUniqueFileName(recordingsPath, cleanName);

                case DuplicateHandlingChoice.Cancel:
                    return null;

                default:
                    return GetUniqueFileName(recordingsPath, cleanName);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in GetUniqueFilePathAsync: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Shows dialog to handle duplicate recording names
    /// </summary>
    /// <param name="recordingName">The duplicate recording name</param>
    /// <returns>User's choice for handling the duplicate</returns>
    private static async Task<DuplicateHandlingChoice> ShowDuplicateHandlingDialogAsync(string recordingName)
    {
        // Since we can't directly access DisplayActionSheet from static context,
        // we'll need to use the MainThread and get the current page
        var tcs = new TaskCompletionSource<DuplicateHandlingChoice>();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var currentPage = Application.Current?.MainPage;
                if (currentPage != null)
                {
                    var action = await currentPage.DisplayActionSheet(
                        $"📁 Recording '{recordingName}' already exists",
                        "❌ Cancel",
                        null,
                        "🔄 Replace existing recording",
                        "✨ Create new recording with different name"
                    );

                    var choice = action switch
                    {
                        "🔄 Replace existing recording" => DuplicateHandlingChoice.Replace,
                        "✨ Create new recording with different name" => DuplicateHandlingChoice.CreateNew,
                        _ => DuplicateHandlingChoice.Cancel
                    };

                    tcs.SetResult(choice);
                }
                else
                {
                    tcs.SetResult(DuplicateHandlingChoice.CreateNew);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing duplicate dialog: {ex.Message}");
                tcs.SetResult(DuplicateHandlingChoice.CreateNew);
            }
        });

        return await tcs.Task;
    }

    /// <summary>
    /// Creates a unique filename by appending numbers if duplicates exist
    /// </summary>
    /// <param name="directory">Directory to check for duplicates</param>
    /// <param name="baseName">Base name for the file</param>
    /// <returns>Unique file path</returns>
    private static string GetUniqueFileName(string directory, string baseName)
    {
        var counter = 1;
        string fileName;
        string fullPath;

        do
        {
            fileName = $"{baseName} ({counter}).mp4";
            fullPath = Path.Combine(directory, fileName);
            counter++;
        }
        while (File.Exists(fullPath));

        return fullPath;
    }

    /// <summary>
    /// Gets the recordings directory path for the current platform
    /// </summary>
    /// <returns>Directory path where recordings should be saved</returns>
    private static string GetRecordingsDirectory()
    {
        string recordingsPath;

        // Create RecordFlow album/folder in appropriate location for each platform
        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            // Windows - Videos folder
            var videosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            recordingsPath = Path.Combine(videosPath, "RecordFlow");
        }
        else if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            // Android - Movies/RecordFlow folder for local storage
#if ANDROID
        var moviesPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).AbsolutePath;
#else
            var moviesPath = "/storage/emulated/0/Movies"; // Fallback path
#endif
            recordingsPath = Path.Combine(moviesPath, "RecordFlow");
        }
        else if (DeviceInfo.Platform == DevicePlatform.iOS)
        {
            // iOS - Documents folder (will sync to Files app)
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            recordingsPath = Path.Combine(documentsPath, "RecordFlow");
        }
        else
        {
            // Other platforms - Documents folder
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            recordingsPath = Path.Combine(documentsPath, "RecordFlow");
        }

        // Create directory if it doesn't exist
        try
        {
            if (!Directory.Exists(recordingsPath))
            {
                Directory.CreateDirectory(recordingsPath);
                Debug.WriteLine($"Created RecordFlow directory: {recordingsPath}");
            }

            // For Windows, ensure directory exists and has proper permissions
            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                // Verify directory is writable
                var testFile = Path.Combine(recordingsPath, "test_write.tmp");
                try
                {
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                    Debug.WriteLine($"Windows RecordFlow directory verified: {recordingsPath}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Windows directory write test failed: {ex.Message}");
                    // Fallback to Videos root if RecordFlow folder has permission issues
                    recordingsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating RecordFlow directory: {ex.Message}");
            // Fallback to platform default if directory creation fails
            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                recordingsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            }
        }

        return recordingsPath;
    }

    /// <summary>
    /// Enum for handling duplicate recording names
    /// </summary>
    public enum DuplicateHandlingChoice
    {
        Replace,
        CreateNew,
        Cancel
    }
}