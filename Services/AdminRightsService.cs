using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.InteropServices;

namespace DiskCleanerGUI.Avalonia.Services;

public static class AdminRightsService
{
    /// <summary>
    /// Проверяет, запущено ли приложение с правами администратора
    /// </summary>
    public static bool IsRunningAsAdmin()
    {
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Перезапускает приложение с правами администратора
    /// </summary>
    public static bool RestartAsAdmin()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName,
                Verb = "runas" // Запрос прав администратора
            };

            Process.Start(processInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Проверяет доступ к критическим системным папкам
    /// </summary>
    public static bool CanAccessSystemFolders()
    {
        try
        {
            // Проверяем доступ к Windows\Temp
            var winTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
            if (Directory.Exists(winTemp))
            {
                Directory.GetFiles(winTemp, "*", SearchOption.TopDirectoryOnly);
            }

            // Проверяем доступ к SoftwareDistribution
            var swDist = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download");
            if (Directory.Exists(swDist))
            {
                Directory.GetFiles(swDist, "*", SearchOption.TopDirectoryOnly);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}