using Avalonia.Controls;
using DiskCleanerGUI.Avalonia.Services;

namespace DiskCleanerGUI.Avalonia.Views;

public partial class UtilitiesView : UserControl
{
    public UtilitiesView()
    {
        InitializeComponent();
        LocalizationService.Instance.LanguageChanged += UpdateLocalization;
        UpdateLocalization();
    }
    
    private void UpdateLocalization()
    {
        var loc = LocalizationService.Instance;
        
        // Update text blocks and buttons based on their names
        if (this.FindControl<TextBlock>("SystemMonitoringTitle") is TextBlock sysMonTitle)
            sysMonTitle.Text = loc.GetString("SystemMonitoring");
        if (this.FindControl<TextBlock>("ProcessorLabel") is TextBlock procLabel)
            procLabel.Text = loc.GetString("Processor");
        if (this.FindControl<TextBlock>("MemoryLabel") is TextBlock memLabel)
            memLabel.Text = loc.GetString("Memory");
        if (this.FindControl<TextBlock>("DisksLabel") is TextBlock disksLabel)
            disksLabel.Text = loc.GetString("Disks");
            
        if (this.FindControl<TextBlock>("QuickUtilitiesTitle") is TextBlock quickUtilTitle)
            quickUtilTitle.Text = loc.GetString("QuickUtilities");
        if (this.FindControl<Button>("RestartExplorerBtn") is Button restartBtn)
            restartBtn.Content = loc.GetString("RestartExplorer");
        if (this.FindControl<Button>("FreeMemoryBtn") is Button freeMemBtn)
            freeMemBtn.Content = loc.GetString("FreeMemory");
        if (this.FindControl<Button>("FlushDnsBtn") is Button flushDnsBtn)
            flushDnsBtn.Content = loc.GetString("FlushDns");
        if (this.FindControl<Button>("ResetNetworkBtn") is Button resetNetBtn)
            resetNetBtn.Content = loc.GetString("ResetNetwork");
            
        if (this.FindControl<TextBlock>("ShutdownTimerTitle") is TextBlock shutdownTitle)
            shutdownTitle.Text = loc.GetString("ShutdownTimer");
        if (this.FindControl<TextBlock>("ShutdownInLabel") is TextBlock shutdownInLabel)
            shutdownInLabel.Text = loc.GetString("ShutdownIn");
        if (this.FindControl<TextBlock>("MinutesLabel") is TextBlock minutesLabel)
            minutesLabel.Text = loc.GetString("Minutes");
        if (this.FindControl<Button>("SetTimerBtn") is Button setTimerBtn)
            setTimerBtn.Content = loc.GetString("SetTimer");
        if (this.FindControl<Button>("CancelTimerBtn") is Button cancelTimerBtn)
            cancelTimerBtn.Content = loc.GetString("CancelTimer");
    }
}