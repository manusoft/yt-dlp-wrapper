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
    public string? fileSize = "n/a";

    [ObservableProperty]
    public string? fps = "n/a";

    [ObservableProperty]
    public string? channels = "n/a";

    [ObservableProperty]
    public string? vCodec = "n/a";

    [ObservableProperty]
    public string? aCodec = "n/a";

    [ObservableProperty]
    public string? moreInfo;

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(FileSize)
            ? $"{Resolution} ({Extension})"
            : $"{Resolution} ({Extension}, {FileSize})";
    }
}