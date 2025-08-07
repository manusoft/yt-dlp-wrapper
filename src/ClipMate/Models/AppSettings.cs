namespace ClipMate.Models;

public static class AppSettings
{
    const string OutputFolderKey = "OutputFolder";
    const string OutputTemplateKey = "OutputTemplate";

    public static string OutputFolder
    {
        get => Preferences.Get(OutputFolderKey, Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
        set => Preferences.Set(OutputFolderKey, value);
    }

    public static string OutputTemplate
    {
        get => Preferences.Get(OutputTemplateKey, "%(upload_date)s_%(title).80s_%(format_id)s.%(ext)s"); //"%(upload_date)s_%(title)s_.%(ext)s
        set => Preferences.Set(OutputTemplateKey, value);
    }
}