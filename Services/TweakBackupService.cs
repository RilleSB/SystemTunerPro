using System.Text.Json;

namespace DiskCleanerGUI.Avalonia.Services;

public sealed class TweakBackupService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _backupFile;

    public TweakBackupService()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TrashClean");
        Directory.CreateDirectory(directory);
        _backupFile = Path.Combine(directory, "tweak-backups.json");
    }

    public TweakBackupState Load()
    {
        if (!File.Exists(_backupFile))
            return new TweakBackupState();

        try
        {
            return JsonSerializer.Deserialize<TweakBackupState>(File.ReadAllText(_backupFile))
                   ?? new TweakBackupState();
        }
        catch (Exception ex)
        {
            throw new InvalidDataException("Файл резервных значений твиков повреждён", ex);
        }
    }

    public void Save(TweakBackupState state)
    {
        var directory = Path.GetDirectoryName(_backupFile)!;
        var temporaryPath = Path.Combine(directory, $"tweak-backups.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(state, JsonOptions));
        File.Move(temporaryPath, _backupFile, overwrite: true);
    }
}

public sealed class TweakBackupState
{
    public RegistryDwordBackup? DefenderDisableAntiSpyware { get; set; }
    public RegistryDwordBackup? WindowsUpdateStart { get; set; }
    public bool? WindowsUpdateWasRunning { get; set; }
    public RegistryDwordBackup? ClearPageFileAtShutdown { get; set; }
    public RegistryDwordBackup? DisablePagingExecutive { get; set; }
    public RegistryDwordBackup? LargeSystemCache { get; set; }

    public bool HasDefenderBackup => DefenderDisableAntiSpyware != null;
    public bool HasWindowsUpdateBackup => WindowsUpdateStart != null && WindowsUpdateWasRunning.HasValue;
    public bool HasPageFileBackup =>
        ClearPageFileAtShutdown != null && DisablePagingExecutive != null && LargeSystemCache != null;
}

public sealed class RegistryDwordBackup
{
    public bool Existed { get; set; }
    public int Value { get; set; }
}
