using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleanerGUI.Avalonia.Models;
using DiskCleanerGUI.Avalonia.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiskCleanerGUI.Avalonia.ViewModels;

public partial class LargeFilesViewModel : ViewModelBase
{
    private readonly LargeFileFinderService _finder = new();
    private readonly SafeDeleteService _safeDelete = new();
    private CancellationTokenSource? _cts;

    [ObservableProperty] private ObservableCollection<LargeFileItem> _files = new();
    [ObservableProperty] private LargeFileItem? _selectedFile;
    [ObservableProperty] private string _scanPath = "C:\\";
    [ObservableProperty] private int _minSizeMB = 100;
    [ObservableProperty] private string _statusText = "Готов к поиску";
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private string _totalSizeText = "";

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (IsScanning) return;

        IsScanning = true;
        Files.Clear();
        _cts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<string>(msg => StatusText = msg);
            var results = await _finder.FindLargeFilesAsync(ScanPath, MinSizeMB, progress, _cts.Token);

            foreach (var file in results)
                Files.Add(file);

            var totalSize = Files.Sum(f => f.Size);
            TotalSizeText = $"Найдено: {Files.Count} файлов, общий размер: {FormatBytes(totalSize)}";
            StatusText = "Поиск завершён";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Поиск отменён";
        }
        catch (Exception ex)
        {
            StatusText = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            _cts?.Dispose();
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    private void OpenFile()
    {
        if (SelectedFile == null) return;
        try
        {
            Process.Start(new ProcessStartInfo(SelectedFile.FullPath) { UseShellExecute = true });
        }
        catch { }
    }

    [RelayCommand]
    private void OpenFolder()
    {
        if (SelectedFile == null) return;
        try
        {
            Process.Start("explorer.exe", $"/select,\"{SelectedFile.FullPath}\"");
        }
        catch { }
    }

    [RelayCommand]
    private async Task DeleteFileAsync()
    {
        if (SelectedFile == null) return;

        try
        {
            var useSafeDelete = MainWindowViewModel.SharedSettings.SafeDelete;
            
            if (useSafeDelete)
            {
                if (!await _safeDelete.SafeDeleteAsync(SelectedFile.FullPath))
                {
                    StatusText = "Не удалось переместить файл во внутреннюю корзину";
                    return;
                }
            }
            else
            {
                File.Delete(SelectedFile.FullPath);
            }

            Files.Remove(SelectedFile);
            var totalSize = Files.Sum(f => f.Size);
            TotalSizeText = $"Найдено: {Files.Count} файлов, общий размер: {FormatBytes(totalSize)}";
        }
        catch (Exception ex)
        {
            StatusText = $"Ошибка удаления: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SelectUserProfile()
    {
        ScanPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    [RelayCommand]
    private void SelectDownloads()
    {
        ScanPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }

    [RelayCommand]
    private void SelectDocuments()
    {
        ScanPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    private static string FormatBytes(long bytes)
    {
        return bytes switch
        {
            < 1024 * 1024 => $"{bytes / 1024:F1} KB",
            < 1024 * 1024 * 1024 => $"{bytes / (1024 * 1024):F1} MB",
            _ => $"{bytes / (1024 * 1024 * 1024):F2} GB"
        };
    }
}
