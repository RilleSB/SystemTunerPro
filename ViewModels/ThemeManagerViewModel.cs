using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleanerGUI.Avalonia.Models;
using DiskCleanerGUI.Avalonia.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Linq;

namespace DiskCleanerGUI.Avalonia.ViewModels;

public partial class ThemeManagerViewModel : LocalizedViewModelBase
{
    private readonly ThemeService _themeService = new();
    private readonly SettingsService _settingsService = new();
    
    public ObservableCollection<Theme> Themes { get; } = new();
    
    [ObservableProperty] private Theme? selectedTheme;
    [ObservableProperty] private string status = "";
    
    // Theme creation properties
    [ObservableProperty] private string newThemeName = "";
    [ObservableProperty] private string newThemeAuthor = "";
    [ObservableProperty] private string newThemeDescription = "";
    [ObservableProperty] private string newPrimaryColor = "#2196F3";
    [ObservableProperty] private string newSecondaryColor = "#1976D2";
    [ObservableProperty] private string newAccentColor = "#4CAF50";
    [ObservableProperty] private string newBackgroundColor = "#121212";
    [ObservableProperty] private string newTextColor = "#FFFFFF";
    [ObservableProperty] private string newGradientStart = "#1e3c72";
    [ObservableProperty] private string newGradientEnd = "#2a5298";
    [ObservableProperty] private string? newBackgroundImagePath;
    [ObservableProperty] private bool useBackgroundImage = false;
    
    public ThemeManagerViewModel()
    {
        LoadThemes();
        
        // Auto-load and apply last used theme
        var settings = _settingsService.LoadSettings();
        if (!string.IsNullOrWhiteSpace(settings.LastUsedTheme))
        {
            var lastTheme = Themes.FirstOrDefault(t => t.Name == settings.LastUsedTheme);
            if (lastTheme != null)
            {
                SelectedTheme = lastTheme;
                ApplyThemeInternal(lastTheme);
            }
        }
        
        Status = string.Format(GetString("ThemesLoaded"), Themes.Count);
    }
    
    protected override void OnLanguageChanged()
    {
        Status = string.Format(GetString("ThemesLoaded"), Themes.Count);
    }
    
    [RelayCommand]
    private void LoadThemes()
    {
        try
        {
            Themes.Clear();
            foreach (var theme in _themeService.GetAllThemes())
            {
                Themes.Add(theme);
            }
            Status = string.Format(GetString("ThemesLoaded"), Themes.Count);
        }
        catch (Exception ex)
        {
            Status = $"Ошибка загрузки тем: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private void ApplyTheme()
    {
        if (SelectedTheme == null) return;
        
        Console.WriteLine($"Applying theme: {SelectedTheme.Name}");
        
        // Apply theme to background
        ApplyThemeInternal(SelectedTheme);
        
        // Save theme to settings
        var settings = _settingsService.LoadSettings();
        settings.LastUsedTheme = SelectedTheme.Name;
        _settingsService.SaveSettings(settings);
        
        Console.WriteLine($"Saved LastUsedTheme: {SelectedTheme.Name}");
        
        Status = $"Применена тема: {SelectedTheme.Name}";
    }
    
    [RelayCommand]
    private void CreateTheme()
    {
        if (string.IsNullOrWhiteSpace(NewThemeName))
        {
            Status = "Укажите название темы";
            return;
        }

        try
        {
            var theme = new Theme
            {
                Name = NewThemeName.Trim(),
                Author = string.IsNullOrWhiteSpace(NewThemeAuthor) ? "Пользователь" : NewThemeAuthor.Trim(),
                Version = "1.0",
                Description = NewThemeDescription,
                PrimaryColor = NewPrimaryColor,
                SecondaryColor = NewSecondaryColor,
                AccentColor = NewAccentColor,
                BackgroundColor = NewBackgroundColor,
                TextColor = NewTextColor,
                GradientStart = NewGradientStart,
                GradientEnd = NewGradientEnd,
                BackgroundImagePath = NewBackgroundImagePath,
                UseBackgroundImage = UseBackgroundImage
            };

            _themeService.SaveTheme(theme);
            LoadThemes();
            SelectedTheme = Themes.FirstOrDefault(item => item.Name == theme.Name);

            NewThemeName = "";
            NewThemeAuthor = "";
            NewThemeDescription = "";
            NewBackgroundImagePath = null;
            UseBackgroundImage = false;
            Status = $"Создана тема: {theme.Name}";
        }
        catch (Exception ex)
        {
            Status = $"Ошибка создания темы: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private async Task SelectBackgroundImageAsync()
    {
        var topLevel = TopLevel.GetTopLevel(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null);
        if (topLevel?.StorageProvider is { } provider)
        {
            var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выбрать фоновое изображение",
                AllowMultiple = false,
                FileTypeFilter = new[] 
                {
                    new FilePickerFileType("Изображения") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp" } }
                }
            });
            
            if (files.Count > 0)
            {
                var path = files[0].TryGetLocalPath();
                if (!string.IsNullOrEmpty(path))
                {
                    NewBackgroundImagePath = path;
                    UseBackgroundImage = true;
                    Status = $"Выбрано изображение: {System.IO.Path.GetFileName(path)}";
                }
            }
        }
    }
    
    [RelayCommand]
    private void DeleteTheme()
    {
        if (SelectedTheme == null) return;
        
        try
        {
            var themeName = SelectedTheme.Name;
            
            // Check if this is the currently used theme
            var settings = _settingsService.LoadSettings();
            if (settings.LastUsedTheme == themeName)
            {
                // Clear the last used theme since we're deleting it
                settings.LastUsedTheme = null;
                _settingsService.SaveSettings(settings);
            }
            
            _themeService.DeleteTheme(SelectedTheme);
            
            // Clear selection first to avoid issues
            SelectedTheme = null;
            
            // Reload themes
            LoadThemes();
            
            Status = $"Удалена тема: {themeName}";
        }
        catch (Exception ex)
        {
            Status = $"Ошибка удаления темы: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private void ApplyLastTheme()
    {
        var settings = _settingsService.LoadSettings();
        if (!string.IsNullOrWhiteSpace(settings.LastUsedTheme))
        {
            var lastTheme = Themes.FirstOrDefault(t => t.Name == settings.LastUsedTheme);
            if (lastTheme != null)
            {
                SelectedTheme = lastTheme;
                ApplyThemeInternal(lastTheme);
                Status = $"Применена последняя тема: {lastTheme.Name}";
            }
            else
            {
                Status = $"Последняя тема '{settings.LastUsedTheme}' не найдена";
            }
        }
        else
        {
            Status = "Последняя тема не сохранена";
        }
    }
    
    [RelayCommand]
    private async Task ExportThemeAsync()
    {
        if (SelectedTheme == null) return;
        
        var topLevel = TopLevel.GetTopLevel(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null);
        if (topLevel?.StorageProvider is { } provider)
        {
            var file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить тему",
                SuggestedFileName = $"{SelectedTheme.Name}.json",
                FileTypeChoices = new[] 
                {
                    new FilePickerFileType("JSON файлы") { Patterns = new[] { "*.json" } }
                }
            });
            
            if (file != null)
            {
                var path = file.TryGetLocalPath();
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        _themeService.ExportTheme(SelectedTheme, path);
                        Status = $"Тема экспортирована: {path}";
                    }
                    catch (Exception ex)
                    {
                        Status = $"Ошибка экспорта темы: {ex.Message}";
                    }
                }
            }
        }
    }
    
