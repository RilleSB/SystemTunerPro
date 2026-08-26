using Avalonia.Media;
using DiskCleanerGUI.Avalonia.Models;
using System.Text.Json;

namespace DiskCleanerGUI.Avalonia.Services;

public sealed class ThemeService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp"
    };

    private readonly string _themesFolder;

    public ThemeService()
    {
        _themesFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TrashClean",
            "Themes");
        Directory.CreateDirectory(_themesFolder);
        CreateDefaultThemes();
    }

    public List<Theme> GetAllThemes()
    {
        var themes = new List<(Theme Theme, DateTime CreatedAt)>();
        foreach (var file in Directory.EnumerateFiles(_themesFolder, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var theme = JsonSerializer.Deserialize<Theme>(File.ReadAllText(file));
                if (theme != null && TryValidateTheme(theme, out _))
                    themes.Add((theme, File.GetCreationTimeUtc(file)));
            }
            catch
            {
                // Повреждённая тема не должна ломать загрузку остальных тем.
            }
        }

        return themes
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => item.Theme)
            .ToList();
    }

    public void SaveTheme(Theme theme)
    {
        ValidateTheme(theme);
        var themePath = GetThemePath(theme);
        if (File.Exists(themePath))
            throw new IOException($"Тема «{theme.Name}» уже существует");

        if (theme.UseBackgroundImage)
        {
            if (string.IsNullOrWhiteSpace(theme.BackgroundImagePath) || !File.Exists(theme.BackgroundImagePath))
            {
                theme.UseBackgroundImage = false;
                theme.BackgroundImagePath = null;
            }
            else
            {
                var sourcePath = Path.GetFullPath(theme.BackgroundImagePath);
                var extension = Path.GetExtension(sourcePath);
                if (!ImageExtensions.Contains(extension))
                    throw new InvalidDataException("Неподдерживаемый формат фонового изображения");

                if (!IsInsideThemesFolder(sourcePath))
                {
                    var imageName = $"{Path.GetFileNameWithoutExtension(theme.FileName)}_background{extension.ToLowerInvariant()}";
                    var destinationPath = Path.Combine(_themesFolder, imageName);
                    File.Copy(sourcePath, destinationPath, overwrite: true);
                    theme.BackgroundImagePath = destinationPath;
                }
            }
        }

        WriteJsonAtomically(themePath, theme);
    }

    public void DeleteTheme(Theme theme)
    {
        var themePath = GetThemePath(theme);
        if (File.Exists(themePath))
            File.Delete(themePath);

        if (!string.IsNullOrWhiteSpace(theme.BackgroundImagePath))
        {
            var imagePath = Path.GetFullPath(theme.BackgroundImagePath);
            if (IsInsideThemesFolder(imagePath) && File.Exists(imagePath))
                File.Delete(imagePath);
        }
    }

    public void ExportTheme(Theme theme, string exportPath)
    {
        ValidateTheme(theme);
        var fullExportPath = Path.GetFullPath(exportPath);
        var exportDirectory = Path.GetDirectoryName(fullExportPath)
            ?? throw new InvalidOperationException("Не удалось определить папку экспорта");
        Directory.CreateDirectory(exportDirectory);

        var exportedTheme = CloneTheme(theme);
        exportedTheme.BackgroundImagePath = null;

        if (theme.UseBackgroundImage && !string.IsNullOrWhiteSpace(theme.BackgroundImagePath) && File.Exists(theme.BackgroundImagePath))
        {
            var extension = Path.GetExtension(theme.BackgroundImagePath);
            if (ImageExtensions.Contains(extension))
            {
                var imagePath = GetUniquePath(Path.Combine(
                    exportDirectory,
                    $"{Path.GetFileNameWithoutExtension(theme.FileName)}_background{extension.ToLowerInvariant()}"));
                File.Copy(theme.BackgroundImagePath, imagePath, overwrite: false);
                exportedTheme.BackgroundImagePath = Path.GetFileName(imagePath);
            }
            else
            {
                exportedTheme.UseBackgroundImage = false;
            }
        }

        WriteJsonAtomically(fullExportPath, exportedTheme);
    }

    public Theme? ImportTheme(string filePath)
    {
        try
        {
            var fullPath = Path.GetFullPath(filePath);
            var fileInfo = new FileInfo(fullPath);
            if (!fileInfo.Exists || fileInfo.Length > 1024 * 1024)
                return null;

            var theme = JsonSerializer.Deserialize<Theme>(File.ReadAllText(fullPath));
            if (theme == null || !TryValidateTheme(theme, out _))
                return null;

            if (theme.UseBackgroundImage)
            {
                var imageReference = theme.BackgroundImagePath;
                if (string.IsNullOrWhiteSpace(imageReference) ||
                    Path.IsPathRooted(imageReference) ||
                    !string.Equals(Path.GetFileName(imageReference), imageReference, StringComparison.Ordinal))
                {
                    return null;
                }

                var importDirectory = Path.GetDirectoryName(fullPath)!;
                var imagePath = Path.GetFullPath(Path.Combine(importDirectory, imageReference));
                if (!File.Exists(imagePath) || !ImageExtensions.Contains(Path.GetExtension(imagePath)))
                    return null;

                theme.BackgroundImagePath = imagePath;
            }

            SaveTheme(theme);
            return theme;
        }
        catch
        {
            return null;
        }
    }

    private string GetThemePath(Theme theme)
    {
        var path = Path.GetFullPath(Path.Combine(_themesFolder, theme.FileName));
        if (!IsInsideThemesFolder(path))
            throw new InvalidDataException("Недопустимое имя темы");
        return path;
    }

    private bool IsInsideThemesFolder(string path)
    {
        var relative = Path.GetRelativePath(_themesFolder, Path.GetFullPath(path));
        return !relative.Equals("..", StringComparison.Ordinal) &&
               !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
               !Path.IsPathRooted(relative);
    }

    private static void ValidateTheme(Theme theme)
    {
        if (!TryValidateTheme(theme, out var error))
            throw new InvalidDataException(error);
    }

    private static bool TryValidateTheme(Theme theme, out string error)
    {
        if (string.IsNullOrWhiteSpace(theme.Name) || theme.Name.Length > 80)
        {
            error = "Название темы должно содержать от 1 до 80 символов";
            return false;
        }

        if (string.IsNullOrWhiteSpace(theme.Author) || theme.Author.Length > 80 ||
            string.IsNullOrWhiteSpace(theme.Version) || theme.Version.Length > 30 ||
            theme.Description?.Length > 1000)
        {
            error = "У темы должны быть указаны автор и версия";
            return false;
        }

        try
        {
            Color.Parse(theme.PrimaryColor);
            Color.Parse(theme.SecondaryColor);
            Color.Parse(theme.AccentColor);
            Color.Parse(theme.BackgroundColor);
            Color.Parse(theme.TextColor);
            if (!string.IsNullOrWhiteSpace(theme.GradientStart)) Color.Parse(theme.GradientStart);
            if (!string.IsNullOrWhiteSpace(theme.GradientEnd)) Color.Parse(theme.GradientEnd);
        }
        catch
        {
            error = "Тема содержит некорректный цвет";
            return false;
        }

        error = "";
        return true;
    }

    private static Theme CloneTheme(Theme theme) => new()
    {
        Name = theme.Name,
        Author = theme.Author,
        Version = theme.Version,
        Description = theme.Description,
        PrimaryColor = theme.PrimaryColor,
        SecondaryColor = theme.SecondaryColor,
        AccentColor = theme.AccentColor,
        BackgroundColor = theme.BackgroundColor,
        TextColor = theme.TextColor,
        GradientStart = theme.GradientStart,
        GradientEnd = theme.GradientEnd,
        UseBackgroundImage = theme.UseBackgroundImage,
        BackgroundImagePath = theme.BackgroundImagePath
    };

    private static void WriteJsonAtomically(string path, Theme theme)
    {
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Не удалось определить папку файла");
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(theme, JsonOptions));
        File.Move(temporaryPath, path, overwrite: true);
    }

    private static string GetUniquePath(string path)
    {
        if (!File.Exists(path)) return path;
        var directory = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        for (var index = 1; ; index++)
        {
            var candidate = Path.Combine(directory, $"{name}_{index}{extension}");
            if (!File.Exists(candidate)) return candidate;
        }
    }

    private void CreateDefaultThemes()
    {
        var themes = new[]
        {
            CreateTheme("Темная", "Классическая темная тема", "#121212", "#1e3c72", "#2a5298"),
            CreateTheme("Светлая", "Светлая тема для дневной работы", "#FAFAFA", "#E3F2FD", "#BBDEFB"),
            CreateTheme("Неон", "Яркая неоновая тема", "#0D1117", "#ff006e", "#8338ec")
        };

        foreach (var theme in themes)
        {
            if (!File.Exists(GetThemePath(theme)))
                SaveTheme(theme);
        }
    }

    private static Theme CreateTheme(string name, string description, string background, string gradientStart, string gradientEnd) => new()
    {
        Name = name,
        Author = "TrashClean",
        Version = "1.0",
        Description = description,
        PrimaryColor = "#2196F3",
        SecondaryColor = "#1976D2",
        AccentColor = "#4CAF50",
        BackgroundColor = background,
        TextColor = name == "Светлая" ? "#212121" : "#FFFFFF",
        GradientStart = gradientStart,
        GradientEnd = gradientEnd
    };
}
