using Avalonia;
using Avalonia.Styling;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleanerGUI.Avalonia.Services;
using DiskCleanerGUI.Avalonia.Models;
using System.Threading.Tasks;

namespace DiskCleanerGUI.Avalonia.ViewModels;

public partial class SettingsViewModel : LocalizedViewModelBase
{
    private readonly SettingsService _settingsService = new();
    private AppSettings _settings;
    
    [ObservableProperty] private bool darkTheme;
    [ObservableProperty] private double uiScale = 1.0;
    [ObservableProperty] private string status = "";
    [ObservableProperty] private string selectedLanguage = "ru";
    [ObservableProperty] private int languageIndex = 0;
    [ObservableProperty] private bool safeDelete = true;
    [ObservableProperty] private bool showNotifications = true;
    
    public List<string> AvailableLanguages => LocalizationService.Instance.AvailableLanguages;
    
    // Localized properties
    public string DarkThemeText => GetString("DarkTheme");
    public string LanguageText => GetString("Language");
    public string UiScaleText => GetString("UiScale");
    public string SaveSettingsBtnText => GetString("SaveSettingsBtn");
    public string ResetSettingsText => GetString("ResetSettings");
    public string SafeDeleteText => GetString("SafeDelete");
    public string ShowNotificationsText => GetString("ShowNotifications");
    
    // Tooltips
    public string DarkThemeTooltipText => GetString("DarkThemeTooltip");
    public string LanguageTooltipText => GetString("LanguageTooltip");
    public string UiScaleTooltipText => GetString("UiScaleTooltip");
    public string SaveSettingsBtnTooltipText => GetString("SaveSettingsBtnTooltip");
    public string ResetSettingsTooltipText => GetString("ResetSettingsTooltip");
    public string SafeDeleteTooltipText => GetString("SafeDeleteTooltip");
    public string ShowNotificationsTooltipText => GetString("ShowNotificationsTooltip");
    
    public SettingsViewModel()
    {
        _settings = _settingsService.LoadSettings();
        LoadSettingsFromModel();
        
        // Применяем настройки UI при загрузке
        ApplyUISettings();
    }
    
    private void ApplyUISettings()
    {
        // Применяем тему
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = DarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
            
            // Применяем масштабирование
            var baseFontSize = 12.0;
            var scaledFontSize = baseFontSize * UiScale;
            
            Application.Current.Resources["DefaultFontSize"] = scaledFontSize;
            Application.Current.Resources["ButtonFontSize"] = scaledFontSize;
            Application.Current.Resources["HeaderFontSize"] = scaledFontSize * 1.5;
            Application.Current.Resources["LargeFontSize"] = scaledFontSize * 2.0;
        }
    }
    
    private void LoadSettingsFromModel()
    {
        DarkTheme = _settings.DarkTheme;
        UiScale = _settings.UIScale;
        SafeDelete = _settings.SafeDelete;
        ShowNotifications = _settings.ShowNotifications;
        
        // Используем текущий язык из LocalizationService, а не перезаписываем его
        SelectedLanguage = LocalizationService.Instance.CurrentLanguage;
        LanguageIndex = SelectedLanguage == "en" ? 1 : 0;
        
        // Обновляем настройки если язык изменился
        if (_settings.Language != SelectedLanguage)
        {
            _settings.Language = SelectedLanguage;
            AutoSave();
        }
        
        // Уведомляем UI об изменениях
        OnPropertyChanged(nameof(DarkTheme));
        OnPropertyChanged(nameof(UiScale));
        OnPropertyChanged(nameof(SelectedLanguage));
        OnPropertyChanged(nameof(LanguageIndex));
    }
    
    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        _settings.DarkTheme = DarkTheme;
        _settings.UIScale = UiScale;
        _settings.Language = SelectedLanguage;
        
        _settingsService.SaveSettings(_settings);
        Status = LocalizationService.Instance.GetString("SettingsSaved");
        
        await Task.Delay(2000);
        Status = "";
    }
    
    [RelayCommand]
    private async Task ResetSettingsAsync()
    {
        _settings = new AppSettings();
        LoadSettingsFromModel();
        _settingsService.SaveSettings(_settings);
        Status = "Settings reset to defaults!"; // TODO: Add to localization
        
        await Task.Delay(2000);
        Status = "";
    }
    
    // Auto-save when properties change
    partial void OnDarkThemeChanged(bool value)
    {
        Application.Current!.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
        AutoSave();
    }
    
    partial void OnUiScaleChanged(double value)
    {
        // Apply font size scaling to application
        if (Application.Current != null)
        {
            var baseFontSize = 12.0;
            var scaledFontSize = baseFontSize * value;
            
            // Update application resources
            Application.Current.Resources["DefaultFontSize"] = scaledFontSize;
            Application.Current.Resources["ButtonFontSize"] = scaledFontSize;
            Application.Current.Resources["HeaderFontSize"] = scaledFontSize * 1.5;
            Application.Current.Resources["LargeFontSize"] = scaledFontSize * 2.0;
        }
        AutoSave();
    }
    


    
    partial void OnSelectedLanguageChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            LocalizationService.Instance.CurrentLanguage = value;
            AutoSave();
        }
    }
    
    partial void OnLanguageIndexChanged(int value)
    {
        SelectedLanguage = value == 1 ? "en" : "ru";
    }
    
    partial void OnSafeDeleteChanged(bool value) => AutoSave();
    partial void OnShowNotificationsChanged(bool value) => AutoSave();
    
    private void AutoSave()
    {
        _settings.DarkTheme = DarkTheme;
        _settings.UIScale = UiScale;
        _settings.Language = SelectedLanguage;
        _settings.SafeDelete = SafeDelete;
        _settings.ShowNotifications = ShowNotifications;
        _settingsService.SaveSettings(_settings);
    }
    
    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(DarkThemeText));
        OnPropertyChanged(nameof(LanguageText));
        OnPropertyChanged(nameof(UiScaleText));
        OnPropertyChanged(nameof(SaveSettingsBtnText));
        OnPropertyChanged(nameof(ResetSettingsText));
        OnPropertyChanged(nameof(SafeDeleteText));
        OnPropertyChanged(nameof(ShowNotificationsText));
        OnPropertyChanged(nameof(DarkThemeTooltipText));
        OnPropertyChanged(nameof(LanguageTooltipText));
        OnPropertyChanged(nameof(UiScaleTooltipText));
        OnPropertyChanged(nameof(SaveSettingsBtnTooltipText));
        OnPropertyChanged(nameof(ResetSettingsTooltipText));
        OnPropertyChanged(nameof(SafeDeleteTooltipText));
        OnPropertyChanged(nameof(ShowNotificationsTooltipText));
    }
    
    public AppSettings GetSettings() => _settings;
}