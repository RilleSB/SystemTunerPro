using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DiskCleanerGUI.Avalonia.Services;
using System.Linq;

namespace DiskCleanerGUI.Avalonia.Views;

public partial class FileViewerHost : UserControl
{
    public FileViewerHost()
    {
        InitializeComponent();
        LocalizationService.Instance.LanguageChanged += UpdateLocalization;
        UpdateLocalization();
    }
    
    private void UpdateLocalization()
    {
        var loc = LocalizationService.Instance;
        
        if (this.FindControl<Button>("BrowseFolderBtn") is Button browseBtn)
            browseBtn.Content = loc.GetString("Browse");
        if (this.FindControl<Button>("LoadBtn") is Button loadBtn)
            loadBtn.Content = loc.GetString("Load");
        if (this.FindControl<TextBox>("SearchBox") is TextBox searchBox)
            searchBox.Watermark = loc.GetString("Search");
        if (this.FindControl<Button>("OpenFileBtn") is Button openBtn)
            openBtn.Content = loc.GetString("OpenFile");
        if (this.FindControl<Button>("OpenInExplorerBtn") is Button explorerBtn)
            explorerBtn.Content = loc.GetString("OpenInExplorer");
        if (this.FindControl<Button>("RemoveFromListBtn") is Button removeBtn)
            removeBtn.Content = loc.GetString("RemoveFromList");
        if (this.FindControl<TextBlock>("MoveFilesTitle") is TextBlock moveTitle)
            moveTitle.Text = loc.GetString("MoveFiles");
        if (this.FindControl<Button>("BrowseMoveBtn") is Button browseMoveBtn)
            browseMoveBtn.Content = loc.GetString("Browse");
        if (this.FindControl<Button>("MoveSelectedBtn") is Button moveSelectedBtn)
            moveSelectedBtn.Content = loc.GetString("MoveSelected");
        if (this.FindControl<Button>("MoveAllBtn") is Button moveAllBtn)
            moveAllBtn.Content = loc.GetString("MoveAll");
            
        // Update watermarks
        if (this.FindControl<TextBox>("FolderPathBox") is TextBox folderPathBox)
            folderPathBox.Watermark = loc.GetString("FolderPath");
        if (this.FindControl<TextBox>("MoveDestinationBox") is TextBox moveDestBox)
            moveDestBox.Watermark = loc.GetString("MoveDestination");
    }
    
    private async void OnBrowseFolder(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is { } provider)
        {
            var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Folder",
                AllowMultiple = false
            });
            
            if (folders.Count > 0 && DataContext is ViewModels.FileViewerViewModel vm)
            {
                var path = folders[0].TryGetLocalPath();
                if (!string.IsNullOrEmpty(path))
                {
                    vm.CurrentPath = path;
                }
            }
        }
    }
    
    private async void OnBrowseMoveFolder(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is { } provider)
        {
            var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Move Destination",
                AllowMultiple = false
            });
            
            if (folders.Count > 0 && DataContext is ViewModels.FileViewerViewModel vm)
            {
                var path = folders[0].TryGetLocalPath();
                if (!string.IsNullOrEmpty(path))
                {
                    vm.MoveToPath = path;
                }
            }
        }
    }
}