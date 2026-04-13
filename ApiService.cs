using System.Net.Http;
using Newtonsoft.Json;

namespace EonsLauncher;

public class ApiService : IDisposable
{
    private readonly HttpClient _http;

    public ApiService()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _http.DefaultRequestHeaders.Add("User-Agent", $"EonsLauncher/{LauncherConfig.LauncherVersion}");
    }

    public async Task<ClientManifest?> FetchManifestAsync(string? manifestUrl = null)
    {
        var url = manifestUrl ?? LauncherConfig.ManifestUrl;
        try
        {
            var json = await _http.GetStringAsync(url);
            return JsonConvert.DeserializeObject<ClientManifest>(json);
        }
        catch (Exception ex)
        {
            throw new Exception($"Impossible de récupérer le manifest depuis {url}\n{ex.Message}", ex);
        }
    }

    public async Task<NewsResponse?> FetchNewsAsync()
    {
        try
        {
            var json = await _http.GetStringAsync(LauncherConfig.NewsApiUrl);
            return JsonConvert.DeserializeObject<NewsResponse>(json);
        }
        catch
        {
            return new NewsResponse
            {
                Success = false,
                VersionInfo = new VersionInfo { ServerStatus = "unknown" },
                News = new List<NewsItem>
                {
                    new() {
                        Id = 0, Title = "Connexion impossible",
                        Content = "Impossible de contacter le serveur. Vérifiez votre connexion internet.",
                        Date = DateTime.Now.ToString("yyyy-MM-dd"), Type = "warning"
                    }
                }
            };
        }
    }

    public void Dispose() => _http.Dispose();
}
