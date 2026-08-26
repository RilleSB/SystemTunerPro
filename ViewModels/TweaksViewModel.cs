using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleanerGUI.Avalonia.Services;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiskCleanerGUI.Avalonia.ViewModels;

public partial class TweaksViewModel : LocalizedViewModelBase
{
    private readonly TweakBackupService _backupService = new();
    private TweakBackupState _backups = new();
    private string? _backupLoadError;

    [ObservableProperty] private string status = "";
    [ObservableProperty] private bool isWorking = false;
    [ObservableProperty] private bool isDefenderEnabled = true;
    [ObservableProperty] private bool isUpdateEnabled = true;
    [ObservableProperty] private bool isTelemetryEnabled = true;
    [ObservableProperty] private bool isCortanaEnabled = true;
    [ObservableProperty] private bool isOneDriveEnabled = true;
    [ObservableProperty] private bool isIndexingEnabled = true;
    [ObservableProperty] private bool isStartMenuAdsEnabled = true;
    [ObservableProperty] private bool isBackgroundAppsEnabled = true;
    [ObservableProperty] private bool hasDefenderBackup;
    [ObservableProperty] private bool hasWindowsUpdateBackup;
    [ObservableProperty] private bool hasPageFileBackup;

    // Localized properties
    public string DisableDefenderText => GetString("DisableDefender");
    public string EnableDefenderText => GetString("EnableDefender");
    public string DisableUpdateText => GetString("DisableUpdate");
    public string EnableUpdateText => GetString("EnableUpdate");
    public string DisableTelemetryText => GetString("DisableTelemetry");
    public string EnableTelemetryText => GetString("EnableTelemetry");
    public string DisableCortanaText => GetString("DisableCortana");
    public string EnableCortanaText => GetString("EnableCortana");
    public string OptimizeVisualText => GetString("OptimizeVisual");
    public string CleanStartupText => GetString("CleanStartup");
    public string CreateGodModeText => GetString("CreateGodMode");
    public string EnableDarkThemeText => GetString("EnableDarkTheme");
    public string DisableOneDriveText => GetString("DisableOneDrive");
    public string EnableOneDriveText => GetString("EnableOneDrive");
    public string DisableIndexingText => GetString("DisableIndexing");
    public string EnableIndexingText => GetString("EnableIndexing");
    public string DisableStartMenuAdsText => GetString("DisableStartMenuAds");
    public string EnableStartMenuAdsText => GetString("EnableStartMenuAds");
    public string DisableBackgroundAppsText => GetString("DisableBackgroundApps");
    public string EnableBackgroundAppsText => GetString("EnableBackgroundApps");
    public string SpeedUpAnimationsText => GetString("SpeedUpAnimations");
    public string FlushDnsText => GetString("FlushDns");
    public string OptimizeSsdText => GetString("OptimizeSsd");
    public string OptimizePageFileText => GetString("OptimizePageFile");
    public string DefenderActionText => HasDefenderBackup ? GetString("RestoreOriginal") : DisableDefenderText;
    public string UpdateActionText => HasWindowsUpdateBackup ? GetString("RestoreOriginal") : DisableUpdateText;
    public string PageFileActionText => HasPageFileBackup ? GetString("RestoreOriginal") : OptimizePageFileText;
    
    // Section headers
    public string PrivacySecurityText => GetString("PrivacySecurity");
    public string PerformanceText => GetString("Performance");
    public string CustomizationText => GetString("Customization");
    public string SystemOptimizationText => GetString("SystemOptimization");
    
    // Tooltips
    public string DefenderTooltipText => GetString("DefenderTooltip");
    public string UpdateTooltipText => GetString("UpdateTooltip");
    public string TelemetryTooltipText => GetString("TelemetryTooltip");
    public string CortanaTooltipText => GetString("CortanaTooltip");
    public string VisualTooltipText => GetString("VisualTooltip");
    public string StartupTooltipText => GetString("StartupTooltip");
    public string GodModeTooltipText => GetString("GodModeTooltip");
    public string DarkThemeTooltipText => GetString("DarkThemeTooltip");
    public string OneDriveTooltipText => GetString("OneDriveTooltip");
    public string IndexingTooltipText => GetString("IndexingTooltip");
    public string StartMenuAdsTooltipText => GetString("StartMenuAdsTooltip");
    public string BackgroundAppsTooltipText => GetString("BackgroundAppsTooltip");
    public string AnimationsTooltipText => GetString("AnimationsTooltip");
    public string DnsTooltipText => GetString("DnsTooltip");
    public string SsdTooltipText => GetString("SsdTooltip");
    public string PageFileTooltipText => GetString("PageFileTooltip");

