# FolderVision - Système de Logging Structuré

## 🎯 Vue d'ensemble

FolderVision dispose maintenant d'un **système de logging structuré complet** avec support multi-niveaux, multi-providers, et formatage personnalisable.

---

## ✨ Fonctionnalités

### Niveaux de log
- **Debug** - Informations de débogage détaillées
- **Info** - Messages informatifs sur le flux normal
- **Warning** - Avertissements pour situations potentiellement problématiques
- **Error** - Erreurs permettant à l'application de continuer
- **Critical** - Erreurs critiques causant l'arrêt de l'application
- **None** - Désactiver le logging

### Providers de sortie
- **ConsoleLogProvider** - Affichage console avec couleurs
- **FileLogProvider** - Fichiers log avec rotation automatique

### Formats
- **Format standard** - Lisible et structuré
- **Format JSON** - Pour parsing automatique et intégration externe

---

## 🚀 Utilisation rapide

### 1. Configuration par défaut (Info + Console + File)

```csharp
var settings = ScanSettings.CreateDefault();
// Logging déjà configuré avec LoggingOptions.Default

var engine = new ScanEngine();
var result = await engine.ScanFolderAsync(folderPath, settings);
```

### 2. Logging verbeux (Debug + tout)

```csharp
var settings = new ScanSettings
{
    LoggingOptions = LoggingOptions.Verbose
};

var engine = new ScanEngine();
var result = await engine.ScanFolderAsync(folderPath, settings);
```

### 3. Logging silencieux (Warning+ uniquement)

```csharp
var settings = new ScanSettings
{
    LoggingOptions = LoggingOptions.Quiet
};
```

### 4. Console uniquement (pas de fichier)

```csharp
var settings = new ScanSettings
{
    LoggingOptions = LoggingOptions.ConsoleOnly
};
```

### 5. Fichier uniquement (pas de console)

```csharp
var settings = new ScanSettings
{
    LoggingOptions = LoggingOptions.FileOnly
};
```

### 6. Désactiver complètement le logging

```csharp
var settings = new ScanSettings
{
    LoggingOptions = LoggingOptions.Disabled
};
```

---

## 🎨 Configuration personnalisée

### Configuration complète

```csharp
var settings = new ScanSettings
{
    LoggingOptions = new LoggingOptions
    {
        MinLevel = LogLevel.Debug,
        EnableConsoleLog = true,
        EnableFileLog = true,
        UseConsoleColors = true,
        UseStructuredFormat = false, // false = lisible, true = JSON
        LogDirectory = @"C:\MyLogs",
        MaxLogFileSizeMB = 20,       // 20 MB par fichier
        MaxLogFileCount = 10,         // Garder 10 fichiers max
        LogFilePrefix = "MyApp"       // MyApp_20251019_143025.log
    }
};
```

### Logging structuré (JSON)

```csharp
var settings = new ScanSettings
{
    LoggingOptions = new LoggingOptions
    {
        UseStructuredFormat = true  // Format JSON pour parsing
    }
};
```

---

## 📁 Emplacement des fichiers log

### Par défaut
```
Windows: C:\Users\<User>\AppData\Local\FolderVision\Logs\
         FolderVision_20251019_143025.log
```

### Personnalisé
```csharp
LogDirectory = @"C:\MonDossierLogs"
```

---

## 📊 Exemples de sorties

### Format standard (par défaut)

```
[2025-10-19 14:30:25.123] [Info    ] [ScanEngine] Starting scan of folder: C:\Users
[2025-10-19 14:30:25.456] [Debug   ] [ScanEngine] Batch processing: 10% complete (1000/10000 directories)
[2025-10-19 14:30:30.789] [Warning ] [ScanEngine] Access denied: C:\Windows\System32
[2025-10-19 14:30:35.012] [Error   ] [ScanEngine] Timeout (10s) in C:\LargeFolder
[2025-10-19 14:31:00.345] [Info    ] [ScanEngine] Scan completed successfully
```

### Format structuré (JSON)

