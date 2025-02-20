using Microsoft.WindowsAPICodePack.Taskbar;
using YtDlpWrapper;

namespace VideoDownloader;

public partial class frmMain : Form
{
    private readonly YtDlpEngine engineV1;
    private readonly Ytdlp engineV2;
    private string currentVersion;
    private readonly string downloadPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

    public frmMain()
    {
        InitializeComponent();

        try
        {
            engineV1 = new YtDlpEngine();

            engineV2 = new Ytdlp();
            engineV2.OnProgressMessage += EngineV2_OnProgressMessage;
            textOutput.Text = downloadPath;
        }
        catch (Exception)
        {
            UpdateStatus("Engine failed to start.");
        }
    }

    private void EngineV2_OnProgressMessage(object? sender, string e)
    {

    }

    private async void frmMain_Load(object sender, EventArgs e)
    {
        comboQuality.Enabled = false;
        buttonDownload.Enabled = false;
        ClearStatus();
        UpdateStatus("Engine started successfully.");
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
        //return await engineV1.GetAvailableFormatsAsync(url);
        return await engineV2.GetAvailableFormatsAsync(url);
    }

    private async Task DownloadVideoAsync(string url, VideoFormat? quality)
    {
        UpdateStatus("Preparing to download...");
        DisableControls();
        textUrl.Clear();
        textDetail.Clear();
        textDetail.AppendText($"Downloading video from: {url}" + Environment.NewLine);

        int progress = 0;
        progressDownload.Value = progress;
        TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
        TaskbarManager.Instance.SetProgressValue(progress, 100);

        // Subscribe all output
        engineV2.OnOutput += (sender, e) => textDetail.AppendText($"{e}" + Environment.NewLine);

        // Subscribe to the download progress event
        //engineV2.OnErrorMessage += (sender, e) => textDetail.AppendText($"{e}" + Environment.NewLine);

        // Subscribe to the download progress event
        //engineV2.OnProgressMessage += (sender, e) => textDetail.AppendText($"{e}" + Environment.NewLine);

        engineV2.OnCompleteDownload += (sender, e) =>
        {
            progressDownload.Value = 100;
            textDetail.AppendText($"{e}" + Environment.NewLine);
            ClearStatus();
        };

        // Subscribe to the download progress event
        engineV2.OnProgressDownload += (sender, e) =>
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
                UpdateStatus("Downloading", progress, e.Size, e.Speed, e.ETA);
            }
            catch (Exception ex)
            {
                // Handle the exception
                Console.WriteLine($"Error parsing percent: {ex.Message}");
            }
        };


        var selectedFormat = quality;

        if (radioAuto.Checked)
        {
            //await engineV1.DownloadVideoAsync(url, textOutput.Text.Trim(), VideoQuality.Best);
            await engineV2.SetOutputFolder(textOutput.Text.Trim()).ExecuteAsync(url);
        }
        else
        {
            if (selectedFormat != null)
            {
                //await engineV1.DownloadVideoAsync(url, textOutput.Text.Trim(), VideoQuality.Custom, selectedFormat.ID);
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


    private async Task AnalizingAsync()
    {
        if (string.IsNullOrEmpty(textUrl.Text))
        {
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

    private void DisableControls()
    {
        radioAuto.Enabled = false;
        radioCustom.Enabled = false;
        buttonBrowseFolder.Enabled = false;
        comboQuality.Enabled = false;
        buttonDownload.Enabled = false;
        textOutput.Enabled = false;
        textUrl.Enabled = false;
    }

    private void EnableControls()
    {
        radioAuto.Enabled = true;
        radioCustom.Enabled = true;
        buttonBrowseFolder.Enabled = true;
        textOutput.Enabled = true;
        textUrl.Enabled = true;
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
        catch (Exception)
        {
        }
    }

    private void ClearStatus()
    {
        toolStripLabelStatus.Text = "Status: Idle";
        toolStripLabelProgress.Text = string.Empty;
        toolStripLabelSize.Text = string.Empty;
        toolStripLabelSpeed.Text = string.Empty;
        toolStripLabelETA.Text = string.Empty;
        //toolStripProgressBar.Value = 0;

        try
        {
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
            TaskbarManager.Instance.SetProgressValue(0, 100);
        }
        catch (Exception)
        {
        }
       
        progressDownload.Value = 0;
    }

    private void buttonBrowseFolder_Click(object sender, EventArgs e)
    {
        FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
        folderBrowserDialog.Description = "Select the folder to save the video";
        folderBrowserDialog.ShowNewFolderButton = true;
        folderBrowserDialog.ShowDialog();
        textOutput.Text = folderBrowserDialog.SelectedPath;
    }

    private void radioAuto_CheckedChanged(object sender, EventArgs e)
    {
        if (radioAuto.Checked)
            comboQuality.Enabled = false;
        else
            comboQuality.Enabled = true;
    }

    private void radioCustom_CheckedChanged(object sender, EventArgs e)
    {
        if (radioCustom.Checked)
            comboQuality.Enabled = true;
        else
            comboQuality.Enabled = false;
    }
}
