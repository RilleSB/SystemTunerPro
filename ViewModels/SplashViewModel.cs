using CommunityToolkit.Mvvm.ComponentModel;
using DiskCleanerGUI.Avalonia.Services;
using DiskCleanerGUI.Avalonia.Views;
using System.Threading.Tasks;
using System;

namespace DiskCleanerGUI.Avalonia.ViewModels;

/// <summary>
/// ViewModel для Splash Screen с корутинами - координирует параллельную загрузку и анимацию
/// Запускает реальную загрузку приложения синхронно с показом прогресса
/// </summary>
public partial class SplashViewModel : ViewModelBase
{
    [ObservableProperty] private string loadingText = "Инициализация...";
    [ObservableProperty] private int progress = 0;
    [ObservableProperty] private bool isVisible = true;

    private readonly ApplicationLoaderService _loaderService = new();

    /// <summary>
    /// Быстрая загрузка приложения без лишних задержек
    /// </summary>
    public async Task<MainWindow> StartLoadingWithRealProgressAsync()
    {
        try
        {
            // Создаем репортер прогресса для синхронизации UI
            var progressReporter = new Progress<(int progress, string text)>(update =>
            {
                Progress = update.progress;
                LoadingText = update.text;
            });

            // Загружаем приложение
            var mainWindow = await _loaderService.LoadApplicationAsync(progressReporter);
            
            // Скрываем заставку
            IsVisible = false;
            
            return mainWindow;
        }
        catch (Exception ex)
        {
            LoadingText = $"Ошибка: {ex.Message}";
            throw;
        }
    }

    /// <summary>
    /// Устаревший метод - оставлен для совместимости
    /// Используйте StartLoadingWithRealProgressAsync() для корутин
    /// </summary>
    [Obsolete("Use StartLoadingWithRealProgressAsync() for coroutine-based loading")]
    public async Task StartLoadingSequenceAsync()
    {
        await StartLoadingWithRealProgressAsync();
    }
}