using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ThingLing.Controls;
using YtDlpWrapper;

namespace AvaloniaDownloader;

public partial class MainWindow : Window
{
  private readonly YtDlpEngine _engine;

  public MainWindow()
  {
    InitializeComponent();
    _engine = new YtDlpEngine();
  }

  private async void Control_OnLoaded(object? sender, RoutedEventArgs e)
  {
    TextUrl.Text = @"https://www.youtube.com/watch?v=Znk5QINe01A";
    TextUrl.Watermark = @"Enter video URL here ...";
    TextFile.Text = @"A:\Videos";
    TextFile.Watermark = @"A:\Videos";
    BtnDownload.Content = "DOWNLOAD";

    CboQuality.IsEnabled = false;
    BtnDownload.IsEnabled = false;
    var formats = await GetVideoFormatsAsync(TextUrl.Text);
    CboQuality.ItemsSource = formats;
  }

  private async Task<List<VideoFormat>> GetVideoFormatsAsync(string url)
  {
    return await _engine.GetAvailableFormatsAsync(url);
  }

  private async void TextUrl_OnTextChanged(object? sender, TextChangedEventArgs e)
  {
    if (string.IsNullOrEmpty(TextUrl.Text))
    {
      CboQuality.IsEnabled = false;
      BtnDownload.IsEnabled = false;
      return;
    }

    if (!TextUrl.Text.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
    {
      CboQuality.IsEnabled = false;
      BtnDownload.IsEnabled = false;
      return;
    }

    //ProgressDownload.Style = ProgressBarStyle.Marquee;
    //ProgressDownload.MarqueeAnimationSpeed = 10;

    var formats = await GetVideoFormatsAsync(TextUrl.Text);

    CboQuality.ItemsSource = formats;
    //CboQuality.SelectedValueBinding = CboQuality.SelectedIndex;
    //CboQuality.SelectedIndex = "Resolution";

    CboQuality.IsEnabled = true;
    BtnDownload.IsEnabled = true;

    // ProgressDownload.Style = ProgressBarStyle.Blocks;
    // ProgressDownload.MarqueeAnimationSpeed = 100;
  }

  private async void Button_OnClick(object? sender, RoutedEventArgs e)
  {
    TextFile.Text ??= TextFile.Watermark;

    if (string.IsNullOrEmpty(TextUrl.Text))
    {
      await MessageBox.ShowAsync("Please enter the video URL", "Error", MessageBoxButton.Ok, MessageBoxImage.Error);
      return;
    }

    CboQuality.SelectedIndex = 10;
    await DownloadVideoAsync(TextUrl.Text!, TextFile.Text!, CboQuality.SelectedItem as VideoFormat);
  }

  private async Task DownloadVideoAsync(string url, string outFile, VideoFormat? quality)
  {
    TextUrl.Text = "";
    TextFile.Text = "";
    quality ??= new VideoFormat();

    int progress = 0;
    ProgressDownload.Value = progress;

    // Subscribe to the download progress event
    _engine.OnErrorMessage += (sender, e) => TxtBlock.Text += $"{e}" + Environment.NewLine;

    // Subscribe to the download progress event
    _engine.OnProgressMessage += (sender, e) => TxtBlock.Text += $"{e}" + Environment.NewLine;

    // Subscribe to the download progress event
    _engine.OnProgressDownload += (sender, e) =>
    {
      try
      {
        // Parse e.Percent as a double
        var percent = double.Parse(e.Percent.ToString());

        // Convert the double to an integer for progress (if needed)
        progress = (int)Math.Round(percent); // Rounds to the nearest integer
        ProgressDownload.Value = progress;

        if (progress >= 100)
        {
          TxtBlock.Text += $"Download completed. FileSize: {e.Size}";
        }
      }
      catch (Exception ex)
      {
        // Handle the exception
        Console.WriteLine($"Error parsing percent: {ex.Message}");
      }
    };

    // Subscribe to the download complete event
    _engine.OnCompleteDownload += (sender, e) => TxtBlock.Text += $"{e}11" + Environment.NewLine;

    var selectedFormat = quality;

    if (TextFile.IsEnabled)
      await _engine.DownloadVideoAsync(url, outFile);
  }
}