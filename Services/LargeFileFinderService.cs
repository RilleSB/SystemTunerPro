using DiskCleanerGUI.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiskCleanerGUI.Avalonia.Services;

public class LargeFileFinderService
{
    public async Task<List<LargeFileItem>> FindLargeFilesAsync(
        string path, 
        long minSizeMB, 
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var files = new List<LargeFileItem>();
        var minSizeBytes = minSizeMB * 1024 * 1024;

        await Task.Run(() => ScanDirectory(path, minSizeBytes, files, progress, cancellationToken), cancellationToken);

        return files.OrderByDescending(f => f.Size).ToList();
    }

    private void ScanDirectory(
        string path, 
        long minSize, 
        List<LargeFileItem> files, 
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report($"Сканирование: {path}");

            foreach (var file in Directory.EnumerateFiles(path))
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var info = new FileInfo(file);
                    
                    if (info.Length >= minSize)
                    {
                        files.Add(new LargeFileItem
                        {
                            FullPath = info.FullName,
                            FileName = info.Name,
                            Directory = info.DirectoryName ?? "",
                            Size = info.Length,
                            SizeFormatted = FormatBytes(info.Length),
                            LastAccessed = info.LastAccessTime,
                            Extension = info.Extension
                        });
                    }
                }
                catch { }
            }

            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                try
                {
                    ScanDirectory(dir, minSize, files, progress, cancellationToken);
                }
                catch { }
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    private static string FormatBytes(long bytes)
    {
        return bytes switch
        {
            < 1024 * 1024 => $"{bytes / 1024:F1} KB",
            < 1024 * 1024 * 1024 => $"{bytes / (1024 * 1024):F1} MB",
            _ => $"{bytes / (1024 * 1024 * 1024):F2} GB"
        };
    }
}
