using System.Collections.Concurrent;

namespace DiskCleanerGUI.Avalonia.Services;

public enum CacheTargetKind
{
    Browser,
    Application
}

public sealed record CacheTarget(string Path, string ApplicationName, CacheTargetKind Kind);

/// <summary>
/// Находит кэши по структуре каталогов, а не по списку абсолютных путей.
/// Поиск намеренно консервативный: целями становятся только каталоги с точными
/// именами кэшей внутри AppData. Каталоги данных (Data, Storage, User Data и т.п.)
/// никогда не считаются кэшем сами по себе.
/// </summary>
public sealed class CacheDiscoveryService
{
    private static readonly HashSet<string> CacheDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cache",
        "Caches",
        "cache2",
        "Code Cache",
        "GPUCache",
        "DawnCache",
        "GrShaderCache",
        "ShaderCache",
        "Media Cache",
        "Media Cache Files",
        "LocalCache",
        "INetCache",
        "WebCache",
        "Temp",
        "tmp",
        "TempState"
    };

    private static readonly string[] BrowserMarkers =
    {
        "chrome", "chromium", "edge", "brave", "vivaldi", "yandexbrowser",
        "opera", "firefox", "waterfox", "librewolf", "floorp", "zen"
    };

    private static readonly HashSet<string> VendorFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Google", "Microsoft", "Mozilla", "BraveSoftware", "Opera Software",
        "Yandex", "Vivaldi", "Adobe"
    };

    private static readonly EnumerationOptions DirectoryOptions = new()
    {
        RecurseSubdirectories = false,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System
    };

    private readonly SemaphoreSlim _scanLock = new(1, 1);
    private IReadOnlyList<CacheTarget>? _cachedTargets;
    private DateTime _cacheCreatedAt;

    public async Task<IReadOnlyList<CacheTarget>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedTargets is not null && DateTime.UtcNow - _cacheCreatedAt < TimeSpan.FromSeconds(30))
            return _cachedTargets;

        await _scanLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cachedTargets is not null && DateTime.UtcNow - _cacheCreatedAt < TimeSpan.FromSeconds(30))
                return _cachedTargets;

            var targets = await Task.Run(() => DiscoverCore(cancellationToken), cancellationToken)
                .ConfigureAwait(false);

            _cachedTargets = targets;
            _cacheCreatedAt = DateTime.UtcNow;
            return targets;
        }
        finally
        {
            _scanLock.Release();
        }
    }

    public void InvalidateCache()
    {
        _cachedTargets = null;
        _cacheCreatedAt = default;
    }

    private static IReadOnlyList<CacheTarget> DiscoverCore(CancellationToken cancellationToken)
    {
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        }
        .Where(Directory.Exists)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

        var found = new ConcurrentDictionary<string, CacheTarget>(StringComparer.OrdinalIgnoreCase);
        var installedApplications = InstalledApplicationRegistry.ReadInstalledApplications();

        Parallel.ForEach(roots, new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = 2
        }, root =>
        {
            ScanRoot(root, found, cancellationToken);
            ScanRegistryMatchedApplicationRoots(
                root,
                installedApplications,
                found,
                cancellationToken);
        });

        return found.Values
            .OrderBy(target => target.ApplicationName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(target => target.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void ScanRegistryMatchedApplicationRoots(
        string root,
        IReadOnlyList<InstalledApplication> installedApplications,
        ConcurrentDictionary<string, CacheTarget> found,
        CancellationToken cancellationToken)
    {
        if (installedApplications.Count == 0)
            return;

        var candidates = GetApplicationRootCandidates(root);
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var application = FindBestRegistryMatch(candidate, installedApplications);
            if (application is null)
                continue;

            ScanConfirmedApplicationRoot(candidate.Path, application.Name, found, cancellationToken);
        }
    }

    private static IReadOnlyList<AppDataRootCandidate> GetApplicationRootCandidates(string root)
    {
        var candidates = new List<AppDataRootCandidate>();
        string[] topLevel;
        try
        {
            topLevel = Directory.EnumerateDirectories(root, "*", DirectoryOptions).ToArray();
        }
        catch (IOException)
        {
            return candidates;
        }
        catch (UnauthorizedAccessException)
        {
            return candidates;
        }

        foreach (var directory in topLevel)
        {
            var name = Path.GetFileName(directory);
            if (name.Equals("Packages", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Temp", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!VendorFolders.Contains(name))
            {
                candidates.Add(new AppDataRootCandidate(directory, name, name));
                continue;
            }

            try
            {
                foreach (var productDirectory in Directory.EnumerateDirectories(directory, "*", DirectoryOptions))
                {
                    var productName = Path.GetFileName(productDirectory);
                    candidates.Add(new AppDataRootCandidate(
                        productDirectory,
                        productName,
                        name + productName));
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        return candidates;
    }

    private static InstalledApplication? FindBestRegistryMatch(
        AppDataRootCandidate candidate,
        IReadOnlyList<InstalledApplication> installedApplications)
    {
        var simpleName = InstalledApplicationRegistry.Normalize(candidate.Name);
        var qualifiedName = InstalledApplicationRegistry.Normalize(candidate.QualifiedName);
        if (simpleName.Length < 3)
            return null;

        InstalledApplication? bestMatch = null;
        var bestScore = 0;

        foreach (var application in installedApplications)
        {
            foreach (var alias in application.Aliases)
            {
                var score = GetAliasMatchScore(alias, simpleName, qualifiedName);
                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestMatch = application;
            }
        }

        return bestScore >= 80 ? bestMatch : null;
    }

    private static int GetAliasMatchScore(string alias, string simpleName, string qualifiedName)
    {
        if (alias.Equals(qualifiedName, StringComparison.OrdinalIgnoreCase))
            return 110;
        if (alias.Equals(simpleName, StringComparison.OrdinalIgnoreCase))
            return 100;

        var shorterLength = Math.Min(alias.Length, qualifiedName.Length);
        if (shorterLength >= 6 &&
            (alias.EndsWith(qualifiedName, StringComparison.OrdinalIgnoreCase) ||
             qualifiedName.EndsWith(alias, StringComparison.OrdinalIgnoreCase)))
            return 85;

        shorterLength = Math.Min(alias.Length, simpleName.Length);
        if (shorterLength >= 6 &&
            (alias.EndsWith(simpleName, StringComparison.OrdinalIgnoreCase) ||
             simpleName.EndsWith(alias, StringComparison.OrdinalIgnoreCase)))
            return 80;

        return 0;
    }

    private static void ScanConfirmedApplicationRoot(
        string applicationRoot,
        string applicationName,
        ConcurrentDictionary<string, CacheTarget> found,
        CancellationToken cancellationToken)
    {
        const int maxDepth = 9;
        const int maxVisitedDirectories = 15_000;
        var pending = new Queue<(string Path, int Depth)>();
        pending.Enqueue((applicationRoot, 0));
        var visited = 0;

        while (pending.Count > 0 && visited < maxVisitedDirectories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (current, depth) = pending.Dequeue();
            visited++;

            string[] children;
            try
            {
                children = Directory.EnumerateDirectories(current, "*", DirectoryOptions).ToArray();
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var child in children)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var folderName = Path.GetFileName(child);
                if (CacheDirectoryNames.Contains(folderName))
                {
                    var targetDepth = depth + 1;
                    var isSafeByStructure = IsSafeCacheTarget(
                        applicationRoot,
                        child,
                        folderName,
                        targetDepth);
                    var isSafeByRegistry = IsRegistryAssistedCacheTarget(folderName, targetDepth);

                    if (!isSafeByStructure && !isSafeByRegistry)
                    {
                        if (depth < maxDepth)
                            pending.Enqueue((child, targetDepth));
                        continue;
                    }

                    if (!IsOwnApplication(applicationName, child) && !IsWindowsSystemCache(child))
                    {
                        var kind = IsBrowserCache(applicationName + Path.DirectorySeparatorChar + child, folderName)
                            ? CacheTargetKind.Browser
                            : CacheTargetKind.Application;
                        var target = new CacheTarget(child, applicationName, kind);
                        found.AddOrUpdate(Path.GetFullPath(child), target, (_, _) => target);
                    }
                    continue;
                }

                if (depth < maxDepth && ShouldDescendInto(folderName))
                    pending.Enqueue((child, depth + 1));
            }
        }
    }

    private static bool IsRegistryAssistedCacheTarget(string folderName, int targetDepth)
    {
        if (targetDepth > 4)
            return false;

        return folderName.Equals("Cache", StringComparison.OrdinalIgnoreCase) ||
               folderName.Equals("Caches", StringComparison.OrdinalIgnoreCase) ||
               folderName.Equals("Temp", StringComparison.OrdinalIgnoreCase) ||
               folderName.Equals("tmp", StringComparison.OrdinalIgnoreCase);
    }

    private static void ScanRoot(
        string root,
        ConcurrentDictionary<string, CacheTarget> found,
        CancellationToken cancellationToken)
    {
        const int maxDepth = 7;
        const int maxVisitedDirectories = 50_000;
        var pending = new Queue<(string Path, int Depth)>();
        pending.Enqueue((root, 0));
        var visited = 0;

        while (pending.Count > 0 && visited < maxVisitedDirectories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (current, depth) = pending.Dequeue();
            visited++;

            string[] children;
            try
            {
                // Материализуем перечисление внутри try: ошибки доступа у Directory.Enumerate*
                // часто возникают только при фактическом чтении результата.
                children = Directory.EnumerateDirectories(current, "*", DirectoryOptions).ToArray();
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var child in children)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var folderName = Path.GetFileName(child);

                if (CacheDirectoryNames.Contains(folderName))
                {
                    // Корневой %LOCALAPPDATA%\Temp обслуживается отдельной категорией
                    // временных файлов и не должен второй раз появляться как кэш приложения.
                    if (depth == 0)
                        continue;

                    if (!IsSafeCacheTarget(root, child, folderName, depth + 1))
                    {
                        if (depth < maxDepth)
                            pending.Enqueue((child, depth + 1));
                        continue;
                    }

                    var appName = ResolveApplicationName(root, child);
                    if (!IsOwnApplication(appName, child) && !IsWindowsSystemCache(child))
                    {
                        var kind = IsBrowserCache(child, folderName)
                            ? CacheTargetKind.Browser
                            : CacheTargetKind.Application;
                        found.TryAdd(Path.GetFullPath(child), new CacheTarget(child, appName, kind));
                    }

                    // Не добавляем вложенные кэши второй раз: при очистке содержимое
                    // этой цели всё равно будет обработано рекурсивно.
                    continue;
                }

                if (depth < maxDepth && ShouldDescendInto(folderName))
                    pending.Enqueue((child, depth + 1));
            }
        }
    }

    private static bool ShouldDescendInto(string folderName) =>
        !folderName.Equals("node_modules", StringComparison.OrdinalIgnoreCase) &&
        !folderName.Equals(".git", StringComparison.OrdinalIgnoreCase) &&
        !folderName.Equals("WindowsApps", StringComparison.OrdinalIgnoreCase);

    private static bool IsSafeCacheTarget(string root, string path, string folderName, int targetDepth)
    {
        var relative = Path.GetRelativePath(root, path);
        var segments = relative.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

        var isPackageCache = segments.Length >= 3 &&
                             segments[0].Equals("Packages", StringComparison.OrdinalIgnoreCase) &&
                             (folderName.Equals("LocalCache", StringComparison.OrdinalIgnoreCase) ||
                              folderName.Equals("TempState", StringComparison.OrdinalIgnoreCase) ||
                              (folderName.Equals("Temp", StringComparison.OrdinalIgnoreCase) &&
                               segments[^2].Equals("AC", StringComparison.OrdinalIgnoreCase)));
        if (isPackageCache)
            return true;

        if (folderName.Equals("Code Cache", StringComparison.OrdinalIgnoreCase) ||
            folderName.Equals("GPUCache", StringComparison.OrdinalIgnoreCase) ||
            folderName.Equals("DawnCache", StringComparison.OrdinalIgnoreCase) ||
            folderName.Equals("GrShaderCache", StringComparison.OrdinalIgnoreCase) ||
            folderName.Equals("ShaderCache", StringComparison.OrdinalIgnoreCase) ||
            folderName.Equals("Media Cache", StringComparison.OrdinalIgnoreCase) ||
            folderName.Equals("Media Cache Files", StringComparison.OrdinalIgnoreCase) ||
            folderName.Equals("INetCache", StringComparison.OrdinalIgnoreCase) ||
            folderName.Equals("WebCache", StringComparison.OrdinalIgnoreCase))
            return true;

        if (folderName.Equals("cache2", StringComparison.OrdinalIgnoreCase))
            return targetDepth <= 4 || IsBrowserCache(path, folderName);

        // Обычные Cache/Temp/tmp безопасно считать служебными только рядом с
        // корнем приложения. Для более глубоких профилей нужна сигнатура Chromium.
        return targetDepth <= 3 || HasChromiumDataRoot(path, root);
    }

    private static bool HasChromiumDataRoot(string path, string root)
    {
        var current = Directory.GetParent(path);
        for (var level = 0; current is not null && level < 6; level++, current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "Local State")))
                return true;

            if (current.FullName.Equals(root, StringComparison.OrdinalIgnoreCase))
                break;
        }

        return false;
    }

    private static bool IsBrowserCache(string path, string folderName)
    {
        if (folderName.Equals("INetCache", StringComparison.OrdinalIgnoreCase) ||
            folderName.Equals("WebCache", StringComparison.OrdinalIgnoreCase))
            return true;

        return path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => BrowserMarkers.Any(marker =>
                segment.Contains(marker, StringComparison.OrdinalIgnoreCase)));
    }

    private static string ResolveApplicationName(string root, string cachePath)
    {
        var relative = Path.GetRelativePath(root, cachePath);
        var segments = relative.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
            return "Приложение";

        if (segments[0].Equals("Packages", StringComparison.OrdinalIgnoreCase) && segments.Length > 1)
            return NormalizePackageName(segments[1]);

        if (VendorFolders.Contains(segments[0]) && segments.Length > 1)
            return segments[1];

        return segments[0];
    }

    private static string NormalizePackageName(string packageFamilyName)
    {
        var publisherSeparator = packageFamilyName.LastIndexOf('_');
        var name = publisherSeparator > 0
            ? packageFamilyName[..publisherSeparator]
            : packageFamilyName;
        return name.Replace('.', ' ');
    }

    private static bool IsOwnApplication(string applicationName, string path) =>
        applicationName.Contains("TrashClean", StringComparison.OrdinalIgnoreCase) ||
        applicationName.Contains("SystemTuner", StringComparison.OrdinalIgnoreCase) ||
        path.Contains($"{Path.DirectorySeparatorChar}TrashClean{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsWindowsSystemCache(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}Microsoft{Path.DirectorySeparatorChar}Windows{Path.DirectorySeparatorChar}Caches",
            StringComparison.OrdinalIgnoreCase);

    private sealed record AppDataRootCandidate(string Path, string Name, string QualifiedName);
}
