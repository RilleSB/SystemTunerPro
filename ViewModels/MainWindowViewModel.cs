using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DiskCleanerGUI.Avalonia.Models;
using DiskCleanerGUI.Avalonia.Services;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DiskCleanerGUI.Avalonia.ViewModels;

/// <summary>
/// Главная ViewModel приложения - управляет всеми вкладками и общим состоянием
/// Использует ленивую инициализацию для улучшения производительности запуска
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly GitHubUpdateService _updateService = new();
    /// <summary>
    /// Общий экземпляр ViewModel для фона - используется всеми окнами
    /// </summary>
    public static BackgroundViewModel SharedBackground { get; } = new();
    
    // Ленивая инициализация ViewModels для каждой вкладки
    private CleaningViewModel? _cleaning;
    private FileViewerViewModel? _fileViewer;
    private UtilitiesViewModel? _utilities;
    private SafetyViewModel? _safety;
    private TweaksViewModel? _tweaks;
    private LargeFilesViewModel? _largeFiles;
    
    // Синглтон для настроек - создается один раз и сохраняется
    public static SettingsViewModel SharedSettings { get; } = new();

    [ObservableProperty] private bool isCheckingForUpdates;
    [ObservableProperty] private bool isUpdateAvailable;
    [ObservableProperty] private string updateStatus = "Проверить обновления";
    private Uri? _latestReleaseUrl;
    
    // Свойства для доступа к ViewModels вкладок (создаются при первом обращении)
    public CleaningViewModel Cleaning => _cleaning ??= new();           // Вкладка очистки
    public FileViewerViewModel FileViewer => _fileViewer ??= new();     // Просмотр файлов
    public UtilitiesViewModel Utilities => _utilities ??= new();        // Системные утилиты
    public SafetyViewModel Safety => _safety ??= new();                 // Безопасность
    public TweaksViewModel Tweaks => _tweaks ??= new();                 // Твики системы
    public LargeFilesViewModel LargeFiles => _largeFiles ??= new();     // Поиск больших файлов
    public BackgroundViewModel Background => SharedBackground;           // Управление фоном
    public SettingsViewModel Settings => SharedSettings;                // Настройки (синглтон)
    public ThemeManagerViewModel ThemeManager { get; } = new();         // Управление темами
    
    /// <summary>
    /// Проверяет, запущено ли приложение с правами администратора
    /// </summary>
    public bool IsRunningAsAdmin => AdminRightsService.IsRunningAsAdmin();
    
    /// <summary>
    /// Перезапускает приложение с правами администратора
    /// </summary>
    [RelayCommand]
    private void RestartAsAdmin()
    {
        if (AdminRightsService.RestartAsAdmin())
        {
            // Закрываем текущий экземпляр
            Environment.Exit(0);
        }
    }
    
    public MainWindowViewModel()
    {
        // Минимальная инициализация - все остальное откладываем
        Task.Run(() =>
        {
            // Устанавливаем фон в фоновом потоке для ускорения запуска
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                SharedBackground.BackgroundBrush ??= CreateDefaultGradient();
            });
        });

        _ = CheckForUpdatesAsync();
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        if (IsCheckingForUpdates)
            return;

        try
        {
            IsCheckingForUpdates = true;
            UpdateStatus = "Проверка обновлений…";

            var result = await _updateService.CheckForUpdatesAsync();
            _latestReleaseUrl = result.ReleaseUrl;
            IsUpdateAvailable = result.IsUpdateAvailable;
            UpdateStatus = result.IsUpdateAvailable
                ? $"Доступна версия {result.LatestTag}"
                : $"Установлена актуальная версия {result.CurrentVersion.Major}.{result.CurrentVersion.Minor}.{result.CurrentVersion.Build}";
        }
        catch (Exception)
        {
            IsUpdateAvailable = false;
            UpdateStatus = "Не удалось проверить обновления";
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    [RelayCommand]
    private void OpenLatestRelease()
    {
        if (_latestReleaseUrl == null)
            return;

        Process.Start(new ProcessStartInfo(_latestReleaseUrl.AbsoluteUri) { UseShellExecute = true });
    }
    
    /// <summary>
    /// Создает кисть для фона на основе настроек темы
    /// Поддерживает изображения, градиенты и сплошные цвета
    /// </summary>
    /// <param name="theme">Тема с настройками фона</param>
    /// <returns>Кисть для отрисовки фона</returns>
    private static IBrush CreateBrushFromTheme(Theme theme)
    {
        try
        {
            // Сначала пробуем фоновое изображение
            if (theme.UseBackgroundImage && !string.IsNullOrWhiteSpace(theme.BackgroundImagePath) && System.IO.File.Exists(theme.BackgroundImagePath))
            {
                var bitmap = new Bitmap(theme.BackgroundImagePath);
                return new ImageBrush(bitmap) 
                { 
                    Stretch = Stretch.UniformToFill,     // Масштабирование с сохранением пропорций
                    AlignmentX = AlignmentX.Center,      // Центрирование по горизонтали
                    AlignmentY = AlignmentY.Center       // Центрирование по вертикали
                };
            }
            // Затем пробуем градиент
            else if (!string.IsNullOrWhiteSpace(theme.GradientStart) && !string.IsNullOrWhiteSpace(theme.GradientEnd))
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
            // В конце используем сплошной цвет
            else
            {
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
    /// Создает градиент по умолчанию для фона приложения
    /// </summary>
    /// <returns>Темный градиент от синего к черному</returns>
    private static LinearGradientBrush CreateDefaultGradient()
    {
        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.Parse("#15203B"), 0),  // Темно-синий
                new GradientStop(Color.Parse("#0A0F1E"), 1)   // Почти черный
            }
        };
    }
}
