using DiskCleanerGUI.Avalonia.ViewModels;
using DiskCleanerGUI.Avalonia.Views;
using DiskCleanerGUI.Avalonia.Models;
using Avalonia.Controls;
using System.Threading.Tasks;

namespace DiskCleanerGUI.Avalonia.Services;

/// <summary>
/// Сервис загрузки приложения с корутинами - выполняет реальную инициализацию компонентов
/// Отчитывается о прогрессе через IProgress для синхронизации с анимацией
/// </summary>
public class ApplicationLoaderService
{
    /// <summary>
    /// Загружает приложение поэтапно с отчетом о прогрессе (корутина)
    /// Выполняет реальную инициализацию: сервисы -> настройки -> ViewModels -> окно
    /// </summary>
    public async Task<MainWindow> LoadApplicationAsync(IProgress<(int progress, string text)> progressReporter)
    {
        // Инициализация сервисов
        progressReporter.Report((25, "🔧 Инициализация..."));
        var settingsService = new SettingsService();
        var localizationService = LocalizationService.Instance;
        await Task.Delay(100);

        // Загрузка настроек
        progressReporter.Report((50, "⚙️ Загрузка настроек..."));
        var settings = settingsService.LoadSettings();
        
        // Устанавливаем язык только если он не установлен или отличается
        if (string.IsNullOrEmpty(localizationService.CurrentLanguage) || localizationService.CurrentLanguage == "ru")
        {
            localizationService.CurrentLanguage = settings.Language ?? "ru";
        }
        await Task.Delay(100);

        // Создание окна
        progressReporter.Report((75, "🎨 Создание интерфейса..."));
        var mainWindow = new MainWindow();
        ApplyWindowSettings(mainWindow, settings);
        await Task.Delay(100);

        // Инициализация ViewModels
        progressReporter.Report((90, "🚀 Подготовка..."));
        var mainViewModel = new MainWindowViewModel();
        mainWindow.DataContext = mainViewModel;
        SetupWindowClosingHandler(mainWindow, settingsService);
        await Task.Delay(100);

        // Готово
        progressReporter.Report((100, "✅ Готово!"));
        await Task.Delay(200);

        return mainWindow;
    }

    /// <summary>
    /// Применяет сохраненные настройки к главному окну
    /// </summary>
    private static void ApplyWindowSettings(MainWindow window, AppSettings settings)
    {
        if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
        {
            window.Width = settings.WindowWidth;
            window.Height = settings.WindowHeight;
        }
        
        if (settings.WindowMaximized)
        {
            window.WindowState = WindowState.Maximized;
        }
    }

    /// <summary>
    /// Настраивает обработчик закрытия окна для сохранения настроек
    /// </summary>
    private static void SetupWindowClosingHandler(MainWindow window, SettingsService settingsService)
    {
        window.Closing += (s, e) =>
        {
            try
            {
                var settings = settingsService.LoadSettings();
                settings.WindowWidth = window.Width;
                settings.WindowHeight = window.Height;
                settings.WindowMaximized = window.WindowState == WindowState.Maximized;
                settingsService.SaveSettings(settings);
            }
            catch { /* Игнорируем ошибки при закрытии */ }
        };
    }


}