using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleanerGUI.Avalonia.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Avalonia.Media.Imaging;

namespace DiskCleanerGUI.Avalonia.ViewModels;

public partial class FileViewerViewModel : LocalizedViewModelBase
{
    public ObservableCollection<FileItem> Files { get; } = new();
    public ObservableCollection<FileItem> FilteredFiles { get; } = new();

    [ObservableProperty] private FileItem? selectedItem;
    [ObservableProperty] private string? currentPath;
    [ObservableProperty] private string? filter;
    [ObservableProperty] private string? moveToPath;
    [ObservableProperty] private string status = "";
    
    protected override void OnLanguageChanged()
    {
        Status = GetString("ReadyToWork");
        if (SelectedItem == null)
        {
            FilePreview = GetString("SelectFileForPreview");
        }
        OnPropertyChanged(nameof(BrowseText));
        OnPropertyChanged(nameof(LoadText));
        OnPropertyChanged(nameof(OpenFileText));
        OnPropertyChanged(nameof(OpenInExplorerText));
        OnPropertyChanged(nameof(RemoveFromListText));
        OnPropertyChanged(nameof(MoveSelectedText));
        OnPropertyChanged(nameof(MoveAllText));
        OnPropertyChanged(nameof(BrowseTooltipText));
        OnPropertyChanged(nameof(LoadTooltipText));
        OnPropertyChanged(nameof(OpenFileTooltipText));
        OnPropertyChanged(nameof(OpenInExplorerTooltipText));
        OnPropertyChanged(nameof(RemoveFromListTooltipText));
        OnPropertyChanged(nameof(BrowseMoveTooltipText));
        OnPropertyChanged(nameof(MoveSelectedTooltipText));
        OnPropertyChanged(nameof(MoveAllTooltipText));
        OnPropertyChanged(nameof(Status));
    }
    
    public FileViewerViewModel()
    {
        Status = GetString("ReadyToWork");
        FilePreview = GetString("SelectFileForPreview");
    }
    [ObservableProperty] private string filePreview = "";
    [ObservableProperty] private Bitmap? previewImage;
    [ObservableProperty] private bool isImagePreview;
    [ObservableProperty] private bool isTextPreview = true;
    [ObservableProperty] private int moveProgress;
    [ObservableProperty] private string moveStatus = "";
    [ObservableProperty] private bool isMoving;
    
    // Localized properties
    public string BrowseText => GetString("Browse");
    public string LoadText => GetString("Load");
    public string OpenFileText => GetString("OpenFile");
    public string OpenInExplorerText => GetString("OpenInExplorer");
    public string RemoveFromListText => GetString("RemoveFromList");
    public string MoveSelectedText => GetString("MoveSelected");
    public string MoveAllText => GetString("MoveAll");
    
