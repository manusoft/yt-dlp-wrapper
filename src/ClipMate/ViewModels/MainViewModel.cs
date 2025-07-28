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
    public ObservableCollection<DownloadJob> Jobs { get; set; } = new ObservableCollection<DownloadJob>();
    public ObservableCollection<MediaFormat> Formats { get; } = new ObservableCollection<MediaFormat>();

    private readonly ConnectivityCheck _connectivityTest;
    private readonly Dictionary<DownloadJob, CancellationTokenSource> _downloadTokens = new();
    private readonly YtdlpService _ytdlpService;
    private readonly JsonService _jsonService;
    private readonly AppLogger _logger;

    [ObservableProperty]
    private string _connectionStatus;

    [ObservableProperty]
    private string _outputPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private MediaFormat? _selectedFormat;

    public MainViewModel()
    {
        _connectivityTest = new ConnectivityCheck(OnConnectivityChanged);
        _logger = new AppLogger();
        _ytdlpService = new YtdlpService(_logger);
        _jsonService = new JsonService();
        InitializeAsync();
    }

    private async void InitializeAsync() => await LoadDownloadListAsync();
    private async void OnConnectivityChanged(string message)
    { 
        ConnectionStatus = message;
        if (!string.IsNullOrWhiteSpace(message))
            await ShowToastAsync(message);
    } 
    public void Dispose() => _connectivityTest.Dispose();


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
            IsAnalizing = true;
            IsAnalyzed = false;

            Formats.Clear();
            var results = await _ytdlpService.GetFormatsAsync(Url);

            var autoFormat = new MediaFormat
            {
                Id = "b",
                Extension = "mp4",
                Resolution = "Auto",
                FileSize = "N/A",
                Fps = "Auto",
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
                    Id = f.ID,
                    Extension = f.Extension ?? "N/A",
                    Resolution = f.Resolution ?? "N/A",
                    FileSize = f.FileSize ?? "N/A",
                    Fps = f.FPS ?? "N/A",
                    Channels = f.Channels ?? "N/A",
                    VCodec = f.VCodec ?? "N/A",
                    ACodec = f.ACodec ?? "N/A",
                    MoreInfo = f.MoreInfo ?? "N/A",
                };

                Formats.Add(mediaFormat);
            }

            if (Formats.Any())
                SelectedFormat = Formats.First();
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, ex.Message);
        }
        finally
        {
            IsAnalizing = false;
            IsAnalyzed = true;
        }
    }

    [RelayCommand]
    private async Task<DownloadJob?> AddJobAsync()
    {
        if (string.IsNullOrWhiteSpace(Url))
            return null;

        // Check for duplicate URL (case-insensitive)
        bool alreadyExists = Jobs.Any(j => string.Equals(j.Url, Url, StringComparison.OrdinalIgnoreCase));
        if (alreadyExists)
        {
            await ShowToastAsync("This download is already in your queue!");
            return null;
        }

        try
        {
            var job = new DownloadJob
            {
                Url = Url,
                Format = SelectedFormat,
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

            // Download completed successfully (no OperationCanceledException)
            // Delete temp thumbnail file
            if (File.Exists(job.Thumbnail))
            {
                try
                {
                    File.Delete(job.Thumbnail);
                    job.Thumbnail = null;
                }
                catch (Exception ex)
                {
                    _logger.Log(LogType.Error, $"Thumbnail delete error: {ex.Message}");
                }
            }

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
    public void CancelDownload(DownloadJob job)
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
                OutputPath = result.Folder.Path;
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, ex.Message);
        }
    }

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
            job.Eta = string.IsNullOrWhiteSpace(job.Eta) ? "N/A" : job.Eta;
            job.Speed = string.IsNullOrWhiteSpace(job.Speed) ? "N/A" : job.Speed;

            Jobs.Add(job);
        }

        IsBusy = false;
    }

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
