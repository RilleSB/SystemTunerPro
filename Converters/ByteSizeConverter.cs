using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace DiskCleanerGUI.Avalonia.Converters;

public class ByteSizeConverter : IValueConverter
{
    public static readonly ByteSizeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is long l) return FormatBytes(l);
        if (value is int i) return FormatBytes(i);
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        if (bytes == 0) return "0 B";
        var i = (int)Math.Floor(Math.Log(bytes) / Math.Log(1024));
        i = Math.Max(0, Math.Min(i, sizes.Length - 1));
        var v = bytes / Math.Pow(1024, i);
        return $"{v:0.##} {sizes[i]}";
    }
}

