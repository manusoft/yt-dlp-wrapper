using ClipMate.Services;
using CommunityToolkit.Maui.Views;
using System.ComponentModel;

namespace ClipMate.Views;

public partial class AboutPopup : Popup, INotifyPropertyChanged
{
    private readonly YtdlpService _ytdlpService;
    private readonly AppLogger _logger;

    private string _ytdlpVersion = $"v{AppInfo.VersionString}";
    public string YtdlpVersion
    {
        get => _ytdlpVersion;
        set
        {
            if (_ytdlpVersion != value)
            {
                _ytdlpVersion = value;
                OnPropertyChanged(nameof(YtdlpVersion));
            }
        }
    }

    public AboutPopup()
    {
        InitializeComponent();
        BindingContext = this;
        _logger = new AppLogger();
        _ytdlpService = new YtdlpService(_logger);
    }

    private async void OnPopupLoaded(object sender, EventArgs e)
    {
        try
        {
            var ytdlpVersion = await _ytdlpService.GetVersionAsync();
            var formattedVersion = string.IsNullOrEmpty(ytdlpVersion) ? "" : $" • {ytdlpVersion}";
            var appVersion = AppInfo.VersionString;

            YtdlpVersion = $"v{appVersion}{formattedVersion}";
        }
        catch (Exception)
        {
        }
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
        try
        {
            await CloseAsync(); // Dismiss the popup
        }
        catch (Exception)
        {
        }
    }

    public new event PropertyChangedEventHandler? PropertyChanged;
    protected override void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}