```json
{"timestamp":"2025-10-19T14:30:25.123","level":"Info","category":"ScanEngine","message":"Starting scan of folder: C:\\Users","threadId":1,"correlationId":"a1b2c3d4","properties":{"FolderPath":"C:\\Users","MaxDepth":50,"MaxThreads":4}}
{"timestamp":"2025-10-19T14:30:25.456","level":"Debug","category":"ScanEngine","message":"Batch processing: 10% complete (1000/10000 directories)","threadId":5,"correlationId":"a1b2c3d4","properties":{"Progress":10,"Processed":1000,"Total":10000}}
{"timestamp":"2025-10-19T14:31:00.345","level":"Info","category":"ScanEngine","message":"Scan completed successfully","threadId":1,"correlationId":"a1b2c3d4","properties":{"TotalFolders":15420,"TotalFiles":98765,"Duration":35.222}}
```

---

## 🔧 Rotation automatique des logs

Les fichiers log sont automatiquement gérés:

### Rotation par taille
```csharp
MaxLogFileSizeMB = 10  // Nouveau fichier quand > 10 MB
```

### Nettoyage automatique
```csharp
MaxLogFileCount = 5    // Garde seulement les 5 plus récents
```

### Exemple de fichiers créés
```
FolderVision_20251019_143025.log  (actuel - 8 MB)
FolderVision_20251019_120000.log  (10 MB - complet)
FolderVision_20251019_090000.log  (10 MB - complet)
FolderVision_20251018_180000.log  (10 MB - complet)
FolderVision_20251018_150000.log  (10 MB - complet)
FolderVision_20251018_120000.log  (sera supprimé - trop vieux)
```

---

## 🎯 Presets disponibles

| Preset | MinLevel | Console | File | Couleurs | Structuré |
|--------|----------|---------|------|----------|-----------|
| **Default** | Info | ✅ | ✅ | ✅ | ❌ |
| **Verbose** | Debug | ✅ | ✅ | ✅ | ✅ |
| **Quiet** | Warning | ❌ | ✅ | N/A | ❌ |
| **ConsoleOnly** | Info | ✅ | ❌ | ✅ | ❌ |
| **FileOnly** | Info | ❌ | ✅ | N/A | ❌ |
| **Disabled** | None | ❌ | ❌ | N/A | ❌ |

---

## 📝 Propriétés contextuelles

Le système log automatiquement des **propriétés contextuelles** :

### Au démarrage du scan
```csharp
["FolderPath"] = "C:\\Users"
["MaxDepth"] = 50
["MaxThreads"] = 4
```

### Pendant le traitement
```csharp
["Progress"] = 10
["Processed"] = 1000
["Total"] = 10000
```

### À la fin du scan
```csharp
["TotalFolders"] = 15420
["TotalFiles"] = 98765
["Duration"] = 35.222
```

### En cas d'erreur
```csharp
["Exception"] = {"type":"IOException","message":"Access denied"}
```

---

## 🔍 Correlation ID

Chaque session de scan reçoit un **Correlation ID unique** pour tracer tous les logs liés:

```
CorrelationId: a1b2c3d4

[2025-10-19 14:30:25.123] [Info] [ScanEngine] Starting scan | CorrelationId: a1b2c3d4
[2025-10-19 14:30:25.456] [Debug] [ScanEngine] Processing... | CorrelationId: a1b2c3d4
[2025-10-19 14:31:00.345] [Info] [ScanEngine] Completed | CorrelationId: a1b2c3d4
```

Permet de filtrer tous les logs d'un scan spécifique.

---

## ⚙️ Configuration pour gros volumes

Pour les scans de gros dossiers, utilisez:

```csharp
var settings = ScanSettings.CreateForLargeFolders();
// Logging automatiquement configuré en mode Verbose
```

Cela active:
- **LogLevel.Debug** - Plus de détails
- **Format structuré** - Meilleure parsing
- **Logs de progression** - Tous les 10 batches

---

## 📊 Statistiques et monitoring

Le logging capture automatiquement:

