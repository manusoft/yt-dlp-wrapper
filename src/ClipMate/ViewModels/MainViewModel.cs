using ClipMate.Models;
using ClipMate.Services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using YtdlpDotNet;

namespace ClipMate.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    public string OutputPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
    private readonly YtdlpService _ytdlpService;
    private readonly JsonService _jsonService;

    public ObservableCollection<DownloadJob> Jobs { get; set; } = new ObservableCollection<DownloadJob>();
    public ObservableCollection<MediaFormat> Formats { get; } = new ObservableCollection<MediaFormat>();

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private MediaFormat? _selectedFormat;


    public MainViewModel()
    {
        _ytdlpService = new YtdlpService(new ConsoleLogger());
        _jsonService = new JsonService();
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        await LoadDownloadListAsync();
    }

    [RelayCommand]
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

        try
        {
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
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
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
            await ShowToastAsync("⚠️ This download is already in your queue!");
            return null;
        }

        try
        {
            var job = new DownloadJob
            {
                Url = Url,
                Format = SelectedFormat,
                OutputPath = OutputPath
            };

            Jobs.Insert(0, job);
            return job;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
        finally
        {
            IsAnalyzed = false;
            Url = string.Empty;
            _jsonService.Save(Jobs);
        }
    }

    [RelayCommand]
    private async Task AddJobAndDownloadAsync()
    {
        var job = await AddJobAsync();
        if (job != null)
            await StartDownloadAsync(job);
    }

    [RelayCommand]
    private async Task StartDownloadAsync(DownloadJob job)
    {
        await _ytdlpService.ExecuteDownloadAsync(job);

        try
        {
            if (File.Exists(job.Thumbnail))
            {
                File.Delete(job.Thumbnail);
                job.Thumbnail = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        _jsonService.Save(Jobs);

        if (string.IsNullOrEmpty(job.ErrorMessage))
            await ShowToastAsync($"✅ Download finished successfully for:{job.Url}");
        else
            await ShowToastAsync($"✅ {job.ErrorMessage}");
    }

    [RelayCommand]
    private void RemoveJob(DownloadJob job)
    {
        Jobs.Remove(job);
        _jsonService.Save(Jobs);
    }

    [RelayCommand]
    private void OpenFolder()
    {
        try
        {
#if WINDOWS
            Process.Start("explorer", OutputPath);
#endif
        }
        catch (Exception)
        {
        }
    }

    private async Task StartAllAsync()
    {
        foreach (var job in Jobs.Where(j => j.Status == DownloadStatus.Pending))
        {
            await _ytdlpService.ExecuteDownloadAsync(job);
        }
    }


    private async Task LoadDownloadListAsync()
    {
        var jobs = await _jsonService.GetAsync();

        foreach (var job in jobs)
            Jobs.Add(job);
    }

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
