using Avalonia.Controls;
using Avalonia.Interactivity;
using DiskCleanerGUI.Avalonia.ViewModels;
using DiskCleanerGUI.Avalonia.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Controls.Primitives;

namespace DiskCleanerGUI.Avalonia.Views;

public partial class ThemeManagerView : UserControl
{
    private readonly string[] _predefinedColors = 
    {
        "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#00FFFF",
        "#FFA500", "#800080", "#008000", "#000080", "#800000", "#808000",
        "#2196F3", "#4CAF50", "#FF9800", "#F44336", "#9C27B0", "#607D8B",
        "#E91E63", "#00E676", "#FF5722", "#795548", "#9E9E9E", "#000000",
        "#FFFFFF", "#212121", "#424242", "#757575", "#BDBDBD", "#E0E0E0"
    };
    
    public ThemeManagerView()
    {
        InitializeComponent();
        LocalizationService.Instance.LanguageChanged += UpdateLocalization;
        UpdateLocalization();
    }
    
    private void UpdateLocalization()
    {
        var loc = LocalizationService.Instance;
        
        // Update all text elements with localized strings
        if (this.FindControl<TextBlock>("ThemeManagerTitle") is TextBlock title)
            title.Text = loc.GetString("ThemeManager");
        if (this.FindControl<TextBlock>("AvailableThemesTitle") is TextBlock availableTitle)
            availableTitle.Text = loc.GetString("AvailableThemes");
        if (this.FindControl<Button>("ApplyThemeBtn") is Button applyBtn)
            applyBtn.Content = loc.GetString("ApplyTheme");
        if (this.FindControl<Button>("ApplyLastThemeBtn") is Button applyLastBtn)
            applyLastBtn.Content = loc.GetString("ApplyLastTheme");
        if (this.FindControl<Button>("DeleteThemeBtn") is Button deleteBtn)
            deleteBtn.Content = loc.GetString("DeleteTheme");
        if (this.FindControl<Button>("RefreshThemesBtn") is Button refreshBtn)
            refreshBtn.Content = loc.GetString("RefreshThemes");
        if (this.FindControl<Button>("ExportThemeBtn") is Button exportBtn)
            exportBtn.Content = loc.GetString("ExportTheme");
        if (this.FindControl<Button>("ImportThemeBtn") is Button importBtn)
            importBtn.Content = loc.GetString("ImportTheme");
        if (this.FindControl<TextBlock>("CreateThemeTitle") is TextBlock createTitle)
            createTitle.Text = loc.GetString("CreateNewTheme");
        if (this.FindControl<Button>("CreateThemeBtn") is Button createBtn)
            createBtn.Content = loc.GetString("CreateTheme");
            
        // Update input fields
        if (this.FindControl<TextBox>("ThemeNameBox") is TextBox nameBox)
            nameBox.Watermark = loc.GetString("ThemeName");
        if (this.FindControl<TextBox>("AuthorBox") is TextBox authorBox)
            authorBox.Watermark = loc.GetString("Author");
        if (this.FindControl<TextBox>("DescriptionBox") is TextBox descBox)
            descBox.Watermark = loc.GetString("ThemeDescription");
            
        // Update color labels
        if (this.FindControl<TextBlock>("PrimaryColorLabel") is TextBlock primaryLabel)
            primaryLabel.Text = loc.GetString("PrimaryColor");
        if (this.FindControl<TextBlock>("SecondaryColorLabel") is TextBlock secondaryLabel)
            secondaryLabel.Text = loc.GetString("SecondaryColor");
        if (this.FindControl<TextBlock>("AccentColorLabel") is TextBlock accentLabel)
            accentLabel.Text = loc.GetString("AccentColor");
        if (this.FindControl<TextBlock>("BackgroundColorLabel") is TextBlock bgLabel)
            bgLabel.Text = loc.GetString("BackgroundColor");
        if (this.FindControl<TextBlock>("Gradient1Label") is TextBlock grad1Label)
            grad1Label.Text = loc.GetString("Gradient1");
        if (this.FindControl<TextBlock>("Gradient2Label") is TextBlock grad2Label)
            grad2Label.Text = loc.GetString("Gradient2");
            
        // Update background image section
        if (this.FindControl<TextBlock>("BackgroundImageTitle") is TextBlock bgImgTitle)
            bgImgTitle.Text = loc.GetString("BackgroundImage");
        if (this.FindControl<CheckBox>("UseBackgroundImageCheck") is CheckBox useImgCheck)
            useImgCheck.Content = loc.GetString("UseBackgroundImage");
        if (this.FindControl<Button>("SelectImageBtn") is Button selectImgBtn)
            selectImgBtn.Content = loc.GetString("SelectImage");
    }
    
    private async void OnPrimaryColorClick(object? sender, RoutedEventArgs e) => await ShowColorPicker("NewPrimaryColor");
    private async void OnSecondaryColorClick(object? sender, RoutedEventArgs e) => await ShowColorPicker("NewSecondaryColor");
    private async void OnAccentColorClick(object? sender, RoutedEventArgs e) => await ShowColorPicker("NewAccentColor");
    private async void OnBackgroundColorClick(object? sender, RoutedEventArgs e) => await ShowColorPicker("NewBackgroundColor");
    private async void OnGradientStartColorClick(object? sender, RoutedEventArgs e) => await ShowColorPicker("NewGradientStart");
    private async void OnGradientEndColorClick(object? sender, RoutedEventArgs e) => await ShowColorPicker("NewGradientEnd");
    
    private async System.Threading.Tasks.Task ShowColorPicker(string propertyName)
    {
        if (DataContext is not ThemeManagerViewModel vm) return;
        
        var dialog = new Window
        {
            Title = "Выбор цвета",
            Width = 400,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };
        
        var grid = new Grid { Margin = new Thickness(10) };
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        
        var colorPanel = new WrapPanel { Orientation = Orientation.Horizontal };
        
        Button? selectedButton = null;
        
        foreach (var color in _predefinedColors)
        {
            var btn = new Button
            {
                Width = 40,
                Height = 40,
                Margin = new Thickness(2),
                Background = new SolidColorBrush(Color.Parse(color)),
                Tag = color
            };
            
            btn.Click += (s, e) =>
            {
                selectedButton = btn;
                dialog.Close(btn.Tag?.ToString());
            };
            
            colorPanel.Children.Add(btn);
        }
        
        var scrollViewer = new ScrollViewer { Content = colorPanel };
        Grid.SetRow(scrollViewer, 0);
        
        var cancelBtn = new Button
        {
            Content = "Отмена",
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(20, 8)
        };
        cancelBtn.Click += (s, e) => dialog.Close();
        Grid.SetRow(cancelBtn, 1);
        
        grid.Children.Add(scrollViewer);
        grid.Children.Add(cancelBtn);
        dialog.Content = grid;
        
        var owner = TopLevel.GetTopLevel(this) as Window;
        var result = owner != null ? await dialog.ShowDialog<string?>(owner) : null;
        
        if (!string.IsNullOrEmpty(result))
        {
            var property = typeof(ThemeManagerViewModel).GetProperty(propertyName);
            property?.SetValue(vm, result);
        }
    }
}