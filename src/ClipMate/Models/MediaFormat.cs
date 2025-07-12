using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace ClipMate.Models;

public partial class MediaFormat : ObservableObject
{
    [ObservableProperty]
    public string id = "b";

    [ObservableProperty]
    public string extension = "mp4";

    [ObservableProperty]
    public string resolution = "Best";

    [ObservableProperty]
    public string? fileSize = "Unknown";

    [ObservableProperty]
    public string? fps = "Unknown";

    [ObservableProperty]
    public string? channels = "Unknown";

    [ObservableProperty]
    public string? vCodec = "Unknown";

    [ObservableProperty]
    public string? aCodec = "Unknown";

    [ObservableProperty]
    public string? moreInfo;

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(FileSize)
            ? $"{Resolution} ({Extension})"
            : $"{Resolution} ({Extension}, {FileSize})";
    }
}