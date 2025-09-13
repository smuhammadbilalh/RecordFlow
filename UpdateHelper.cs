using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace RecordFlow
{
    public partial class UpdateHelper
    {
        // Update Models
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        public class UpdateInfo
        {
            public string Version { get; set; } = string.Empty;
            public string ReleaseNotes { get; set; } = string.Empty;
            public DateTime ReleaseDate { get; set; }
            public bool IsUpdateAvailable { get; set; }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        public class GitHubRelease
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; } = string.Empty;

            [JsonPropertyName("body")]
            public string Body { get; set; } = string.Empty;

            [JsonPropertyName("published_at")]
            public DateTime PublishedAt { get; set; }
        }

        // Update Service
        public class UpdateService
        {
            private readonly string _currentVersion;
            private readonly string _updateCheckUrl;
            private readonly string _githubReleasesUrl = "https://github.com/smuhammadbilalh/RecordFlow/releases/latest";

            public event EventHandler<UpdateInfo>? UpdateAvailable;

            public UpdateService()
            {
                _currentVersion = AppInfo.VersionString;
                _updateCheckUrl = "https://api.github.com/repos/smuhammadbilalh/RecordFlow/releases/latest";
            }

            public async Task<bool> IsInternetAvailableAsync()
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("Checking internet connectivity...");

                    // First check network access
                    var networkAccess = Connectivity.Current.NetworkAccess;
                    System.Diagnostics.Debug.WriteLine($"Network Access Status: {networkAccess}");

                    if (networkAccess != NetworkAccess.Internet)
                    {
                        System.Diagnostics.Debug.WriteLine("NetworkAccess is not Internet, returning false");
                        return false;
                    }

                    // Try multiple endpoints for better reliability
                    var testUrls = new[]
                    {
                        "https://api.github.com",
                        "https://www.google.com",
                        "https://httpbin.org/get"
                    };

                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(10); // Increased timeout
                    client.DefaultRequestHeaders.Add("User-Agent", "RecordFlow-UpdateChecker");

                    foreach (var url in testUrls)
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"Testing connectivity to: {url}");
                            var response = await client.GetAsync(url);
                            if (response.IsSuccessStatusCode)
                            {
                                System.Diagnostics.Debug.WriteLine($"Successfully connected to {url}");
                                return true;
                            }
                            System.Diagnostics.Debug.WriteLine($"Failed to connect to {url}: {response.StatusCode}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Exception testing {url}: {ex.Message}");
                        }
                    }

                    System.Diagnostics.Debug.WriteLine("All connectivity tests failed");
                    return false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"IsInternetAvailableAsync exception: {ex.Message}");
                    return false;
                }
            }

            public async Task<UpdateInfo> CheckForUpdatesAsync(bool showUpToDateMessage = false)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"CheckForUpdatesAsync called with showUpToDateMessage: {showUpToDateMessage}");

                    // Check internet connectivity with detailed logging
                    var isInternetAvailable = await IsInternetAvailableAsync();
                    if (!isInternetAvailable)
                    {
                        System.Diagnostics.Debug.WriteLine("No internet available - aborting update check");

                        if (showUpToDateMessage && Application.Current?.MainPage != null)
                        {
                            await Application.Current.MainPage.DisplayAlert(
                                "No Internet Connection",
                                "Unable to check for updates. Please check your internet connection and try again.",
                                "OK"
                            );
                        }

                        return new UpdateInfo { IsUpdateAvailable = false };
                    }

                    System.Diagnostics.Debug.WriteLine("Internet connectivity confirmed");

                    // Check if we've already checked recently (avoid spam) - but allow manual checks
                    var lastCheckTime = Preferences.Get("LastUpdateCheck", DateTime.MinValue);
                    if (!showUpToDateMessage && DateTime.Now - lastCheckTime < TimeSpan.FromHours(1))
                    {
                        System.Diagnostics.Debug.WriteLine("Recent check found, skipping");
                        return new UpdateInfo { IsUpdateAvailable = false };
                    }

                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(30); // Increased timeout for API calls
                    client.DefaultRequestHeaders.Add("User-Agent", "RecordFlow-UpdateChecker");

                    System.Diagnostics.Debug.WriteLine($"Fetching release info from: {_updateCheckUrl}");
                    var response = await client.GetStringAsync(_updateCheckUrl);
                    System.Diagnostics.Debug.WriteLine($"GitHub API response received, length: {response.Length}");

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
                    };

                    var releaseInfo = JsonSerializer.Deserialize<GitHubRelease>(response, options);

                    if (releaseInfo == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Failed to deserialize release info");
                        return new UpdateInfo { IsUpdateAvailable = false };
                    }

                    var updateInfo = new UpdateInfo
                    {
                        Version = releaseInfo.TagName?.TrimStart('v') ?? "0.0.0",
                        ReleaseNotes = string.IsNullOrWhiteSpace(releaseInfo.Body) ? "No release notes available." : releaseInfo.Body,
                        ReleaseDate = releaseInfo.PublishedAt
                    };

                    System.Diagnostics.Debug.WriteLine($"Current version: {_currentVersion}, Latest version: {updateInfo.Version}");

                    // Compare versions
                    var versionComparison = CompareVersions(updateInfo.Version, _currentVersion);
                    updateInfo.IsUpdateAvailable = versionComparison > 0;

                    System.Diagnostics.Debug.WriteLine($"Version comparison result: {versionComparison}, Update available: {updateInfo.IsUpdateAvailable}");

                    // Save last check time
                    Preferences.Set("LastUpdateCheck", DateTime.Now);

                    if (updateInfo.IsUpdateAvailable)
                    {
                        System.Diagnostics.Debug.WriteLine("Update available, triggering event");
                        UpdateAvailable?.Invoke(this, updateInfo);
                    }
                    else if (showUpToDateMessage)
                    {
                        var message = versionComparison == 0
                            ? $"You're up to date! 🎉\nCurrent version: {_currentVersion}"
                            : $"You're ahead! 🚀\nYour version: {_currentVersion}\nLatest: {updateInfo.Version}";

                        if (Application.Current?.MainPage != null)
                        {
                            await Application.Current.MainPage.DisplayAlert(
                                "No Updates Available",
                                message,
                                "OK"
                            );
                        }
                    }

                    return updateInfo;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"RecordFlow: Update check failed: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Exception details: {ex}");

                    if (showUpToDateMessage && Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Update Check Failed",
                            "Unable to check for updates. Please check your internet connection and try again.",
                            "OK"
                        );
                    }

                    return new UpdateInfo { IsUpdateAvailable = false };
                }
            }

            public async Task OpenGitHubReleasesAsync()
            {
                try
                {
                    await Launcher.OpenAsync(_githubReleasesUrl);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to open GitHub releases: {ex.Message}");

                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Error",
                            "Unable to open download page. Please visit GitHub manually to download the update.",
                            "OK"
                        );
                    }
                }
            }

            private int CompareVersions(string newVersion, string currentVersion)
            {
                try
                {
                    var cleanNewVersion = newVersion.TrimStart('v').Trim();
                    var cleanCurrentVersion = currentVersion.TrimStart('v').Trim();

                    var newParts = cleanNewVersion.Split('.').Select(part =>
                    {
                        if (int.TryParse(part, out int value))
                            return value;
                        return 0;
                    }).ToArray();

                    var currentParts = cleanCurrentVersion.Split('.').Select(part =>
                    {
                        if (int.TryParse(part, out int value))
                            return value;
                        return 0;
                    }).ToArray();

                    var maxLength = Math.Max(newParts.Length, currentParts.Length);
                    Array.Resize(ref newParts, maxLength);
                    Array.Resize(ref currentParts, maxLength);

                    for (int i = 0; i < maxLength; i++)
                    {
                        if (newParts[i] > currentParts[i])
                            return 1;
                        if (newParts[i] < currentParts[i])
                            return -1;
                    }

                    return 0;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Version comparison error: {ex.Message}");
                    return 0;
                }
            }
        }

        // Update Manager
        public class UpdateManager : IDisposable
        {
            private readonly UpdateService _updateService;
            private readonly Timer _checkTimer;
            private bool _isCheckingForUpdates;
            private bool _disposed;

            public UpdateManager()
            {
                _updateService = new UpdateService();

                // Subscribe to connectivity changes
                Connectivity.ConnectivityChanged += OnConnectivityChanged;

                // Subscribe to update notifications
                _updateService.UpdateAvailable += OnUpdateAvailable;

                // Set up periodic check (every 24 hours when connected)
                // Increased initial delay to allow app to fully initialize
                _checkTimer = new Timer(PeriodicUpdateCheck, null, TimeSpan.FromMinutes(10), TimeSpan.FromHours(24));
            }

            private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
            {
                System.Diagnostics.Debug.WriteLine($"Connectivity changed: {e.NetworkAccess}");

                if (e.NetworkAccess == NetworkAccess.Internet && !_isCheckingForUpdates)
                {
                    // Give some time for the connection to stabilize
                    await Task.Delay(5000);
                    await CheckForUpdatesAsync();
                }
            }

            private async void PeriodicUpdateCheck(object? state)
            {
                System.Diagnostics.Debug.WriteLine("Periodic update check triggered");

                if (!_isCheckingForUpdates)
                {
                    await CheckForUpdatesAsync();
                }
            }

            public async Task ManualUpdateCheckAsync()
            {
                System.Diagnostics.Debug.WriteLine("Manual update check requested");
                await CheckForUpdatesAsync(true);
            }

            public async Task CheckForUpdatesAsync(bool showUpToDateMessage = false)
            {
                if (_isCheckingForUpdates || _disposed)
                {
                    System.Diagnostics.Debug.WriteLine($"Skipping update check: isChecking={_isCheckingForUpdates}, disposed={_disposed}");
                    return;
                }

                _isCheckingForUpdates = true;
                System.Diagnostics.Debug.WriteLine("Starting update check...");

                try
                {
                    await _updateService.CheckForUpdatesAsync(showUpToDateMessage);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Update check error: {ex.Message}");
                }
                finally
                {
                    _isCheckingForUpdates = false;
                    System.Diagnostics.Debug.WriteLine("Update check completed");
                }
            }

            private void OnUpdateAvailable(object? sender, UpdateInfo updateInfo)
            {
                System.Diagnostics.Debug.WriteLine($"Update available event received for version {updateInfo.Version}");

                if (Application.Current?.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Dispatch(async () =>
                    {
                        await ShowUpdateDialogAsync(updateInfo);
                    });
                }
                else
                {
                    // Fallback for when Dispatcher is not available
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await ShowUpdateDialogAsync(updateInfo);
                    });
                }
            }

            private async Task ShowUpdateDialogAsync(UpdateInfo updateInfo)
            {
                try
                {
                    if (Application.Current?.MainPage == null)
                    {
                        System.Diagnostics.Debug.WriteLine("MainPage is null, cannot show update dialog");
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine($"Showing update dialog for version {updateInfo.Version}");

                    var result = await Application.Current.MainPage.DisplayAlert(
                        "Update Available",
                        $"Version {updateInfo.Version} is available.\n\n{updateInfo.ReleaseNotes}",
                        "Download",
                        "Later"
                    );

                    if (result)
                    {
                        await _updateService.OpenGitHubReleasesAsync();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error showing update dialog: {ex.Message}");
                }
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                _checkTimer?.Dispose();
                Connectivity.ConnectivityChanged -= OnConnectivityChanged;
                _updateService.UpdateAvailable -= OnUpdateAvailable;
                GC.SuppressFinalize(this);
            }
        }
    }
}