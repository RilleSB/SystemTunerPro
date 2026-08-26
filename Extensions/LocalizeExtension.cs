using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using DiskCleanerGUI.Avalonia.Services;
using System;

namespace DiskCleanerGUI.Avalonia.Extensions;

public class LocalizeExtension : MarkupExtension
{
    public string Key { get; set; } = "";

    public LocalizeExtension() { }
    public LocalizeExtension(string key) => Key = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new ReflectionBindingExtension($"[{Key}]")
        {
            Source = LocalizationService.Instance
        };
        return binding.ProvideValue(serviceProvider);
    }
}