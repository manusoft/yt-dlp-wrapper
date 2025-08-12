﻿namespace ClipMate.Models;

public static class AppSettings
{
    const string OutputFolderKey = "OutputFolder";
    const string OutputTemplateKey = "OutputTemplate";
    const string MetadataTimeoutKey = "MetadataTimeout"; // in seconds

    private const int DefaultMetadataTimeout = 15;
    private const int MinMetadataTimeout = 5;
    private const int MaxMetadataTimeout = 60;

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

    public static int MetadataTimeout
    {
        get
        {
            var value = Preferences.Get(MetadataTimeoutKey, DefaultMetadataTimeout);
            return Clamp(value, MinMetadataTimeout, MaxMetadataTimeout);
        }
        set
        {
            Preferences.Set(MetadataTimeoutKey, Clamp(value, MinMetadataTimeout, MaxMetadataTimeout));
        }
    }

    // Helper methods
    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}