using System.IO;

namespace EonsLauncher;

public static class LauncherConfig
{
    public const string NewsApiUrl   = "https://eons-world.eu/eons_launcher/news.php";
    public const string ManifestUrl  = "https://eons-world.eu/eons_launcher/manifest.php";

    public const string WebsiteUrl  = "https://eons-world.eu/";
    public const string RegisterUrl = "https://eons-world.eu/auth.php";

    public static string ClientPath
    {
        get => _clientPath ?? DefaultClientPath;
        set => _clientPath = value;
    }
    private static string? _clientPath;

    public static string DefaultClientPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Eons", "Client");

    public static string ConfigFilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EonsLauncher", "config.json");

    public const int    MaxConcurrentDownloads = 3;
    public const int    HttpTimeoutSeconds      = 30;
    public const int    DownloadBufferSize      = 65536;
    public const int    MaxRetries              = 3;

    public const string LauncherVersion = "1.0.0";
    public const string ServerName      = "Eons World";
    public const string GameVersion     = "WoW 3.3.5a";
    public const string GameExecutable  = "Eons.exe";
}
