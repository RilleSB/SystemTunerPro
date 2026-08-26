using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.IO;

namespace DiskCleanerGUI.Avalonia.ViewModels;

public partial class BackgroundViewModel : ViewModelBase
{
    [ObservableProperty] private string mode = "gradient"; // gradient|color|image
    [ObservableProperty] private string colorHex = "#15203B";
    [ObservableProperty] private string? imagePath;
    [ObservableProperty] private IBrush backgroundBrush = new SolidColorBrush(Colors.Transparent);

    [RelayCommand]
    private void Apply() => UpdateBrush();

    [RelayCommand]
    private void SetOcean()
    {
        BackgroundBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = { new GradientStop(Color.Parse("#667eea"), 0), new GradientStop(Color.Parse("#764ba2"), 1) }
        };
    }

    [RelayCommand]
    private void SetSunset()
    {
        BackgroundBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = { new GradientStop(Color.Parse("#f093fb"), 0), new GradientStop(Color.Parse("#f5576c"), 1) }
        };
    }

    [RelayCommand]
    private void SetForest()
    {
        BackgroundBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = { new GradientStop(Color.Parse("#56ab2f"), 0), new GradientStop(Color.Parse("#a8e6cf"), 1) }
        };
    }

    [RelayCommand]
    private void SetSpace()
    {
        BackgroundBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = { new GradientStop(Color.Parse("#2c3e50"), 0), new GradientStop(Color.Parse("#4a6741"), 1) }
        };
    }

    [RelayCommand]
    private void SetFire()
    {
        BackgroundBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = { new GradientStop(Color.Parse("#ff6b6b"), 0), new GradientStop(Color.Parse("#ffa726"), 1) }
        };
    }

    [RelayCommand]
    private void SetIce()
    {
        BackgroundBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = { new GradientStop(Color.Parse("#74b9ff"), 0), new GradientStop(Color.Parse("#0984e3"), 1) }
        };
    }

    partial void OnModeChanged(string value) => UpdateBrush();
    partial void OnColorHexChanged(string value) => UpdateBrush();
    partial void OnImagePathChanged(string? value) => UpdateBrush();

    private void UpdateBrush()
    {
        try
        {
            switch (Mode)
            {
                case "color":
                    BackgroundBrush = new SolidColorBrush(Color.Parse(string.IsNullOrWhiteSpace(ColorHex) ? "#222" : ColorHex));
                    break;
                case "image":
                    if (!string.IsNullOrWhiteSpace(ImagePath) && File.Exists(ImagePath))
                    {
                        var bmp = new Bitmap(ImagePath);
                        BackgroundBrush = new ImageBrush(bmp) { Stretch = Stretch.UniformToFill, AlignmentX = AlignmentX.Center, AlignmentY = AlignmentY.Center };
                    }
                    else BackgroundBrush = DefaultGradient();
                    break;
                default:
                    BackgroundBrush = DefaultGradient();
                    break;
            }
        }
        catch
        {
            BackgroundBrush = DefaultGradient();
        }
    }

    private static LinearGradientBrush DefaultGradient() => new()
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint   = new RelativePoint(1, 1, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.Parse("#15203B"), 0),
            new GradientStop(Color.Parse("#0A0F1E"), 1),
        }
    };
}
