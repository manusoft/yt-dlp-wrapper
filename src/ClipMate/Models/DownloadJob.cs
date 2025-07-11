using System.ComponentModel;
using System.Runtime.CompilerServices;
using YtdlpDotNet;

namespace ClipMate.Models;

public class DownloadJob : INotifyPropertyChanged
{
    private string _url = string.Empty;
    public string Url
    {
        get => _url;
        set { _url = value; OnPropertyChanged(); }
    }
    private MediaFormat? _format;
    public MediaFormat? Format
    {
        get => _format;
        set
        {
            if (_format != value)
            {
                _format = value;
                OnPropertyChanged();
            }
        }
    }

    public string OutputPath { get; set; } = string.Empty;

    private DownloadStatus _status = DownloadStatus.Pending;
    public DownloadStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    private string _thumbnail = "dotnet_bot.png";
    public string Thumbnail
    {
        get => _thumbnail;
        set
        {
            if (_thumbnail != value)
            {
                _thumbnail = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ThumbnailImage)); // update Image binding
            }
        }
    }

    private double _progress;
    public double Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(); }
    }

    private string? _speed;
    public string? Speed
    {
        get => _speed;
        set { _speed = value; OnPropertyChanged(); }
    }

    private string? _eta;
    public string? ETA
    {
        get => _eta;
        set { _eta = value; OnPropertyChanged(); }
    }

    private bool _isMerging;
    public bool IsMerging
    {
        get => _isMerging;
        set { _isMerging = value; OnPropertyChanged(); }
    }

    private bool _isCompleted;
    public bool IsCompleted
    {
        get => _isCompleted;
        set { _isCompleted = value; OnPropertyChanged(); }
    }

    private string _errorMessage = "";
    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    public ImageSource? ThumbnailImage => string.IsNullOrWhiteSpace(Thumbnail) ? null : ImageSource.FromFile(Thumbnail);

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
