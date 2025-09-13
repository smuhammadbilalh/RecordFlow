using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using static RecordFlow.UpdateHelper;

namespace RecordFlow;

public partial class App : Application
{
    private UpdateManager? _updateManager;
    private bool _isInitialized = false;

    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();

        // Initialize update system after a short delay
        Task.Run(InitializeUpdateSystemAsync);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);

        // Set window properties
        // Set minimum window size for desktop platforms
        window.MinimumHeight = 500;
        window.MinimumWidth = 500;
        window.MaximumHeight = 700;
        window.MaximumWidth = 600;

        return window;
    }

    private async Task InitializeUpdateSystemAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Starting update system initialization...");

            // Wait for app to fully initialize
            await Task.Delay(3000); // Reduced initial delay

            _updateManager = new UpdateManager();
            _isInitialized = true;

            System.Diagnostics.Debug.WriteLine("Update manager created successfully");

            // Wait a bit more before first check to ensure connectivity is stable
            await Task.Delay(5000);

            if (_updateManager != null)
            {
                System.Diagnostics.Debug.WriteLine("Performing initial update check...");
                await _updateManager.CheckForUpdatesAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RecordFlow: Update system initialization failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Full exception: {ex}");
        }
    }

    protected override void OnResume()
    {
        base.OnResume();
        System.Diagnostics.Debug.WriteLine("App resumed");

        if (_isInitialized && _updateManager != null)
        {
            Task.Run(async () =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("Performing resume update check...");
                    await Task.Delay(2000); // Reduced delay
                    await _updateManager.CheckForUpdatesAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"RecordFlow: Resume update check failed: {ex.Message}");
                }
            });
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Update system not ready - Initialized: {_isInitialized}, Manager: {_updateManager != null}");
        }
    }

    public bool IsUpdateSystemReady => _isInitialized && _updateManager != null;

    public async Task CheckForUpdatesManually()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Manual update check requested");

            if (!_isInitialized)
            {
                System.Diagnostics.Debug.WriteLine("Update system not initialized, creating manager...");
                if (_updateManager == null)
                {
                    _updateManager = new UpdateManager();
                }
                _isInitialized = true;
            }

            if (_updateManager != null)
            {
                System.Diagnostics.Debug.WriteLine("Calling manual update check...");
                await _updateManager.ManualUpdateCheckAsync();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Update manager is still null");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RecordFlow: Manual update check failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Full exception: {ex}");

            if (MainPage != null)
            {
                await MainPage.DisplayAlert(
                    "Update Check Failed",
                    "Unable to check for updates at this time. Please check your internet connection and try again later.",
                    "OK"
                );
            }
        }
    }

    protected override void CleanUp()
    {
        System.Diagnostics.Debug.WriteLine("App cleanup called");
        _updateManager?.Dispose();
        base.CleanUp();
    }
}