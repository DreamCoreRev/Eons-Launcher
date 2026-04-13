<?php
/**
 * Eons Launcher - Manifest avec Cache
 * À placer dans : C:/xampp/htdocs/eons_launcher/manifest.php
 *
 * Les fichiers sont servis depuis XAMPP via l'alias Apache /eons_client/
 * configuré dans httpd-vhosts.conf (déjà en place).
 */

header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');

// =============================================
// CONFIGURATION
// =============================================

// Chemin physique du client sur le serveur local
define('CLIENT_BASE_PATH', 'C:/Servers/Client/Eons/');

// ⚠️ URL locale via l'alias Apache — NE PAS mettre eons-world.eu ici
// Les fichiers sont téléchargés depuis XAMPP, pas depuis le site web
define('CLIENT_BASE_URL', 'http://localhost/eons_client/');

define('CACHE_FILE',     __DIR__ . '/cache/manifest_cache.json');
define('CACHE_DURATION', 3600); // 1 heure

// Token pour forcer la régénération du cache :
// http://localhost/eons_launcher/manifest.php?refresh=1&token=XXX
define('REFRESH_TOKEN', '62b69a792f5e205dfaf537ba5e8fd1aea28f51e6bab74c5ec8761cf8f400a374');

// =============================================
// CACHE
// =============================================

function isCacheValid(): bool
{
    if (!file_exists(CACHE_FILE)) return false;
    return (time() - filemtime(CACHE_FILE)) < CACHE_DURATION;
}

function loadCache(): string
{
    return file_get_contents(CACHE_FILE);
}

function saveCache(string $json): void
{
    $dir = dirname(CACHE_FILE);
    if (!is_dir($dir)) mkdir($dir, 0755, true);
    file_put_contents(CACHE_FILE, $json);
}

// =============================================
// REFRESH FORCÉ
// =============================================

$forceRefresh = isset($_GET['refresh']) && isset($_GET['token']) && $_GET['token'] === REFRESH_TOKEN;

if (!$forceRefresh && isCacheValid()) {
    header('X-Cache: HIT');
    echo loadCache();
    exit;
}

header('X-Cache: MISS');

// =============================================
// SCAN DU DOSSIER CLIENT
// =============================================

function scanClientDirectory(string $basePath, string $baseUrl): array
{
    $files    = [];
    $iterator = new RecursiveIteratorIterator(
        new RecursiveDirectoryIterator($basePath, RecursiveDirectoryIterator::SKIP_DOTS),
        RecursiveIteratorIterator::SELF_FIRST
    );

    foreach ($iterator as $file) {
        if (!$file->isFile()) continue;

        // Normaliser le chemin en slashes
        $absolutePath = str_replace('\\', '/', $file->getPathname());
        $cleanBase    = str_replace('\\', '/', $basePath);
        $relativePath = ltrim(str_replace($cleanBase, '', $absolutePath), '/');

        // Encoder chaque segment du chemin pour l'URL (gère les espaces, accents, etc.)
        $encodedUrl = $baseUrl . implode('/', array_map('rawurlencode', explode('/', $relativePath)));

        $files[] = [
            'path'     => $relativePath,
            'url'      => $encodedUrl,
            'size'     => $file->getSize(),
            'md5'      => md5_file($absolutePath),
            'modified' => $file->getMTime(),
        ];
    }

    return $files;
}

// =============================================
// GÉNÉRATION DU MANIFEST
// =============================================

try {
    if (!is_dir(CLIENT_BASE_PATH)) {
        http_response_code(500);
        echo json_encode(['success' => false, 'error' => 'Dossier client introuvable : ' . CLIENT_BASE_PATH]);
        exit;
    }

    $files     = scanClientDirectory(CLIENT_BASE_PATH, CLIENT_BASE_URL);
    $totalSize = array_sum(array_column($files, 'size'));

    $manifest = [
        'success'       => true,
        'version'       => '3.3.5a',
        'generated_at'  => date('Y-m-d H:i:s'),
        'total_files'   => count($files),
        'total_size'    => $totalSize,
        'total_size_mb' => round($totalSize / 1048576, 2),
        'base_url'      => CLIENT_BASE_URL,
        'files'         => $files,
    ];

    $json = json_encode($manifest, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE);
    saveCache($json);
    echo $json;

} catch (Exception $e) {
    http_response_code(500);
    echo json_encode(['success' => false, 'error' => $e->getMessage()]);
}
