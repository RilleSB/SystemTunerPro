using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using DiskCleanerGUI.Avalonia.ViewModels;
using DiskCleanerGUI.Avalonia.Views;
using DiskCleanerGUI.Avalonia.Services;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Media;
using DiskCleanerGUI.Avalonia.Models;
using System;
using System.Threading;

namespace DiskCleanerGUI.Avalonia;

/// <summary>
/// Главный класс приложения - управляет инициализацией, настройками и жизненным циклом
/// Поддерживает консольный режим для диагностики, автоматическое сохранение настроек при закрытии
/// </summary>
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Вызывается после завершения инициализации фреймворка
    /// Настраивает главное окно, загружает настройки, применяет тему и масштабирование
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        // Диагностический вывод для отладки
        var args = Environment.GetCommandLineArgs();
        if (args.Contains("--console"))
        {
            Console.WriteLine("SystemTuner Pro - Консольный режим");
            Console.WriteLine($"Аргументы: {string.Join(" ", args)}");
            Console.WriteLine($"Рабочая папка: {Environment.CurrentDirectory}");
            Console.WriteLine($"Версия .NET: {Environment.Version}");
        }
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                if (args.Contains("--console")) Console.WriteLine("Начало инициализации...");
                
                // Отключаем валидацию аннотаций данных Avalonia для улучшения производительности
                DisableAvaloniaDataAnnotationValidation();
                
                if (args.Contains("--console")) Console.WriteLine("Загрузка настроек...");
                var settingsService = new DiskCleanerGUI.Avalonia.Services.SettingsService();
                var settings = settingsService.LoadSettings();
                
                if (args.Contains("--console")) Console.WriteLine("Применение темы...");
                // Применяем тему (светлая/темная) из настроек
                Current!.RequestedThemeVariant = settings.DarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
                
                // Применяем масштабирование интерфейса, если оно отличается от стандартного
                if (Math.Abs(settings.UIScale - 1.0) > 0.01)
                {
                    ApplyUIScale(settings.UIScale);
                }
                
                if (args.Contains("--console")) Console.WriteLine("Запуск Splash Screen...");
                
                // Создаем и показываем Splash Screen
                var splashWindow = new SplashWindow();
                splashWindow.Show();
                
                // Загрузка в фоновом потоке
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var mainWindow = await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            return await splashWindow.StartLoadingWithCoroutinesAsync();
                        });
                        
                        await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            desktop.MainWindow = mainWindow;
                            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                            splashWindow.Close();
                            mainWindow.Show();
                        });
                    }
                    catch (Exception ex)
                    {
                        // Логирование ошибок только в режиме отладки
                        if (args.Contains("--console"))
                        {
                            Console.WriteLine($"Ошибка загрузки: {ex.Message}");
                        }
                        throw;
                    }
                });
                
                if (args.Contains("--console")) Console.WriteLine("Инициализация завершена успешно!");
            }
            catch (Exception ex)
            {
                if (args.Contains("--console"))
                {
                    Console.WriteLine($"Ошибка инициализации: {ex.Message}");
                    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
                }
                throw;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Отключает валидацию аннотаций данных Avalonia для улучшения производительности
    /// </summary>
    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
    
    /// <summary>
    /// Создает кисть для фона на основе настроек темы
    /// Поддерживает изображения, градиенты и сплошные цвета с обработкой ошибок
    /// </summary>
    /// <param name="theme">Настройки темы</param>
    /// <returns>Кисть для отрисовки фона</returns>
    private static IBrush CreateBrushFromTheme(Theme theme)
    {
        try
        {
            // Проверяем наличие градиента
            if (!string.IsNullOrWhiteSpace(theme.GradientStart) && !string.IsNullOrWhiteSpace(theme.GradientEnd))
            {
                return new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.Parse(theme.GradientStart), 0),
                        new GradientStop(Color.Parse(theme.GradientEnd), 1)
                    }
                };
            }
            else
            {
                // Используем сплошной цвет фона
                return new SolidColorBrush(Color.Parse(theme.BackgroundColor));
            }
        }
        catch
        {
            // При ошибке возвращаем темный цвет по умолчанию
            return new SolidColorBrush(Color.Parse("#121212"));
        }
    }
    
    /// <summary>
    /// Применяет масштабирование к элементам интерфейса
    /// Изменяет размеры шрифтов для различных элементов
    /// </summary>
    /// <param name="scale">Коэффициент масштабирования (0.5 - 2.0)</param>
    private void ApplyUIScale(double scale)
    {
        var baseFontSize = 12.0;
        var scaledFontSize = baseFontSize * scale;
        
        // Применяем масштабирование к различным элементам интерфейса
        Current!.Resources["DefaultFontSize"] = scaledFontSize;
        Current.Resources["ButtonFontSize"] = scaledFontSize;
        Current.Resources["HeaderFontSize"] = scaledFontSize * 1.5;
        Current.Resources["LargeFontSize"] = scaledFontSize * 2.0;
    }
    
    /// <summary>
    /// Применяет сохраненные настройки окна (размер, состояние)
    /// </summary>
    /// <param name="window">Главное окно приложения</param>
    /// <param name="settings">Настройки приложения</param>
    private static void ApplyWindowSettings(MainWindow window, AppSettings settings)
    {
        // Восстанавливаем размер окна, если он был сохранен
        if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
        {
            window.Width = settings.WindowWidth;
            window.Height = settings.WindowHeight;
        }
        
        // Восстанавливаем состояние окна (развернуто/обычное)
        if (settings.WindowMaximized)
        {
            window.WindowState = WindowState.Maximized;
        }
    }
    
    /// <summary>
    /// Асинхронно сохраняет настройки приложения при закрытии
    /// Сохраняет размер окна, настройки очистки и общие настройки
    /// </summary>
    /// <param name="window">Главное окно</param>
    /// <param name="settingsService">Сервис для работы с настройками</param>
    private static async Task SaveSettingsAsync(MainWindow window, DiskCleanerGUI.Avalonia.Services.SettingsService settingsService)
    {
        await Task.Run(() =>
        {
            // Сохраняем настройки окна
            var settings = settingsService.LoadSettings();
            settings.WindowWidth = window.Width;
            settings.WindowHeight = window.Height;
            settings.WindowMaximized = window.WindowState == WindowState.Maximized;
            settingsService.SaveSettings(settings);
            
            // Сохраняем настройки очистки и общие настройки, если ViewModel доступна
            if (window.DataContext is MainWindowViewModel vm)
            {
                // Сохраняем настройки очистки
                DiskCleanerGUI.Avalonia.Services.FileConfigService.SaveSettings(
                    vm.Cleaning.TempUser, vm.Cleaning.TempWindows, vm.Cleaning.Browsers,
                    vm.Cleaning.Apps, vm.Cleaning.RecycleBin, vm.Cleaning.WindowsUpdate);
                    
                // Сохраняем общие настройки приложения
                var vmSettings = vm.Settings.GetSettings();
                settingsService.SaveSettings(vmSettings);
            }
        });
    }
}