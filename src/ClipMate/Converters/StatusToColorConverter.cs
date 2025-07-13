using ClipMate.Models;
using System.Globalization;

namespace ClipMate.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DownloadStatus status)
            return Colors.Transparent;

        return status switch
        {
            DownloadStatus.Pending => Colors.Gray,
            DownloadStatus.Downloading => Colors.DodgerBlue,
            DownloadStatus.Merging => Colors.Orange,
            DownloadStatus.Completed => Colors.LimeGreen,
            DownloadStatus.Failed => Colors.Red,
            DownloadStatus.Warning => Colors.Gold,
            DownloadStatus.Cancelled => Colors.DarkGray,
            _ => Colors.Transparent
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
