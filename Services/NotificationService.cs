using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DiskCleanerGUI.Avalonia.Services;

/// <summary>
/// Сервис для отображения системных уведомлений Windows
/// </summary>
public static class NotificationService
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private const uint WM_CLOSE = 0x0010;

    /// <summary>
    /// Показывает уведомление об успешной очистке
    /// </summary>
    public static void ShowCleaningComplete(long filesDeleted, long bytesFreed)
    {
        if (!IsNotificationsEnabled()) return;

        var message = $"Удалено файлов: {filesDeleted}\nОсвобождено: {FormatBytes(bytesFreed)}";
        ShowNotification("✅ Очистка завершена", message);
    }

    /// <summary>
    /// Показывает уведомление об ошибке
    /// </summary>
    public static void ShowError(string message)
    {
        if (!IsNotificationsEnabled()) return;
        ShowNotification("❌ Ошибка", message);
    }

    /// <summary>
    /// Показывает информационное уведомление
    /// </summary>
    public static void ShowInfo(string title, string message)
    {
        if (!IsNotificationsEnabled()) return;
        ShowNotification(title, message);
    }

    private static bool IsNotificationsEnabled()
    {
        try
        {
            var settingsService = new SettingsService();
            var settings = settingsService.LoadSettings();
            return settings.ShowNotifications;
        }
        catch
        {
            return true; // По умолчанию включено
        }
    }

    private static void ShowNotification(string title, string message)
    {
        Task.Run(() =>
        {
            try
            {
                // Используем PowerShell для показа Toast уведомления
                var script = $@"
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

$template = @""
<toast>
    <visual>
        <binding template='ToastGeneric'>
            <text>{title}</text>
            <text>{message}</text>
        </binding>
    </visual>
</toast>
""@

$xml = New-Object Windows.Data.Xml.Dom.XmlDocument
$xml.LoadXml($template)
$toast = [Windows.UI.Notifications.ToastNotification]::new($xml)
[Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('SystemTuner Pro').Show($toast)
";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = System.Diagnostics.Process.Start(psi);
                process?.WaitForExit(3000);
            }
            catch
            {
                // Если не удалось показать Toast, пропускаем
            }
        });
    }

    private static string FormatBytes(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024:F1} KB",
            < 1024 * 1024 * 1024 => $"{bytes / (1024 * 1024):F1} MB",
            _ => $"{bytes / (1024 * 1024 * 1024):F1} GB"
        };
    }
}
