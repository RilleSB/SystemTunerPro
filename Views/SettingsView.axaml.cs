using Avalonia.Controls;
using DiskCleanerGUI.Avalonia.Services;

namespace DiskCleanerGUI.Avalonia.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        LocalizationService.Instance.LanguageChanged += UpdateLocalization;
        UpdateLocalization();
        
        // Handle language ComboBox selection
        var langCombo = this.FindControl<ComboBox>("LanguageComboBox");
        if (langCombo != null)
        {
            langCombo.SelectionChanged += (s, e) =>
            {
                if (langCombo.SelectedItem is ComboBoxItem item && item.Tag is string langCode)
                {
                    LocalizationService.Instance.CurrentLanguage = langCode;
                }
            };
        }
    }
    
    private void UpdateLocalization()
    {
        var loc = LocalizationService.Instance;
        
        if (this.FindControl<TextBlock>("GeneralSettingsTitle") is TextBlock title)
            title.Text = loc.GetString("GeneralSettings");
        if (this.FindControl<ToggleSwitch>("DarkThemeSwitch") is ToggleSwitch darkSwitch)
            darkSwitch.Content = loc.GetString("DarkTheme");
        if (this.FindControl<TextBlock>("UiScaleLabel") is TextBlock scaleLabel)
            scaleLabel.Text = loc.GetString("UiScale");
        if (this.FindControl<TextBlock>("LanguageLabel") is TextBlock langLabel)
            langLabel.Text = loc.GetString("Language");
        if (this.FindControl<Button>("SaveSettingsBtn") is Button saveBtn)
            saveBtn.Content = loc.GetString("SaveSettingsBtn");
        if (this.FindControl<Button>("ResetSettingsBtn") is Button resetBtn)
            resetBtn.Content = loc.GetString("ResetSettings");
            
        // Update additional text elements
        if (this.FindControl<TextBlock>("DragToResizeText") is TextBlock dragText)
            dragText.Text = loc.GetString("DragToResizeHint");
        if (this.FindControl<TextBlock>("ThemeHintText") is TextBlock themeHint)
            themeHint.Text = loc.GetString("ThemeHint");
        if (this.FindControl<Slider>("UiScaleSlider") is Slider slider)
            ToolTip.SetTip(slider, loc.GetString("UiScaleTooltip"));
    }
}