    public TweaksViewModel()
    {
        Status = GetString("ReadyToWork");
        try
        {
            _backups = _backupService.Load();
            RefreshBackupStates();
        }
        catch (Exception ex)
        {
            _backupLoadError = ex.Message;
            Status = $"⚠️ {ex.Message}";
        }
        CheckCurrentStates();
    }

    protected override void OnLanguageChanged()
    {
        Status = GetString("ReadyToWork");
        OnPropertyChanged(nameof(DisableDefenderText));
        OnPropertyChanged(nameof(EnableDefenderText));
        OnPropertyChanged(nameof(DisableUpdateText));
        OnPropertyChanged(nameof(EnableUpdateText));
        OnPropertyChanged(nameof(DisableTelemetryText));
        OnPropertyChanged(nameof(EnableTelemetryText));
        OnPropertyChanged(nameof(DisableCortanaText));
        OnPropertyChanged(nameof(EnableCortanaText));
        OnPropertyChanged(nameof(OptimizeVisualText));
        OnPropertyChanged(nameof(CleanStartupText));
        OnPropertyChanged(nameof(CreateGodModeText));
        OnPropertyChanged(nameof(EnableDarkThemeText));
        OnPropertyChanged(nameof(DefenderTooltipText));
        OnPropertyChanged(nameof(UpdateTooltipText));
        OnPropertyChanged(nameof(TelemetryTooltipText));
        OnPropertyChanged(nameof(CortanaTooltipText));
        OnPropertyChanged(nameof(VisualTooltipText));
        OnPropertyChanged(nameof(StartupTooltipText));
        OnPropertyChanged(nameof(GodModeTooltipText));
        OnPropertyChanged(nameof(DarkThemeTooltipText));
        OnPropertyChanged(nameof(PrivacySecurityText));
        OnPropertyChanged(nameof(PerformanceText));
        OnPropertyChanged(nameof(CustomizationText));
        OnPropertyChanged(nameof(SystemOptimizationText));
        OnPropertyChanged(nameof(DisableOneDriveText));
        OnPropertyChanged(nameof(EnableOneDriveText));
        OnPropertyChanged(nameof(DisableIndexingText));
        OnPropertyChanged(nameof(EnableIndexingText));
        OnPropertyChanged(nameof(DisableStartMenuAdsText));
        OnPropertyChanged(nameof(EnableStartMenuAdsText));
        OnPropertyChanged(nameof(DisableBackgroundAppsText));
        OnPropertyChanged(nameof(EnableBackgroundAppsText));
        OnPropertyChanged(nameof(SpeedUpAnimationsText));
        OnPropertyChanged(nameof(FlushDnsText));
        OnPropertyChanged(nameof(OptimizeSsdText));
        OnPropertyChanged(nameof(OptimizePageFileText));
        OnPropertyChanged(nameof(DefenderActionText));
        OnPropertyChanged(nameof(UpdateActionText));
        OnPropertyChanged(nameof(PageFileActionText));
        OnPropertyChanged(nameof(OneDriveTooltipText));
        OnPropertyChanged(nameof(IndexingTooltipText));
        OnPropertyChanged(nameof(StartMenuAdsTooltipText));
        OnPropertyChanged(nameof(BackgroundAppsTooltipText));
        OnPropertyChanged(nameof(AnimationsTooltipText));
        OnPropertyChanged(nameof(DnsTooltipText));
        OnPropertyChanged(nameof(SsdTooltipText));
        OnPropertyChanged(nameof(PageFileTooltipText));
        OnPropertyChanged(nameof(Status));
    }

