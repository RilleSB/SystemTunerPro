namespace DiskCleanerGUI.Avalonia.Models;

/// <summary>
/// Модель темы оформления - содержит все настройки цветов и фона
/// </summary>
public class Theme
{
    // Основная информация о теме
    public required string Name { get; set; }        // Название темы
    public required string Author { get; set; }     // Автор темы
    public required string Version { get; set; }    // Версия темы
    public string? Description { get; set; }        // Описание темы
    
    // Основные цвета интерфейса
    public required string PrimaryColor { get; set; }   // Основной цвет
    public required string SecondaryColor { get; set; } // Вторичный цвет
    public required string AccentColor { get; set; }    // Акцентный цвет
    public required string BackgroundColor { get; set; }// Цвет фона
    public required string TextColor { get; set; }      // Цвет текста
    
    // Настройки градиента
    public string? GradientStart { get; set; }  // Начальный цвет градиента
    public string? GradientEnd { get; set; }    // Конечный цвет градиента
    
    // Настройки фонового изображения
    public string? BackgroundImage { get; set; }     // Base64 изображения
    public string? BackgroundImagePath { get; set; } // Путь к файлу изображения
    public bool UseBackgroundImage { get; set; } = false; // Использовать ли фоновое изображение
    
    /// <summary>
    /// Генерирует имя файла для сохранения темы
    /// </summary>
    public string FileName
    {
        get
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(Name
                .Trim()
                .Select(character => invalidChars.Contains(character) ? '_' : character)
                .ToArray())
                .Trim('.', ' ');
            return $"{(string.IsNullOrWhiteSpace(safeName) ? "Theme" : safeName)}.json";
        }
    }
}
