using Microsoft.WindowsAPICodePack.Taskbar;
using System.Reflection;
using VideoDownloader.Core;
using VideoDownloader.UI;
using YtdlpNET;

namespace VideoDownloader;

public partial class frmMain : Form
{
    private const string YTDLP_PATH = @".\Tools\yt-dlp.exe";
    private const string FFMPEG_PATH = @".\Tools\ffmpeg.exe";

    private const string DefaultOutputTemplate =
        "%(upload_date>%Y-%m-%d)s - %(title).90s [%(resolution)s].%(ext)s";

    private readonly string _downloadPath =
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

    private readonly AppLogger _logger = new();

    private YtdlpService _service = null!;
    private readonly DownloadSession _session = new();


    public frmMain()
    {
        InitializeComponent();
        textOutput.Text = _downloadPath;
    }

    private async void frmMain_Load(object sender, EventArgs e)
    {
        _service = new YtdlpService(YTDLP_PATH, _logger);

        _service.Progress += OnProgress;
        _service.Log += AppendDetail;
        _service.Error += OnError;
        _service.Completed += OnCompleted;
        _service.MergeCompleted += OnMergeCompleted;

        var version = await _service.GetVersionAsync();
        UpdateStatus($"Engine ready ({version})");
    }

    //---------------------------------------------
    // Analyze
    //---------------------------------------------

    private async void btnAnalyze_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(textUrl.Text))
        {
            ShowError("Enter URL");
            return;
        }

        SetAnalyzeState(true);

        var formats = await _service.GetFormatsAsync(textUrl.Text);
        UpdateQuality(formats);

        SetAnalyzeState(false);
    }

    private void SetAnalyzeState(bool state)
    {
        UIThread.Run(progressDownload, () =>
        {
            progressDownload.Style = state
                ? ProgressBarStyle.Marquee
                : ProgressBarStyle.Blocks;

            TaskbarManager.Instance.SetProgressState(
                state
                    ? TaskbarProgressBarState.Indeterminate
                    : TaskbarProgressBarState.NoProgress);
        });
    }

    private void UpdateQuality(List<Format> formats)
    {
        UIThread.Run(comboQuality, () =>
        {
            comboQuality.Format -= ComboQuality_Format;
            comboQuality.Format += ComboQuality_Format;

            comboQuality.DataSource = formats
                .Where(f => !f.IsStoryboard)
                .ToList();

            comboQuality.ValueMember = "Id";
        });
    }

    private void ComboQuality_Format(object? sender, ListControlConvertEventArgs e)
    {
        if (e.ListItem is not Format f)
            return;

        if (f.IsVideo)
            e.Value = $"{f.Height}p • {f.Extension}";
        else
            e.Value = $"Audio • {f.Extension}";
    }

    //---------------------------------------------
    // Download
    //---------------------------------------------

    private async void buttonDownload_Click(object sender, EventArgs e)
    {
        if (comboQuality.SelectedItem is not Format format)
        {
            ShowError("Select quality");
            return;
        }

        string fmt = format.IsVideo
            ? $"{format.Id}+bestaudio"
            : format.Id;

        DisableControls();

        await _service.DownloadAsync(
            textUrl.Text,
            fmt,
            textOutput.Text,
            FFMPEG_PATH,
            DefaultOutputTemplate);
    }

    //---------------------------------------------
    // Events from service
    //---------------------------------------------

    private void OnProgress(DownloadProgressEventArgs e)
    {
        if ((DateTime.Now - _session.LastProgressUpdate).TotalMilliseconds < 120)
            return;

        _session.LastProgressUpdate = DateTime.Now;

        int p = (int)e.Percent;

        UIThread.Run(progressDownload, () =>
        {
            progressDownload.Value = p;
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
            TaskbarManager.Instance.SetProgressValue(p, 100);
        });

        UpdateStatus("Downloading", p, e.Size, e.Speed, e.ETA);
    }

    private void OnCompleted()
    {
        UpdateStatus("Download completed");
        progressDownload.Value = 0;
    }

    private void OnMergeCompleted()
    {
        EnableControls();
        UpdateStatus("Merging completed");

        if (checkAutoClose.Checked && !_session.HasError)
            Close();
    }

    private void OnError(string msg)
    {
        _session.HasError = true;
        AppendDetail(msg);
        EnableControls();
        UpdateStatus("Error");
    }

    //---------------------------------------------
    // UI helpers
    //---------------------------------------------

    private void AppendDetail(string text)
    {
        UIThread.Run(textDetail, () =>
        {
            textDetail.AppendText(text + Environment.NewLine);
        });
    }

    private void UpdateStatus(
        string status,
        int progress = 0,
        string size = "",
        string speed = "",
        string eta = "")
    {
        UIThread.Run(this, () =>
        {
            toolStripLabelStatus.Text = status;
            toolStripLabelProgress.Text = progress == 0 ? "" : $"{progress}%";
            toolStripLabelSize.Text = size;
            toolStripLabelSpeed.Text = speed;
            toolStripLabelETA.Text = eta;
        });
    }

    private void DisableControls()
    {
        UIThread.Run(this, () =>
        {
            buttonDownload.Enabled = false;
            comboQuality.Enabled = false;
            textUrl.Enabled = false;
            textOutput.Enabled = false;
        });
    }

    private void EnableControls()
    {
        UIThread.Run(this, () =>
        {
            buttonDownload.Enabled = true;
            comboQuality.Enabled = true;
            textUrl.Enabled = true;
            textOutput.Enabled = true;
        });
    }

    private void ShowError(string message)
    {
        MessageBox.Show(message, "Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
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
}
