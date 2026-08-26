using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleanerGUI.Avalonia.Services;
using DiskCleanerGUI.Avalonia.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Threading;

namespace DiskCleanerGUI.Avalonia.ViewModels;

public partial class UtilitiesViewModel : LocalizedViewModelBase
{
    private readonly SystemUtilitiesService _systemService = new();
    private readonly Timer _systemInfoTimer;
    private int _isRefreshingSystemInfo;
    
    [ObservableProperty] private string status = "";
    
    protected override void OnLanguageChanged()
    {
        Status = GetString("ReadyToWork");
        OnPropertyChanged(nameof(RestartExplorerText));
        OnPropertyChanged(nameof(FreeMemoryText));
        OnPropertyChanged(nameof(FlushDnsText));
        OnPropertyChanged(nameof(ResetNetworkText));
        OnPropertyChanged(nameof(SetTimerText));
        OnPropertyChanged(nameof(CancelTimerText));
        OnPropertyChanged(nameof(RestartExplorerTooltipText));
        OnPropertyChanged(nameof(FreeMemoryTooltipText));
        OnPropertyChanged(nameof(FlushDnsTooltipText));
        OnPropertyChanged(nameof(ResetNetworkTooltipText));
        OnPropertyChanged(nameof(SetTimerTooltipText));
        OnPropertyChanged(nameof(CancelTimerTooltipText));
        OnPropertyChanged(nameof(Status));
    }
    [ObservableProperty] private bool isWorking = false;
    [ObservableProperty] private string shutdownMinutes = "30";
    
    // Localized properties
    public string RestartExplorerText => GetString("RestartExplorer");
    public string FreeMemoryText => GetString("FreeMemory");
    public string FlushDnsText => GetString("FlushDns");
    public string ResetNetworkText => GetString("ResetNetwork");
    public string SetTimerText => GetString("SetTimer");
    public string CancelTimerText => GetString("CancelTimer");
    
    // Tooltips
    public string RestartExplorerTooltipText => GetString("RestartExplorerTooltip");
    public string FreeMemoryTooltipText => GetString("FreeMemoryTooltip");
    public string FlushDnsTooltipText => GetString("FlushDnsTooltip");
    public string ResetNetworkTooltipText => GetString("ResetNetworkTooltip");
    public string SetTimerTooltipText => GetString("SetTimerTooltip");
    public string CancelTimerTooltipText => GetString("CancelTimerTooltip");
    
    // System monitoring
    [ObservableProperty] private int cpuUsage = 0;
    [ObservableProperty] private long memoryUsageMB = 0;
    [ObservableProperty] private string memoryUsageText = "0 MB";
    
    public ObservableCollection<DiskInfo> DiskUsage { get; } = new();
    
    // Large files finder
    private readonly LargeFileFinderService _finder = new();
    private readonly SafeDeleteService _safeDelete = new();
    private CancellationTokenSource? _cts;
    
    [ObservableProperty] private ObservableCollection<LargeFileItem> _largeFiles = new();
    [ObservableProperty] private LargeFileItem? _selectedLargeFile;
    [ObservableProperty] private string _scanPath = "C:\\";
    [ObservableProperty] private int _minSizeMB = 100;
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private string _largeFilesStatus = "Готов к поиску";
    
