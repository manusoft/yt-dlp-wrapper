using ClipMate.Services;
using System.Globalization;

namespace ClipMate.Converters;

public class ConnectionStateToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ConnectionState state)
            return Colors.Transparent;

        return state switch
        {
            ConnectionState.Available => Colors.LimeGreen,            
            ConnectionState.Limited => Colors.Orange,
            ConnectionState.Lost => Colors.Red,
            _ => Colors.Transparent
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}