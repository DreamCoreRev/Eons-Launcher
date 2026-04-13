# Eons Launcher — Guide d'installation

## Architecture du projet

```
EonsLauncher/
├── Backend/                        ← Scripts PHP (XAMPP)
│   ├── generate_manifest.php       ← Génération manuelle du manifest
│   ├── manifest.php                ← Manifest avec cache (à utiliser en prod)
│   └── news.php                    ← API actualités + statut serveur
│
└── WPF/                            ← Solution Visual Studio 2022
    ├── EonsLauncher.sln
    └── EonsLauncher/
        ├── App.xaml / App.xaml.cs
        ├── app.manifest
        ├── EonsLauncher.csproj
        ├── Models/
        │   └── Models.cs           ← Modèles de données (manifest, news, etc.)
        ├── Services/
        │   ├── ApiService.cs       ← Appels API (manifest + news)
        │   ├── ConfigService.cs    ← Sauvegarde config locale
        │   ├── DownloadService.cs  ← Téléchargement + vérification MD5
        │   └── LauncherConfig.cs   ← Configuration centrale (URLs, chemins)
        ├── ViewModels/
        │   └── MainViewModel.cs    ← Logique de l'interface
        └── Views/
            ├── MainWindow.xaml     ← Interface graphique
            └── MainWindow.xaml.cs  ← Code-behind
```

---

## 1. Installation du Backend PHP (XAMPP)

### Étape 1 : Copier les fichiers PHP

```
C:/xampp/htdocs/eons_launcher/
    ├── manifest.php
    ├── generate_manifest.php
    └── news.php
```

### Étape 2 : Créer un alias Apache pour le dossier client

Ouvre `C:/xampp/apache/conf/extra/httpd-vhosts.conf` et ajoute :

```apache
Alias /eons_client "C:/Servers/Client/Eons/"
<Directory "C:/Servers/Client/Eons/">
    Options Indexes FollowSymLinks
    AllowOverride None
    Require all granted
</Directory>
```

Puis redémarre Apache dans le panneau XAMPP.

### Étape 3 : Configurer manifest.php

Ouvre `manifest.php` et modifie ces constantes :

```php
define('CLIENT_BASE_PATH', 'C:/Servers/Client/Eons/');
define('CLIENT_BASE_URL',  'https://eons-world.eu/eons_client/');
```

### Étape 4 : Tester le manifest

Ouvre dans ton navigateur : https://eons-world.eu/eons_launcher/manifest.php

Tu dois voir un JSON avec la liste de tous tes fichiers.

---

## 2. Configuration du Launcher C#

### Étape 1 : Ouvrir la solution

Ouvre `WPF/EonsLauncher.sln` dans Visual Studio 2022.

### Étape 2 : Restaurer les packages NuGet

```
Outils → Gestionnaire de packages NuGet → Console
> dotnet restore
```

### Étape 3 : Configurer les URLs

Ouvre `Services/LauncherConfig.cs` et modifie :

```csharp
// Pour développement local
public const string NewsApiUrl    = "https://eons-world.eu/eons_launcher/news.php";
public const string ManifestUrl   = "https://eons-world.eu/eons_launcher/manifest.php";

// Pour production (eons-world.eu)
// public const string NewsApiUrl = "https://eons-world.eu/launcher/news.php";
// public const string ManifestUrl= "https://eons-world.eu/launcher/manifest.php";
```

### Étape 4 : Compiler et tester

- **F5** pour lancer en mode Debug
- **Ctrl+Shift+B** pour compiler

---

## 3. Déploiement en production (eons-world.eu)

### Backend (hébergeur)

1. Crée le dossier `/public_html/launcher/` sur ton hébergeur
2. Copie `manifest.php` et `news.php`
3. Modifie `CLIENT_BASE_URL` → `'https://eons-world.eu/uploads/client/Eons/'`
4. Modifie `CLIENT_BASE_PATH` → chemin réel sur le serveur

### Launcher (publier)

Dans Visual Studio :
```
Build → Publier EonsLauncher
Cible : Dossier
Configuration : Release
Déploiement : Autonome (Self-contained)
Exécutable unique : ✓
```

---

## 4. Fonctionnalités du Launcher

| Fonctionnalité | Description |
|---|---|
| **Vérification MD5** | Chaque fichier est comparé au hash distant |
| **Téléchargement parallèle** | 3 fichiers simultanément (configurable) |
| **Reprise sur erreur** | 3 tentatives automatiques par fichier |
| **Fichier temporaire** | Téléchargement dans `.tmp` avant validation |
| **Cache manifest** | 1 heure de cache côté PHP |
| **Actualités dynamiques** | Chargées depuis `news.php` |
| **Statut serveur** | Indicateur en temps réel |
| **Config persistante** | Chemin client sauvegardé |
| **Annulation** | Arrêt propre du téléchargement |
| **Lanceur sans barre** | Fenêtre chromeless déplaçable |

---

## 5. Ajouter des actualités

Modifie le tableau `$news` dans `news.php` :

```php
[
    'id'      => 4,
    'title'   => 'Titre de la news',
    'content' => 'Contenu de la news...',
    'date'    => '2026-04-13',
    'type'    => 'event',  // info | warning | event | update
    'image'   => null,
],
```

---

## 6. Prérequis

| Composant | Version |
|---|---|
| Visual Studio | 2022 |
| .NET SDK | 8.0+ |
| XAMPP | 7.4.15+ |
| PHP | 7.4+ |
| Windows | 10/11 x64 |
