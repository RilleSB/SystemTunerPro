using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace DiskCleanerGUI.Avalonia.Services;

public class FastFileService
{
    private static readonly ConcurrentDictionary<string, bool> _accessCache = new();
    private readonly SemaphoreSlim _semaphore = new(Environment.ProcessorCount);

    public async Task<bool> CanAccessAsync(string path, CancellationToken ct = default)
    {
        if (_accessCache.TryGetValue(path, out var cached))
            return cached;

        await _semaphore.WaitAsync(ct);
        try
        {
            var canAccess = await Task.Run(() =>
            {
                try
                {
                    using var fs = File.OpenRead(path);
                    return true;
                }
                catch
                {
                    return false;
                }
            }, ct);

            _accessCache.TryAdd(path, canAccess);
            return canAccess;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<long> GetFileSizeAsync(string path, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                return new FileInfo(path).Length;
            }
            catch
            {
                return 0;
            }
        }, ct);
    }

    public async Task<bool> DeleteFileAsync(string path, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (File.Exists(path))
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                    File.Delete(path);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }, ct);
    }

    public static void ClearCache() => _accessCache.Clear();
}