using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;

namespace DiskCleanerGUI.Avalonia.Services;

/// <summary>
/// Сервис локализации - управляет переводами интерфейса на разные языки
/// Поддерживает русский и английский языки, автоматически уведомляет об изменениях
/// Реализован как синглтон для глобального доступа из любой части приложения
/// </summary>
public class LocalizationService : INotifyPropertyChanged
{
    private static LocalizationService? _instance;
    /// <summary>
    /// Глобальный экземпляр сервиса локализации (паттерн Singleton)
    /// </summary>
    public static LocalizationService Instance => _instance ??= new LocalizationService();
    
    private string _currentLanguage = "ru";  // Текущий язык (по умолчанию русский)
    private readonly Dictionary<string, Dictionary<string, string>> _translations = new(); // Словарь переводов
    
    /// <summary>
    /// Событие изменения языка - подписчики получают уведомление для обновления интерфейса
    /// </summary>
    public event Action? LanguageChanged;
    public event PropertyChangedEventHandler? PropertyChanged;
    
    /// <summary>
    /// Индексатор для получения переведенной строки по ключу
    /// </summary>
    /// <param name="key">Ключ для поиска перевода</param>
    /// <returns>Переведенная строка</returns>
    public string this[string key] => GetString(key);
    
    /// <summary>
    /// Текущий язык интерфейса
    /// </summary>
    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                // Уведомляем всех подписчиков об изменении языка
                LanguageChanged?.Invoke();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            }
        }
    }
    
    /// <summary>
    /// Список доступных языков
    /// </summary>
    public List<string> AvailableLanguages => new() { "ru", "en" };
    
    private LocalizationService()
    {
        LoadTranslations();
    }
    
    /// <summary>
    /// Получает переведенную строку по ключу с поддержкой fallback на английский
    /// </summary>
    /// <param name="key">Ключ для поиска в словаре переводов</param>
    /// <returns>Переведенная строка или сам ключ, если перевод не найден</returns>
    public string GetString(string key)
    {
        // Ищем перевод для текущего языка
        if (_translations.TryGetValue(_currentLanguage, out var langDict) && 
            langDict.TryGetValue(key, out var translation))
        {
            return translation;
        }
        
        // Fallback на английский, если текущий язык не русский
        if (_currentLanguage != "en" && 
            _translations.TryGetValue("en", out var enDict) && 
            enDict.TryGetValue(key, out var enTranslation))
        {
            return enTranslation;
        }
        
        // Возвращаем ключ, если перевод не найден
        return key;
    }
    
    /// <summary>
    /// Загружает все переводы для поддерживаемых языков
    /// Содержит полный словарь строк интерфейса на русском и английском
    /// </summary>
    private void LoadTranslations()
    {
        // Русские переводы
        _translations["ru"] = new Dictionary<string, string>
        {
            // Основное окно и вкладки
            ["SystemTuner Pro"] = "SystemTuner Pro",
            ["Cleaning"] = "Очистка",
            ["FileViewer"] = "Просмотр файлов",
            ["LargeFiles"] = "Большие файлы",
            ["Utilities"] = "Утилиты",
            ["Trash"] = "Корзина",
            ["Themes"] = "Темы",
            ["Settings"] = "Настройки",
            ["Tweaks"] = "Твики",
            
            // Поиск больших файлов
            ["ScanPath"] = "Путь для сканирования:",
            ["MinFileSize"] = "Минимальный размер файла (MB):",
            ["StartScan"] = "🔍 Начать поиск",
            ["Cancel"] = "❌ Отмена",
            ["ReadyToScan"] = "Готов к поиску",
            ["ScanComplete"] = "Поиск завершён",
            ["ScanCancelled"] = "Поиск отменён",
            ["FoundFiles"] = "Найдено: {0} файлов, общий размер: {1}",
            ["FileName"] = "Имя файла",
            ["Size"] = "Размер",
            ["Type"] = "Тип",
            ["LastAccess"] = "Последний доступ",
            ["Path"] = "Путь",
            ["OpenFolder"] = "📂 Открыть папку",
            ["DeleteFile"] = "🗑️ Удалить",
            ["DeleteError"] = "Ошибка удаления: {0}",
            ["Profile"] = "👤 Профиль",
            ["Downloads"] = "📥 Загрузки",
            ["Documents"] = "📄 Документы",
            
            // Вкладка очистки
            ["TempFiles"] = "Временные файлы",
            ["SystemFiles"] = "Системные файлы",
            ["BrowserCache"] = "Кэш браузеров",
            ["AppCache"] = "Кэш приложений",
            ["RecycleBin"] = "Корзина",
            ["WindowsUpdate"] = "Обновления Windows",
            ["MultithreadScan"] = "Сканировать",
            ["MultithreadClean"] = "Очистить",
            ["GlobalClean"] = "Очистка всего мусора",
            ["RestoreOriginal"] = "Вернуть как было",
            ["OptimizedClean"] = "Оптимизированная",
            ["SaveSettings"] = "Сохранить настройки",
            ["Ready"] = "Готово",
            ["Scanning"] = "Сканирование...",
            ["Cleaning"] = "Очистка...",
            ["Found"] = "Найдено: {0} файлов",
            ["ScanError"] = "Ошибка сканирования: {0}",
            ["SettingsSaved"] = "Настройки очистки сохранены!",
            ["SelectAll"] = "Выделить всё",
            ["ClearSelection"] = "Снять всё",
            ["NoFilesSelected"] = "Не выбрано ни одного файла",
            ["FilesSelected"] = "Выбрано файлов: {0}",
            ["SelectionCleared"] = "Выделение снято",
            ["FoundSizeSummary"] = "Всего найдено: {0}   •   Выбрано: {1}",
            
            // Утилиты
            ["SystemMonitoring"] = "Мониторинг системы",
            ["Processor"] = "Процессор",
            ["Memory"] = "Память",
            ["Disks"] = "Диски",
            ["QuickUtilities"] = "Быстрые утилиты",
            ["RestartExplorer"] = "Перезапуск проводника",
            ["FreeMemory"] = "Освободить память",
            ["FlushDns"] = "Очистить DNS",
            ["ResetNetwork"] = "Сбросить сеть",
            ["ShutdownTimer"] = "Таймер выключения",
            ["ShutdownIn"] = "Выключить через:",
            ["Minutes"] = "минут",
            ["SetTimer"] = "Установить таймер",
            ["CancelTimer"] = "Отменить таймер",
            ["ReadyToWork"] = "Готов к работе",
            
            // Настройки
            ["GeneralSettings"] = "Общие настройки",
            ["DarkTheme"] = "Темная тема",
            ["UiScale"] = "Масштаб интерфейса",
            ["Language"] = "Язык",
            ["SaveSettingsBtn"] = "Сохранить настройки",
            ["ResetSettings"] = "Сбросить к умолчаниям",
            
            // Дополнительные строки
            ["CleaningError"] = "❌ Ошибка очистки: {0}",
            ["CleaningComplete"] = "✅ Очистка завершена",
            ["Search"] = "Поиск...",
            
            // Менеджер тем
            ["ThemeManager"] = "Менеджер тем",
            ["AvailableThemes"] = "Доступные темы",
            ["ApplyTheme"] = "Применить тему",
            ["ApplyLastTheme"] = "Применить последнюю",
            ["DeleteTheme"] = "Удалить тему",
            ["RefreshThemes"] = "Обновить список",
            ["ExportTheme"] = "Экспорт темы",
            ["ImportTheme"] = "Импорт темы",
            ["CreateTheme"] = "Создать тему",
            ["CreateNewTheme"] = "Создать новую тему",
            ["ThemeName"] = "Название темы",
            ["Author"] = "Автор",
            ["ThemeDescription"] = "Описание темы",
            ["PrimaryColor"] = "Основной:",
            ["SecondaryColor"] = "Вторичный:",
            ["AccentColor"] = "Акцент:",
            ["BackgroundColor"] = "Фон:",
            ["Gradient1"] = "Градиент 1:",
            ["Gradient2"] = "Градиент 2:",
            ["BackgroundImage"] = "Фоновое изображение",
            ["UseBackgroundImage"] = "Использовать фоновое изображение",
            ["SelectImage"] = "Выбрать изображение",
            
            // Общие строки интерфейса
            ["FolderPath"] = "Путь к папке",
            ["DragToResize"] = "Перетащите для увеличения или уменьшения",
            ["MoveDestination"] = "Папка для перемещения",
            
            // Подсказки для вкладок
            ["CleaningTooltip"] = "Очистка временных файлов, кэша браузеров, приложений и системных файлов",
            ["TempFilesTooltip"] = "Очистка временных файлов пользователя (%TEMP%)",
            ["SystemFilesTooltip"] = "Очистка системных временных файлов (Windows\\Temp)",
            ["BrowserCacheTooltip"] = "Очистка кэша Chrome, Edge, Firefox",
            ["AppCacheTooltip"] = "Очистка кэша Discord, Telegram, Steam, Teams, Roblox, Fortnite, YouTube Music, Photoshop, Postman и 100+ других приложений",
            ["RecycleBinTooltip"] = "Очистка корзины Windows",
            ["WindowsUpdateTooltip"] = "Очистка загруженных файлов обновлений Windows",
            ["ScanTooltip"] = "Многопоточное сканирование выбранных категорий файлов",
            ["CleanTooltip"] = "Удалить только отмеченные файлы из результатов сканирования",
            ["GlobalCleanTooltip"] = "Поиск и очистка распознанных кэшей приложений и браузеров в AppData",
            ["OptimizedCleanTooltip"] = "Быстрая оптимизированная очистка с минимальной нагрузкой на систему",
            ["SaveSettingsTooltip"] = "Сохранить текущие настройки очистки",
            ["SearchTooltip"] = "Фильтр для поиска файлов по имени или пути",
            
            // Подсказки для настроек
            ["DarkThemeTooltip"] = "Переключение между светлой и темной темой оформления",
            ["LanguageTooltip"] = "Выбор языка интерфейса приложения",
            ["SaveSettingsBtnTooltip"] = "Сохранить все изменения настроек",
            ["ResetSettingsTooltip"] = "Восстановить настройки по умолчанию",
            
            // Подсказки для утилит
            ["RestartExplorerTooltip"] = "Перезапустить процесс проводника Windows",
            ["FreeMemoryTooltip"] = "Освободить оперативную память системы",
            ["FlushDnsTooltip"] = "Очистить кэш DNS для решения проблем с сетью",
            ["ResetNetworkTooltip"] = "Сбросить настройки сети и перезапустить сетевые адаптеры",
            ["SetTimerTooltip"] = "Установить таймер автоматического выключения компьютера",
            ["CancelTimerTooltip"] = "Отменить запланированное выключение компьютера",
            
            // Подсказки для просмотра файлов
            ["BrowseTooltip"] = "Выбрать папку для просмотра файлов",
            ["LoadTooltip"] = "Загрузить список файлов из выбранной папки",
            ["OpenFileTooltip"] = "Открыть выбранный файл в связанном приложении",
            ["OpenInExplorerTooltip"] = "Показать файл в проводнике Windows",
            ["RemoveFromListTooltip"] = "Удалить файл из списка (не с диска)",
            ["BrowseMoveTooltip"] = "Выбрать папку назначения для перемещения",
            ["MoveSelectedTooltip"] = "Переместить выбранный файл в указанную папку",
            ["MoveAllTooltip"] = "Переместить все файлы из списка в указанную папку",
            
            // Подсказки для корзины
            ["LoadTrashTooltip"] = "Загрузить список файлов из внутренней корзины",
            ["EmptyTrashTooltip"] = "Окончательно удалить все файлы из корзины",
            ["RestoreFileTooltip"] = "Восстановить файл в исходное местоположение",
            
            // Твики системы
            ["DisableDefender"] = "Отключить Defender",
            ["EnableDefender"] = "Включить Defender",
            ["DisableUpdate"] = "Отключить Update",
            ["EnableUpdate"] = "Включить Update",
            ["DisableTelemetry"] = "Отключить телеметрию",
            ["EnableTelemetry"] = "Включить телеметрию",
            ["DisableCortana"] = "Отключить Cortana",
            ["EnableCortana"] = "Включить Cortana",
            ["OptimizeVisual"] = "Оптимизировать эффекты",
            ["CleanStartup"] = "Очистить автозагрузку",
            ["CreateGodMode"] = "Создать God Mode",
            ["EnableDarkTheme"] = "Темная тема",
            
            // Подсказки для твиков
            ["DefenderTooltip"] = "Отключает/включает встроенный антивирус Windows",
            ["UpdateTooltip"] = "Отключает/включает автоматические обновления Windows",
            ["TelemetryTooltip"] = "Отключает/включает сбор данных о использовании Windows",
            ["CortanaTooltip"] = "Отключает/включает голосового помощника Cortana",
            ["VisualTooltip"] = "Отключает визуальные эффекты для повышения производительности",
            ["StartupTooltip"] = "Открывает MSConfig для управления программами в автозагрузке",
            ["GodModeTooltip"] = "Создает папку с доступом ко всем настройкам Windows",
            ["DarkThemeTooltip"] = "Включает темную тему для Windows и приложений",
            ["FileViewerTooltip"] = "Просмотр и управление файлами с возможностью поиска и открытия в проводнике",
            ["UtilitiesTooltip"] = "Системные утилиты: мониторинг, перезапуск проводника, очистка DNS, таймер выключения",
            ["TrashTooltip"] = "Безопасное удаление файлов с возможностью восстановления из внутренней корзины",
            ["ThemesTooltip"] = "Создание, управление и применение пользовательских тем оформления",
            ["SettingsTooltip"] = "Настройки приложения: масштабирование интерфейса, переключение тем",
            ["TweaksTooltip"] = "Полезные настройки и оптимизации Windows для улучшения производительности и приватности",
            
            // Новые твики
            ["DisableOneDrive"] = "Отключить OneDrive",
            ["EnableOneDrive"] = "Включить OneDrive",
            ["DisableIndexing"] = "Отключить индексацию",
            ["EnableIndexing"] = "Включить индексацию",
            ["DisableStartMenuAds"] = "Отключить рекламу в Пуск",
            ["EnableStartMenuAds"] = "Включить рекламу в Пуск",
            ["DisableBackgroundApps"] = "Отключить фоновые приложения",
            ["EnableBackgroundApps"] = "Включить фоновые приложения",
            ["SpeedUpAnimations"] = "Ускорить анимации",
            ["FlushDns"] = "Очистить DNS кэш",
            ["OptimizeSsd"] = "Оптимизировать SSD",
            ["OptimizePageFile"] = "Оптимизировать файл подкачки",
            
            // Подсказки для новых твиков
            ["OneDriveTooltip"] = "Отключает/включает синхронизацию OneDrive",
            ["IndexingTooltip"] = "Отключает/включает службу индексации поиска Windows",
            ["StartMenuAdsTooltip"] = "Отключает/включает рекламные предложения в меню Пуск",
            ["BackgroundAppsTooltip"] = "Отключает/включает работу приложений в фоновом режиме",
            ["AnimationsTooltip"] = "Ускоряет анимации меню и окон для повышения отзывчивости",
            ["DnsTooltip"] = "Очищает кэш DNS для решения проблем с интернетом",
            ["SsdTooltip"] = "Оптимизирует настройки системы для работы с SSD дисками",
            ["PageFileTooltip"] = "Оптимизирует настройки файла подкачки для лучшей производительности",
            
            // Разделы твиков
            ["PrivacySecurity"] = "Приватность и безопасность",
            ["Performance"] = "Производительность",
            ["Customization"] = "Персонализация",
            ["SystemOptimization"] = "Системная оптимизация",
            
            // Специфичные для настроек
            ["DragToResizeHint"] = "Перетащите для увеличения или уменьшения размера текста и кнопок",
            ["ThemeHint"] = "Для смены фона перейдите на вкладку 'Темы'",
            ["UiScaleTooltip"] = "Изменяет размер шрифтов и элементов интерфейса от 50% до 200%",
            
            // Специфичные для просмотра файлов
            ["SelectFileForPreview"] = "Выберите файл для предпросмотра",
            
            // Специфичные для менеджера тем
            ["LastThemeApplied"] = "Применена последняя тема: {0}",
            ["LastThemeNotFound"] = "Последняя тема '{0}' не найдена",
            ["ThemesLoaded"] = "Загружено {0} тем",
            
            // Просмотр файлов
            ["FileViewer"] = "Просмотр файлов",
            ["Browse"] = "Обзор...",
            ["Load"] = "Загрузить",
            ["OpenFile"] = "Открыть",
            ["OpenInExplorer"] = "Показать в проводнике",
            ["RemoveFromList"] = "Удалить из списка",
            ["MoveFiles"] = "Перемещение файлов",
            ["MoveSelected"] = "Переместить выбранный",
            ["MoveAll"] = "Переместить все",
            
            // Корзина
            ["SafetyTitle"] = "Управление корзиной",
            ["LoadTrash"] = "Загрузить корзину",
            ["TrashFiles"] = "Файлы в корзине",
            ["RestoreFile"] = "↩Восстановить",
            ["EmptyTrash"] = "Очистить корзину",
            
            // Названия языков
            ["Russian"] = "Русский",
            ["English"] = "English",
            
            // Права администратора
            ["AdminRequired"] = "Требуются права администратора",
            ["RestartAsAdmin"] = "Перезапустить как администратор",
            ["AdminRequiredMessage"] = "Для очистки системных файлов требуются права администратора. Перезапустите приложение от имени администратора.",
            
            // Новые настройки безопасности
            ["SafeDelete"] = "Безопасное удаление",
            ["ShowNotifications"] = "Показывать уведомления",
            ["SafeDeleteTooltip"] = "Перемещать файлы в корзину вместо окончательного удаления",
            ["ShowNotificationsTooltip"] = "Показывать уведомления при завершении очистки"
        };
        
        // Английские переводы (сокращенная версия для примера)
        _translations["en"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "SystemTuner Pro",
            ["Cleaning"] = "Cleaning",
            ["FileViewer"] = "File Viewer",
            ["LargeFiles"] = "Large Files",
            ["Utilities"] = "Utilities", 
            ["Trash"] = "TrashBin",
            ["Themes"] = "Themes",
            ["Settings"] = "Settings",
            ["Tweaks"] = "Tweaks",
            
            // Large file finder
            ["ScanPath"] = "Scan path:",
            ["MinFileSize"] = "Minimum file size (MB):",
            ["StartScan"] = "🔍 Start Scan",
            ["Cancel"] = "❌ Cancel",
            ["ReadyToScan"] = "Ready to scan",
            ["ScanComplete"] = "Scan complete",
            ["ScanCancelled"] = "Scan cancelled",
            ["FoundFiles"] = "Found: {0} files, total size: {1}",
            ["FileName"] = "File Name",
            ["Size"] = "Size",
            ["Type"] = "Type",
            ["LastAccess"] = "Last Access",
            ["Path"] = "Path",
            ["OpenFolder"] = "📂 Open Folder",
            ["DeleteFile"] = "🗑️ Delete",
            ["DeleteError"] = "Delete error: {0}",
            ["Profile"] = "👤 Profile",
            ["Downloads"] = "📥 Downloads",
            ["Documents"] = "📄 Documents",
            
            ["TempFiles"] = "Temp Files",
            ["SystemFiles"] = "System Files",
            ["BrowserCache"] = "Browser Cache",
            ["AppCache"] = "App Cache",
            ["RecycleBin"] = "Recycle Bin",
            ["WindowsUpdate"] = "Windows Update",
            ["MultithreadScan"] = "Scan",
            ["MultithreadClean"] = "Clean",
            ["GlobalClean"] = "Clean All Junk",
            ["RestoreOriginal"] = "Restore original",
            ["OptimizedClean"] = "Clean",
            ["SaveSettings"] = "Save settings",
            ["Ready"] = "Ready",
            ["Scanning"] = "Scanning...",
            ["Cleaning"] = "Cleaning...",
            ["Found"] = "Found: {0} files",
            ["ScanError"] = "Scan error: {0}",
            ["SettingsSaved"] = "Cleaning settings saved!",
            ["SelectAll"] = "Select all",
            ["ClearSelection"] = "Clear all",
            ["NoFilesSelected"] = "No files selected",
            ["FilesSelected"] = "Files selected: {0}",
            ["SelectionCleared"] = "Selection cleared",
            ["FoundSizeSummary"] = "Total found: {0}   •   Selected: {1}",
            
            ["SystemMonitoring"] = "System Monitoring",
            ["Processor"] = "Processor",
            ["Memory"] = "Memory",
            ["Disks"] = "Disks",
            ["QuickUtilities"] = "Quick Utilities",
            ["RestartExplorer"] = "Restart Explorer",
            ["FreeMemory"] = "Free Memory",
            ["FlushDns"] = "Flush DNS",
            ["ResetNetwork"] = "Reset Network",
            ["ShutdownTimer"] = "Shutdown Timer",
            ["ShutdownIn"] = "Shutdown in:",
            ["Minutes"] = "Minutes",
            ["SetTimer"] = "Set Timer",
            ["CancelTimer"] = "Cancel Timer",
            ["ReadyToWork"] = "Ready to work",
            
            ["GeneralSettings"] = "General Settings",
            ["DarkTheme"] = "Dark Theme",
            ["UiScale"] = "UI Scale",
            ["Language"] = "Language",
            ["SaveSettingsBtn"] = "Save Settings",
            ["ResetSettings"] = "Reset to Defaults",
            
            ["CleaningError"] = "Cleaning error: {0}",
            ["CleaningComplete"] = "Cleaning completed",
            ["Search"] = "Search...",
            
            // Твики системы (английские)
            ["DisableDefender"] = "Disable Defender",
            ["EnableDefender"] = "Enable Defender",
            ["DisableUpdate"] = "Disable Update",
            ["EnableUpdate"] = "Enable Update",
            ["DisableTelemetry"] = "Disable Telemetry",
            ["EnableTelemetry"] = "Enable Telemetry",
            ["DisableCortana"] = "Disable Cortana",
            ["EnableCortana"] = "Enable Cortana",
            ["OptimizeVisual"] = "Optimize Effects",
            ["CleanStartup"] = "Clean Startup",
            ["CreateGodMode"] = "Create God Mode",
            ["EnableDarkTheme"] = "Dark Theme",
            
            // Новые твики (английские)
            ["DisableOneDrive"] = "Disable OneDrive",
            ["EnableOneDrive"] = "Enable OneDrive",
            ["DisableIndexing"] = "Disable Indexing",
            ["EnableIndexing"] = "Enable Indexing",
            ["DisableStartMenuAds"] = "Disable Start Menu Ads",
            ["EnableStartMenuAds"] = "Enable Start Menu Ads",
            ["DisableBackgroundApps"] = "Disable Background Apps",
            ["EnableBackgroundApps"] = "Enable Background Apps",
            ["SpeedUpAnimations"] = "Speed Up Animations",
            ["FlushDns"] = "Flush DNS Cache",
            ["OptimizeSsd"] = "Optimize SSD",
            ["OptimizePageFile"] = "Optimize Page File",
            
            // Разделы твиков (английские)
            ["PrivacySecurity"] = "Privacy & Security",
            ["Performance"] = "Performance",
            ["Customization"] = "Customization",
            ["SystemOptimization"] = "System Optimization",
            
            // Подсказки для вкладок (английские)
            ["CleaningTooltip"] = "Clean temporary files, browser cache, applications and system files",
            ["TempFilesTooltip"] = "Clean user temporary files (%TEMP%)",
            ["SystemFilesTooltip"] = "Clean system temporary files (Windows\\Temp)",
            ["BrowserCacheTooltip"] = "Clean Chrome, Edge, Firefox cache",
            ["AppCacheTooltip"] = "Clean Discord, Telegram, Steam, Teams, Roblox, Fortnite, YouTube Music, Photoshop, Postman and 100+ other apps cache",
            ["RecycleBinTooltip"] = "Clean Windows Recycle Bin",
            ["WindowsUpdateTooltip"] = "Clean downloaded Windows Update files",
            ["ScanTooltip"] = "Multi-threaded scanning of selected file categories",
            ["CleanTooltip"] = "Delete only selected files from the scan results",
            ["GlobalCleanTooltip"] = "Find and clean recognized application and browser caches in AppData",
            ["OptimizedCleanTooltip"] = "Fast optimized cleaning with minimal system load",
            ["SaveSettingsTooltip"] = "Save current cleaning settings",
            ["SearchTooltip"] = "Filter to search files by name or path",
            
            // Подсказки для настроек (английские)
            ["DarkThemeTooltip"] = "Switch between light and dark theme",
            ["LanguageTooltip"] = "Select application interface language",
            ["SaveSettingsBtnTooltip"] = "Save all settings changes",
            ["ResetSettingsTooltip"] = "Restore default settings",
            
            // Подсказки для утилит (английские)
            ["RestartExplorerTooltip"] = "Restart Windows Explorer process",
            ["FreeMemoryTooltip"] = "Free system RAM memory",
            ["FlushDnsTooltip"] = "Clear DNS cache to solve network problems",
            ["ResetNetworkTooltip"] = "Reset network settings and restart network adapters",
            ["SetTimerTooltip"] = "Set automatic computer shutdown timer",
            ["CancelTimerTooltip"] = "Cancel scheduled computer shutdown",
            
            // Подсказки для просмотра файлов (английские)
            ["BrowseTooltip"] = "Select folder to view files",
            ["LoadTooltip"] = "Load file list from selected folder",
            ["OpenFileTooltip"] = "Open selected file in associated application",
            ["OpenInExplorerTooltip"] = "Show file in Windows Explorer",
            ["RemoveFromListTooltip"] = "Remove file from list (not from disk)",
            ["BrowseMoveTooltip"] = "Select destination folder for moving",
            ["MoveSelectedTooltip"] = "Move selected file to specified folder",
            ["MoveAllTooltip"] = "Move all files from list to specified folder",
            
            // Подсказки для корзины (английские)
            ["LoadTrashTooltip"] = "Load file list from internal trash",
            ["EmptyTrashTooltip"] = "Permanently delete all files from trash",
            ["RestoreFileTooltip"] = "Restore file to original location",
            
            // Подсказки для твиков (английские)
            ["DefenderTooltip"] = "Disable/enable Windows built-in antivirus",
            ["UpdateTooltip"] = "Disable/enable Windows automatic updates",
            ["TelemetryTooltip"] = "Disable/enable Windows usage data collection",
            ["CortanaTooltip"] = "Disable/enable Cortana voice assistant",
            ["VisualTooltip"] = "Disable visual effects to improve performance",
            ["StartupTooltip"] = "Open MSConfig to manage startup programs",
            ["GodModeTooltip"] = "Create folder with access to all Windows settings",
            ["FileViewerTooltip"] = "View and manage files with search and explorer opening capabilities",
            ["UtilitiesTooltip"] = "System utilities: monitoring, restart explorer, DNS flush, shutdown timer",
            ["TrashTooltip"] = "Safe file deletion with recovery option from internal trash",
            ["ThemesTooltip"] = "Create, manage and apply custom interface themes",
            ["SettingsTooltip"] = "Application settings: interface scaling, theme switching",
            ["TweaksTooltip"] = "Useful Windows settings and optimizations to improve performance and privacy",
            
            // Подсказки для новых твиков (английские)
            ["OneDriveTooltip"] = "Disable/enable OneDrive synchronization",
            ["IndexingTooltip"] = "Disable/enable Windows search indexing service",
            ["StartMenuAdsTooltip"] = "Disable/enable advertising suggestions in Start menu",
            ["BackgroundAppsTooltip"] = "Disable/enable background app execution",
            ["AnimationsTooltip"] = "Speed up menu and window animations for better responsiveness",
            ["DnsTooltip"] = "Clear DNS cache to solve internet problems",
            ["SsdTooltip"] = "Optimize system settings for SSD drives",
            ["PageFileTooltip"] = "Optimize page file settings for better performance",
            
            // Специфичные для настроек (английские)
            ["DragToResizeHint"] = "Drag to increase or decrease text and button size",
            ["ThemeHint"] = "To change background go to 'Themes' tab",
            ["UiScaleTooltip"] = "Change font and interface element size from 50% to 200%",
            
            // Специфичные для просмотра файлов (английские)
            ["SelectFileForPreview"] = "Select file for preview",
            
            // Специфичные для менеджера тем (английские)
            ["LastThemeApplied"] = "Last theme applied: {0}",
            ["LastThemeNotFound"] = "Last theme '{0}' not found",
            ["ThemesLoaded"] = "Loaded {0} themes",
            
            // Менеджер тем (английские)
            ["ThemeManager"] = "Theme Manager",
            ["AvailableThemes"] = "Available Themes",
            ["ApplyTheme"] = "Apply Theme",
            ["ApplyLastTheme"] = "Apply Last",
            ["DeleteTheme"] = "Delete Theme",
            ["RefreshThemes"] = "Refresh List",
            ["ExportTheme"] = "Export Theme",
            ["ImportTheme"] = "Import Theme",
            ["CreateTheme"] = "Create Theme",
            ["CreateNewTheme"] = "Create New Theme",
            ["ThemeName"] = "Theme Name",
            ["Author"] = "Author",
            ["ThemeDescription"] = "Theme Description",
            ["PrimaryColor"] = "Primary:",
            ["SecondaryColor"] = "Secondary:",
            ["AccentColor"] = "Accent:",
            ["BackgroundColor"] = "Background:",
            ["Gradient1"] = "Gradient 1:",
            ["Gradient2"] = "Gradient 2:",
            ["BackgroundImage"] = "Background Image",
            ["UseBackgroundImage"] = "Use Background Image",
            ["SelectImage"] = "Select Image",
            
            // Общие строки интерфейса (английские)
            ["FolderPath"] = "Folder Path",
            ["DragToResize"] = "Drag to resize",
            ["MoveDestination"] = "Move Destination Folder",
            
            // Просмотр файлов (английские)
            ["Browse"] = "Browse...",
            ["Load"] = "Load",
            ["OpenFile"] = "Open",
            ["OpenInExplorer"] = "Show in Explorer",
            ["RemoveFromList"] = "Remove from List",
            ["MoveFiles"] = "Move Files",
            ["MoveSelected"] = "Move Selected",
            ["MoveAll"] = "Move All",
            
            // Корзина (английские)
            ["SafetyTitle"] = "Trash Management",
            ["LoadTrash"] = "Load Trash",
            ["TrashFiles"] = "Files in Trash",
            ["RestoreFile"] = "↩Restore",
            ["EmptyTrash"] = "Empty Trash",
            
            ["Russian"] = "Русский",
            ["English"] = "English",
            
            // Права администратора (английские)
            ["AdminRequired"] = "Administrator rights required",
            ["RestartAsAdmin"] = "Restart as Administrator",
            ["AdminRequiredMessage"] = "Administrator rights are required to clean system files. Please restart the application as administrator.",
            
            // Новые настройки безопасности (английские)
            ["SafeDelete"] = "Safe Delete",
            ["ShowNotifications"] = "Show Notifications",
            ["SafeDeleteTooltip"] = "Move files to recycle bin instead of permanent deletion",
            ["ShowNotificationsTooltip"] = "Show notifications when cleaning is complete"
        };
    }
}
