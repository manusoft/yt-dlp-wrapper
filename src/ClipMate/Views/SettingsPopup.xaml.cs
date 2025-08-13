using ClipMate.Models;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Views;
using System.ComponentModel;

namespace ClipMate.Views;

public partial class SettingsPopup : Popup, INotifyPropertyChanged
{
	public SettingsPopup()
	{
		InitializeComponent();
        BindingContext = this;

        // Initialize properties with current settings
        OutputDirectory = AppSettings.OutputFolder;
        OutputTemplate = AppSettings.OutputTemplate;
        MetadataTimeout = AppSettings.MetadataTimeout;
    }

    private async void OnBrowseClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FolderPicker.Default.PickAsync();
            if (result.IsSuccessful)
            {
                AppSettings.OutputFolder = result.Folder.Path;
                OutputDirectory = AppSettings.OutputFolder;
            }
        }
        catch (Exception)
        {
        }
    }

    private void OnResetClicked(object sender, EventArgs e)
    {
        try
        {
            // Save settings logic here
            OutputDirectory = AppSettings.OutputFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            OutputTemplate = AppSettings.OutputTemplate = "%(upload_date)s_%(title).80s_%(format_id)s.%(ext)s";
            MetadataTimeout = AppSettings.MetadataTimeout = 15;
        }
        catch (Exception)
        {
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            // Save settings logic here
            AppSettings.OutputFolder = OutputDirectory;
            AppSettings.OutputTemplate = OutputTemplate;
            AppSettings.MetadataTimeout = MetadataTimeout;

            // Optionally, you can notify the user that settings have been saved
            await CloseAsync();
        }
        catch (Exception)
        {
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        try
        {
            await CloseAsync();
        }
        catch (Exception)
        {
        }
    }

    // Properties for the settings can be added here
    private string _outputDirectory = AppSettings.OutputFolder;
    public string OutputDirectory
    {
        get => _outputDirectory;
        set
        {
            if (_outputDirectory != value)
            {
                _outputDirectory = value;
                OnPropertyChanged(nameof(OutputDirectory));
            }
        }
    }

    private string _outputTemplate = AppSettings.OutputTemplate;
    public string OutputTemplate
    {
        get => _outputTemplate;
        set
        {
            if (_outputTemplate != value)
            {
                _outputTemplate = value;
                OnPropertyChanged(nameof(OutputTemplate));
            }
        }
    }

    private int _metadataTimeout = AppSettings.MetadataTimeout;
    public int MetadataTimeout
    {
        get => _metadataTimeout;
        set
        {
            if (_metadataTimeout != value)
            {
                _metadataTimeout = value;
                OnPropertyChanged(nameof(MetadataTimeout));
            }
        }
    }

    // Implementing INotifyPropertyChanged to notify changes in properties
    public new event PropertyChangedEventHandler? PropertyChanged;
    protected override void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));   
}