using Microsoft.WindowsAPICodePack.Taskbar;
using System.Reflection;
using System.Threading.Tasks;
using YtdlpDotNet;

namespace VideoDownloader;

public partial class frmMain : Form
{
    private const string YTDLP_PATH = @".\Tools\yt-dlp.exe";
    private const string DEFAULT_YTDLP_VERSION = "2025.12.08";
    private const string BEST_FORMAT = "bestvideo+bestaudio";
    private const string DefaultOutputTemplate = "%(upload_date>%Y-%m-%d)s - %(title).90s [%(resolution)s - %(format_id)s].%(ext)s";
    private readonly string _downloadPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
    private readonly Ytdlp _ytdlp;
    private readonly ILogger _logger = new ConsoleLogger();
    private bool _hasError;
    private DateTime _lastProgressUpdate = DateTime.MinValue;

    public frmMain()
    {
        InitializeComponent();
        try
        {
            _ytdlp = new Ytdlp(YTDLP_PATH, _logger);
            SubscribeToYtdlpEvents();
            textOutput.Text = _downloadPath;
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Failed to initialize yt-dlp: {ex.Message}");
            UpdateStatus("Engine failed to start.");
        }
    }

    private async void frmMain_Load(object sender, EventArgs e)
    {
        try
        {
            var version = GetVersion();
            this.Text = $"Video Downloader v{version[0]}.{version[1]}.{version[2]}";
            var ytdlpVersion = await GetYtdlpVersionAsync();
            comboQuality.Enabled = false;
            buttonDownload.Enabled = false;
            UpdateStatus($"Engine started successfully. v{ytdlpVersion}");
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Error during form load: {ex.Message}");
            UpdateStatus("Failed to initialize application.");
        }
    }

    private async Task<string> GetYtdlpVersionAsync()
    {
        try
        {
            return await _ytdlp.GetVersionAsync() ?? DEFAULT_YTDLP_VERSION;
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Failed to get yt-dlp version: {ex.Message}");
            return DEFAULT_YTDLP_VERSION;
        }
    }

    private bool _isMerging;
    private void SubscribeToYtdlpEvents()
    {
        _ytdlp.OnError += error => _logger.Log(LogType.Error, $"Error: {error}");
        _ytdlp.OnOutputMessage += (s, e) => AppendTextDetail(e);
        _ytdlp.OnProgressMessage += (s, e) =>
        {
            _logger.Log(LogType.Info, e);
            if (e.Contains("Merging formats"))
            {
                _isMerging = true;
                _logger.Log(LogType.Info, "Set _isMerging=true in OnProgressMessage");
            }
        };
        _ytdlp.OnProgressDownload += HandleProgressDownload;
        _ytdlp.OnCompleteDownload += HandleCompleteDownload;
        _ytdlp.OnPostProcessingComplete += (s, e) =>
        {
            _logger.Log(LogType.Info, $"Subscribed OnPostProcessingComplete event triggered: {e}");
            HandlePostProcessingComplete(s, e);
        }; // Handle merging completion
        _ytdlp.OnErrorMessage += HandleErrorMessage;
        _ytdlp.OnCommandCompleted += (success, message) =>
        {
            _logger.Log(success ? LogType.Info : LogType.Error, message);
            if (success && !_hasError && checkAutoClose.Checked && !_isMerging)
            {
                _logger.Log(LogType.Info, "Auto-closing after command completion (no merging).");
                Application.DoEvents();
                Application.Exit();
            }
        };
        _logger.Log(LogType.Info, "Subscribed to all Ytdlp events.");
    }


    private void HandleProgressDownload(object? sender, DownloadProgressEventArgs e)
    {
        try
        {
            if ((DateTime.Now - _lastProgressUpdate).TotalMilliseconds < 100) return;
            _lastProgressUpdate = DateTime.Now;
            int progress = (int)Math.Round(double.Parse(e.Percent.ToString()));
            UpdateProgressBar(progress);
            UpdateStatus("Downloading...", progress, e.Size, e.Speed, e.ETA);
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Progress download error: {ex.Message}");
        }
    }