    // Tooltips
    public string BrowseTooltipText => GetString("BrowseTooltip");
    public string LoadTooltipText => GetString("LoadTooltip");
    public string OpenFileTooltipText => GetString("OpenFileTooltip");
    public string OpenInExplorerTooltipText => GetString("OpenInExplorerTooltip");
    public string RemoveFromListTooltipText => GetString("RemoveFromListTooltip");
    public string BrowseMoveTooltipText => GetString("BrowseMoveTooltip");
    public string MoveSelectedTooltipText => GetString("MoveSelectedTooltip");
    public string MoveAllTooltipText => GetString("MoveAllTooltip");

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedItem is { } item)
        {
            Files.Remove(item);
            FilteredFiles.Remove(item);
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentPath) || !Directory.Exists(CurrentPath)) 
        {
            Status = "Неверный путь";
            return;
        }
        
        Status = "Загрузка...";
        Files.Clear();
        FilteredFiles.Clear();
        
        try
        {
            var files = await Task.Run(() => 
            {
                var fileList = new List<FileItem>();
                
                try
                {
                    // Get files in current directory first
                    foreach (var f in Directory.EnumerateFiles(CurrentPath))
                    {
                        try
                        {
                            var info = new FileInfo(f);
                            fileList.Add(new FileItem { Path = f, Size = info.Length });
                        }
                        catch { }
                    }
                    
                    // Get subdirectories and process them safely
                    foreach (var dir in Directory.EnumerateDirectories(CurrentPath))
                    {
                        try
                        {
                            foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                            {
                                try
                                {
                                    var info = new FileInfo(f);
                                    fileList.Add(new FileItem { Path = f, Size = info.Length });
                                }
                                catch { }
                            }
                        }
                        catch { } // Skip directories we can't access
                    }
                }
                catch { } // Skip if we can't access the main directory
                
                return fileList;
            });
            
            foreach (var file in files)
            {
                Files.Add(file);
            }
            
            ApplyFilter();
            Status = $"Найдено: {Files.Count} файлов";
        }
        catch (Exception ex)
        {
            Status = $"Ошибка: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenSelected()
    {
        if (SelectedItem == null) return;
        try { Process.Start(new ProcessStartInfo(SelectedItem.Path) { UseShellExecute = true }); } catch { }
    }

    [RelayCommand]
    private void OpenSelectedFolder()
    {
        if (SelectedItem == null) return;
        try { Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{SelectedItem.Path}\"") { UseShellExecute = true }); } catch { }
    }

    [RelayCommand]
    private async Task MoveSelectedAsync()
    {
        if (SelectedItem == null || !TryGetMoveRoots(out var sourceRoot, out var destinationRoot)) return;
        
        IsMoving = true;
        MoveProgress = 0;
        MoveStatus = "Перемещение файла...";
        
        try
        {
            var item = SelectedItem;
            if (IsPathInside(item.Path, destinationRoot))
            {
                MoveStatus = "Файл уже находится в папке назначения";
                Status = MoveStatus;
                return;
            }

            var newPath = await Task.Run(() =>
            {
                var targetPath = GetDestinationPathWithStructure(item.Path, sourceRoot, destinationRoot);
                if (PathsEqual(item.Path, targetPath))
                    throw new IOException("Исходный файл и путь назначения совпадают");

                targetPath = GetUniqueFileName(targetPath);
                var destDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                File.Move(item.Path, targetPath);
                return targetPath;
            });

            item.Path = newPath;
            MoveProgress = 100;
            MoveStatus = "Файл перемещён";
            Status = $"Перемещён: {Path.GetFileName(newPath)}";
        }
        catch (Exception ex)
        {
            MoveStatus = $"Ошибка: {ex.Message}";
            await Task.Delay(2000);
        }
        finally
        {
            IsMoving = false;
        }
    }

    [RelayCommand]
    private async Task MoveAllFilteredAsync()
    {
        if (!TryGetMoveRoots(out var sourceRoot, out var destinationRoot)) return;

        var filesToMove = FilteredFiles
            .Where(item => !IsPathInside(item.Path, destinationRoot))
            .ToList();
        if (filesToMove.Count == 0)
        {
            Status = "Нет файлов для перемещения";
            return;
        }
        
        IsMoving = true;
        MoveProgress = 0;
        
        try
        {
            MoveStatus = $"Подготовка к перемещению {filesToMove.Count} файлов...";

            IProgress<int> progress = new Progress<int>(completed =>
            {
                MoveProgress = completed * 100 / filesToMove.Count;
                MoveStatus = $"Обработано {completed} из {filesToMove.Count} файлов...";
            });

            var result = await Task.Run(() =>
            {
                var moved = new List<(FileItem Item, string NewPath)>();
                var errors = new List<string>();

                foreach (var item in filesToMove)
                {
                    try
                    {
                        var newPath = GetDestinationPathWithStructure(item.Path, sourceRoot, destinationRoot);
                        if (PathsEqual(item.Path, newPath))
                            throw new IOException("исходный файл и путь назначения совпадают");

                        newPath = GetUniqueFileName(newPath);
                        var destDir = Path.GetDirectoryName(newPath);
                        if (!string.IsNullOrEmpty(destDir))
                            Directory.CreateDirectory(destDir);

                        File.Move(item.Path, newPath);
                        moved.Add((item, newPath));
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{item.Path}: {ex.Message}");
                    }

                    progress.Report(moved.Count + errors.Count);
                }

                return (moved, errors);
            });

            foreach (var movedFile in result.moved)
                movedFile.Item.Path = movedFile.NewPath;

            MoveStatus = result.errors.Count == 0
                ? $"Перемещено {result.moved.Count} файлов"
                : $"Перемещено {result.moved.Count}, ошибок: {result.errors.Count}";
            Status = MoveStatus;
        }
        catch (Exception ex)
        {
            MoveStatus = $"Ошибка: {ex.Message}";
            await Task.Delay(2000);
        }
        finally
        {
            IsMoving = false;
        }
    }

    partial void OnFilterChanged(string? value) => ApplyFilter();
    
    partial void OnSelectedItemChanged(FileItem? value)
    {
        // Reset preview state
        IsImagePreview = false;
        IsTextPreview = true;
        PreviewImage?.Dispose();
        PreviewImage = null;
        
        if (value == null)
        {
            FilePreview = GetString("SelectFileForPreview");
            return;
        }
        
        try
        {
            var ext = Path.GetExtension(value.Path).ToLower();
            var fileName = Path.GetFileName(value.Path);
            var fileInfo = new FileInfo(value.Path);
            
            var preview = $"Файл: {fileName}\n";
            preview += $"Путь: {value.Path}\n";
            preview += $"Размер: {FormatBytes(value.Size)}\n";
            preview += $"Создан: {fileInfo.CreationTime:dd.MM.yyyy HH:mm}\n";
            preview += $"Изменен: {fileInfo.LastWriteTime:dd.MM.yyyy HH:mm}\n\n";
            
            // Try to preview images
            if (ext is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".webp" or ".ico")
            {
                try
                {
                    if (value.Size < 50 * 1024 * 1024) // Max 50MB
                    {
                        PreviewImage = new Bitmap(value.Path);
                        IsImagePreview = true;
                        IsTextPreview = false;
                        preview += $"Разрешение: {PreviewImage.PixelSize.Width}x{PreviewImage.PixelSize.Height}";
                    }
                    else
                    {
                        preview += "Изображение слишком большое для предпросмотра";
                    }
                }
                catch
                {
                    preview += "Ошибка загрузки изображения";
                }
            }
            // Try to preview text files
            else if (ext is ".txt" or ".log" or ".ini" or ".cfg" or ".xml" or ".json" or ".cs" or ".js" or ".html" or ".css")
            {
                if (value.Size < 10000) // Only small files
                {
                    var content = File.ReadAllText(value.Path);
                    preview += "Содержимое:\n" + content.Substring(0, Math.Min(content.Length, 500));
                    if (content.Length > 500) preview += "\n...";
                }
                else
                {
                    preview += "Файл слишком большой для предпросмотра";
                }
            }
            // Video files info
            else if (ext is ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" or ".webm")
            {
                preview += $"Видеофайл: {ext.ToUpper().TrimStart('.')}\n";
                preview += "Предпросмотр видео не поддерживается";
            }
            else
            {
                preview += $"Тип файла: {ext.ToUpper().TrimStart('.')}";
            }
            
            FilePreview = preview;
        }
        catch (Exception ex)
        {
            FilePreview = $"Ошибка предпросмотра: {ex.Message}";
        }
    }

    private void ApplyFilter()
    {
        FilteredFiles.Clear();
        
        var itemsToShow = string.IsNullOrWhiteSpace(Filter) 
            ? Files.ToList()
            : Files.Where(i => i.Path.Contains(Filter!, System.StringComparison.OrdinalIgnoreCase)).ToList();
        
        foreach (var item in itemsToShow)
        {
            FilteredFiles.Add(item);
        }
    }
    
    /// <summary>
    /// Создает путь назначения с сохранением структуры подпапок
    /// </summary>
    private static string GetDestinationPathWithStructure(string filePath, string sourcePath, string destinationPath)
    {
        try
        {
            // Получаем относительный путь от корневой папки
            var relativePath = Path.GetRelativePath(sourcePath, filePath);
            
            // Объединяем с папкой назначения
            return Path.Combine(destinationPath, relativePath);
        }
        catch
        {
            // Если не удалось получить относительный путь, используем только имя файла
            return Path.Combine(destinationPath, Path.GetFileName(filePath));
        }
    }

    private bool TryGetMoveRoots(out string sourceRoot, out string destinationRoot)
    {
        sourceRoot = "";
        destinationRoot = "";

        if (string.IsNullOrWhiteSpace(CurrentPath) || !Directory.Exists(CurrentPath))
        {
            Status = "Исходная папка не существует";
            return false;
        }

        if (string.IsNullOrWhiteSpace(MoveToPath))
        {
            Status = "Укажите папку назначения";
            return false;
        }

        try
        {
            sourceRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(CurrentPath));
            destinationRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(MoveToPath));
            if (PathsEqual(sourceRoot, destinationRoot))
            {
                Status = "Исходная папка и папка назначения совпадают";
                return false;
            }

            Directory.CreateDirectory(destinationRoot);
            return true;
        }
        catch (Exception ex)
        {
            Status = $"Неверный путь назначения: {ex.Message}";
            return false;
        }
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)),
            StringComparison.OrdinalIgnoreCase);

    private static bool IsPathInside(string path, string directory)
    {
        var relative = Path.GetRelativePath(directory, Path.GetFullPath(path));
        return !relative.Equals("..", StringComparison.Ordinal) &&
               !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
               !Path.IsPathRooted(relative);
    }
    
    /// <summary>
    /// Получает уникальное имя файла, если файл уже существует
    /// </summary>
    private static string GetUniqueFileName(string filePath)
    {
        if (!File.Exists(filePath)) return filePath;
        
        var directory = Path.GetDirectoryName(filePath) ?? "";
        var nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        
        int counter = 1;
        string newPath;
        
        do
        {
            newPath = Path.Combine(directory, $"{nameWithoutExt}_{counter}{extension}");
            counter++;
        } while (File.Exists(newPath));
        
        return newPath;
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