    partial void OnHasDefenderBackupChanged(bool value) => OnPropertyChanged(nameof(DefenderActionText));
    partial void OnHasWindowsUpdateBackupChanged(bool value) => OnPropertyChanged(nameof(UpdateActionText));
    partial void OnHasPageFileBackupChanged(bool value) => OnPropertyChanged(nameof(PageFileActionText));
    
    private void CheckCurrentStates()
    {
        try
        {
            using var defenderKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender");
            IsDefenderEnabled = defenderKey?.GetValue("DisableAntiSpyware") as int? != 1;

            using var updateKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\wuauserv");
            IsUpdateEnabled = Convert.ToInt32(updateKey?.GetValue("Start", 3)) != 4;
            
            using var telemetryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection");
            IsTelemetryEnabled = telemetryKey?.GetValue("AllowTelemetry") as int? != 0;
            
            using var cortanaKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search");
            IsCortanaEnabled = cortanaKey?.GetValue("AllowCortana") as int? != 0;
            
            using var oneDriveKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\OneDrive");
            IsOneDriveEnabled = oneDriveKey?.GetValue("DisableFileSyncNGSC") as int? != 1;
            
            using var indexingKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WSearch");
            IsIndexingEnabled = indexingKey?.GetValue("Start") as int? != 4;
            
            using var adsKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager");
            IsStartMenuAdsEnabled = adsKey?.GetValue("SystemPaneSuggestionsEnabled") as int? != 0;
            
            using var appsKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications");
            IsBackgroundAppsEnabled = appsKey?.GetValue("GlobalUserDisabled") as int? != 1;
        }
        catch { }
    }

