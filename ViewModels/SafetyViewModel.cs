using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleanerGUI.Avalonia.Services;
using System.Collections.ObjectModel;

namespace DiskCleanerGUI.Avalonia.ViewModels;

public partial class SafetyViewModel : LocalizedViewModelBase
{
    private readonly SafeDeleteService _safeDelete = new();
    
    [ObservableProperty] private string status = "";
    
    protected override void OnLanguageChanged()
    {
        Status = GetString("ReadyToWork");
        OnPropertyChanged(nameof(LoadTrashText));
        OnPropertyChanged(nameof(EmptyTrashText));
        OnPropertyChanged(nameof(RestoreFileText));
        OnPropertyChanged(nameof(LoadTrashTooltipText));
        OnPropertyChanged(nameof(EmptyTrashTooltipText));
        OnPropertyChanged(nameof(RestoreFileTooltipText));
        OnPropertyChanged(nameof(Status));
    }
    
    public SafetyViewModel()
    {
        Status = GetString("ReadyToWork");
    }
    
    public ObservableCollection<TrashItem> TrashItems { get; } = new();
    
    // Localized properties
    public string LoadTrashText => GetString("LoadTrash");
    public string EmptyTrashText => GetString("EmptyTrash");
    public string RestoreFileText => GetString("RestoreFile");
    
    // Tooltips
    public string LoadTrashTooltipText => GetString("LoadTrashTooltip");
    public string EmptyTrashTooltipText => GetString("EmptyTrashTooltip");
    public string RestoreFileTooltipText => GetString("RestoreFileTooltip");
    
    [RelayCommand]
    private async Task LoadTrashAsync()
    {
        Status = "Загрузка корзины...";
        
        try
        {
            // Получаем все элементы: внутренняя корзина + системная
            var items = await _safeDelete.GetAllTrashItemsAsync();
            
            TrashItems.Clear();
            foreach (var item in items)
            {
                TrashItems.Add(item);
            }
            
            var internalCount = items.Count(i => !i.IsSystemRecycleBin);
            var systemCount = items.Count(i => i.IsSystemRecycleBin);
            
            if (systemCount > 0)
            {
                Status = $"Внутренняя корзина: {internalCount}, Системная: {systemCount}";
            }
            else
            {
                Status = $"Внутренняя корзина: {internalCount} файлов";
            }
            

        }
        catch (Exception ex)
        {
            Status = $"Ошибка: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private async Task RestoreFileAsync(TrashItem? item)
    {
        if (item == null) return;
        
        if (item.IsSystemRecycleBin)
        {
            Status = "Системная корзина не поддерживает восстановление";
            return;
        }
        
        Status = $"Восстановление: {Path.GetFileName(item.OriginalPath)}";
        
        if (await _safeDelete.RestoreFileAsync(item))
        {
            TrashItems.Remove(item);
            Status = "Файл восстановлен успешно!";
        }
        else
        {
            Status = "Ошибка восстановления файла";
        }
    }
    
    [RelayCommand]
    private async Task ClearTrashAsync()
    {
        Status = "Очистка корзины...";
        
        try
        {
            // Очищаем внутреннюю корзину
            await _safeDelete.ClearTrashAsync();
            
            // Очищаем системную корзину
            var systemCleared = await _safeDelete.EmptySystemRecycleBinAsync();

            var remaining = await _safeDelete.GetTrashItemsAsync();
            TrashItems.Clear();
            foreach (var item in remaining)
                TrashItems.Add(item);

            Status = remaining.Count == 0 && systemCleared
                ? "Обе корзины очищены"
                : $"Осталось во внутренней корзине: {remaining.Count}; системная корзина: {(systemCleared ? "очищена" : "ошибка")}";
        }
        catch (Exception ex)
        {
            Status = $"Ошибка очистки: {ex.Message}";
        }
    }
    

    
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        if (bytes == 0) return "0 B";
        var i = (int)Math.Floor(Math.Log(bytes) / Math.Log(1024));
        i = Math.Max(0, Math.Min(i, sizes.Length - 1));
        var v = bytes / Math.Pow(1024, i);
        return $"{v:0.##} {sizes[i]}";
    }
}