✅ **Performance**
- Durée totale du scan
- Nombre de dossiers/fichiers traités
- Progression en temps réel

✅ **Erreurs**
- Accès refusés
- Timeouts
- Chemins trop longs
- Erreurs I/O

✅ **Mémoire**
- Dépassements de limite mémoire
- Déclenchements de GC

✅ **Threading**
- ID du thread pour chaque log
- Détection de race conditions potentielles

---

## 🔐 Sécurité et confidentialité

- ❌ **Pas de données sensibles loggées** (contenu fichiers, credentials)
- ✅ Seulement chemins de fichiers et métadonnées
- ✅ Rotation automatique pour limiter l'espace disque
- ✅ Logs locaux uniquement (pas d'envoi externe)

---

## 📚 Architecture

### Composants

```
ILogger (interface)
  └─ Logger (implémentation)
      ├─ ILogProvider
      │   ├─ ConsoleLogProvider
      │   └─ FileLogProvider
      └─ LogEntry (entrée structurée)
          ├─ Timestamp
          ├─ Level
          ├─ Category
          ├─ Message
          ├─ Exception?
          ├─ ThreadId
          ├─ CorrelationId?
          └─ Properties?
```

### Flux

```
Application
  └─> Logger.Info("message", properties)
       └─> LogEntry créé
            └─> Pour chaque ILogProvider:
                 ├─> ConsoleLogProvider.Write(entry) → Console
                 └─> FileLogProvider.Write(entry) → Fichier
```

---

## 💡 Best Practices

### ✅ À FAIRE

```csharp
// Utiliser les presets pour simplicité
settings.LoggingOptions = LoggingOptions.Verbose;

// Activer le logging pour debugging
settings.LoggingOptions = new LoggingOptions { MinLevel = LogLevel.Debug };

// Désactiver console en production/background
settings.LoggingOptions = LoggingOptions.FileOnly;

// Limiter la taille des logs
settings.LoggingOptions.MaxLogFileSizeMB = 10;
settings.LoggingOptions.MaxLogFileCount = 5;
```

### ❌ À ÉVITER

```csharp
// Ne pas désactiver complètement en production
settings.LoggingOptions = LoggingOptions.Disabled; // ❌ Pas de traces !

// Ne pas mettre des fichiers trop gros
settings.LoggingOptions.MaxLogFileSizeMB = 1000; // ❌ 1GB !

// Ne pas garder trop de fichiers
settings.LoggingOptions.MaxLogFileCount = 1000; // ❌ Trop !
```

---

## 🆕 Nouveautés par rapport à l'ancien système

| Fonctionnalité | Ancien | Nouveau |
|----------------|--------|---------|
| **Niveaux** | Aucun | 5 niveaux (Debug-Critical) |
| **Fichiers log** | ❌ | ✅ Avec rotation |
| **Format structuré** | ❌ | ✅ JSON disponible |
| **Couleurs console** | ❌ | ✅ Par niveau |
| **Correlation ID** | ❌ | ✅ Pour traçabilité |
| **Propriétés contextuelles** | ❌ | ✅ Key-value pairs |
| **Configuration** | Hardcodé | ✅ Totalement configurable |
| **Thread ID** | ❌ | ✅ Automatique |
| **Timestamp** | ❌ | ✅ Millisecondes |
| **Rotation automatique** | ❌ | ✅ Par taille et nombre |

---

## 🎉 Conclusion

Le nouveau système de logging offre:
- ✅ **Flexibilité totale** - Console, fichier, ou les deux
- ✅ **Niveaux configurables** - Debug à Critical
- ✅ **Format au choix** - Lisible ou structuré (JSON)
- ✅ **Gestion automatique** - Rotation et nettoyage
- ✅ **Traçabilité** - Correlation ID et propriétés
- ✅ **Performance** - Asynchrone et optimisé
- ✅ **Production-ready** - Testé et validé

---

*Généré le: 2025-10-19*
*Version: FolderVision 2.1 - Système de Logging Structuré*
