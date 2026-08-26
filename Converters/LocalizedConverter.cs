using Avalonia.Data.Converters;
using DiskCleanerGUI.Avalonia.Services;
using System;
using System.Globalization;

namespace DiskCleanerGUI.Avalonia.Converters;

public class LocalizedConverter : IValueConverter
{
    public static readonly LocalizedConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key)
        {
            return LocalizationService.Instance.GetString(key);
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}