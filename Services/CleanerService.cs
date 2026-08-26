using DiskCleanerGUI.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;

namespace DiskCleanerGUI.Avalonia.Services;

/// <summary>
/// Основной сервис для очистки системы - выполняет сканирование и удаление временных файлов
/// Поддерживает многопоточность и безопасное удаление с обработкой ошибок
/// </summary>
public class CleanerService : IDisposable
{
    // Семафор для ограничения количества одновременных операций с файлами
    private readonly SemaphoreSlim _semaphore = new(Environment.ProcessorCount, Environment.ProcessorCount);
    private readonly CacheDiscoveryService _cacheDiscovery = new();

    public Task<CleaningResult> DeleteFilesAsync(
        IEnumerable<FileItem> files,
        IProgress<int> progress,
        CancellationToken ct = default,
        bool moveToRecycleBin = false)
    {
        var targets = files
            .GroupBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

        return Task.Run(() =>
        {
            var result = new CleaningResult();
            if (targets.Length == 0)
            {
                progress.Report(100);
                return result;
            }

            for (var index = 0; index < targets.Length; index++)
            {
                ct.ThrowIfCancellationRequested();
                var item = targets[index];

                try
                {
                    if (!File.Exists(item.Path))
                    {
                        result.Errors.Add($"{item.Path}: файл больше не существует");
                    }
                    else if (DeleteFileSafe(item.Path, moveToRecycleBin))
                    {
                        result.FilesDeleted++;
                        result.BytesFreed += item.Size;
                        result.DeletedPaths.Add(item.Path);
                    }
                    else
                    {
                        result.Errors.Add($"{item.Path}: удалить файл не удалось");
                    }
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    result.Errors.Add($"{item.Path}: {exception.Message}");
                }

                progress.Report((index + 1) * 100 / targets.Length);
            }

            _cacheDiscovery.InvalidateCache();
            return result;
        }, ct);
    }
    
    public async Task<IEnumerable<FileItem>> EnumerateUserTempAsync(CancellationToken ct = default)
    {
        var tempPath = Path.GetTempPath();
        return await EnumerateDirectoryAsync(tempPath, ct).ConfigureAwait(false);
    }
    
    public async Task<IEnumerable<FileItem>> EnumerateWindowsTempAsync(CancellationToken ct = default)
    {
        var items = new List<FileItem>();
        
        // Основная папка Windows\Temp
        var winTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
        items.AddRange(await EnumerateDirectoryAsync(winTemp, ct).ConfigureAwait(false));
        
        // Отчеты об ошибках Windows
        items.AddRange(await EnumerateDirectoryAsync(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "WER"), ct));
        
        // Системные кэши
        items.AddRange(await EnumerateDirectoryAsync(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Caches"), ct));
        
        return items;
    }

    public Task<IEnumerable<FileItem>> EnumerateBrowserCachesAsync(CancellationToken ct = default) =>
        EnumerateDiscoveredCachesAsync(CacheTargetKind.Browser, ct);

    public Task<IEnumerable<FileItem>> EnumerateAppCachesAsync(CancellationToken ct = default) =>
        EnumerateDiscoveredCachesAsync(CacheTargetKind.Application, ct);

    public async Task<IEnumerable<FileItem>> EnumerateWindowsUpdateAsync()
    {
        var items = new List<FileItem>();
        items.AddRange(await EnumerateDirectoryAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download")));
        return items;
    }

    public Task CleanUserTempAsync(IProgress<int> progress, CancellationToken ct = default, bool safeDelete = false) => 
        DeleteDirectorySafeAsync(Environment.GetEnvironmentVariable("TEMP"), progress, ct, safeDelete);
        
    public Task CleanWindowsTempAsync(IProgress<int> progress, CancellationToken ct = default) => Task.Run(() =>
    {
        var targets = new List<string?>
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "WER"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Caches")
        };
        int i = 0; int total = Math.Max(targets.Count, 1);
        foreach (var dir in targets)
        {
            DeleteDirectorySafe(dir, new Progress<int>(_ => { }));
            i++; progress.Report(i * 100 / total);
        }
        progress.Report(100);
    });

    public Task CleanBrowsersAsync(IProgress<int> progress, CancellationToken ct = default) =>
        CleanDiscoveredCachesAsync(CacheTargetKind.Browser, progress, ct);

    public Task CleanAppsAsync(IProgress<int> progress, CancellationToken ct = default) =>
        CleanDiscoveredCachesAsync(CacheTargetKind.Application, progress, ct);

    private async Task<IEnumerable<FileItem>> EnumerateDiscoveredCachesAsync(
        CacheTargetKind kind,
        CancellationToken ct)
    {
        var targets = (await _cacheDiscovery.DiscoverAsync(ct).ConfigureAwait(false))
            .Where(target => target.Kind == kind)
            .ToArray();

        var scans = targets.Select(target => EnumerateCacheDirectoryAsync(target, ct));
        var results = await Task.WhenAll(scans).ConfigureAwait(false);

        return results
            .SelectMany(items => items)
            .GroupBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    private static async Task<List<FileItem>> EnumerateCacheDirectoryAsync(
        CacheTarget target,
        CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            var items = new List<FileItem>();
            if (!Directory.Exists(target.Path))
                return items;

            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            };

            try
            {
                foreach (var path in Directory.EnumerateFiles(target.Path, "*", options))
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var info = new FileInfo(path);
                        items.Add(new FileItem
                        {
                            Path = path,
                            Size = info.Length,
                            ApplicationName = target.ApplicationName
                        });
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            return items;
        }, ct).ConfigureAwait(false);
    }

