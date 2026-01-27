using System.Globalization;
using System.Windows.Data;

namespace Wpf.Ui.Gallery.Helpers;

internal sealed class FirstWordConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string text = value?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string[] parts = text.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

internal sealed class RestWordsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string text = value?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string[] parts = text.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 1)
        {
            return string.Empty;
        }

        return " " + string.Join(" ", parts.Skip(1));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

internal sealed class CommandOptionHeaderConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isCmd = values.Length > 0 && values[0] is bool b0 && b0;
        bool isPs = values.Length > 1 && values[1] is bool b1 && b1;
        bool isBash = values.Length > 2 && values[2] is bool b2 && b2;

        if (isPs)
        {
            return "PS";
        }

        if (isBash)
        {
            return "Bash";
        }

        return "CMD";
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
