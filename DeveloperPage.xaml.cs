using Microsoft.Maui.ApplicationModel.Communication;

namespace RecordFlow;

public partial class DeveloperPage : ContentPage
{
    public DeveloperPage()
    {
        InitializeComponent();
    }

    private async void OnDemoClicked(object sender, EventArgs e)
    {
        try
        {
            // Replace with your actual Google Drive video URL
            string demoVideoUrl = "https://drive.google.com/file/d/YOUR_VIDEO_ID/view";
            await Browser.Default.OpenAsync(demoVideoUrl, BrowserLaunchMode.SystemPreferred);
            await DisplayAlert("Demo Video", "Opening demo video in your browser...", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Unable to open demo video. Please check your internet connection.", "OK");
        }
    }


    private async void OnPortfolioClicked(object sender, EventArgs e)
    {
        try
        {
            // Replace with your actual portfolio URL
            string portfolioUrl = "https://www.muhammadbilal.tararg.com";
            await Browser.Default.OpenAsync(portfolioUrl, BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Unable to open portfolio. Please check your internet connection.", "OK");
        }
    }





    // Social Media Event Handlers
    private async void OnGitHubTapped(object sender, EventArgs e)
    {
        try
        {
            await Browser.Default.OpenAsync("https://github.com/smuhammadbilalh", BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Unable to open GitHub. Please check your internet connection.", "OK");
        }
    }

    private async void OnTwitterTapped(object sender, EventArgs e)
    {
        try
        {
            await Browser.Default.OpenAsync("https://x.com/Muhamma32151108", BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Unable to open Twitter/X. Please check your internet connection.", "OK");
        }
    }

    private async void OnInstagramTapped(object sender, EventArgs e)
    {
        try
        {
            await Browser.Default.OpenAsync("https://www.instagram.com/smuhammad_bilalh/", BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Unable to open Instagram. Please check your internet connection.", "OK");
        }
    }

    private async void OnLinkedInTapped(object sender, EventArgs e)
    {
        try
        {
            await Browser.Default.OpenAsync("https://www.linkedin.com/in/muhammadbilaltech/", BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Unable to open LinkedIn. Please check your internet connection.", "OK");
        }
    }

    private async void OnFacebookTapped(object sender, EventArgs e)
    {
        try
        {
            await Browser.Default.OpenAsync("https://www.facebook.com/muhammad.bilal.285914/", BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Unable to open Facebook. Please check your internet connection.", "OK");
        }
    }
    private async void OnEmailTapped(object sender, EventArgs e)
    {
        try
        {
            var emailAddress = "smuhammadbilalh@gmail.com";
            var subject = "Wants to Build an Application";
            var body = "Hi!";

            // First try using Email.Default.ComposeAsync
            try
            {
                var message = new EmailMessage
                {
                    To = new List<string> { emailAddress },
                    Subject = subject,
                    Body = body
                };

                await Email.Default.ComposeAsync(message);
                return; // If successful, return early
            }
            catch (FeatureNotSupportedException)
            {
                // If Email.Default.ComposeAsync fails, fall back to mailto URL
                var mailtoUrl = $"mailto:{emailAddress}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";

                if (await Launcher.CanOpenAsync(mailtoUrl))
                {
                    await Launcher.OpenAsync(mailtoUrl);
                    return;
                }

                // If Launcher fails, try Browser as last resort
                await Browser.Default.OpenAsync(mailtoUrl, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                // Check if there's a specific platform error that suggests no email app
                if (ex.Message.Contains("no email app") || ex.Message.Contains("not configured"))
                {
                    await DisplayAlert("Email Setup Required",
                        "Please set up an email account on your device to use this feature.", "OK");
                }
                else
                {
                    // For other errors, try mailto URL as fallback
                    var mailtoUrl = $"mailto:{emailAddress}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";
                    await Browser.Default.OpenAsync(mailtoUrl, BrowserLaunchMode.SystemPreferred);
                }
            }
        }
        catch (Exception ex)
        {
            // Final fallback error message
            await DisplayAlert("Error",
                "Unable to open email. Please ensure you have an email app installed and configured.", "OK");
        }
    }

    private async void OnPhoneTapped(object sender, EventArgs e)
    {
        // Handle phone tap - open phone dialer
        await Launcher.TryOpenAsync("tel:+9203255806291");
    }
    private async void OnWhatsAppBusinessTapped(object sender, EventArgs e)
    {
        try
        {
            // Using the provided WhatsApp business link
            await Browser.Default.OpenAsync("https://wa.me/message/ZUCBRGI34GZ4G1", BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Unable to open WhatsApp Business. Please check your internet connection.", "OK");
        }
    }
}