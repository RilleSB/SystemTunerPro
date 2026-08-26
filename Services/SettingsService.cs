using DiskCleanerGUI.Avalonia.Models;
using System.IO;
using System.Text.Json;

namespace DiskCleanerGUI.Avalonia.Services;

/// <summary>
/// Сервис для управления настройками приложения - сохранение и загрузка пользовательских предпочтений
/// Использует JSON для хранения настроек в папке AppData пользователя
/// </summary>
public class SettingsService
{
    private readonly string _settingsFile;  // Путь к файлу настроек
    private AppSettings? _settings;         // Кэшированные настройки
    
    public SettingsService()
    {
        // Создаем папку для настроек приложения в AppData
        var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TrashClean");
        Directory.CreateDirectory(appDataFolder);
        _settingsFile = Path.Combine(appDataFolder, "settings.json");
    }
    
    /// <summary>
    /// Загружает настройки из файла или создает настройки по умолчанию
    /// </summary>
    /// <returns>Объект с настройками приложения</returns>
    public AppSettings LoadSettings()
    {
        if (_settings != null) return _settings;
        
        try
        {
            if (File.Exists(_settingsFile))
            {
                // Загружаем существующие настройки из JSON файла
                var json = File.ReadAllText(_settingsFile);
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            else
            {
                // Создаем настройки по умолчанию и сохраняем их
                _settings = new AppSettings();
                SaveSettings(_settings);
            }
        }
        catch
        {
            // При ошибке используем настройки по умолчанию
            _settings = new AppSettings();
        }
        
        return _settings;
    }
    
    /// <summary>
    /// Сохраняет настройки в JSON файл
    /// </summary>
    /// <param name="settings">Настройки для сохранения</param>
    public void SaveSettings(AppSettings settings)
    {
        try
        {
            _settings = settings;
            // Сериализуем настройки в JSON с отступами для читаемости
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFile, json);
        }
        catch { /* Игнорируем ошибки сохранения */ }
    }
    
    /// <summary>
    /// Обновляет отдельную настройку по имени свойства
    /// </summary>
    /// <typeparam name="T">Тип значения настройки</typeparam>
    /// <param name="propertyName">Имя свойства в классе AppSettings</param>
    /// <param name="value">Новое значение</param>
    public void UpdateSetting<T>(string propertyName, T value)
    {
        var settings = LoadSettings();
        var property = typeof(AppSettings).GetProperty(propertyName);
        if (property != null && property.CanWrite)
        {
            property.SetValue(settings, value);
            SaveSettings(settings);
        }
    }
}