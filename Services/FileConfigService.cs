namespace DiskCleanerGUI.Avalonia.Services;

/// <summary>
/// Обертка для совместимости - использует UnifiedConfigService
/// </summary>
public static class FileConfigService
{
    public static void SaveSettings(bool tempUser, bool tempWindows, bool browsers, bool apps, bool recycleBin, bool windowsUpdate)
    {
        UnifiedConfigService.SaveCleaningSettings(tempUser, tempWindows, browsers, apps, recycleBin, windowsUpdate);
    }
    
    public static (bool tempUser, bool tempWindows, bool browsers, bool apps, bool recycleBin, bool windowsUpdate) LoadSettings()
    {
        return UnifiedConfigService.LoadCleaningSettings();
    }
}