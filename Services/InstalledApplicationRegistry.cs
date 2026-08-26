using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace DiskCleanerGUI.Avalonia.Services;

public sealed record InstalledApplication(
    string Name,
    string? Publisher,
    string? InstallLocation,
    string? ExecutableName,
    IReadOnlySet<string> Aliases);

/// <summary>
/// Читает каталог установленных Win32-приложений. Данные реестра используются
/// только для идентификации папки приложения; пути из реестра не удаляются.
/// </summary>
public static class InstalledApplicationRegistry
{
    private const string UninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    private const string AppPathsPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";

    public static IReadOnlyList<InstalledApplication> ReadInstalledApplications()
    {
        if (!OperatingSystem.IsWindows())
            return Array.Empty<InstalledApplication>();

        return ReadWindowsRegistry();
    }

    [SupportedOSPlatform("windows")]
    private static IReadOnlyList<InstalledApplication> ReadWindowsRegistry()
    {
        var applications = new Dictionary<string, InstalledApplication>(StringComparer.OrdinalIgnoreCase);

        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                ReadUninstallEntries(baseKey, applications);
                ReadAppPathEntries(baseKey, applications);
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }

        return applications.Values
            .OrderBy(application => application.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    [SupportedOSPlatform("windows")]
    private static void ReadUninstallEntries(
        RegistryKey baseKey,
        Dictionary<string, InstalledApplication> applications)
    {
        using var uninstall = baseKey.OpenSubKey(UninstallPath);
        if (uninstall is null)
            return;

        foreach (var subKeyName in uninstall.GetSubKeyNames())
        {
            try
            {
                using var entry = uninstall.OpenSubKey(subKeyName);
                if (entry is null)
                    continue;

                var name = entry.GetValue("DisplayName") as string;
                if (string.IsNullOrWhiteSpace(name) ||
                    entry.GetValue("SystemComponent") is int systemComponent && systemComponent == 1)
                    continue;

                var publisher = entry.GetValue("Publisher") as string;
                var installLocation = CleanPath(entry.GetValue("InstallLocation") as string);
                var executableName = ExtractExecutableName(entry.GetValue("DisplayIcon") as string);
                AddOrMerge(applications, name.Trim(), publisher, installLocation, executableName);
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }
    }

    [SupportedOSPlatform("windows")]
    private static void ReadAppPathEntries(
        RegistryKey baseKey,
        Dictionary<string, InstalledApplication> applications)
    {
        using var appPaths = baseKey.OpenSubKey(AppPathsPath);
        if (appPaths is null)
            return;

        foreach (var subKeyName in appPaths.GetSubKeyNames())
        {
            try
            {
                using var entry = appPaths.OpenSubKey(subKeyName);
                var executablePath = CleanPath(entry?.GetValue(null) as string);
                var executableName = Path.GetFileNameWithoutExtension(subKeyName);
                if (string.IsNullOrWhiteSpace(executableName))
                    continue;

                AddOrMerge(
                    applications,
                    executableName,
                    publisher: null,
                    installLocation: Path.GetDirectoryName(executablePath),
                    executableName);
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }
    }

    private static void AddOrMerge(
        Dictionary<string, InstalledApplication> applications,
        string name,
        string? publisher,
        string? installLocation,
        string? executableName)
    {
        var displayName = CleanDisplayName(name);
        var aliases = BuildAliases(displayName, installLocation, executableName);
        if (aliases.Count == 0)
            return;

        var key = $"{Normalize(displayName)}|{Normalize(executableName)}";
        applications[key] = new InstalledApplication(
            displayName,
            publisher,
            installLocation,
            executableName,
            aliases);
    }

    private static string CleanDisplayName(string name)
    {
        var cleaned = Regex.Replace(name.Trim(), @"^(Uninstall|Remove)\s+", "", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"\.exe$", "", RegexOptions.IgnoreCase);
        return cleaned.Trim();
    }

    private static HashSet<string> BuildAliases(
        string name,
        string? installLocation,
        string? executableName)
    {
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddAlias(aliases, name);
        AddAlias(aliases, Regex.Replace(name, @"\s*[\[(].*?[\])]\s*", " "));
        AddAlias(aliases, Regex.Replace(name, @"\s+v?\d+(?:[.\-_]\d+)*.*$", ""));
        AddAlias(aliases, executableName);

        if (!string.IsNullOrWhiteSpace(installLocation))
            AddAlias(aliases, Path.GetFileName(Path.TrimEndingDirectorySeparator(installLocation)));

        return aliases;
    }

    private static void AddAlias(HashSet<string> aliases, string? value)
    {
        var normalized = Normalize(value);
        if (normalized.Length >= 3)
            aliases.Add(normalized);
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private static string? ExtractExecutableName(string? displayIcon)
    {
        var path = CleanPath(displayIcon);
        return string.IsNullOrWhiteSpace(path) ? null : Path.GetFileNameWithoutExtension(path);
    }

    private static string? CleanPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var path = value.Trim().Trim('"');
        var iconIndex = path.LastIndexOf(',');
        if (iconIndex > 2 && int.TryParse(path[(iconIndex + 1)..], out _))
            path = path[..iconIndex].Trim().Trim('"');
        return Environment.ExpandEnvironmentVariables(path);
    }
}
