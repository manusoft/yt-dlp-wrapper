using Microsoft.WindowsAPICodePack.Taskbar;
using System.Reflection;
using YtDlpWrapper;

namespace VideoDownloader;

public partial class frmMain : Form
{
    private int progress = 0;
    private readonly Ytdlp engineV2 = new Ytdlp($"{AppContext.BaseDirectory}\\Tools\\yt-dlp.exe");
    private readonly string downloadPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

    public frmMain()
    {
        InitializeComponent();

        try
        {
            engineV2.OnErrorMessage += EngineV2_OnErrorMessage;
            engineV2.OnProgressMessage += EngineV2_OnProgressMessage;
            engineV2.OnOutputMessage += Enginev2_OnOutputMessage;
            engineV2.OnProgressDownload += EngineV2_OnProgressDownload;
            engineV2.OnCompleteDownload += Enginev2_OnCompleteDownload;
            textOutput.Text = downloadPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            UpdateStatus("Engine failed to start.");
        }
    }

    private void frmMain_Load(object sender, EventArgs e)
    {
        try
        {
            var version = GetVersion();
            var major = version.Split('.')[0];
            var minor = version.Split('.')[1];
            var build = version.Split('.')[2];
            var revision = version.Split('.')[3];
            this.Text = $"Video Downloader v{major}.{minor}.{build}";
            comboQuality.Enabled = false;
            buttonDownload.Enabled = false;
            ClearStatus();
            UpdateStatus("Engine started successfully.");
        }
        catch (Exception) { }
    }

    private void EngineV2_OnProgressDownload(object? sender, DownloadProgressEventArgs e)
    {
        try
        {
            // Parse e.Percent as a double
            double percent = double.Parse(e.Percent.ToString());

            // Convert the double to an integer for progress (if needed)
            progress = (int)Math.Round(percent); // Rounds to the nearest integer
            progressDownload.Value = progress;
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
            TaskbarManager.Instance.SetProgressValue(progress, 100);

            // Update the status
            UpdateStatus("Downloading...", progress, e.Size, e.Speed, e.ETA);
        }
        catch (Exception) { }
    }

    private void EngineV2_OnErrorMessage(object? sender, string e)
    {
        try
        {
            progressDownload.Value = 0;
            textDetail.AppendText($"{e}" + Environment.NewLine);
            ClearStatus();
        }
        catch (Exception) { }
    }

    private void Enginev2_OnCompleteDownload(object? sender, string e)
    {
        try
        {
            progressDownload.Value = 100;
            textDetail.AppendText($"{e}" + Environment.NewLine);
            ClearStatus();
        }
        catch (Exception) { }
    }

    private void Enginev2_OnOutputMessage(object? sender, string e)
    {
        try
        {
            textDetail.AppendText($"{e}" + Environment.NewLine);
        }
        catch (Exception) { }
    }

    private void EngineV2_OnProgressMessage(object? sender, string e)
    {
        Console.WriteLine(e);
    }


    private async void textUrl_TextChanged(object sender, EventArgs e)
    {
        await AnalizingAsync();
    }

