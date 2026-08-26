using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace DiskCleanerGUI.Avalonia.Services;

public sealed class GitHubUpdateService
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/RilleSB/SystemTunerPro/releases/latest";

    private static readonly HttpClient HttpClient = CreateHttpClient();

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUrl);
        using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"GitHub вернул код {(int)response.StatusCode}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var root = document.RootElement;
        var tagName = root.GetProperty("tag_name").GetString();
        var releaseUrl = root.GetProperty("html_url").GetString();

        if (string.IsNullOrWhiteSpace(tagName) || string.IsNullOrWhiteSpace(releaseUrl))
        {
            throw new InvalidOperationException("GitHub вернул релиз без версии или ссылки");
        }

        if (!Version.TryParse(tagName.TrimStart('v', 'V'), out var latestVersion))
        {
            throw new InvalidOperationException($"Некорректная версия релиза: {tagName}");
        }

        var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0, 0);
        return new UpdateCheckResult(currentVersion, latestVersion, tagName, new Uri(releaseUrl));
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SystemTunerPro", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }
}

public sealed record UpdateCheckResult(Version CurrentVersion, Version LatestVersion, string LatestTag, Uri ReleaseUrl)
{
    public bool IsUpdateAvailable => LatestVersion > CurrentVersion;
}
