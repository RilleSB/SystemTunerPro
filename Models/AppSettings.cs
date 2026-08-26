namespace DiskCleanerGUI.Avalonia.Models;

/// <summary>
/// Модель настроек приложения - хранит пользовательские предпочтения
/// </summary>
public class AppSettings
{
    // Настройки темы
    public bool DarkTheme { get; set; } = true;  // Темная тема по умолчанию
    public string? LastUsedTheme { get; set; }   // Последняя использованная тема
    
    // Пути для работы с файлами
    public string? LastScanPath { get; set; }    // Последний путь сканирования
    public string? LastMoveToPath { get; set; }  // Последний путь перемещения
    
    // Настройки автоочистки
    public bool AutoCleanTemp { get; set; } = true;           // Автоочистка временных файлов
    public bool AutoCleanBrowsers { get; set; } = true;       // Автоочистка браузеров
    public bool AutoCleanApps { get; set; } = false;          // Автоочистка приложений
    public bool AutoCleanRecycleBin { get; set; } = false;    // Автоочистка корзины
    public bool AutoCleanWindowsUpdate { get; set; } = false; // Автоочистка Windows Update
    
    // Настройки окна
    public double WindowWidth { get; set; } = 1000;   // Ширина окна
    public double WindowHeight { get; set; } = 700;   // Высота окна
    public bool WindowMaximized { get; set; } = false; // Развернуто на весь экран
    
    // Масштабирование интерфейса
    public double UIScale { get; set; } = 1.0;
    
    // Язык интерфейса
    public string? Language { get; set; } = "ru";
    
    // Безопасность
    public bool SafeDelete { get; set; } = true; // Перемещать в корзину вместо удаления
    public bool ShowNotifications { get; set; } = true; // Показывать уведомления
}