    private async void buttonDownload_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(textUrl.Text))
        {
            MessageBox.Show("Please enter the video URL", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        await DownloadVideoAsync(textUrl.Text, comboQuality.SelectedItem as VideoFormat);
    }

    private async Task<List<VideoFormat>> GetVideoFormatsAsync(string url)
    {
        try
        {
            return await engineV2.GetAvailableFormatsAsync(url);
        }
        catch (Exception)
        {
            return new List<VideoFormat>();
        }
    }

    private async Task DownloadVideoAsync(string url, VideoFormat? quality)
    {
        try
        {
            UpdateStatus("Preparing to download...");
            DisableControls();
            //textUrl.Clear();
            textDetail.Clear();
            textDetail.AppendText($"Downloading video from: {url}" + Environment.NewLine);

            int progress = 0;
            progressDownload.Value = progress;
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
            TaskbarManager.Instance.SetProgressValue(progress, 100);

            var selectedFormat = quality;

            if (radioAuto.Checked)
            {
                await engineV2.SetOutputFolder(textOutput.Text.Trim()).ExecuteAsync(url);                
            }
            else
            {
                if (selectedFormat != null)
                {

                    await engineV2.SetOutputFolder(textOutput.Text.Trim())
                        .SetFormat(selectedFormat.ID)
                        .ExecuteAsync(url);
                }
                else
                {
                    MessageBox.Show("Please select a video quality", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (checkAutoClose.Checked)
            {
                Application.Exit();
            }
            else
            {
                ClearStatus();
                EnableControls();
            }

        }
        catch (Exception) { }
    }


    private async Task AnalizingAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(textUrl.Text))
            {
                radioAuto.Checked = true;
                comboQuality.Enabled = false;
                buttonDownload.Enabled = false;
                return;
            }

            if (!textUrl.Text.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
            {
                comboQuality.Enabled = false;
                buttonDownload.Enabled = false;
                return;
            }

            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Indeterminate);
            progressDownload.Style = ProgressBarStyle.Marquee;
            progressDownload.MarqueeAnimationSpeed = 10;
            UpdateStatus("Analyzing...");

            var formats = await GetVideoFormatsAsync(textUrl.Text);
            if (formats != null)
            {
                comboQuality.DataSource = formats;
                comboQuality.ValueMember = "ID";
                comboQuality.DisplayMember = "Name";

                buttonDownload.Enabled = true;
            }
            else
            {
                comboQuality.DataSource = null;
                comboQuality.Items.Add("Best");
                comboQuality.Items.Add("Worst");
            }

            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
            progressDownload.Style = ProgressBarStyle.Blocks;
            progressDownload.MarqueeAnimationSpeed = 100;
            UpdateStatus("Analyzed");
        }
        catch (Exception) { }
    }

    private void DisableControls()
    {
        try
        {
            radioAuto.Enabled = false;
            radioCustom.Enabled = false;
            buttonBrowseFolder.Enabled = false;
            comboQuality.Enabled = false;
            buttonDownload.Enabled = false;
            textOutput.Enabled = false;
            textUrl.Enabled = false;
        }
        catch (Exception) { }
    }

    private void EnableControls()
    {
        try
        {
            radioAuto.Enabled = true;
            radioCustom.Enabled = true;
            buttonBrowseFolder.Enabled = true;
            textOutput.Enabled = true;
            textUrl.Enabled = true;
        }
        catch (Exception) { }
    }

    private void UpdateStatus(string status, int progress = 0, string size = "", string speed = "", string eta = "")
    {
        try
        {
            toolStripLabelStatus.Text = $"Status: {status}";
            toolStripLabelProgress.Text = progress == 0 ? "" : $"Progress: {progress}%";
            toolStripLabelSize.Text = string.IsNullOrEmpty(size) ? "" : $"Size: {size}";
            toolStripLabelSpeed.Text = string.IsNullOrEmpty(speed) ? "" : $"Speed: {speed}";
            toolStripLabelETA.Text = string.IsNullOrEmpty(eta) ? "" : $"ETA: {eta}";
            //toolStripProgressBar.Value = progress;
        }
        catch (Exception) { }
    }

    private void ClearStatus()
    {
        try
        {
            toolStripLabelStatus.Text = "Status: Idle";
            toolStripLabelProgress.Text = string.Empty;
            toolStripLabelSize.Text = string.Empty;
            toolStripLabelSpeed.Text = string.Empty;
            toolStripLabelETA.Text = string.Empty;
            //toolStripProgressBar.Value = 0;
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
            TaskbarManager.Instance.SetProgressValue(0, 100);
        }
        catch (Exception) { }
        finally
        {
            progressDownload.Value = 0;
        }
    }

    private void buttonBrowseFolder_Click(object sender, EventArgs e)
    {
        try
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Select the folder to save the video";
            folderBrowserDialog.ShowNewFolderButton = true;
            folderBrowserDialog.ShowDialog();
            textOutput.Text = folderBrowserDialog.SelectedPath;
        }
        catch (Exception) { }
    }

    private void radioAuto_CheckedChanged(object sender, EventArgs e)
    {
        try
        {
            if (radioAuto.Checked)
                comboQuality.Enabled = false;
            else
                comboQuality.Enabled = true;
        }
        catch (Exception) { }
    }

    private void radioCustom_CheckedChanged(object sender, EventArgs e)
    {
        try
        {
            if (radioCustom.Checked)
                comboQuality.Enabled = true;
            else
                comboQuality.Enabled = false;
        }
        catch (Exception) { }
    }

    public string GetVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version!.ToString();
            return version;
        }
        catch (Exception)
        {
            return "v1.5";
        }
    }
}
