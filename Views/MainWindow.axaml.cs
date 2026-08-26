using Avalonia.Controls;
using DiskCleanerGUI.Avalonia.Services;
using System;

namespace DiskCleanerGUI.Avalonia.Views;

/// <summary>
/// Главное окно приложения - содержит TabControl с различными функциями
/// Автоматически обновляет локализацию при смене языка и управляет заголовками вкладок
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Подписываемся на изменения языка для автоматического обновления интерфейса
        LocalizationService.Instance.LanguageChanged += UpdateLocalization;
        UpdateLocalization();
        
    }
    
    /// <summary>
    /// Обновляет локализованные строки в интерфейсе при смене языка
    /// Изменяет заголовок окна, названия вкладок и их подсказки
    /// </summary>
    private void UpdateLocalization()
    {
        var loc = LocalizationService.Instance;
        Title = loc.GetString("AppTitle");
        
        // Обновляем заголовки и подсказки вкладок, если они существуют
        if (this.FindControl<TabControl>("MainTabControl") is TabControl tabControl)
        {
            if (tabControl.Items.Count >= 7)
            {
                // Вкладка "Очистка"
                var tab0 = (TabItem)tabControl.Items[0]!;
                tab0.Header = loc.GetString("Cleaning");
                ToolTip.SetTip(tab0, loc.GetString("CleaningTooltip"));
                
                // Вкладка "Просмотр файлов"
                var tab1 = (TabItem)tabControl.Items[1]!;
                tab1.Header = loc.GetString("FileViewer");
                ToolTip.SetTip(tab1, loc.GetString("FileViewerTooltip"));
                
                // Вкладка "Утилиты"
                var tab2 = (TabItem)tabControl.Items[2]!;
                tab2.Header = loc.GetString("Utilities");
                ToolTip.SetTip(tab2, loc.GetString("UtilitiesTooltip"));
                
                // Вкладка "Корзина"
                var tab3 = (TabItem)tabControl.Items[3]!;
                tab3.Header = loc.GetString("Trash");
                ToolTip.SetTip(tab3, loc.GetString("TrashTooltip"));
                
                // Вкладка "Твики"
                var tab4 = (TabItem)tabControl.Items[4]!;
                tab4.Header = loc.GetString("Tweaks");
                ToolTip.SetTip(tab4, loc.GetString("TweaksTooltip"));
                
                // Вкладка "Темы"
                var tab5 = (TabItem)tabControl.Items[5]!;
                tab5.Header = loc.GetString("Themes");
                ToolTip.SetTip(tab5, loc.GetString("ThemesTooltip"));
                
                // Вкладка "Настройки"
                var tab6 = (TabItem)tabControl.Items[6]!;
                tab6.Header = loc.GetString("Settings");
                ToolTip.SetTip(tab6, loc.GetString("SettingsTooltip"));
            }
        }
        
        // Обновляем заголовок приложения в интерфейсе, если элемент существует
        if (this.FindControl<TextBlock>("AppTitleText") is TextBlock titleText)
        {
            titleText.Text = loc.GetString("AppTitle");
        }
    }
}
