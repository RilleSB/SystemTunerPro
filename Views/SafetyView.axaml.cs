using Avalonia.Controls;
using DiskCleanerGUI.Avalonia.Services;

namespace DiskCleanerGUI.Avalonia.Views;

public partial class SafetyView : UserControl
{
    public SafetyView()
    {
        InitializeComponent();
        LocalizationService.Instance.LanguageChanged += UpdateLocalization;
        UpdateLocalization();
    }
    
    private void UpdateLocalization()
    {
        var loc = LocalizationService.Instance;
        
        if (this.FindControl<TextBlock>("SafetyTitle") is TextBlock title)
            title.Text = loc.GetString("SafetyTitle");
        if (this.FindControl<Button>("LoadTrashBtn") is Button loadBtn)
            loadBtn.Content = loc.GetString("LoadTrash");
        if (this.FindControl<Button>("EmptyTrashBtn") is Button emptyBtn)
            emptyBtn.Content = loc.GetString("EmptyTrash");
        if (this.FindControl<TextBlock>("TrashFilesTitle") is TextBlock trashTitle)
            trashTitle.Text = loc.GetString("TrashFiles");
    }
}