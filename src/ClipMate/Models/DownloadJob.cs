using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace ClipMate.Models;

public partial class DownloadJob : ObservableObject
{
    [ObservableProperty]
    private string url = string.Empty;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]    
    private string formatId;

    [ObservableProperty]
    private MediaFormat? mediaFormat;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormatFileSize))]
    private string fileSize = "0";

    [ObservableProperty]
    private DownloadStatus status = DownloadStatus.Pending;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThumbnailImage))]
    private string? thumbnail;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedProgress))]
    private double progress;

    [JsonIgnore]
    [ObservableProperty]
    private string? speed;

    [JsonIgnore]
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

    [ObservableProperty]
    private string message;

    [JsonIgnore]
    public bool IsNotDownloading => !IsDownloading;

    [JsonIgnore]
    public string FormattedProgress => $"{Progress * 100:0.00}%";

    [JsonIgnore]
    public string ThumbnailImage => Thumbnail ?? "videoimage.png"; 

    [JsonIgnore]
    public string FormatFileSize => FileSize ?? "n/a";

    public string OutputPath { get; set; } = string.Empty;

}


//[JsonIgnore]
//public ImageSource? ThumbnailImage
//{
//    get
//    {
//        if (!string.IsNullOrWhiteSpace(ThumbnailBase64) && ThumbnailBase64.StartsWith("data:image"))
//        {
//            try
//            {
//                var base64Data = ThumbnailBase64.Split(',')[1];
//                byte[] imageBytes = Convert.FromBase64String(base64Data);
//                return ImageSource.FromStream(() => new MemoryStream(imageBytes));
//            }
//            catch
//            {
//                return null;
//            }
//        }

//        return !string.IsNullOrWhiteSpace(Thumbnail) ? ImageSource.FromFile(Thumbnail) : null;
//    }
//}