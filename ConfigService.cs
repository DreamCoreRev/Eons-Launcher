using System.IO;
using Newtonsoft.Json;

namespace EonsLauncher;

public class ConfigService
{
    private LauncherSettings _settings = new();
    public LauncherSettings Settings => _settings;

    public void Load()
    {
        try
        {
            if (!File.Exists(LauncherConfig.ConfigFilePath)) return;
            var json = File.ReadAllText(LauncherConfig.ConfigFilePath);
            _settings = JsonConvert.DeserializeObject<LauncherSettings>(json) ?? new LauncherSettings();
        }
        catch { _settings = new LauncherSettings(); }
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(LauncherConfig.ConfigFilePath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(LauncherConfig.ConfigFilePath,
                JsonConvert.SerializeObject(_settings, Formatting.Indented));
        }
        catch { }
    }
}

public class LauncherSettings
{
    [JsonProperty("client_path")]      public string   ClientPath { get; set; } = LauncherConfig.DefaultClientPath;
    [JsonProperty("last_check")]       public DateTime? LastCheck  { get; set; }
    [JsonProperty("auto_update")]      public bool     AutoUpdate { get; set; } = true;
    [JsonProperty("remember_account")] public bool     RememberAccount { get; set; } = false;
}
