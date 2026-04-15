using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace EonsLauncher;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ApiService      _api      = new();
    private readonly DownloadService _download = new();
    private readonly ConfigService   _config   = new();

    private string _statusText = "Initialisation...";
    public  string StatusText  { get => _statusText;  set => SetProp(ref _statusText, value); }

    private string _subStatusText = "";
    public  string SubStatusText  { get => _subStatusText; set => SetProp(ref _subStatusText, value); }

    private double _globalProgress;
    public  double GlobalProgress { get => _globalProgress; set => SetProp(ref _globalProgress, value); }

    private string _progressText = "";
    public  string ProgressText  { get => _progressText; set => SetProp(ref _progressText, value); }

    // Vitesse de téléchargement affichée dans la barre du bas (ex : "3,42 Mo/s")
    private string _speedText = "";
    public  string SpeedText  { get => _speedText; set => SetProp(ref _speedText, value); }

    private bool _isProgressVisible;
    public  bool IsProgressVisible { get => _isProgressVisible; set => SetProp(ref _isProgressVisible, value); }

    private bool _canPlay;
    public  bool CanPlay       { get => _canPlay;       set => SetProp(ref _canPlay, value); }

    private bool _canUpdate;
    public  bool CanUpdate     { get => _canUpdate;     set => SetProp(ref _canUpdate, value); }

    private bool _isDownloading;
    public  bool IsDownloading { get => _isDownloading; set => SetProp(ref _isDownloading, value); }

    private string _serverStatus = "Vérification...";
    public  string ServerStatus  { get => _serverStatus;  set => SetProp(ref _serverStatus, value); }

    private string _onlinePlayers = "---";
    public  string OnlinePlayers  { get => _onlinePlayers; set => SetProp(ref _onlinePlayers, value); }

    private bool _isServerOnline;
    public  bool IsServerOnline   { get => _isServerOnline; set => SetProp(ref _isServerOnline, value); }

    public ObservableCollection<NewsItem> News { get; } = new();
    public ObservableCollection<string>   Logs { get; } = new();

    private string _clientPath = "";
    public  string ClientPath
    {
        get => _clientPath;
        set
        {
            if (SetProp(ref _clientPath, value))
            {
                _config.Settings.ClientPath = value;
                LauncherConfig.ClientPath   = value;
            }
        }
    }

    public MainViewModel()
    {
        _download.OnStatusChanged   += msg => Dispatch(() => SubStatusText = msg);
        _download.OnError           += msg => Dispatch(() => AddLog($"❌ {msg}"));
        _download.OnProgressChanged += (cur, tot, pct) => Dispatch(() =>
        {
            GlobalProgress = pct;
            ProgressText   = $"{cur} / {tot} fichiers  ({pct:F0}%)";
        });
        // Mise à jour de la vitesse depuis le timer interne du DownloadService
        _download.OnSpeedChanged += speed => Dispatch(() => SpeedText = speed);

        _config.Load();
        ClientPath = _config.Settings.ClientPath;
    }

    public async Task InitializeAsync()
    {
        StatusText = "Connexion au serveur...";
        await LoadNewsAsync();
        StatusText = "Vérification des fichiers...";
        await CheckFilesAsync();
    }

    private async Task LoadNewsAsync()
    {
        try
        {
            var response = await _api.FetchNewsAsync();
            if (response == null) return;

            if (response.VersionInfo != null)
            {
                IsServerOnline = response.VersionInfo.ServerStatus == "online";
                ServerStatus   = IsServerOnline ? "En ligne" : "Hors ligne";
                OnlinePlayers  = response.VersionInfo.OnlinePlayers > 0
                    ? $"{response.VersionInfo.OnlinePlayers} joueurs" : "---";
            }

            Dispatch(() => {
                News.Clear();
                foreach (var item in response.News) News.Add(item);
            });
        }
        catch (Exception ex)
        {
            ServerStatus = "Erreur réseau";
            AddLog($"Erreur chargement news : {ex.Message}");
        }
    }

    private List<ClientFile>? _filesToDownload;

    public async Task CheckFilesAsync()
    {
        if (IsDownloading) return;

        CanPlay    = false;
        CanUpdate  = false;
        StatusText = "Récupération du manifest...";

        try
        {
            var manifest = await _api.FetchManifestAsync();
            if (manifest == null || !manifest.Success)
            {
                StatusText = "Erreur : manifest invalide";
                return;
            }

            StatusText = $"Vérification de {manifest.TotalFiles} fichiers...";
            var progress = new Progress<string>(msg => SubStatusText = msg);
            _filesToDownload = await Task.Run(() =>
                _download.GetFilesToDownload(manifest.Files, ClientPath, progress));

            SubStatusText = "";

            if (_filesToDownload.Count == 0)
            {
                StatusText = "✓ Client à jour - Prêt à jouer !";
                CanPlay    = true;
            }
            else
            {
                var totalMb = _filesToDownload.Sum(f => f.Size) / 1_048_576.0;
                StatusText  = $"{_filesToDownload.Count} fichier(s) à télécharger ({totalMb:F0} Mo)";
                CanUpdate   = true;
                CanPlay     = File.Exists(Path.Combine(ClientPath, LauncherConfig.GameExecutable));
            }
        }
        catch (Exception ex)
        {
            StatusText = "Erreur de vérification";
            AddLog($"Erreur manifest : {ex.Message}");
            CanPlay = File.Exists(Path.Combine(ClientPath, LauncherConfig.GameExecutable));
        }

        _config.Settings.LastCheck = DateTime.Now;
        _config.Save();
    }

    private CancellationTokenSource? _downloadCts;

    public async Task StartDownloadAsync()
    {
        if (_filesToDownload == null || _filesToDownload.Count == 0) return;

        IsDownloading     = true;
        CanUpdate         = false;
        CanPlay           = false;
        IsProgressVisible = true;
        GlobalProgress    = 0;
        ProgressText      = "Démarrage...";
        SpeedText         = "";
        _downloadCts      = new CancellationTokenSource();

        try
        {
            await _download.DownloadFilesAsync(_filesToDownload, ClientPath, _downloadCts.Token);
            StatusText     = "✓ Mise à jour terminée - Prêt à jouer !";
            SubStatusText  = "";
            GlobalProgress = 100;
            SpeedText      = "";
            CanPlay        = true;
            _filesToDownload?.Clear();
        }
        catch (OperationCanceledException)
        {
            StatusText    = "Téléchargement annulé";
            SubStatusText = "";
            SpeedText     = "";
            CanUpdate     = _filesToDownload?.Count > 0;
        }
        catch (Exception ex)
        {
            StatusText = "Erreur pendant le téléchargement";
            SpeedText  = "";
            AddLog($"Erreur : {ex.Message}");
        }
        finally
        {
            IsDownloading = false;
            _downloadCts?.Dispose();
        }
    }

    public void CancelDownload()
    {
        _download.Cancel();
        _downloadCts?.Cancel();
    }

    public void LaunchGame()
    {
        var exePath = Path.Combine(ClientPath, LauncherConfig.GameExecutable);
        if (!File.Exists(exePath))
        {
            System.Windows.MessageBox.Show(
                $"Impossible de trouver {LauncherConfig.GameExecutable}\nChemin : {exePath}",
                "Fichier introuvable",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName         = exePath,
                WorkingDirectory = ClientPath,
                UseShellExecute  = false
            });
            System.Windows.Application.Current.MainWindow?.Hide();
            Task.Run(async () => {
                await Task.Delay(3000);
                Dispatch(() => System.Windows.Application.Current.MainWindow?.Show());
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Erreur au lancement :\n{ex.Message}", "Erreur",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    public void OpenWebsite()  => OpenUrl(LauncherConfig.WebsiteUrl);
    public void OpenRegister() => OpenUrl(LauncherConfig.RegisterUrl);
    private static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    public void BrowseClientPath()
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description            = "Sélectionnez le dossier du client Eons (contenant Eons.exe)",
            UseDescriptionForTitle = true,
            SelectedPath           = ClientPath,
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            ClientPath = dialog.SelectedPath;
            _config.Save();
        }
    }

    private void AddLog(string message) =>
        Dispatch(() => Logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}"));

    private static void Dispatch(Action action) =>
        System.Windows.Application.Current.Dispatcher.Invoke(action);

    public event PropertyChangedEventHandler? PropertyChanged;
    protected bool SetProp<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
