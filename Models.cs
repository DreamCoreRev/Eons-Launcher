using Newtonsoft.Json;

namespace EonsLauncher;

public class ClientManifest
{
    [JsonProperty("success")]      public bool   Success      { get; set; }
    [JsonProperty("version")]      public string Version      { get; set; } = "";
    [JsonProperty("generated_at")] public string GeneratedAt  { get; set; } = "";
    [JsonProperty("total_files")]  public int    TotalFiles   { get; set; }
    [JsonProperty("total_size")]   public long   TotalSize    { get; set; }
    [JsonProperty("total_size_mb")]public double TotalSizeMb  { get; set; }
    [JsonProperty("base_url")]     public string BaseUrl      { get; set; } = "";
    [JsonProperty("files")]        public List<ClientFile> Files { get; set; } = new();
}

public class ClientFile
{
    [JsonProperty("path")]     public string Path     { get; set; } = "";
    [JsonProperty("url")]      public string Url      { get; set; } = "";
    [JsonProperty("size")]     public long   Size     { get; set; }
    [JsonProperty("md5")]      public string Md5      { get; set; } = "";
    [JsonProperty("modified")] public long   Modified { get; set; }

    public string SizeDisplay => Size switch
    {
        >= 1_073_741_824 => $"{Size / 1_073_741_824.0:F1} Go",
        >= 1_048_576     => $"{Size / 1_048_576.0:F1} Mo",
        >= 1_024         => $"{Size / 1_024.0:F0} Ko",
        _                => $"{Size} o"
    };
}

public class NewsResponse
{
    [JsonProperty("success")]      public bool        Success     { get; set; }
    [JsonProperty("version_info")] public VersionInfo? VersionInfo { get; set; }
    [JsonProperty("news")]         public List<NewsItem> News      { get; set; } = new();
}

public class VersionInfo
{
    [JsonProperty("required_version")] public string RequiredVersion { get; set; } = "";
    [JsonProperty("launcher_version")] public string LauncherVersion { get; set; } = "";
    [JsonProperty("manifest_url")]     public string ManifestUrl     { get; set; } = "";
    [JsonProperty("website_url")]      public string WebsiteUrl      { get; set; } = "";
    [JsonProperty("register_url")]     public string RegisterUrl     { get; set; } = "";
    [JsonProperty("server_status")]    public string ServerStatus    { get; set; } = "unknown";
    [JsonProperty("online_players")]   public int    OnlinePlayers   { get; set; }
}

public class NewsItem
{
    [JsonProperty("id")]      public int     Id      { get; set; }
    [JsonProperty("title")]   public string  Title   { get; set; } = "";
    [JsonProperty("content")] public string  Content { get; set; } = "";
    [JsonProperty("date")]    public string  Date    { get; set; } = "";
    [JsonProperty("type")]    public string  Type    { get; set; } = "info";
    [JsonProperty("image")]   public string? Image   { get; set; }
}

// Convertisseur booléen inversé (utilisé dans le XAML)
public class InverseBoolConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => value is bool b && !b;
}
