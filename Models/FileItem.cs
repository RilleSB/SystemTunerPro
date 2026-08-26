using CommunityToolkit.Mvvm.ComponentModel;

namespace DiskCleanerGUI.Avalonia.Models;

/// <summary>
/// Модель файла для отображения в списках и таблицах
/// </summary>
public partial class FileItem : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private string _path = "";                         // Полный путь к файлу
    public long Size { get; init; }                     // Размер файла в байтах
    public string Category { get; set; } = "Неизвестно"; // Категория файла (браузер, приложение и т.д.)
    public string? ApplicationName { get; set; }        // Имя приложения, которому принадлежит файл
}
