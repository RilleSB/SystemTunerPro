using Avalonia.Controls;
using DiskCleanerGUI.Avalonia.Services;

namespace DiskCleanerGUI.Avalonia.Views;

public partial class CleaningView : UserControl
{
    public CleaningView()
    {
        InitializeComponent();
        LocalizationService.Instance.LanguageChanged += UpdateLocalization;
        UpdateLocalization();
    }
    
    private void UpdateLocalization()
    {
        var loc = LocalizationService.Instance;
        
        // Update checkboxes
        if (TempFilesCheckBox != null)
            TempFilesCheckBox.Content = loc.GetString("TempFiles");
        if (SystemFilesCheckBox != null)
            SystemFilesCheckBox.Content = loc.GetString("SystemFiles");
        if (BrowserCacheCheckBox != null)
            BrowserCacheCheckBox.Content = loc.GetString("BrowserCache");
        if (AppCacheCheckBox != null)
            AppCacheCheckBox.Content = loc.GetString("AppCache");
        if (WindowsUpdateCheckBox != null)
            WindowsUpdateCheckBox.Content = loc.GetString("WindowsUpdate");
            
        // Update buttons
        if (ScanButton != null)
            ScanButton.Content = loc.GetString("MultithreadScan");
        if (CleanButton != null)
            CleanButton.Content = loc.GetString("MultithreadClean");
        // Update search box
        if (SearchBox != null)
            SearchBox.Watermark = loc.GetString("Search");
    }
}
