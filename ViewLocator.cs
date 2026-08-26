using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DiskCleanerGUI.Avalonia.ViewModels;

namespace DiskCleanerGUI.Avalonia;

/// <summary>
/// Локатор представлений - автоматически связывает ViewModel с соответствующим View
/// </summary>
public class ViewLocator : IDataTemplate
{
    /// <summary>
    /// Создает экземпляр View на основе переданной ViewModel
    /// </summary>
    /// <param name="param">ViewModel для которой нужно найти View</param>
    /// <returns>Соответствующий View или сообщение об ошибке</returns>
    public Control? Build(object? param)
    {
        if (param is null)
            return null;
        
        // Заменяем "ViewModel" на "View" в имени типа
        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            // Создаем экземпляр найденного View
            return (Control)Activator.CreateInstance(type)!;
        }
        
        // Если View не найден, показываем сообщение об ошибке
        return new TextBlock { Text = "Not Found: " + name };
    }

    /// <summary>
    /// Проверяет, подходит ли данный объект для обработки этим локатором
    /// </summary>
    /// <param name="data">Объект для проверки</param>
    /// <returns>true если объект является ViewModelBase</returns>
    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
