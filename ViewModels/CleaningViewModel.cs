using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleanerGUI.Avalonia.Models;
using DiskCleanerGUI.Avalonia.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiskCleanerGUI.Avalonia.ViewModels;

/// <summary>
/// ViewModel для вкладки очистки - управляет процессом сканирования и удаления файлов
/// Поддерживает многопоточное сканирование, фильтрацию результатов и различные режимы очистки
/// </summary>
public partial class CleaningViewModel : LocalizedViewModelBase
{
    // Сервис для выполнения операций очистки
    private readonly CleanerService _service = new();

    private CancellationTokenSource? _cancellationTokenSource;
    
    // Локализованные свойства для отображения текста в интерфейсе
    public string TempFilesText => GetString("TempFiles");
    public string SystemFilesText => GetString("SystemFiles");
    public string BrowserCacheText => GetString("BrowserCache");
    public string AppCacheText => GetString("AppCache");
    public string RecycleBinText => GetString("RecycleBin");
    public string WindowsUpdateText => GetString("WindowsUpdate");
    public string ScanButtonText => GetString("MultithreadScan");
    public string CleanButtonText => GetString("MultithreadClean");
    public string SaveSettingsButtonText => GetString("SaveSettings");
    public string SearchText => GetString("Search");
    public string SelectAllText => GetString("SelectAll");
    public string ClearSelectionText => GetString("ClearSelection");
    public string FoundSizeSummary => string.Format(
        GetString("FoundSizeSummary"),
        FormatBytes(PreviewItems.Sum(item => item.Size)),
        FormatBytes(PreviewItems.Where(item => item.IsSelected).Sum(item => item.Size)));
    
    // Подсказки для элементов интерфейса
    public string TempFilesTooltip => GetString("TempFilesTooltip");
    public string SystemFilesTooltip => GetString("SystemFilesTooltip");
    public string BrowserCacheTooltip => GetString("BrowserCacheTooltip");
    public string AppCacheTooltip => GetString("AppCacheTooltip");
    public string RecycleBinTooltip => GetString("RecycleBinTooltip");
    public string WindowsUpdateTooltip => GetString("WindowsUpdateTooltip");
    public string ScanTooltip => GetString("ScanTooltip");
    public string CleanTooltip => GetString("CleanTooltip");
    public string SaveSettingsTooltip => GetString("SaveSettingsTooltip");
    public string SearchTooltip => GetString("SearchTooltip");

    // Настройки очистки - какие категории файлов очищать
    [ObservableProperty] private bool tempUser;        // Временные файлы пользователя
    [ObservableProperty] private bool tempWindows;     // Системные временные файлы
    [ObservableProperty] private bool browsers;        // Кэш браузеров
    [ObservableProperty] private bool apps;            // Кэш приложений
    [ObservableProperty] private bool recycleBin;      // Корзина
    [ObservableProperty] private bool windowsUpdate;   // Файлы Windows Update

    // Состояние процесса очистки
    [ObservableProperty] private int progress;         // Прогресс выполнения (0-100)
    [ObservableProperty] private string status = "";  // Текущий статус операции
    
