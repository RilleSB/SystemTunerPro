using Avalonia.Controls;
using DiskCleanerGUI.Avalonia.ViewModels;
using System.Threading.Tasks;
using System;

namespace DiskCleanerGUI.Avalonia.Views;

/// <summary>
/// Splash Screen - окно заставки с анимацией загрузки
/// Показывается при запуске приложения пока грузятся сервисы
/// </summary>
public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        DataContext = new SplashViewModel();
    }

    /// <summary>
    /// Корутинный метод: запускает параллельную загрузку и возвращает готовое главное окно
    /// </summary>
    public async Task<MainWindow> StartLoadingWithCoroutinesAsync()
    {
        if (DataContext is SplashViewModel viewModel)
        {
            return await viewModel.StartLoadingWithRealProgressAsync();
        }
        throw new InvalidOperationException("SplashViewModel not found in DataContext");
    }
    
    /// <summary>
    /// Альтернативный метод для совместимости
    /// </summary>
    public async Task<MainWindow> StartLoadingWithRealProgressAsync()
    {
        return await StartLoadingWithCoroutinesAsync();
    }

    /// <summary>
    /// Устаревший метод - оставлен для совместимости
    /// </summary>
    [Obsolete("Use StartLoadingWithCoroutinesAsync() for better performance")]
    public async Task StartLoadingAsync()
    {
        await StartLoadingWithCoroutinesAsync();
    }
}