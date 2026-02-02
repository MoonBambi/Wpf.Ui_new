using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Wpf.Ui.Gallery.Helpers;

internal sealed class TerminalToCardBackgroundConverter : IValueConverter
{
    private static readonly SolidColorBrush MistBlueBrush =
        new((Color)ColorConverter.ConvertFromString("#A1C2FA")!);

    private static readonly SolidColorBrush SageGreenBrush =
        new((Color)ColorConverter.ConvertFromString("#B4E3B4")!);

    private static readonly SolidColorBrush DustyRoseBrush =
        new((Color)ColorConverter.ConvertFromString("#D1C4E9")!);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var terminal = value?.ToString()?.Trim() ?? string.Empty;

        if (terminal.Length == 0)
        {
            return DependencyProperty.UnsetValue;
        }

        var lowered = terminal.ToLowerInvariant();

        if (lowered.Contains("powershell") || lowered.Contains("ps"))
        {
            return MistBlueBrush;
        }

        if (lowered.Contains("bash"))
        {
            return SageGreenBrush;
        }

        if (lowered.Contains("cmd"))
        {
            return DustyRoseBrush;
        }

        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

