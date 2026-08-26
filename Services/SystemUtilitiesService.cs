using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DiskCleanerGUI.Avalonia.Services;

public class SystemUtilitiesService
{
    [DllImport("kernel32.dll")]
    private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

    public async Task<string> RestartExplorerAsync()
    {
        try
        {
            // Kill explorer
            var explorerProcesses = Process.GetProcessesByName("explorer");
            foreach (var process in explorerProcesses)
            {
                process.Kill();
                await process.WaitForExitAsync();
            }
            
            await Task.Delay(1000);
            
            // Start explorer
            Process.Start("explorer.exe");
            return "✅ Проводник перезапущен";
        }
        catch (Exception ex)
        {
            return $"❌ Ошибка: {ex.Message}";
        }
    }
    
    public async Task<string> FlushDnsAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c ipconfig /flushdns",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            await process.WaitForExitAsync();
            
            return "✅ DNS кэш очищен";
        }
        catch (Exception ex)
        {
            return $"❌ Ошибка: {ex.Message}";
        }
    }
    
    public async Task<string> ResetNetworkAsync()
    {
        try
        {
            var commands = new[]
            {
                "netsh winsock reset",
                "netsh int ip reset",
                "ipconfig /release",
                "ipconfig /renew"
            };
            
            foreach (var cmd in commands)
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {cmd}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                await process.WaitForExitAsync();
            }
            
            return "✅ Сеть сброшена (требуется перезагрузка)";
        }
        catch (Exception ex)
        {
            return $"❌ Ошибка: {ex.Message}";
        }
    }
    
    public string FreeMemory()
    {
        try
        {
            var beforeMB = GC.GetTotalMemory(false) / 1024 / 1024;
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            // Force working set trim
            SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, (IntPtr)(-1), (IntPtr)(-1));
            
            var afterMB = GC.GetTotalMemory(false) / 1024 / 1024;
            var freedMB = beforeMB - afterMB;
            
            return $"✅ Освобождено ~{freedMB}MB памяти";
        }
        catch (Exception ex)
        {
            return $"❌ Ошибка: {ex.Message}";
        }
    }
    
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public SystemInfo GetSystemInfo()
    {
        try
        {
            var info = new SystemInfo();
            
            // CPU usage
            using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue();
            Thread.Sleep(100);
            info.CpuUsage = (int)cpuCounter.NextValue();
            
            // Memory usage
            var totalMemory = GC.GetTotalMemory(false);
            var workingSet = Environment.WorkingSet;
            info.MemoryUsageMB = workingSet / 1024 / 1024;
            
            // Disk usage
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
            foreach (var drive in drives)
            {
                var usedSpace = drive.TotalSize - drive.AvailableFreeSpace;
                var usagePercent = (int)((double)usedSpace / drive.TotalSize * 100);
                info.DiskUsage.Add(new DiskInfo 
                { 
                    Drive = drive.Name, 
                    UsagePercent = usagePercent,
                    FreeSpaceGB = drive.AvailableFreeSpace / 1024 / 1024 / 1024,
                    TotalSpaceGB = drive.TotalSize / 1024 / 1024 / 1024
                });
            }
            
            return info;
        }
        catch
        {
            return new SystemInfo();
        }
    }
    
    public async Task<string> SetShutdownTimerAsync(int minutes)
    {
        try
        {
            if (minutes <= 0)
            {
                // Cancel shutdown
                var cancelProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "shutdown",
                        Arguments = "/a",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                cancelProcess.Start();
                await cancelProcess.WaitForExitAsync();
                return "✅ Таймер выключения отменен";
            }
            else
            {
                // Set shutdown timer
                var seconds = minutes * 60;
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "shutdown",
                        Arguments = $"/s /t {seconds}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                await process.WaitForExitAsync();
                return $"✅ ПК выключится через {minutes} минут";
            }
        }
        catch (Exception ex)
        {
            return $"❌ Ошибка: {ex.Message}";
        }
    }
}

public class SystemInfo
{
    public int CpuUsage { get; set; }
    public long MemoryUsageMB { get; set; }
    public List<DiskInfo> DiskUsage { get; set; } = new();
}

public class DiskInfo
{
    public string Drive { get; set; } = "";
    public int UsagePercent { get; set; }
    public long FreeSpaceGB { get; set; }
    public long TotalSpaceGB { get; set; }
}