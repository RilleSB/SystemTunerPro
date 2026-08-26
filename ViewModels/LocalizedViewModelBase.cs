using CommunityToolkit.Mvvm.ComponentModel;
using DiskCleanerGUI.Avalonia.Services;

namespace DiskCleanerGUI.Avalonia.ViewModels;

/// <summary>
/// Базовый класс для ViewModel с поддержкой локализации - автоматически обновляет интерфейс при смене языка
/// </summary>
public partial class LocalizedViewModelBase : ObservableObject
{
    /// <summary>
    /// Сервис локализации для получения переведенных строк
    /// </summary>
    protected LocalizationService Localization => LocalizationService.Instance;

    public LocalizedViewModelBase()
    {
        // Подписываемся на событие смены языка
        LocalizationService.Instance.LanguageChanged += OnLanguageChangedHandler;
    }
    
    /// <summary>
    /// Обработчик события смены языка
    /// </summary>
    private void OnLanguageChangedHandler()
    {
        OnLanguageChanged();
    }

    /// <summary>
    /// Получает локализованную строку по ключу
    /// </summary>
    /// <param name="key">Ключ для поиска в файлах локализации</param>
    /// <returns>Переведенная строка</returns>
    protected string GetString(string key) => Localization.GetString(key);
    
    /// <summary>
    /// Вызывается при смене языка - обновляет все свойства для перерисовки интерфейса
    /// </summary>
    protected virtual void OnLanguageChanged()
    {
        OnPropertyChanged(string.Empty); // Обновляем все свойства
    }
    
    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
    }
}