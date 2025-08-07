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


    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Save settings logic here
        AppSettings.OutputFolder = OutputDirectory;
        AppSettings.OutputTemplate = OutputTemplate;

        // Optionally, you can notify the user that settings have been saved
        await CloseAsync();

    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await CloseAsync();
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

    private string outputTemplate = AppSettings.OutputTemplate;
    public string OutputTemplate
    {
        get => outputTemplate;
        set
        {
            if (outputTemplate != value)
            {
                outputTemplate = value;
                OnPropertyChanged(nameof(OutputTemplate));
            }
        }
    }


    // Implementing INotifyPropertyChanged to notify changes in properties
    public new event PropertyChangedEventHandler? PropertyChanged;
    protected override void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

   
}