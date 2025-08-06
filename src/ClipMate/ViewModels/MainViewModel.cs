using ClipMate.Helpers;
using ClipMate.Models;
using ClipMate.Services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using YtdlpDotNet;

namespace ClipMate.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    public SortableObservableCollection<DownloadJob> Jobs { get; set; }
    public ObservableCollection<MediaFormat> Formats { get; } = new ObservableCollection<MediaFormat>();

    private readonly ConnectivityCheck? _connectivityCheck;
    private readonly Dictionary<DownloadJob, CancellationTokenSource> _downloadTokens = new();
    private readonly YtdlpService _ytdlpService;
    private readonly JsonService _jsonService;
    private readonly AppLogger _logger;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private ConnectionState _connectionStatus;

    [ObservableProperty]
    private string _outputPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

    [ObservableProperty]
    private string _outputTemplate = "%(upload_date)s_%(title)s_.%(ext)s";

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _videoTitle = string.Empty;

    [ObservableProperty]
    private string _thumbnailUrl = string.Empty;

    [ObservableProperty]
    private MediaFormat? _selectedFormat;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotDefaultFilter))]
    private string _currentFilter = "all";

    public bool IsNotDefaultFilter => !string.IsNullOrWhiteSpace(CurrentFilter) && CurrentFilter.ToLowerInvariant() != "all";
    public string EmptyViewText => IsNotDefaultFilter ? $"No {CurrentFilter.ToLowerInvariant()} downloads" : "Your download queue is empty";

    public MainViewModel()
    {
        ConnectionStatus = ConnectionState.Available;
        OutputPath = AppSettings.OutputFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        OutputTemplate = AppSettings.OutputTemplate ?? "%(upload_date)s_%(title).80s_%(format_id)s.%(ext)s";

        _connectivityCheck = new ConnectivityCheck(OnConnectivityChanged);
        _logger = new AppLogger();
        _ytdlpService = new YtdlpService(_logger);
        _jsonService = new JsonService();

        Jobs = new SortableObservableCollection<DownloadJob>(sortKeySelector: d => GetStatusPriority(d.Status), propertyToWatch: nameof(DownloadJob.Status));

        InitializeAsync();
    }

    private async void InitializeAsync() => await LoadDownloadListAsync();

    private void OnConnectivityChanged(ConnectionState state) => ConnectionStatus = state;

    public void Dispose()
    {
        _connectivityCheck?.Dispose();
        Jobs.Dispose();
    }

    // Relay commands for UI actions
    [RelayCommand]
    private async Task AnalyzeAsync()
    {
        if (!IsValidUrl(Url))
        {
            IsAnalyzed = false;
            IsAnalizing = false;
            await ShowToastAsync("Please enter a valid video URL.");
            return;
        }

        try
        {
            _cts = new CancellationTokenSource();
            IsAnalizing = true;
            IsAnalyzed = false;

            ThumbnailUrl = "videoimage.png";

            Formats.Clear();

            var metadata = await _ytdlpService.GetMetadataAsync(Url, _cts.Token);

            if (metadata == null)
            {
                IsAnalyzed = false;
                IsAnalizing = false;
                await ShowToastAsync("Failed to retrieve video metadata. Please check the URL.");
                return;
            }

            // Set video title
            VideoTitle = metadata.Title ?? Url;
            // Set thumbnail URL
            ThumbnailUrl = metadata.Thumbnail ?? string.Empty;

            //var results = await _ytdlpService.GetFormatsAsync(Url, _cts.Token);

            var fileSize = metadata.RequestedFormats?.Sum(x => x.Filesize) ?? 0; // bytes   
            var fileSizeFormatted = fileSize > 0 ? $"{fileSize / (1024 * 1024):F2} Mb" : "n/a";
            var fps = metadata.RequestedFormats?.FirstOrDefault()?.Fps ?? 0;
            var formatedFps = fps > 0 ? $"{fps} fps" : "n/a";

            var autoFormat = new MediaFormat
            {
                Id = "b",
                Extension = "mp4",
                Resolution = $"auto {metadata.RequestedFormats?.FirstOrDefault(x => !x.IsAudio)?.Resolution}" ?? "auto",
                FileSize = fileSizeFormatted,
                Fps = formatedFps,
                Channels = metadata.RequestedFormats?.FirstOrDefault(x => x.IsAudio)?.AudioChannels.ToString() ?? "n/a",
                VCodec = metadata.RequestedFormats?.FirstOrDefault(x => !x.IsAudio)?.Vcodec ?? "n/a",
                ACodec = metadata.RequestedFormats?.FirstOrDefault(x => x.IsAudio)?.Acodec ?? "n/a",
                MoreInfo = "n/a",
            };

            Formats.Add(autoFormat);

            foreach (var f in metadata?.Formats!)
            {
                fileSizeFormatted = f.Filesize.HasValue ? $"{f.Filesize.Value / (1024 * 1024):F2} Mb" : "n/a";
                formatedFps = f.Fps > 0 ? $"{f.Fps} fps" : "n/a";

                var mediaFormat = new MediaFormat
                {
                    Id = f.FormatId,
                    Extension = f.Ext ?? "n/a",
                    Resolution = f.Resolution ?? "n/a",
                    FileSize = fileSizeFormatted,
                    Fps = f.Fps.ToString() ?? "n/a",
                    Channels = f.AudioChannels.ToString() ?? "n/a",
                    VCodec = f.Vcodec ?? "n/a",
                    ACodec = f.Acodec ?? "n/a",
                    MoreInfo = "n/a",
                };

                Formats.Add(mediaFormat);
            }

            if (Formats.Any())
                SelectedFormat = Formats.First();

            IsAnalyzed = true;
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogType.Info, "Analysis cancelled by user.");
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, ex.Message);
        }
        finally
        {
            IsAnalizing = false;
        }
    }

    [RelayCommand]
    private void CancelAnalyze()
    {
        try
        {
            if (!_cts!.IsCancellationRequested)
            {
                _cts.Cancel();
                IsAnalyzed = false;
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, ex.Message);
        }
    }

    [RelayCommand]
    private void CloseAnalyzeView()
    {
        try
        {
            IsAnalyzed = false;
            VideoTitle = string.Empty;
            ThumbnailUrl = "videoimage.png";
            Formats.Clear();
            SelectedFormat = null;
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, ex.Message);
        }
    }

    [RelayCommand]
    private async Task<DownloadJob?> AddJobAsync()
    {
        if (string.IsNullOrWhiteSpace(Url))
            return null;

        // Check for duplicate URL (case-insensitive)
        bool alreadyExists = Jobs.Any(j =>
            string.Equals(j.Url, Url, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(j.FormatId, SelectedFormat?.Id, StringComparison.OrdinalIgnoreCase));

        if (alreadyExists)
        {
            await ShowToastAsync("This format is already in your queue for this video!");
            return null;
        }

        try
        {
            var job = new DownloadJob
            {
                Url = Url,
                Title = VideoTitle,
                Thumbnail = ThumbnailUrl,
                FormatId = SelectedFormat?.Id ?? "b",
                MediaFormat = SelectedFormat,
                FileSize = SelectedFormat?.FileSize ?? "n/a",
                OutputPath = OutputPath,
                ErrorMessage = string.Empty,
                IsCompleted = false,
                IsDownloading = false,
                IsMerging = false,
                Status = DownloadStatus.Pending,
            };

            Jobs.Insert(0, job);
            return job;
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, ex.Message);
            return null;
        }
        finally
        {
            IsAnalyzed = false;
            Url = string.Empty;
            _jsonService.Save(Jobs);
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task AddJobAndDownloadAsync()
    {
        try
        {
            var job = await AddJobAsync();
            if (job != null)
                await StartDownloadAsync(job);
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, ex.Message);
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task StartDownloadAsync(DownloadJob job)
    {
        if (job == null || job.IsDownloading)
            return;

        var cts = new CancellationTokenSource();
        _downloadTokens[job] = cts;

        try
        {
            job.Status = DownloadStatus.Downloading;
            job.IsDownloading = true;

            await _ytdlpService.ExecuteDownloadAsync(job, cts.Token);

            job.Progress = 0;

            _jsonService.Save(Jobs);

            if (string.IsNullOrWhiteSpace(job.ErrorMessage))
                await ShowToastAsync($"Download finished successfully for: {job.Url}");
            else
                await ShowToastAsync(job.ErrorMessage);
        }
        catch (OperationCanceledException)
        {
            job.Status = DownloadStatus.Cancelled;
            job.Progress = 0;
            job.IsDownloading = false;
            job.ErrorMessage = "Download canceled.";

            _jsonService.Save(Jobs);
            await ShowToastAsync(job.ErrorMessage);
        }
        catch (Exception ex)
        {
            job.Status = DownloadStatus.Failed;
            job.Progress = 0;
            job.IsDownloading = false;
            job.ErrorMessage = $"Unexpected error: {ex.Message}";

            _logger.Log(LogType.Error, ex.Message);
            _jsonService.Save(Jobs);
            await ShowToastAsync(job.ErrorMessage);
        }
        finally
        {
            _downloadTokens.Remove(job);
        }
    }

    [RelayCommand]
    private void CancelDownload(DownloadJob job)
    {
        try
        {
            if (_downloadTokens.TryGetValue(job, out var cts))
            {
                cts.Cancel();
                job.IsDownloading = false;
                job.IsCompleted = false;
                job.Status = DownloadStatus.Cancelled;
                _downloadTokens.Remove(job);
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, ex.Message);
        }
        finally
        {
            _jsonService.Save(Jobs);
        }
    }

    [RelayCommand]
    private void RemoveJob(DownloadJob job)
    {
        try
        {
            Jobs.Remove(job);
            _jsonService.Save(Jobs);
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, ex.Message);
        }
    }

    [RelayCommand]
    private void OpenFolder(DownloadJob? job)
    {
        if (job == null) return;

        try
        {
#if WINDOWS
            Process.Start("explorer",  job.OutputPath);
#endif
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, ex.Message);
        }
    }

    [RelayCommand]
    private async Task ClipboardCopyAsync(DownloadJob? job)
    {
        try
        {
            if (job != null)
                await Clipboard.Default.SetTextAsync(job.Url);
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, ex.Message);
        }
    }

    [RelayCommand]
    private async Task ClipboardPasteAsync()
    {
        try
        {
            var clipboardText = await Clipboard.Default.GetTextAsync();
            if (!string.IsNullOrWhiteSpace(clipboardText) && IsValidUrl(clipboardText))
            {
                Url = clipboardText;
                await AnalyzeAsync();
            }
            else
            {
                await ShowToastAsync("Clipboard is empty or does not contain a valid URL.");
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, ex.Message);
        }
    }

    [RelayCommand]
    private async Task OpenLinkAsync(string url)
    {
        try
        {
            await Launcher.Default.OpenAsync(url);
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, ex.Message);
        }
    }

    [RelayCommand]
    private async Task ChangeOutputFolderAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await FolderPicker.Default.PickAsync(cancellationToken);
            if (result.IsSuccessful)
            {
                AppSettings.OutputFolder = result.Folder.Path;
                OutputPath = AppSettings.OutputFolder;
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, ex.Message);
        }
    }

    [RelayCommand]
    private async Task LoadDownloadListAsync()
    {
        IsBusy = true;

        var jobs = await _jsonService.GetAsync();

        foreach (var job in jobs)
        {
            // Normalize invalid or stale states
            switch (job.Status)
            {
                case DownloadStatus.Downloading:
                    job.Status = DownloadStatus.Pending;
                    job.IsCompleted = false;
                    break;

                case DownloadStatus.Failed:
                    job.IsCompleted = false;
                    break;
            }

            // Common reset values for non-complete states
            job.IsDownloading = false;
            job.ErrorMessage = string.Empty;
            job.Progress = 0;
            //job.Eta = string.IsNullOrWhiteSpace(job.Eta) ? "N/A" : job.Eta;
            //job.Speed = string.IsNullOrWhiteSpace(job.Speed) ? "N/A" : job.Speed;

            Jobs.Add(job);
        }

        IsBusy = false;
    }

    // Sort downloads by status priority
    private int GetStatusPriority(DownloadStatus status) => status switch
    {
        DownloadStatus.Downloading => 0,
        DownloadStatus.Pending => 1,
        DownloadStatus.Failed or DownloadStatus.Cancelled => 2,
        DownloadStatus.Completed => 3,
        _ => 4
    };

    // Helper method to validate URL format
    private static bool IsValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        url = url.Trim();

        if (Uri.TryCreate(url, UriKind.Absolute, out var uriResult))
        {
            bool isValidScheme = uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps;
            return isValidScheme;
        }
        else
        {
            return false;
        }
    }

    // Toast settings
    public async Task ShowToastAsync(string message)
    {
        try
        {
            var toast = Toast.Make(message, ToastDuration.Long, 14);
            await toast.Show(new CancellationTokenSource().Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

}