    private async Task CleanDiscoveredCachesAsync(
        CacheTargetKind kind,
        IProgress<int> progress,
        CancellationToken ct)
    {
        var targets = (await _cacheDiscovery.DiscoverAsync(ct).ConfigureAwait(false))
            .Where(target => target.Kind == kind)
            .ToArray();

        if (targets.Length == 0)
        {
            progress.Report(100);
            return;
        }

        for (var index = 0; index < targets.Length; index++)
        {
            ct.ThrowIfCancellationRequested();
            await DeleteCacheContentsAsync(targets[index].Path, ct).ConfigureAwait(false);
            progress.Report((index + 1) * 100 / targets.Length);
        }

        _cacheDiscovery.InvalidateCache();
    }

    private static async Task DeleteCacheContentsAsync(string directory, CancellationToken ct)
    {
        await DeleteCacheContentsWithStatsAsync(directory, ct).ConfigureAwait(false);
    }

    private static Task<(long size, int files)> DeleteCacheContentsWithStatsAsync(
        string directory,
        CancellationToken ct) => Task.Run(() =>
    {
        if (!Directory.Exists(directory))
            return (0L, 0);

        long deletedSize = 0;
        var deletedFiles = 0;

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*", options))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var size = new FileInfo(file).Length;
                    if (DeleteFileSafe(file, moveToRecycleBin: false))
                    {
                        deletedSize += size;
                        deletedFiles++;
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }

            // Удаляем только опустевшие вложенные папки. Сам каталог кэша остаётся,
            // чтобы приложение не потеряло ожидаемую структуру профиля.
            foreach (var child in Directory.EnumerateDirectories(directory, "*", options)
                         .OrderByDescending(path => path.Length))
            {
                ct.ThrowIfCancellationRequested();
                try { Directory.Delete(child, recursive: false); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        return (deletedSize, deletedFiles);
    }, ct);

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, int flags);
    
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        public string pFrom;
        public string pTo;
        public ushort fFlags;
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string lpszProgressTitle;
    }
    
    private const uint FO_DELETE = 0x0003;
    private const ushort FOF_ALLOWUNDO = 0x0040;
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_SILENT = 0x0004;

    public Task CleanRecycleBinAsync(IProgress<int> progress)
    {
        return Task.Run(() =>
        {
            try { SHEmptyRecycleBin(IntPtr.Zero, null, 0x00000001 | 0x00000002 | 0x00000004); }
            catch { }
            progress.Report(100);
        });
    }

