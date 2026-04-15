using System.IO;
using System.Net.Http;
using System.Security.Cryptography;

namespace EonsLauncher;

public class DownloadService : IDisposable
{
    private readonly HttpClient _http;
    private bool _disposed;
    private CancellationTokenSource? _cts;

    public event Action<string>?           OnStatusChanged;
    public event Action<int, int, double>? OnProgressChanged;
    public event Action<string, bool>?     OnFileCompleted;
    public event Action<string>?           OnError;

    public DownloadService()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(LauncherConfig.HttpTimeoutSeconds) };
        _http.DefaultRequestHeaders.Add("User-Agent", $"EonsLauncher/{LauncherConfig.LauncherVersion}");
    }

    public static string ComputeMd5(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var md5    = MD5.Create();
        return Convert.ToHexString(md5.ComputeHash(stream)).ToLowerInvariant();
    }

    public static bool IsFileUpToDate(string localPath, string expectedMd5)
    {
        if (!File.Exists(localPath)) return false;
        try   { return ComputeMd5(localPath).Equals(expectedMd5, StringComparison.OrdinalIgnoreCase); }
        catch { return false; }
    }

    public List<ClientFile> GetFilesToDownload(List<ClientFile> remoteFiles, string clientPath,
                                                IProgress<string>? progress = null)
    {
        var toDownload = new List<ClientFile>();
        foreach (var f in remoteFiles)
        {
            var localPath = Path.Combine(clientPath, f.Path.Replace('/', Path.DirectorySeparatorChar));
            progress?.Report($"Vérification : {Path.GetFileName(f.Path)}");
            if (!IsFileUpToDate(localPath, f.Md5))
                toDownload.Add(f);
        }
        return toDownload;
    }

    public async Task DownloadFilesAsync(List<ClientFile> files, string clientPath,
                                          CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _cts.Token;

        int completed = 0;
        int total     = files.Count;

        using var sem = new SemaphoreSlim(LauncherConfig.MaxConcurrentDownloads);

        var tasks = files.Select(async file =>
        {
            await sem.WaitAsync(token);
            try
            {
                await DownloadSingleFileAsync(file, clientPath, token);
                int cur = Interlocked.Increment(ref completed);
				double pct = cur * 100.0 / total;
				OnProgressChanged?.Invoke(cur, total, pct);
            }
            finally { sem.Release(); }
        });

        await Task.WhenAll(tasks);
    }

    private async Task DownloadSingleFileAsync(ClientFile file, string clientPath, CancellationToken token)
    {
        var localPath = Path.Combine(clientPath, file.Path.Replace('/', Path.DirectorySeparatorChar));
        var tempPath  = localPath + ".tmp";

        OnStatusChanged?.Invoke($"Téléchargement : {Path.GetFileName(file.Path)} ({file.SizeDisplay})");

        for (int attempt = 1; attempt <= LauncherConfig.MaxRetries; attempt++)
        {
            try
            {
                var dir = Path.GetDirectoryName(localPath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                using var response = await _http.GetAsync(file.Url, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();

                await using var contentStream = await response.Content.ReadAsStreamAsync(token);
                await using var fileStream    = new FileStream(tempPath, FileMode.Create, FileAccess.Write,
                                                               FileShare.None, LauncherConfig.DownloadBufferSize, true);

                var buffer = new byte[LauncherConfig.DownloadBufferSize];
                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer, token)) > 0)
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), token);

                await fileStream.FlushAsync(token);
                fileStream.Close();

                var downloadedMd5 = ComputeMd5(tempPath);
                if (!downloadedMd5.Equals(file.Md5, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(tempPath);
                    throw new Exception($"MD5 invalide pour {file.Path}");
                }

                if (File.Exists(localPath)) File.Delete(localPath);
                File.Move(tempPath, localPath);

                OnFileCompleted?.Invoke(file.Path, true);
                return;
            }
            catch (OperationCanceledException)
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
                throw;
            }
            catch (Exception ex)
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
                if (attempt == LauncherConfig.MaxRetries)
                {
                    OnError?.Invoke($"Échec ({attempt} tentatives) : {file.Path}\n{ex.Message}");
                    OnFileCompleted?.Invoke(file.Path, false);
                }
                else
                    await Task.Delay(1000 * attempt, token);
            }
        }
    }

    public void Cancel() => _cts?.Cancel();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts?.Dispose();
        _http.Dispose();
    }
}
