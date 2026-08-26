using System.IO;
using System.Text.Json;

namespace DiskCleanerGUI.Avalonia.Services;

/// <summary>
/// Универсальный сервис конфигурации - объединяет функциональность всех конфиг-сервисов
/// </summary>
public static class UnifiedConfigService
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "TrashClean", 
        "config.json"
    );
    
    static UnifiedConfigService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
    }
    
    // Настройки очистки
    public static void SaveCleaningSettings(bool tempUser, bool tempWindows, bool browsers, bool apps, bool recycleBin, bool windowsUpdate)
    {
        var settings = new
        {
            TempUser = tempUser,
            TempWindows = tempWindows,
            Browsers = browsers,
            Apps = apps,
            RecycleBin = recycleBin,
            WindowsUpdate = windowsUpdate
        };
        
        SetValue("CleaningSettings", settings);
    }
    
    public static (bool tempUser, bool tempWindows, bool browsers, bool apps, bool recycleBin, bool windowsUpdate) LoadCleaningSettings()
    {
        try
        {
            var settings = GetValue<JsonElement?>("CleaningSettings", null);
            if (settings.HasValue)
            {
                var element = settings.Value;
                return (
                    element.GetProperty("TempUser").GetBoolean(),
                    element.GetProperty("TempWindows").GetBoolean(),
                    element.GetProperty("Browsers").GetBoolean(),
                    element.GetProperty("Apps").GetBoolean(),
                    element.GetProperty("RecycleBin").GetBoolean(),
                    element.GetProperty("WindowsUpdate").GetBoolean()
                );
            }
        }
        catch { }
        
        return (true, true, true, false, false, false);
    }
    
    // Универсальные методы
    public static T GetValue<T>(string key, T defaultValue)
    {
        try
        {
            if (!File.Exists(ConfigPath)) return defaultValue;
            
            var json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            if (config != null && config.ContainsKey(key))
            {
                var value = config[key];
                if (value is JsonElement element)
                {
                    return element.Deserialize<T>() ?? defaultValue;
                }
                return (T)Convert.ChangeType(value, typeof(T));
            }
        }
        catch { }
        
        return defaultValue;
    }
    
    public static void SetValue<T>(string key, T value)
    {
        try
        {
            Dictionary<string, object> config;
            
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                config = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
            }
            else
            {
                config = new Dictionary<string, object>();
            }
            
            config[key] = value!;
            
            var newJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, newJson);
        }
        catch { }
    }
    
    // Простые булевы значения
    public static bool GetBool(string key, bool defaultValue = false) => GetValue(key, defaultValue);
    public static void SetBool(string key, bool value) => SetValue(key, value);
}
