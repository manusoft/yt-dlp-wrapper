using CommunityToolkit.Maui.Views;

namespace ClipMate.Views;

public partial class AboutPopup : Popup
{
	public AboutPopup()
	{
		InitializeComponent();
	}

    private async void OnWebsiteClicked(object sender, EventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync("https://manojbabu.in");
        }
        catch (Exception)
        {
        }
    }

    private async void OnEmailClicked(object sender, EventArgs e)
    {
		try
		{
            var message = new EmailMessage
            {
                Subject = "Support - ClipMate",
                To = new List<string> { "support@yourdomain.com" }
            };

            await Email.Default.ComposeAsync(message);
        }
		catch (Exception)
		{
		}
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await CloseAsync(); // Dismiss the popup
    }
}