    public UtilitiesViewModel()
    {
        Status = GetString("ReadyToWork");
        // Update system info every 2 seconds
        _systemInfoTimer = new Timer(UpdateSystemInfo, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    }
    
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private void UpdateSystemInfo(object? state)
    {
        if (Interlocked.Exchange(ref _isRefreshingSystemInfo, 1) != 0)
            return;

        try
        {
            var info = _systemService.GetSystemInfo();
            Dispatcher.UIThread.Post(() =>
            {
                CpuUsage = info.CpuUsage;
                MemoryUsageMB = info.MemoryUsageMB;
                MemoryUsageText = $"{info.MemoryUsageMB} MB";

                DiskUsage.Clear();
                foreach (var disk in info.DiskUsage.OrderBy(disk => disk.Drive, StringComparer.OrdinalIgnoreCase))
                    DiskUsage.Add(disk);
            });
        }
        catch
        {
            // Monitoring failure should not affect the rest of the utilities screen.
        }
        finally
        {
            Volatile.Write(ref _isRefreshingSystemInfo, 0);
        }
    }
    
    [RelayCommand]
    private async Task RestartExplorerAsync()
    {
        if (IsWorking) return;
        
        try
        {
            IsWorking = true;
            Status = "🔄 Перезапуск проводника...";
            
            var result = await _systemService.RestartExplorerAsync();
            Status = result;
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }
    
    [RelayCommand]
    private async Task FlushDnsAsync()
    {
        if (IsWorking) return;
        
        try
        {
            IsWorking = true;
            Status = "🌐 Очистка DNS кэша...";
            
            var result = await _systemService.FlushDnsAsync();
            Status = result;
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }
    
    [RelayCommand]
    private async Task ResetNetworkAsync()
    {
        if (IsWorking) return;
        
        try
        {
            IsWorking = true;
            Status = "🔧 Сброс сетевых настроек...";
            
            var result = await _systemService.ResetNetworkAsync();
            Status = result;
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }
    
    [RelayCommand]
    private void FreeMemory()
    {
        if (IsWorking) return;
        
        try
        {
            IsWorking = true;
            Status = "🧠 Освобождение памяти...";
            
            var result = _systemService.FreeMemory();
            Status = result;
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }
    
    [RelayCommand]
    private async Task SetShutdownTimerAsync()
    {
        if (IsWorking) return;
        
        try
        {
            IsWorking = true;
            Status = "⏰ Установка таймера...";
            
            if (!int.TryParse(ShutdownMinutes, out int minutes) || minutes < 1 || minutes > 1440)
            {
                Status = "❌ Введите число от 1 до 1440";
                return;
            }
            
            var result = await _systemService.SetShutdownTimerAsync(minutes);
            Status = result;
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }
    
    [RelayCommand]
    private async Task CancelShutdownAsync()
    {
        if (IsWorking) return;
        
        try
        {
            IsWorking = true;
            Status = "❌ Отмена таймера...";
            
            var result = await _systemService.SetShutdownTimerAsync(0);
            Status = result;
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }
    
    [RelayCommand]
    private async Task ScanLargeFilesAsync()
    {
        if (IsScanning) return;

        IsScanning = true;
        LargeFiles.Clear();
        _cts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<string>(msg => LargeFilesStatus = msg);
            var results = await _finder.FindLargeFilesAsync(ScanPath, MinSizeMB, progress, _cts.Token);

            foreach (var file in results)
                LargeFiles.Add(file);

            var totalSize = LargeFiles.Sum(f => f.Size);
            LargeFilesStatus = $"Найдено: {LargeFiles.Count} файлов, {FormatBytes(totalSize)}";
        }
        catch (OperationCanceledException)
        {
            LargeFilesStatus = "Поиск отменён";
        }
        catch (Exception ex)
        {
            LargeFilesStatus = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            _cts?.Dispose();
        }
    }

    [RelayCommand]
    private void CancelScan()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    private void OpenLargeFile()
    {
        if (SelectedLargeFile == null) return;
        try
        {
            Process.Start(new ProcessStartInfo(SelectedLargeFile.FullPath) { UseShellExecute = true });
        }
        catch { }
    }

    [RelayCommand]
    private void OpenLargeFileFolder()
    {
        if (SelectedLargeFile == null) return;
        try
        {
            Process.Start("explorer.exe", $"/select,\"{SelectedLargeFile.FullPath}\"");
        }
        catch { }
    }

    [RelayCommand]
    private async Task DeleteLargeFileAsync()
    {
        if (SelectedLargeFile == null) return;

        try
        {
            var useSafeDelete = MainWindowViewModel.SharedSettings.SafeDelete;
            
            if (useSafeDelete)
            {
                if (!await _safeDelete.SafeDeleteAsync(SelectedLargeFile.FullPath))
                {
                    LargeFilesStatus = "Не удалось переместить файл во внутреннюю корзину";
                    return;
                }
            }
            else
            {
                File.Delete(SelectedLargeFile.FullPath);
            }

            LargeFiles.Remove(SelectedLargeFile);
            var totalSize = LargeFiles.Sum(f => f.Size);
            LargeFilesStatus = $"Найдено: {LargeFiles.Count} файлов, {FormatBytes(totalSize)}";
        }
        catch (Exception ex)
        {
            LargeFilesStatus = $"Ошибка удаления: {ex.Message}";
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
    
    public void Dispose()
    {
        _systemInfoTimer?.Dispose();
        _cts?.Dispose();
    }
}
