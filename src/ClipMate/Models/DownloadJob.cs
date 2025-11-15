using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace ClipMate.Models;

public partial class DownloadJob : ObservableObject
{
    [ObservableProperty] private string url = string.Empty;
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string formatId;
    [ObservableProperty] private MediaFormat? mediaFormat;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(FormatFileSize))] private string fileSize = "0";
    [ObservableProperty] private DownloadStatus status = DownloadStatus.Pending;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ThumbnailImage))] private string? thumbnail;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(FormattedProgress))] private double progress;

    [JsonIgnore, ObservableProperty] private string? speed;
    [JsonIgnore, ObservableProperty] private string? eta;

    [ObservableProperty] private bool isMerging;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsNotDownloading))] private bool isDownloading;
    [ObservableProperty] private bool isCompleted;
    [ObservableProperty] private string errorMessage;

    [JsonIgnore, ObservableProperty] private string message;

    [JsonIgnore] public bool IsNotDownloading => !IsDownloading;
    [JsonIgnore] public string FormattedProgress => $"{Progress * 100:0.00}%";
    [JsonIgnore] public string ThumbnailImage => Thumbnail ?? "videoimage.png";
    [JsonIgnore] public string FormatFileSize => FileSize ?? "none";

    public string OutputPath { get; set; } = string.Empty;
}
