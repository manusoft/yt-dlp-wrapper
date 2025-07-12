using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace ClipMate.Models;

public partial class DownloadJob : ObservableObject
{
    [ObservableProperty]
    private string url = string.Empty;

    [ObservableProperty]
    private MediaFormat? format;

    public string OutputPath { get; set; } = string.Empty;

    [ObservableProperty]
    private DownloadStatus status = DownloadStatus.Pending;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThumbnailImage))]
    private string? thumbnail = "videoimage.png";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThumbnailImage))]
    private string? thumbnailBase64;

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private string? speed;

    [ObservableProperty]
    private string? eta;

    [ObservableProperty]
    private bool isMerging;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotDownloading))]
    private bool isDownloading;

    [ObservableProperty]
    private bool isCompleted;

    [ObservableProperty]
    private string errorMessage = "";

    [JsonIgnore]
    public bool IsNotDownloading => !IsDownloading;

    [JsonIgnore]
    public string FormatFileSize => Format?.FileSize ?? "N/A";

    [JsonIgnore]
    public ImageSource? ThumbnailImage
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ThumbnailBase64) && ThumbnailBase64.StartsWith("data:image"))
            {
                try
                {
                    var base64Data = ThumbnailBase64.Split(',')[1];
                    byte[] imageBytes = Convert.FromBase64String(base64Data);
                    return ImageSource.FromStream(() => new MemoryStream(imageBytes));
                }
                catch
                {
                    return null;
                }
            }

            return !string.IsNullOrWhiteSpace(Thumbnail) ? ImageSource.FromFile(Thumbnail) : null;
        }
    }
}