    private async void HandleCompleteDownload(object? sender, string e)
    {
        try
        {
            _logger.Log(LogType.Info, $"HandleCompleteDownload called: {e}, _isMerging={_isMerging}");
            AppendTextDetail(e);
            // Only auto-close if no merging is expected
            if (!_hasError && checkAutoClose.Checked && !_isMerging)
            {
                _logger.Log(LogType.Info, "Auto-closing application (no merging required).");
                UpdateStatus("Download completed.");
                await Task.Delay(5000);
                Application.Exit();
            }
            else
            {
                UpdateStatus("Download completed.");
                EnableControls();
            }

        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Complete download error: {ex.Message}");
        }
    }

    private void HandlePostProcessingComplete(object? sender, string e)
    {
        try
        {
            _logger.Log(LogType.Info, $"Post-processing complete: {e}");
            AppendTextDetail(e);
            UpdateProgressBar(100);
            UpdateStatus("Download and merging completed.");
            _isMerging = false; // Reset after merging
            Application.DoEvents();
            if (!_hasError && checkAutoClose.Checked)
            {
                _logger.Log(LogType.Info, "Calling Application.Exit() for auto-close.");
                Application.Exit();
            }
            else
            {
                _logger.Log(LogType.Info, $"Not auto-closing: _hasError={_hasError}, checkAutoClose={checkAutoClose.Checked}");
                EnableControls();
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Post-processing completion error: {ex.Message}");
            AppendTextDetail($"Completion error: {ex.Message}");
            UpdateStatus("Post-processing failed.");
            EnableControls();
        }
    }

    private void HandleErrorMessage(object? sender, string e)
    {
        try
        {
            _hasError = true;
            _logger.Log(LogType.Error, $"Setting _hasError due to: {e}");
            UpdateProgressBar(0);
            AppendTextDetail(e);
            UpdateStatus("Download or merging failed.");
            EnableControls();
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Error message handling: {ex.Message}");
            AppendTextDetail($"Error handling failed: {ex.Message}");
        }
    }

    private void UpdateProgressBar(int progress)
    {
        InvokeIfRequired(progressDownload, () =>
        {
            progressDownload.Value = progress;
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
            TaskbarManager.Instance.SetProgressValue(progress, 100);
        });
    }

    private void AppendTextDetail(string text)
    {
        InvokeIfRequired(textDetail, () =>
        {
            textDetail.AppendText($"{text}{Environment.NewLine}");
        });
    }

    private void InvokeIfRequired(Control control, Action action)
    {
        if (control.InvokeRequired)
        {
            control.Invoke(action);
        }
        else
        {
            action();
        }
    }

    private async void textUrl_TextChanged(object sender, EventArgs e)
    {
        await AnalyzeAsync();
    }

    private async void buttonDownload_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(textUrl.Text))
        {
            ShowError("Please enter a video URL.");
            return;
        }