    public Task CleanWindowsUpdateAsync(IProgress<int> progress) => Task.Run(() =>
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download");
        DeleteDirectorySafe(dir, progress);
    });

    private async Task<IEnumerable<FileItem>> EnumerateDirectoryAsync(string? dir, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) 
            return Array.Empty<FileItem>();
            
        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await Task.Run(() =>
            {
                var items = new List<FileItem>(1000);
                try
                {
                    var options = new EnumerationOptions
                    {
                        RecurseSubdirectories = true,
                        IgnoreInaccessible = true,
                        AttributesToSkip = FileAttributes.ReparsePoint
                    };
                    var files = Directory.EnumerateFiles(dir, "*", options);
                        
                    foreach (var path in files)
                    {
                        ct.ThrowIfCancellationRequested();
                        try
                        {
                            var info = new FileInfo(path);
                            items.Add(new FileItem { Path = path, Size = info.Length });
                        }
                        catch (UnauthorizedAccessException) { /* Skip files without access */ }
                        catch (DirectoryNotFoundException) { /* Skip missing directories */ }
                        catch (FileNotFoundException) { /* Skip missing files */ }
                        catch (IOException) { /* Skip locked files */ }
                        catch { /* Skip other errors */ }
                    }
                }
                catch (UnauthorizedAccessException) { /* Skip directories without access */ }
                catch (DirectoryNotFoundException) { /* Skip missing directories */ }
                catch (IOException) { /* Skip locked directories */ }
                catch { /* Skip other directory errors */ }
                return items;
            }, ct).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Удаляет файл безопасно (в корзину или навсегда)
    /// </summary>
    private static bool DeleteFileSafe(string filePath, bool moveToRecycleBin)
    {
        try
        {
            if (moveToRecycleBin)
            {
                // Удаление в корзину
                var shf = new SHFILEOPSTRUCT
                {
                    wFunc = FO_DELETE,
                    pFrom = filePath + "\0",
                    fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT
                };
                return SHFileOperation(ref shf) == 0;
            }
            else
            {
                // Окончательное удаление
                File.SetAttributes(filePath, FileAttributes.Normal);
                File.Delete(filePath);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }
    
    private static void DeleteDirectorySafe(string? dir, IProgress<int> progress, bool safeDelete = false)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) { progress.Report(100); return; }
        
        try
        {
            var files = Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly).Take(1000).ToArray();
            var total = Math.Max(files.Length, 1);
            
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    DeleteFileSafe(files[i], safeDelete);
                }
                catch (UnauthorizedAccessException)
                {
                    // Пытаемся получить права на файл
                    try
                    {
                        var fileInfo = new FileInfo(files[i]);
                        var fileSecurity = fileInfo.GetAccessControl();
                        fileSecurity.SetOwner(System.Security.Principal.WindowsIdentity.GetCurrent().User!);
                        fileInfo.SetAccessControl(fileSecurity);
                        
                        File.SetAttributes(files[i], FileAttributes.Normal);
                        File.Delete(files[i]);
                    }
                    catch { /* Пропускаем файлы без доступа */ }
                }
                catch (IOException)
                {
                    // Файл заблокирован процессом - пропускаем
                }
                catch { /* Пропускаем другие ошибки */ }
                
                if (i % 10 == 0)
                    progress.Report((i + 1) * 100 / total);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Нет доступа к папке - пропускаем
        }
        catch { /* Пропускаем другие ошибки */ }
        
        progress.Report(100);
    }
    
    private static async Task DeleteDirectorySafeAsync(string? dir, IProgress<int> progress, CancellationToken ct = default, bool safeDelete = false)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) 
        {
            progress.Report(100);
            return;
        }
        
        await Task.Run(() =>
        {
            try
            {
                var files = Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly).Take(1000).ToArray();
                var total = Math.Max(files.Length, 1);
                
                for (int i = 0; i < files.Length; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        DeleteFileSafe(files[i], safeDelete);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Пытаемся получить права на файл
                        try
                        {
                            var fileInfo = new FileInfo(files[i]);
                            var fileSecurity = fileInfo.GetAccessControl();
                            fileSecurity.SetOwner(System.Security.Principal.WindowsIdentity.GetCurrent().User!);
                            fileInfo.SetAccessControl(fileSecurity);
                            
                            File.SetAttributes(files[i], FileAttributes.Normal);
                            File.Delete(files[i]);
                        }
                        catch { /* Пропускаем файлы без доступа */ }
                    }
                    catch (IOException)
                    {
                        // Файл заблокирован - пропускаем
                    }
                    catch { /* Пропускаем другие ошибки */ }
                    
                    if (i % 10 == 0)
                        progress.Report((i + 1) * 100 / total);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Нет доступа к папке
            }
            catch { /* Пропускаем другие ошибки */ }
            
            progress.Report(100);
        }, ct).ConfigureAwait(false);
    }
    
    public void Dispose()
    {
        _semaphore?.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class CleaningResult
{
    public long FilesDeleted;
    public long BytesFreed;
    public List<string> Errors { get; set; } = new();
    public HashSet<string> DeletedPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