    [RelayCommand]
    private async Task ToggleWindowsDefenderAsync()
    {
        if (IsWorking || !CanChangeProtectedTweaks()) return;
        IsWorking = true;
        Status = HasDefenderBackup
            ? "🛡️ Восстановление исходной политики Defender..."
            : "🛡️ Сохранение состояния и применение политики Defender...";
        
        try
        {
            await Task.Run(() =>
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender");
                if (key == null)
                    throw new UnauthorizedAccessException("Не удалось открыть раздел Defender для записи");

                if (!HasDefenderBackup)
                {
                    _backups.DefenderDisableAntiSpyware = CaptureDword(key, "DisableAntiSpyware");
                    _backupService.Save(_backups);
                    key.SetValue("DisableAntiSpyware", 1, RegistryValueKind.DWord);

                    if (Convert.ToInt32(key.GetValue("DisableAntiSpyware", 0)) != 1)
                        throw new InvalidOperationException("Windows не приняла новое значение политики Defender");
                }
                else
                {
                    var backup = _backups.DefenderDisableAntiSpyware!;
                    RestoreDword(key, "DisableAntiSpyware", backup);
                    _backups.DefenderDisableAntiSpyware = null;
                    try
                    {
                        _backupService.Save(_backups);
                    }
                    catch
                    {
                        _backups.DefenderDisableAntiSpyware = backup;
                        throw;
                    }
                }
            });

            RefreshBackupStates();
            CheckCurrentStates();
            Status = HasDefenderBackup
                ? "⚠️ Политика отключения Defender записана. Защита от изменений Windows может её проигнорировать"
                : "✅ Исходная политика Defender восстановлена";
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task ToggleWindowsUpdateAsync()
    {
        if (IsWorking || !CanChangeProtectedTweaks()) return;
        IsWorking = true;
        Status = HasWindowsUpdateBackup
            ? "⚙️ Восстановление исходного состояния Windows Update..."
            : "⚙️ Сохранение состояния и отключение Windows Update...";
        
        try
        {
            await Task.Run(async () =>
            {
                using var serviceKey = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\wuauserv", writable: true);
                if (serviceKey == null)
                    throw new UnauthorizedAccessException("Не удалось открыть настройки службы Windows Update");

                if (!HasWindowsUpdateBackup)
                {
                    _backups.WindowsUpdateStart = CaptureDword(serviceKey, "Start");
                    _backups.WindowsUpdateWasRunning = await IsServiceRunningAsync("wuauserv").ConfigureAwait(false);
                    _backupService.Save(_backups);

                    await RunScAsync("config", "wuauserv", "start=", "disabled").ConfigureAwait(false);
                    EnsureDwordValue(serviceKey, "Start", 4);
                    if (_backups.WindowsUpdateWasRunning == true)
                        await RunScAsync("stop", "wuauserv").ConfigureAwait(false);
                }
                else
                {
                    var startValue = _backups.WindowsUpdateStart!.Existed
                        ? _backups.WindowsUpdateStart.Value
                        : 3;
                    await RunScAsync("config", "wuauserv", "start=", GetScStartMode(startValue)).ConfigureAwait(false);
                    EnsureDwordValue(serviceKey, "Start", startValue);

                    var isRunning = await IsServiceRunningAsync("wuauserv").ConfigureAwait(false);
                    if (_backups.WindowsUpdateWasRunning == true && !isRunning)
                        await RunScAsync("start", "wuauserv").ConfigureAwait(false);
                    else if (_backups.WindowsUpdateWasRunning == false && isRunning)
                        await RunScAsync("stop", "wuauserv").ConfigureAwait(false);

                    var startBackup = _backups.WindowsUpdateStart;
                    var runningBackup = _backups.WindowsUpdateWasRunning;
                    _backups.WindowsUpdateStart = null;
                    _backups.WindowsUpdateWasRunning = null;
                    try
                    {
                        _backupService.Save(_backups);
                    }
                    catch
                    {
                        _backups.WindowsUpdateStart = startBackup;
                        _backups.WindowsUpdateWasRunning = runningBackup;
                        throw;
                    }
                }
            });

            RefreshBackupStates();
            CheckCurrentStates();
            Status = HasWindowsUpdateBackup
                ? "✅ Windows Update отключён, исходное состояние сохранено"
                : "✅ Исходное состояние Windows Update восстановлено";
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task ToggleTelemetryAsync()
    {
        if (IsWorking) return;
        IsWorking = true;
        Status = IsTelemetryEnabled ? "📊 Отключение телеметрии..." : "📊 Включение телеметрии...";
        
        try
        {
            await Task.Run(() =>
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection");
                using var key2 = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection");
                
                if (IsTelemetryEnabled)
                {
                    key?.SetValue("AllowTelemetry", 0, RegistryValueKind.DWord);
                    key2?.SetValue("AllowTelemetry", 0, RegistryValueKind.DWord);
                }
                else
                {
                    key?.DeleteValue("AllowTelemetry", false);
                    key2?.DeleteValue("AllowTelemetry", false);
                }
            });
            IsTelemetryEnabled = !IsTelemetryEnabled;
            Status = IsTelemetryEnabled ? "✅ Телеметрия включена" : "✅ Телеметрия отключена";
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task EnableGodModeAsync()
    {
        if (IsWorking) return;
        IsWorking = true;
        Status = "🔧 Создание папки God Mode...";
        
        try
        {
            await Task.Run(() =>
            {
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var godModeFolder = Path.Combine(desktop, "GodMode.{ED7BA470-8E54-465E-825C-99712043E01C}");
                Directory.CreateDirectory(godModeFolder);
            });
            Status = "✅ God Mode создан на рабочем столе";
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task ToggleCortanaAsync()
    {
        if (IsWorking) return;
        IsWorking = true;
        Status = IsCortanaEnabled ? "🎤 Отключение Cortana..." : "🎤 Включение Cortana...";
        
        try
        {
            await Task.Run(() =>
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search");
                if (IsCortanaEnabled)
                {
                    key?.SetValue("AllowCortana", 0, RegistryValueKind.DWord);
                }
                else
                {
                    key?.DeleteValue("AllowCortana", false);
                }
            });
            IsCortanaEnabled = !IsCortanaEnabled;
            Status = IsCortanaEnabled ? "✅ Cortana включена" : "✅ Cortana отключена";
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task OptimizeVisualEffectsAsync()
    {
        if (IsWorking) return;
        IsWorking = true;
        Status = "🎨 Оптимизация визуальных эффектов...";
        
        try
        {
            await Task.Run(() =>
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true);
                key?.SetValue("VisualFXSetting", 2, RegistryValueKind.DWord); // Best performance
            });
            Status = "✅ Визуальные эффекты оптимизированы";
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task CleanStartupAsync()
    {
        if (IsWorking) return;
        IsWorking = true;
        Status = "🚀 Очистка автозагрузки...";
        
        try
        {
            await Task.Run(() =>
            {
                Process.Start(new ProcessStartInfo("msconfig") { UseShellExecute = true });
            });
            Status = "✅ Открыт MSConfig для настройки автозагрузки";
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task EnableDarkThemeAsync()
    {
        if (IsWorking) return;
        IsWorking = true;
        Status = "🌙 Включение темной темы Windows...";
        
        try
        {
            await Task.Run(() =>
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", true);
                key?.SetValue("AppsUseLightTheme", 0, RegistryValueKind.DWord);
                key?.SetValue("SystemUsesLightTheme", 0, RegistryValueKind.DWord);
            });
            Status = "✅ Темная тема Windows включена";
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task ToggleOneDriveAsync()
    {
        if (IsWorking) return;
        IsWorking = true;
        Status = IsOneDriveEnabled ? "☁️ Отключение OneDrive..." : "☁️ Включение OneDrive...";
        
        try
        {
            await Task.Run(() =>
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\OneDrive");
                if (IsOneDriveEnabled)
                {
                    key?.SetValue("DisableFileSyncNGSC", 1, RegistryValueKind.DWord);
                }
                else
                {
                    key?.DeleteValue("DisableFileSyncNGSC", false);
                }
            });
            IsOneDriveEnabled = !IsOneDriveEnabled;
            Status = IsOneDriveEnabled ? "✅ OneDrive включен" : "✅ OneDrive отключен";
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task ToggleIndexingAsync()
    {
        if (IsWorking) return;
        IsWorking = true;
        Status = IsIndexingEnabled ? "🔍 Отключение индексации..." : "🔍 Включение индексации...";
        
        try
        {
            await Task.Run(() =>
            {
                if (IsIndexingEnabled)
                {
                    Process.Start(new ProcessStartInfo("sc", "config WSearch start= disabled") { UseShellExecute = false, CreateNoWindow = true })?.WaitForExit();
                    Process.Start(new ProcessStartInfo("sc", "stop WSearch") { UseShellExecute = false, CreateNoWindow = true })?.WaitForExit();
                }
                else
                {
                    Process.Start(new ProcessStartInfo("sc", "config WSearch start= auto") { UseShellExecute = false, CreateNoWindow = true })?.WaitForExit();
                    Process.Start(new ProcessStartInfo("sc", "start WSearch") { UseShellExecute = false, CreateNoWindow = true })?.WaitForExit();
                }
            });
            IsIndexingEnabled = !IsIndexingEnabled;
            Status = IsIndexingEnabled ? "✅ Индексация включена" : "✅ Индексация отключена";
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task ToggleStartMenuAdsAsync()
    {
        if (IsWorking) return;
        IsWorking = true;
        Status = IsStartMenuAdsEnabled ? "📢 Отключение рекламы в Пуск..." : "📢 Включение рекламы в Пуск...";
        
        try
        {
            await Task.Run(() =>
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", true);
                if (IsStartMenuAdsEnabled)
                {
                    key?.SetValue("SystemPaneSuggestionsEnabled", 0, RegistryValueKind.DWord);
                    key?.SetValue("SilentInstalledAppsEnabled", 0, RegistryValueKind.DWord);
                    key?.SetValue("PreInstalledAppsEnabled", 0, RegistryValueKind.DWord);
                }
                else
                {
                    key?.SetValue("SystemPaneSuggestionsEnabled", 1, RegistryValueKind.DWord);
                    key?.SetValue("SilentInstalledAppsEnabled", 1, RegistryValueKind.DWord);
                    key?.SetValue("PreInstalledAppsEnabled", 1, RegistryValueKind.DWord);
                }
            });
            IsStartMenuAdsEnabled = !IsStartMenuAdsEnabled;
            Status = IsStartMenuAdsEnabled ? "✅ Реклама в Пуск включена" : "✅ Реклама в Пуск отключена";
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task ToggleBackgroundAppsAsync()
    {
        if (IsWorking) return;
        IsWorking = true;
        Status = IsBackgroundAppsEnabled ? "📱 Отключение фоновых приложений..." : "📱 Включение фоновых приложений...";
        
        try
        {
            await Task.Run(() =>
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", true);
                if (IsBackgroundAppsEnabled)
                {
                    key?.SetValue("GlobalUserDisabled", 1, RegistryValueKind.DWord);
                }
                else
                {
                    key?.DeleteValue("GlobalUserDisabled", false);
                }
            });
            IsBackgroundAppsEnabled = !IsBackgroundAppsEnabled;
            Status = IsBackgroundAppsEnabled ? "✅ Фоновые приложения включены" : "✅ Фоновые приложения отключены";
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task SpeedUpAnimationsAsync()
    {
        if (IsWorking) return;
        IsWorking = true;
        Status = "⚡ Ускорение анимаций...";
        
        try
        {
            await Task.Run(() =>
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
                key?.SetValue("MenuShowDelay", "100", RegistryValueKind.String);
                key?.SetValue("WaitToKillAppTimeout", "2000", RegistryValueKind.String);
                key?.SetValue("HungAppTimeout", "1000", RegistryValueKind.String);
                key?.SetValue("AutoEndTasks", "1", RegistryValueKind.String);
            });
            Status = "✅ Анимации ускорены";
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task FlushDnsAsync()
    {
        if (IsWorking) return;
        IsWorking = true;
        Status = "🌐 Очистка DNS кэша...";
        
        try
        {
            await Task.Run(() =>
            {
                Process.Start(new ProcessStartInfo("ipconfig", "/flushdns") { UseShellExecute = false, CreateNoWindow = true })?.WaitForExit();
            });
            Status = "✅ DNS кэш очищен";
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task OptimizeSsdAsync()
    {
        if (IsWorking) return;
        IsWorking = true;
        Status = "💾 Оптимизация SSD...";
        
        try
        {
            await Task.Run(() =>
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem", true);
                key?.SetValue("DisableLastAccessUpdate", 1, RegistryValueKind.DWord);
                
                // Отключение дефрагментации для SSD
                Process.Start(new ProcessStartInfo("schtasks", "/Change /TN \"Microsoft\\Windows\\Defrag\\ScheduledDefrag\" /Disable") { UseShellExecute = false, CreateNoWindow = true })?.WaitForExit();
            });
            Status = "✅ SSD оптимизирован";
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task OptimizePageFileAsync()
    {
        if (IsWorking || !CanChangeProtectedTweaks()) return;
        IsWorking = true;
        Status = HasPageFileBackup
            ? "📄 Восстановление исходных параметров памяти..."
            : "📄 Сохранение состояния и применение параметров памяти...";
        
        try
        {
            await Task.Run(() =>
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true);
                if (key == null)
                    throw new UnauthorizedAccessException("Не удалось открыть параметры управления памятью");

                if (!HasPageFileBackup)
                {
                    _backups.ClearPageFileAtShutdown = CaptureDword(key, "ClearPageFileAtShutdown");
                    _backups.DisablePagingExecutive = CaptureDword(key, "DisablePagingExecutive");
                    _backups.LargeSystemCache = CaptureDword(key, "LargeSystemCache");
                    _backupService.Save(_backups);

                    key.SetValue("ClearPageFileAtShutdown", 0, RegistryValueKind.DWord);
                    key.SetValue("DisablePagingExecutive", 1, RegistryValueKind.DWord);
                    key.SetValue("LargeSystemCache", 0, RegistryValueKind.DWord);
                    EnsureDwordValue(key, "ClearPageFileAtShutdown", 0);
                    EnsureDwordValue(key, "DisablePagingExecutive", 1);
                    EnsureDwordValue(key, "LargeSystemCache", 0);
                }
                else
                {
                    var clearBackup = _backups.ClearPageFileAtShutdown!;
                    var pagingBackup = _backups.DisablePagingExecutive!;
                    var cacheBackup = _backups.LargeSystemCache!;
                    RestoreDword(key, "ClearPageFileAtShutdown", clearBackup);
                    RestoreDword(key, "DisablePagingExecutive", pagingBackup);
                    RestoreDword(key, "LargeSystemCache", cacheBackup);
                    _backups.ClearPageFileAtShutdown = null;
                    _backups.DisablePagingExecutive = null;
                    _backups.LargeSystemCache = null;
                    try
                    {
                        _backupService.Save(_backups);
                    }
                    catch
                    {
                        _backups.ClearPageFileAtShutdown = clearBackup;
                        _backups.DisablePagingExecutive = pagingBackup;
                        _backups.LargeSystemCache = cacheBackup;
                        throw;
                    }
                }
            });

            RefreshBackupStates();
            Status = HasPageFileBackup
                ? "⚠️ Параметры памяти изменены; для применения нужна перезагрузка"
                : "✅ Исходные параметры памяти восстановлены; нужна перезагрузка";
        }
        catch (Exception ex)
        {
            Status = $"❌ Ошибка: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }

    private bool CanChangeProtectedTweaks()
    {
        if (!string.IsNullOrEmpty(_backupLoadError))
        {
            Status = $"❌ {_backupLoadError}. Исправьте резервный файл перед изменением твиков";
            return false;
        }

        if (!AdminRightsService.IsRunningAsAdmin())
        {
            Status = "⚠️ Для этого действия запустите программу от имени администратора";
            return false;
        }

        return true;
    }

    private void RefreshBackupStates()
    {
        HasDefenderBackup = _backups.HasDefenderBackup;
        HasWindowsUpdateBackup = _backups.HasWindowsUpdateBackup;
        HasPageFileBackup = _backups.HasPageFileBackup;
    }

    private static RegistryDwordBackup CaptureDword(RegistryKey key, string valueName)
    {
        var value = key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        return new RegistryDwordBackup
        {
            Existed = value != null,
            Value = value == null ? 0 : Convert.ToInt32(value)
        };
    }

    private static void RestoreDword(RegistryKey key, string valueName, RegistryDwordBackup backup)
    {
        if (backup.Existed)
        {
            key.SetValue(valueName, backup.Value, RegistryValueKind.DWord);
            EnsureDwordValue(key, valueName, backup.Value);
        }
        else
        {
            key.DeleteValue(valueName, throwOnMissingValue: false);
            if (key.GetValue(valueName) != null)
                throw new InvalidOperationException($"Не удалось удалить исходно отсутствовавшее значение {valueName}");
        }
    }

    private static void EnsureDwordValue(RegistryKey key, string valueName, int expectedValue)
    {
        var actualValue = key.GetValue(valueName);
        if (actualValue == null || Convert.ToInt32(actualValue) != expectedValue)
            throw new InvalidOperationException($"Windows не применила значение {valueName}");
    }

    private static async Task<bool> IsServiceRunningAsync(string serviceName)
    {
        var result = await RunProcessAsync("sc.exe", "query", serviceName).ConfigureAwait(false);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Не удалось прочитать состояние службы {serviceName}: {result.Error}");

        var match = Regex.Match(result.Output, @"(?m)^\s*[^:]+:\s*([1-7])\s+");
        if (!match.Success)
            throw new InvalidOperationException($"Не удалось распознать состояние службы {serviceName}");

        return match.Groups[1].Value == "4";
    }

    private static async Task RunScAsync(params string[] arguments)
    {
        var result = await RunProcessAsync("sc.exe", arguments).ConfigureAwait(false);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Команда sc завершилась с кодом {result.ExitCode}: {result.Error}");
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunProcessAsync(
        string fileName,
        params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Не удалось запустить {fileName}");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().ConfigureAwait(false);
        return (process.ExitCode, await outputTask.ConfigureAwait(false), await errorTask.ConfigureAwait(false));
    }

    private static string GetScStartMode(int startValue) => startValue switch
    {
        2 => "auto",
        3 => "demand",
        4 => "disabled",
        _ => throw new InvalidOperationException($"Неподдерживаемый исходный тип запуска службы: {startValue}")
    };
}