    [RelayCommand]
    private async Task ImportThemeAsync()
    {
        var topLevel = TopLevel.GetTopLevel(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null);
        if (topLevel?.StorageProvider is { } provider)
        {
            var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Загрузить тему",
                AllowMultiple = false,
                FileTypeFilter = new[] 
                {
                    new FilePickerFileType("JSON файлы") { Patterns = new[] { "*.json" } }
                }
            });
            
            if (files.Count > 0)
            {
                var path = files[0].TryGetLocalPath();
                if (!string.IsNullOrEmpty(path))
                {
                    var theme = _themeService.ImportTheme(path);
                    if (theme != null)
                    {
                        LoadThemes();
                        Status = $"Тема импортирована: {theme.Name}";
                    }
                    else
                    {
                        Status = "Ошибка импорта темы";
                    }
                }
            }
        }
    }
    
    private static IBrush CreateBrushFromTheme(Theme theme)
    {
        try
        {
            // Try background image first
            if (theme.UseBackgroundImage && !string.IsNullOrWhiteSpace(theme.BackgroundImagePath) && System.IO.File.Exists(theme.BackgroundImagePath))
            {
                var bitmap = new Bitmap(theme.BackgroundImagePath);
                return new ImageBrush(bitmap) 
                { 
                    Stretch = Stretch.UniformToFill, 
                    AlignmentX = AlignmentX.Center, 
                    AlignmentY = AlignmentY.Center 
                };
            }
            // Then try gradient
            else if (!string.IsNullOrWhiteSpace(theme.GradientStart) && !string.IsNullOrWhiteSpace(theme.GradientEnd))
            {
                return new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.Parse(theme.GradientStart), 0),
                        new GradientStop(Color.Parse(theme.GradientEnd), 1)
                    }
                };
            }
            // Finally solid color
            else
            {
                return new SolidColorBrush(Color.Parse(theme.BackgroundColor));
            }
        }
        catch
        {
            return new SolidColorBrush(Color.Parse("#121212"));
        }
    }
    
    private void LoadLastUsedTheme()
    {
        var settings = _settingsService.LoadSettings();
        if (!string.IsNullOrWhiteSpace(settings.LastUsedTheme))
        {
            var lastTheme = Themes.FirstOrDefault(t => t.Name == settings.LastUsedTheme);
            if (lastTheme != null)
            {
                SelectedTheme = lastTheme;
                // Auto-apply the last used theme
                ApplyThemeInternal(lastTheme);
            }
        }
    }
    
    private void ApplyThemeInternal(Theme theme)
    {
        var backgroundBrush = CreateBrushFromTheme(theme);
        if (MainWindowViewModel.SharedBackground != null)
        {
            MainWindowViewModel.SharedBackground.BackgroundBrush = backgroundBrush;
        }
    }
}
