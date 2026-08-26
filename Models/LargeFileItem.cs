using System;

namespace DiskCleanerGUI.Avalonia.Models;

public class LargeFileItem
{
    public string FullPath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Directory { get; set; } = "";
    public long Size { get; set; }
    public string SizeFormatted { get; set; } = "";
    public DateTime LastAccessed { get; set; }
    public string Extension { get; set; } = "";
}
