using CommunityToolkit.Mvvm.ComponentModel;
using DiskCleanerGUI.Avalonia.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DiskCleanerGUI.Avalonia.ViewModels;

/// <summary>
/// Группа результатов очистки с общей трёхсостояниевой галочкой.
/// </summary>
public sealed class FileGroupViewModel : ObservableObject, IDisposable
{
    private readonly IReadOnlyList<FileItem> _allItems;
    private bool _isUpdatingChildren;

    public FileGroupViewModel(
        string key,
        IReadOnlyList<FileItem> allItems,
        IEnumerable<FileItem> visibleItems)
    {
        Key = key;
        _allItems = allItems;
        Items = new ObservableCollection<FileItem>(visibleItems);
        TotalSize = allItems.Sum(item => item.Size);

        foreach (var item in _allItems)
            item.PropertyChanged += OnItemPropertyChanged;
    }

    public string Key { get; }
    public ObservableCollection<FileItem> Items { get; }
    public int FileCount => _allItems.Count;
    public long TotalSize { get; }

    public bool? IsSelected
    {
        get
        {
            var selectedCount = _allItems.Count(item => item.IsSelected);
            if (selectedCount == 0)
                return false;
            if (selectedCount == _allItems.Count)
                return true;
            return null;
        }
        set
        {
            if (!value.HasValue || _allItems.Count == 0)
                return;

            _isUpdatingChildren = true;
            try
            {
                foreach (var item in _allItems)
                    item.IsSelected = value.Value;
            }
            finally
            {
                _isUpdatingChildren = false;
            }

            OnPropertyChanged();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? SelectionChanged;

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName != nameof(FileItem.IsSelected) || _isUpdatingChildren)
            return;

        OnPropertyChanged(nameof(IsSelected));
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        foreach (var item in _allItems)
            item.PropertyChanged -= OnItemPropertyChanged;
    }
}
