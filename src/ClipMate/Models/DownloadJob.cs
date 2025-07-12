using CommunityToolkit.Mvvm.ComponentModel;

namespace ClipMate.Models;

public partial class DownloadJob : ObservableObject
{
    [ObservableProperty]
    private string url = string.Empty;
    //public string Url
    //{
    //    get => _url;
    //    set { SetProperty(ref _url, value); }
    //}

    [ObservableProperty]
    private MediaFormat? format;
    //public MediaFormat? Format
    //{
    //    get => _format;
    //    set
    //    {
    //        if (_format != value)
    //        {
    //            SetProperty(ref _format, value);
    //        }
    //    }
    //}

    public string OutputPath { get; set; } = string.Empty;

    [ObservableProperty]
    private DownloadStatus status = DownloadStatus.Pending;
    //public DownloadStatus Status
    //{
    //    get => _status;
    //    set { SetProperty(ref _status, value); }
    //}

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThumbnailImage))]
    private string? thumbnail = "videoimage.png";
    //public string? Thumbnail
    //{
    //    get => _thumbnail;
    //    set
    //    {
    //        if (_thumbnail != value)
    //        {
    //            _thumbnail = value;
    //            SetProperty(ref _thumbnail, value);
    //            OnPropertyChanged(nameof(ThumbnailImage));
    //        }
    //    }
    //}

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThumbnailImage))]
    private string? thumbnailBase64;
    //public string? ThumbnailBase64
    //{
    //    get => _thumbnailBase64;
    //    set
    //    {
    //        if (_thumbnailBase64 != value)
    //        {
    //            _thumbnailBase64 = value;
    //            OnPropertyChanged();
    //            OnPropertyChanged(nameof(ThumbnailImage));
    //        }
    //    }
    //}

    [ObservableProperty]
    private double progress;
    //public double Progress
    //{
    //    get => _progress;
    //    set { SetProperty(ref _progress, value); }
    //}

    [ObservableProperty]
    private string? speed;
    //public string? Speed
    //{
    //    get => _speed;
    //    set { SetProperty(ref _speed, value); }
    //}

    [ObservableProperty]
    private string? eta;
    //public string? ETA
    //{
    //    get => _eta;
    //    set { SetProperty(ref _eta, value); }
    //}

    [ObservableProperty]
    private bool isMerging;
    //public bool IsMerging
    //{
    //    get => _isMerging;
    //    set { SetProperty(ref _isMerging, value); }
    //}

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotDownloading))]
    private bool isDownloading;
    //public bool IsDownloading
    //{
    //    get => _isDownloading;
    //    set
    //    {
    //        SetProperty(ref _isDownloading, value);
    //        OnPropertyChanged(nameof(IsNotDownloading));
    //    }
    //}

    [ObservableProperty]
    private bool isCompleted;
    //public bool IsCompleted
    //{
    //    get => _isCompleted;
    //    set { SetProperty(ref _isCompleted, value); }
    //}

    [ObservableProperty]
    private string errorMessage = "";
    //public string ErrorMessage
    //{
    //    get => _errorMessage;
    //    set { SetProperty(ref _errorMessage, value); }
    //}

    public bool IsNotDownloading => !IsDownloading;

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

    //private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    //{
    //    if (!EqualityComparer<T>.Default.Equals(field, value))
    //    {
    //        field = value;
    //        OnPropertyChanged(propertyName);
    //    }
    //}
}
