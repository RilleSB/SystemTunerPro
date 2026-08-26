using Avalonia.Data.Converters;
using System.Globalization;

namespace DiskCleanerGUI.Avalonia.ViewModels;

/// <summary>
/// Конвертер для прогресс-бара - превращает процент в ширину
/// Используется для анимации заполнения прогресс-бара в Splash Screen
/// </summary>
public class ProgressToWidthConverter : IValueConverter
{
    public static readonly ProgressToWidthConverter Instance = new();

    /// <summary>
    /// Конвертирует процент (0-100) в ширину пикселей (0-300)
    /// </summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int progress)
        {
            // Максимальная ширина прогресс-бара 300 пикселей
            return (progress / 100.0) * 300.0;
        }
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}