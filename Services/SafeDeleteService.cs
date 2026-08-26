using System.Runtime.InteropServices;
using System.Text.Json;

namespace DiskCleanerGUI.Avalonia.Services;

public sealed class SafeDeleteService
{
    private static readonly SemaphoreSlim IndexLock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _trashFolder;
    private readonly string _indexFile;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct SHQUERYRBINFO
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    public SafeDeleteService()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TrashClean");
        _trashFolder = Path.Combine(appData, "Trash");
        _indexFile = Path.Combine(_trashFolder, "index.json");
        Directory.CreateDirectory(_trashFolder);
    }

    public async Task<bool> SafeDeleteAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        await IndexLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var sourcePath = Path.GetFullPath(filePath);
            if (!File.Exists(sourcePath))
                return false;

            var trashPath = CreateUniqueTrashPath(Path.GetFileName(sourcePath));
            File.Move(sourcePath, trashPath);

            try
            {
                var items = await LoadIndexUnlockedAsync().ConfigureAwait(false);
                items.Add(new TrashItem
                {
                    OriginalPath = sourcePath,
                    TrashPath = trashPath,
                    DeletedAt = DateTime.Now,
                    Size = new FileInfo(trashPath).Length
                });
                await SaveIndexUnlockedAsync(items).ConfigureAwait(false);
                return true;
            }
            catch
            {
                try { File.Move(trashPath, sourcePath); } catch { }
                return false;
            }
        }
        catch
        {
            return false;
        }
        finally
        {
            IndexLock.Release();
        }
    }

    public async Task<List<TrashItem>> GetTrashItemsAsync()
    {
        await IndexLock.WaitAsync().ConfigureAwait(false);
        try
        {
            return await LoadIndexUnlockedAsync().ConfigureAwait(false);
        }
        finally
        {
            IndexLock.Release();
        }
    }

    public async Task<bool> RestoreFileAsync(TrashItem item)
    {
        if (item.IsSystemRecycleBin)
            return false;

        await IndexLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var trashPath = Path.GetFullPath(item.TrashPath);
            var originalPath = Path.GetFullPath(item.OriginalPath);
            if (!IsInsideTrash(trashPath) || !File.Exists(trashPath) || File.Exists(originalPath))
                return false;

            var directory = Path.GetDirectoryName(originalPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.Move(trashPath, originalPath);

            var items = await LoadIndexUnlockedAsync().ConfigureAwait(false);
            items.RemoveAll(entry => PathsEqual(entry.TrashPath, trashPath));
            await SaveIndexUnlockedAsync(items).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            IndexLock.Release();
        }
    }

    public async Task ClearTrashAsync()
    {
        await IndexLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var items = await LoadIndexUnlockedAsync().ConfigureAwait(false);
            var remaining = new List<TrashItem>();

            foreach (var item in items)
            {
                try
                {
                    var trashPath = Path.GetFullPath(item.TrashPath);
                    if (!IsInsideTrash(trashPath))
                    {
                        remaining.Add(item);
                        continue;
                    }

                    if (File.Exists(trashPath))
                        File.Delete(trashPath);
                }
                catch
                {
                    remaining.Add(item);
                }
            }

            await SaveIndexUnlockedAsync(remaining).ConfigureAwait(false);
        }
        finally
        {
            IndexLock.Release();
        }
    }

    public Task<(long size, long count)> GetSystemRecycleBinInfoAsync() => Task.Run(() =>
    {
        try
        {
            var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf<SHQUERYRBINFO>() };
            return SHQueryRecycleBin(null, ref info) == 0
                ? (info.i64Size, info.i64NumItems)
                : (0L, 0L);
        }
        catch
        {
            return (0L, 0L);
        }
    });

    public Task<bool> EmptySystemRecycleBinAsync() => Task.Run(() =>
    {
        try
        {
            const uint noConfirmation = 0x01;
            const uint noProgressUi = 0x02;
            const uint noSound = 0x04;
            return SHEmptyRecycleBin(IntPtr.Zero, null, noConfirmation | noProgressUi | noSound) == 0;
        }
        catch
        {
            return false;
        }
    });

    public async Task<List<TrashItem>> GetAllTrashItemsAsync()
    {
        var items = await GetTrashItemsAsync().ConfigureAwait(false);
        var (systemSize, systemCount) = await GetSystemRecycleBinInfoAsync().ConfigureAwait(false);

        if (systemCount > 0)
        {
            items.Add(new TrashItem
            {
                OriginalPath = $"Системная корзина Windows ({systemCount} шт.)",
                TrashPath = "SYSTEM_RECYCLE_BIN",
                DeletedAt = DateTime.Now,
                Size = systemSize,
                IsSystemRecycleBin = true
            });
        }

        return items;
    }

    private async Task<List<TrashItem>> LoadIndexUnlockedAsync()
    {
        if (!File.Exists(_indexFile))
            return new List<TrashItem>();

        try
        {
            var json = await File.ReadAllTextAsync(_indexFile).ConfigureAwait(false);
            return JsonSerializer.Deserialize<List<TrashItem>>(json) ?? new List<TrashItem>();
        }
        catch
        {
            throw new InvalidDataException("Индекс внутренней корзины повреждён");
        }
    }

    private async Task SaveIndexUnlockedAsync(List<TrashItem> items)
    {
        var temporaryPath = Path.Combine(_trashFolder, $"index.{Guid.NewGuid():N}.tmp");
        var json = JsonSerializer.Serialize(items, JsonOptions);
        await File.WriteAllTextAsync(temporaryPath, json).ConfigureAwait(false);
        File.Move(temporaryPath, _indexFile, overwrite: true);
    }

    private string CreateUniqueTrashPath(string fileName) => Path.Combine(
        _trashFolder,
        $"{DateTime.Now:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}_{Path.GetFileName(fileName)}");

    private bool IsInsideTrash(string path)
    {
        var relative = Path.GetRelativePath(_trashFolder, path);
        return !relative.Equals("..", StringComparison.Ordinal) &&
               !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
               !Path.IsPathRooted(relative);
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
}

public sealed class TrashItem
{
    public string OriginalPath { get; set; } = "";
    public string TrashPath { get; set; } = "";
    public DateTime DeletedAt { get; set; }
    public long Size { get; set; }
    public bool IsSystemRecycleBin { get; set; }
}
