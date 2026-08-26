using Avalonia;
using System;
using System.IO;

namespace DiskCleanerGUI.Avalonia;

/// <summary>
/// Главный класс приложения - точка входа для TrashClean
/// </summary>
sealed class Program
{
    /// <summary>
    /// Точка входа в приложение
    /// </summary>
    /// <param name="args">Аргументы командной строки</param>
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // Запуск Avalonia приложения с классическим десктопным интерфейсом
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            // Сохранение информации о критической ошибке в файл
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "avalonia_crash.txt"), ex.ToString()); } catch { }
            throw;
        }
    }

    /// <summary>
    /// Конфигурация Avalonia приложения
    /// </summary>
    /// <returns>Настроенный AppBuilder</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()    // Автоопределение платформы
            .WithInterFont()        // Использование Inter шрифта
            .LogToTrace();          // Логирование в Trace
}

