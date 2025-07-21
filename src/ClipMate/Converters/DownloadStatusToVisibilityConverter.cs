using ClipMate.Models;
using System.Globalization;

namespace ClipMate.Converters;

public class DownloadStatusToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DownloadStatus status || parameter is not string expected)
            return false;

        var expectedStates = expected.Split('|', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => s.Trim())
                                     .ToList();

        return expectedStates.Any(s => string.Equals(s, status.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