    /// <summary>
    /// Обновляет локализованные строки при смене языка
    /// </summary>
    protected override void OnLanguageChanged()
    {
        Status = GetString("Ready");
        // Обновляем все локализованные свойства
        OnPropertyChanged(nameof(TempFilesText));
        OnPropertyChanged(nameof(SystemFilesText));
        OnPropertyChanged(nameof(BrowserCacheText));
        OnPropertyChanged(nameof(AppCacheText));
        OnPropertyChanged(nameof(RecycleBinText));
        OnPropertyChanged(nameof(WindowsUpdateText));
        OnPropertyChanged(nameof(ScanButtonText));
        OnPropertyChanged(nameof(CleanButtonText));
        OnPropertyChanged(nameof(SaveSettingsButtonText));
        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(SelectAllText));
        OnPropertyChanged(nameof(ClearSelectionText));
        OnPropertyChanged(nameof(FoundSizeSummary));
        OnPropertyChanged(nameof(TempFilesTooltip));
        OnPropertyChanged(nameof(SystemFilesTooltip));
        OnPropertyChanged(nameof(BrowserCacheTooltip));
        OnPropertyChanged(nameof(AppCacheTooltip));
        OnPropertyChanged(nameof(RecycleBinTooltip));
        OnPropertyChanged(nameof(WindowsUpdateTooltip));
        OnPropertyChanged(nameof(ScanTooltip));
        OnPropertyChanged(nameof(CleanTooltip));
        OnPropertyChanged(nameof(SaveSettingsTooltip));
        OnPropertyChanged(nameof(SearchTooltip));
        OnPropertyChanged(nameof(Status));
    }
    
    // Статистика очистки
    [ObservableProperty] private long totalCleaned;    // Общий объем очищенных данных
    [ObservableProperty] private int filesDeleted;     // Количество удаленных файлов
    [ObservableProperty] private bool isOptimizedMode = false; // Режим оптимизированной очистки
    [ObservableProperty] private bool isWorking = false;       // Выполняется ли операция

    // Коллекции для отображения найденных файлов
    public ObservableCollection<FileItem> PreviewItems { get; } = new();     // Все найденные файлы
    public ObservableCollection<FileItem> FilteredItems { get; } = new();    // Отфильтрованные файлы
    public ObservableCollection<FileGroupViewModel> GroupedItems { get; } = new(); // Группированные по категориям

    [ObservableProperty]
    private string? filter; // Фильтр для поиска файлов

    public CleaningViewModel()
    {
        LoadSettings();
        FilteredItems = new ObservableCollection<FileItem>();
        Status = GetString("Ready");
    }
    
    /// <summary>
    /// Загружает сохраненные настройки очистки
    /// </summary>
    private void LoadSettings()
    {
        var settings = FileConfigService.LoadSettings();
        TempUser = settings.tempUser;
        TempWindows = settings.tempWindows;
        Browsers = settings.browsers;
        Apps = settings.apps;
        RecycleBin = settings.recycleBin;
        WindowsUpdate = settings.windowsUpdate;
    }
    
    /// <summary>
    /// Сохраняет текущие настройки очистки
    /// </summary>
    private void SaveSettings()
    {
        FileConfigService.SaveSettings(TempUser, TempWindows, Browsers, Apps, RecycleBin, WindowsUpdate);
    }
    
    /// <summary>
    /// Команда для ручного сохранения настроек
    /// </summary>
    [RelayCommand]
    private void SaveCleaningSettings()
    {
        SaveSettings();
        Status = GetString("SettingsSaved");
    }
    
    // Автосохранение при изменении настроек
    partial void OnTempUserChanged(bool value) => SaveSettings();
    partial void OnTempWindowsChanged(bool value) => SaveSettings();
    partial void OnBrowsersChanged(bool value) => SaveSettings();
    partial void OnAppsChanged(bool value) => SaveSettings();
    partial void OnRecycleBinChanged(bool value) => SaveSettings();
    partial void OnWindowsUpdateChanged(bool value) => SaveSettings();

    /// <summary>
    /// Многопоточное сканирование файлов для очистки
    /// Создает отдельные задачи для каждой категории файлов
    /// </summary>
    [RelayCommand]
    private async Task ScanAsync()
    {
        try
        {
            IsWorking = true;
            Status = GetString("Scanning");
            PreviewItems.Clear();
            FilteredItems.Clear();
            Progress = 0;
            
            var tasks = new List<Task<List<FileItem>>>();
            var completedTasks = 0;
            
            // Создаем задачи для каждой выбранной категории
            if (TempUser) 
            {
                tasks.Add(Task.Run(async () => 
                {
                    var result = await _service.EnumerateUserTempAsync();
                    var items = result.Select(f => new FileItem { Path = f.Path, Size = f.Size, Category = GetTempCategory(f.Path) }).ToList();
                    Interlocked.Increment(ref completedTasks);
                    Progress = (completedTasks * 100) / Math.Max(tasks.Count, 1);
                    return items;
                }));
            }
            
            if (TempWindows) 
            {
                tasks.Add(Task.Run(async () => 
                {
                    var result = await _service.EnumerateWindowsTempAsync();
                    var items = result.Select(f => new FileItem { Path = f.Path, Size = f.Size, Category = "💻 Системные файлы" }).ToList();
                    Interlocked.Increment(ref completedTasks);
                    Progress = (completedTasks * 100) / Math.Max(tasks.Count, 1);
                    return items;
                }));
            }
            
            if (Browsers) 
            {
                tasks.Add(Task.Run(async () => 
                {
                    var result = await _service.EnumerateBrowserCachesAsync();
                    var items = result.Select(f => new FileItem { Path = f.Path, Size = f.Size, Category = GetBrowserCategory(f.Path), ApplicationName = f.ApplicationName }).ToList();
                    Interlocked.Increment(ref completedTasks);
                    Progress = (completedTasks * 100) / Math.Max(tasks.Count, 1);
                    return items;
                }));
            }
            
            if (Apps) 
            {
                tasks.Add(Task.Run(async () => 
                {
                    var result = await _service.EnumerateAppCachesAsync();
                    var items = result.Select(f => new FileItem { Path = f.Path, Size = f.Size, Category = f.ApplicationName ?? "📦 Приложение", ApplicationName = f.ApplicationName }).ToList();
                    Interlocked.Increment(ref completedTasks);
                    Progress = (completedTasks * 100) / Math.Max(tasks.Count, 1);
                    return items;
                }));
            }
            
            if (WindowsUpdate) 
            {
                tasks.Add(Task.Run(async () => 
                {
                    var result = await _service.EnumerateWindowsUpdateAsync();
                    var items = result.Select(f => new FileItem { Path = f.Path, Size = f.Size, Category = "⚙️ Windows Update" }).ToList();
                    Interlocked.Increment(ref completedTasks);
                    Progress = (completedTasks * 100) / Math.Max(tasks.Count, 1);
                    return items;
                }));
            }
            
            // Ожидаем завершения всех задач и собираем результаты
            if (tasks.Count > 0)
            {
                var results = await Task.WhenAll(tasks);
                foreach (var fileList in results)
                {
                    foreach (var file in fileList)
                    {
                        PreviewItems.Add(file);
                    }
                }
            }
            
            Progress = 100;
            ApplyFilter();
            Status = string.Format(GetString("Found"), PreviewItems.Count);
        }
        catch (Exception ex)
        {
            Status = string.Format(GetString("ScanError"), ex.Message);
        }
        finally
        {
            IsWorking = false;
        }
    }

    /// <summary>
    /// Удаляет только файлы, отмеченные пользователем в результатах сканирования.
    /// </summary>
    [RelayCommand]
    private async Task CleanAsync()
    {
        var selectedItems = PreviewItems.Where(item => item.IsSelected).ToArray();
        if (selectedItems.Length == 0)
        {
            Status = GetString("NoFilesSelected");
            return;
        }

        var containsSystemFiles = selectedItems.Any(item =>
            item.Category.Contains("Системные", StringComparison.OrdinalIgnoreCase) ||
            item.Category.Contains("Windows Update", StringComparison.OrdinalIgnoreCase));

        if (containsSystemFiles && !AdminRightsService.CanAccessSystemFolders())
        {
            Status = "⚠️ " + GetString("AdminRequiredMessage");
            return;
        }
        
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        
        try
        {
            IsWorking = true;
            Status = GetString("Cleaning");
            Progress = 0;
            TotalCleaned = 0;
            FilesDeleted = 0;

            var result = await _service.DeleteFilesAsync(
                selectedItems,
                new Progress<int>(value => Progress = value),
                _cancellationTokenSource.Token,
                MainWindowViewModel.SharedSettings.SafeDelete);

            TotalCleaned = result.BytesFreed;
            FilesDeleted = (int)Math.Min(result.FilesDeleted, int.MaxValue);

            foreach (var deletedItem in PreviewItems
                         .Where(item => result.DeletedPaths.Contains(item.Path))
                         .ToArray())
            {
                PreviewItems.Remove(deletedItem);
            }

            ApplyFilter();
            Progress = 100;
            Status = result.Errors.Count == 0
                ? $"✅ Удалено: {result.FilesDeleted} файлов, освобождено: {FormatBytes(result.BytesFreed)}"
                : $"⚠️ Удалено: {result.FilesDeleted}, не удалось: {result.Errors.Count}, освобождено: {FormatBytes(result.BytesFreed)}";
            NotificationService.ShowCleaningComplete(result.FilesDeleted, result.BytesFreed);
        }
        catch (OperationCanceledException)
        {
            Status = "❌ Отменено";
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
            NotificationService.ShowError(ex.Message);
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        var items = GetSelectionScope();
        foreach (var item in items)
            item.IsSelected = true;

        OnPropertyChanged(nameof(FoundSizeSummary));
        Status = string.Format(GetString("FilesSelected"), items.Count);
    }

    [RelayCommand]
    private void ClearSelection()
    {
        var items = GetSelectionScope();
        foreach (var item in items)
            item.IsSelected = false;

        OnPropertyChanged(nameof(FoundSizeSummary));
        Status = GetString("SelectionCleared");
    }

    private List<FileItem> GetSelectionScope() => PreviewItems.ToList();

    /// <summary>
    /// Обработчик изменения фильтра - автоматически применяет фильтрацию
    /// </summary>
    partial void OnFilterChanged(string? value)
    {
        ApplyFilter();
    }

    /// <summary>
    /// Применяет фильтр к списку найденных файлов и группирует их по приложениям
    /// </summary>
    private void ApplyFilter()
    {
        FilteredItems.Clear();

        foreach (var oldGroup in GroupedItems)
            oldGroup.Dispose();
        GroupedItems.Clear();

        var matchingItems = string.IsNullOrWhiteSpace(Filter)
            ? PreviewItems.ToList()
            : PreviewItems
                .Where(item => item.Path.Contains(Filter!, StringComparison.OrdinalIgnoreCase))
                .ToList();
        var itemsToShow = matchingItems;
        
        foreach (var item in itemsToShow)
        {
            FilteredItems.Add(item);
        }
        
        // Размер и общая галочка учитывают всю группу без скрытого лимита строк.
        var visiblePaths = itemsToShow
            .Select(item => item.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var grouped = matchingItems
            .GroupBy(GetGroupKey)
            .OrderBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase);

        foreach (var group in grouped)
        {
            var allGroupItems = group.ToList();
            var visibleGroupItems = allGroupItems
                .Where(item => visiblePaths.Contains(item.Path))
                .ToList();
            if (visibleGroupItems.Count == 0)
                continue;

            var groupViewModel = new FileGroupViewModel(group.Key, allGroupItems, visibleGroupItems);
            groupViewModel.SelectionChanged += OnGroupSelectionChanged;
            GroupedItems.Add(groupViewModel);
        }

        OnPropertyChanged(nameof(FoundSizeSummary));
    }

    private static string GetGroupKey(FileItem item) =>
        !string.IsNullOrEmpty(item.ApplicationName) ? item.ApplicationName : item.Category;

    private void OnGroupSelectionChanged(object? sender, EventArgs eventArgs) =>
        OnPropertyChanged(nameof(FoundSizeSummary));
    
    /// <summary>
    /// Определяет категорию браузера по пути к файлу
    /// </summary>
    private static string GetBrowserCategory(string path)
    {
        if (path.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
            return "🌐 Google Chrome";
        if (path.Contains("Edge", StringComparison.OrdinalIgnoreCase))
            return "🌐 Microsoft Edge";
        if (path.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
            return "🌐 Mozilla Firefox";
        return "🌐 Браузер";
    }
    
    /// <summary>
    /// Определяет категорию временного файла по пути
    /// </summary>
    private static string GetTempCategory(string path)
    {
        if (path.Contains("Microsoft", StringComparison.OrdinalIgnoreCase))
            return "📁 Microsoft временные";
        if (path.Contains("Adobe", StringComparison.OrdinalIgnoreCase))
            return "📁 Adobe временные";
        if (path.Contains("Google", StringComparison.OrdinalIgnoreCase))
            return "📁 Google временные";
        if (path.Contains("Windows", StringComparison.OrdinalIgnoreCase))
            return "📁 Windows временные";
        if (path.Contains(".tmp", StringComparison.OrdinalIgnoreCase) || path.Contains(".temp", StringComparison.OrdinalIgnoreCase))
            return "📁 .tmp файлы";
        if (path.Contains("cache", StringComparison.OrdinalIgnoreCase))
            return "📁 Кэш файлы";
        if (path.Contains("log", StringComparison.OrdinalIgnoreCase))
            return "📁 Логи";
        return "📁 Прочие временные";
    }
    
    /// <summary>
    /// Определяет категорию приложения по пути к файлу
    /// </summary>
    private static string GetAppCategory(string path)
    {
        if (path.Contains("Discord", StringComparison.OrdinalIgnoreCase))
            return "💬 Discord";
        if (path.Contains("Telegram", StringComparison.OrdinalIgnoreCase))
            return "💬 Telegram";
        if (path.Contains("Steam", StringComparison.OrdinalIgnoreCase))
            return "🎮 Steam";
        if (path.Contains("Spotify", StringComparison.OrdinalIgnoreCase))
            return "🎵 Spotify";
        return "📦 Приложение";
    }

    /// <summary>
    /// Форматирует размер в байтах в удобочитаемый вид
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        if (bytes == 0) return "0 B";
        var i = (int)Math.Floor(Math.Log(bytes) / Math.Log(1024));
        i = Math.Max(0, Math.Min(i, sizes.Length - 1));
        var v = bytes / Math.Pow(1024, i);
        return $"{v:0.##} {sizes[i]}";
    }
    
    /// <summary>
    /// Отменяет все выполняющиеся операции
    /// </summary>
    public void CancelOperations()
    {
        _cancellationTokenSource?.Cancel();
    }
}
