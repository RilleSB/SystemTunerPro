using Avalonia.Data.Converters;
using DiskCleanerGUI.Avalonia.Services;
using System;
using System.Globalization;

namespace DiskCleanerGUI.Avalonia.Converters;

public class LanguageConverter : IValueConverter
{
    public static readonly LanguageConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string langCode)
        {
            return langCode switch
            {
                "ru" => LocalizationService.Instance.GetString("Russian"),
                "en" => LocalizationService.Instance.GetString("English"),
                "de" => LocalizationService.Instance.GetString("German"),
                "fr" => LocalizationService.Instance.GetString("French"),
                "es" => LocalizationService.Instance.GetString("Spanish"),
                _ => langCode
            };
        }
        return value;
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}