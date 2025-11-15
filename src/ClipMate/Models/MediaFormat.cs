using CommunityToolkit.Mvvm.ComponentModel;

namespace ClipMate.Models;

public partial class MediaFormat : ObservableObject
{
    [ObservableProperty] private string id = "b";
    [ObservableProperty] private string extension = "mp4";
    [ObservableProperty] private string resolution = "Best";
    [ObservableProperty] private string? fileSize = "none";
    [ObservableProperty] private string? fps = "none";
    [ObservableProperty] private string? channels = "none";
    [ObservableProperty] private string? vCodec = "none";
    [ObservableProperty] private string? aCodec = "none";
    [ObservableProperty] private string? moreInfo;
    [ObservableProperty] private bool isAudio = false;

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(FileSize)
            ? $"{Resolution} ({Extension})"
            : $"{Resolution} ({Extension}, {FileSize})";
    }
}