        await DownloadVideoAsync(textUrl.Text, comboQuality.SelectedItem as VideoFormat);
    }

    private async Task<List<VideoFormat>> GetVideoFormatsAsync(string url)
    {
        try
        {
            return await _ytdlp.GetAvailableFormatsAsync(url) ?? new List<VideoFormat>();
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Failed to get video formats: {ex.Message}");
            return new List<VideoFormat>();
        }
    }

    private async Task AnalyzeAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(textUrl.Text) || !textUrl.Text.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                comboQuality.Enabled = false;
                buttonDownload.Enabled = false;
                UpdateStatus("Idle");
                return;
            }

            SetAnalyzingState(true);
            var formats = await GetVideoFormatsAsync(textUrl.Text);
            UpdateQualityCombo(formats);
            SetAnalyzingState(false);
            UpdateStatus(formats.Any() ? "Analyzed" : "No formats available.");
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Analysis error: {ex.Message}");
            AppendTextDetail($"Analysis error: {ex.Message}");
            SetAnalyzingState(false);
            UpdateStatus("Analysis failed.");
        }
    }

    private void SetAnalyzingState(bool isAnalyzing)
    {
        InvokeIfRequired(progressDownload, () =>
        {
            progressDownload.Style = isAnalyzing ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
            progressDownload.MarqueeAnimationSpeed = isAnalyzing ? 10 : 100;
            TaskbarManager.Instance.SetProgressState(isAnalyzing ? TaskbarProgressBarState.Indeterminate : TaskbarProgressBarState.NoProgress);
        });
    }

    private void UpdateQualityCombo(List<VideoFormat> formats)
    {
        InvokeIfRequired(comboQuality, () =>
        {
            comboQuality.DataSource = formats.Any() ? formats : new List<VideoFormat>
                {
                    new VideoFormat { ID = "best", Resolution = "Best" },
                    new VideoFormat { ID = "worst", Resolution = "Worst" }
                };
            comboQuality.ValueMember = "ID";
            comboQuality.DisplayMember = null;
            comboQuality.Format += (s, e) =>
            {
                var format = e.ListItem as VideoFormat;
                e.Value = $"{format?.Resolution} ({format?.Extension}, {format?.FileSize ?? "Unknown"})";
            };
            comboQuality.Enabled = true;
            buttonDownload.Enabled = true;
        });
    }

    private async Task DownloadVideoAsync(string url, VideoFormat? quality)
    {
        try
        {
            _hasError = false;
            _isMerging = false;
            UpdateStatus("Preparing to download...");
            DisableControls();
            AppendTextDetail($"Downloading video from: {url}");
            UpdateProgressBar(0);


            if (quality != null)
            {
                var isAudio = quality.Channels is null ? false : true;

                // If audio, just use formatId or "b" for best
                // If video, merge with best audio
                string format = isAudio
                    ? (quality.ID ?? "b")
                    : (quality.ID != null ? $"{quality.ID}+bestaudio" : "best");

                await _ytdlp
                    .SetFormat(format)
                    .AddCustomCommand("--restrict-filenames")
                    .AddCustomCommand("--js-runtimes deno") // this line not fix the js error, can remove this line.
                    .AddCustomCommand("--remote-components ejs:npm")
                    .SetOutputTemplate(DefaultOutputTemplate)
                    .ExecuteAsync(url);
            }
            else
            {
                _hasError = true;
                ShowError("Please select a video quality.");
                EnableControls();
            }
        }
        catch (Exception ex)
        {
            _hasError = true;
            _logger.Log(LogType.Error, $"Download error: {ex.Message}");
            AppendTextDetail($"Error: {ex.Message}");
            UpdateStatus("Download failed.");
            EnableControls();
        }
    }


    private void DisableControls()
    {
        InvokeIfRequired(this, () =>
        {
            buttonBrowseFolder.Enabled = false;
            comboQuality.Enabled = false;
            buttonDownload.Enabled = false;
            textOutput.Enabled = false;
            textUrl.Enabled = false;
        });
    }

    private void EnableControls()
    {
        InvokeIfRequired(this, () =>
        {
            buttonBrowseFolder.Enabled = true;
            textOutput.Enabled = true;
            textUrl.Enabled = true;
            buttonDownload.Enabled = !_hasError;
        });
    }

    private void UpdateStatus(string status, int progress = 0, string size = "", string speed = "", string eta = "")
    {
        InvokeIfRequired(this, () =>
        {
            toolStripLabelStatus.Text = $"Status: {status}";
            toolStripLabelProgress.Text = progress == 0 ? "" : $"Progress: {progress}%";
            toolStripLabelSize.Text = string.IsNullOrEmpty(size) ? "" : $"Size: {size}";
            toolStripLabelSpeed.Text = string.IsNullOrEmpty(speed) ? "" : $"Speed: {speed}";
            toolStripLabelETA.Text = string.IsNullOrEmpty(eta) ? "" : $"ETA: {eta}";
        });
    }

    private void ShowError(string message)
    {
        _logger.Log(LogType.Error, message);
        AppendTextDetail($"Error: {message}");
        MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void buttonBrowseFolder_Click(object sender, EventArgs e)
    {
        try
        {
            using var folderBrowserDialog = new FolderBrowserDialog
            {
                Description = "Select the folder to save the video",
                ShowNewFolderButton = true
            };
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                textOutput.Text = folderBrowserDialog.SelectedPath;
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Folder selection error: {ex.Message}");
            ShowError($"Failed to select folder: {ex.Message}");
        }
    }

    public string[] GetVersion()
    {
        try
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            return version?.Split('.') ?? new[] { "1", "7", "0" };
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Failed to get application version: {ex.Message}");
            return new[] { "1", "8", "0" };
        }
    }

    private class ConsoleLogger : ILogger
    {
        public void Log(LogType type, string message)
        {
            Console.WriteLine($"[{type}] {message}");
        }
    }
}
