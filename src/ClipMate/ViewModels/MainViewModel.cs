using ClipMate.Models;
using ClipMate.Services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YtdlpDotNet;

namespace ClipMate.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public ObservableCollection<DownloadJob> Jobs { get; } = new();
    public ObservableCollection<MediaFormat> Formats { get; } = new();
    public ICommand AnalyzeCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand StartAllCommand { get; }
    public ICommand StartCommand { get; }

    private string _url = string.Empty;
    public string Url
    {
        get => _url;
        set { _url = value; OnPropertyChanged(); }
    }

    private bool _isAnalizing = false;
    public bool IsAnalizing
    {
        get => _isAnalizing;
        set
        {
            if (_isAnalizing == value) return;
            _isAnalizing = value;
            OnPropertyChanged();
        }
    }

    private bool _isAnalyzed;
    public bool IsAnalyzed
    {
        get => _isAnalyzed;
        set { _isAnalyzed = value; OnPropertyChanged(); }
    }

    private MediaFormat? _selectedFormat;
    public MediaFormat? SelectedFormat
    {
        get => _selectedFormat;
        set
        {
            if (_selectedFormat != value)
            {
                _selectedFormat = value;
                OnPropertyChanged();
            }
        }
    }

    public string OutputPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
    private readonly YtdlpService _ytdlpService;

    public MainViewModel()
    {
        _ytdlpService = new YtdlpService(new ConsoleLogger());
        AddCommand = new Command(AddJob);
        StartCommand = new Command<DownloadJob>(async job => await Startsync(job));
        StartAllCommand = new Command(async () => await StartAllAsync());
        AnalyzeCommand = new Command(async () => await AnalyzeAsync());
    }

    private async Task AnalyzeAsync()
    {
        if (string.IsNullOrWhiteSpace(Url)) return;

        if (string.IsNullOrWhiteSpace(Url) || !Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            IsAnalyzed = false;
            IsAnalizing = false;
            await ShowToastAsync("🚫 Please enter a valid video URL.");
            return;
        }

        IsAnalizing = true;
        IsAnalyzed = false;

        Formats.Clear();
        var results = await _ytdlpService.GetFormatsAsync(Url);

        var autoFormat = new MediaFormat
        {
            ID = "b",
            Extension = "mp4",
            Resolution = "Auto",
            FileSize = "N/A",
            FPS = "Auto",
            Channels = "2",
            VCodec = "Best",
            ACodec = "Best",
            MoreInfo = "N/A",
        };

        Formats.Add(autoFormat);

        foreach (var f in results)
        {
            var mediaFormat = new MediaFormat
            {
                ID = f.ID,
                Extension = f.Extension ?? "N/A",
                Resolution = f.Resolution ?? "N/A",
                FileSize = f.FileSize ?? "N/A",
                FPS = f.FPS ?? "N/A",
                Channels = f.Channels ?? "N/A",
                VCodec = f.VCodec ?? "N/A",
                ACodec = f.ACodec ?? "N/A",
                MoreInfo = f.MoreInfo ?? "N/A",
            };

            Formats.Add(mediaFormat);
        }
        if (Formats.Any())
            SelectedFormat = Formats.First();
        OnPropertyChanged(nameof(SelectedFormat));

        IsAnalizing = false;
        IsAnalyzed = true;
    }

    private async void AddJob()
    {
        if (string.IsNullOrWhiteSpace(Url))
            return;

        // Check for duplicate URL (case-insensitive)
        bool alreadyExists = Jobs.Any(j => string.Equals(j.Url, Url, StringComparison.OrdinalIgnoreCase));
        if (alreadyExists)
        {
            await ShowToastAsync("⚠️ This download is already in your queue!");
            return;
        }
            

        var job = new DownloadJob
        {
            Url = Url,
            Format = SelectedFormat,
            OutputPath = OutputPath
        };

        Jobs.Add(job);
        IsAnalyzed =false;
        Url = string.Empty;
    }


    private async Task Startsync(DownloadJob job)
    {
        await _ytdlpService.ExecuteDownloadAsync(job);
    }

    private async Task StartAllAsync()
    {
        foreach (var job in Jobs.Where(j => j.Status == DownloadStatus.Pending))
        {
            await _ytdlpService.ExecuteDownloadAsync(job);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private class ConsoleLogger : ILogger
    {
        public void Log(LogType type, string message)
        {
            Console.WriteLine($"[{type}] {message}");
        }
    }

    // Toast settings
    public async Task ShowToastAsync(string message)
    {
        var toast = Toast.Make(message, ToastDuration.Long, 14);
        await toast.Show(new CancellationTokenSource().Token);
    }
}
