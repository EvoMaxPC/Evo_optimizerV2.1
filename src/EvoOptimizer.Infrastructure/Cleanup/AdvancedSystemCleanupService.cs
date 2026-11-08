using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace EvoOptimizer.Infrastructure.Cleanup;

public enum CleanupCategoryId
{
    UserTemporaryFiles,
    WindowsTemporaryFiles,
    WindowsErrorReports,
    OldCrashDumps,
    ThumbnailCache,
    ShaderCaches,
    EdgeCache,
    ChromeCache,
    FirefoxCache,
    WindowsUpdateCache,
    DeliveryOptimizationCache,
    Prefetch,
    RecycleBin,
    RegistryDiagnostics
}

public sealed record CleanupFileCandidate(
    string FullPath,
    long Length,
    DateTime LastWriteTimeUtc);

public sealed record CleanupCategoryScanResult(
    CleanupCategoryId Id,
    string Name,
    string Description,
    string Location,
    bool IsCleanable,
    bool IsAvailable,
    int FileCount,
    long EstimatedBytes,
    int AccessErrorCount,
    IReadOnlyList<CleanupFileCandidate> Files,
    string Status);

public sealed record CleanupScanReport(
    DateTimeOffset ScannedAt,
    IReadOnlyList<CleanupCategoryScanResult> Categories,
    int FileCount,
    long EstimatedBytes,
    int AccessErrorCount);

public sealed record CleanupHistoryEntry(
    DateTimeOffset Timestamp,
    string Category,
    string Path,
    string Status,
    long Bytes,
    string Detail);

public sealed record CleanupCategoryRunResult(
    string Category,
    int DeletedFiles,
    long FreedBytes,
    int SkippedFiles,
    int ErrorCount);

public sealed record CleanupRunReport(
    DateTimeOffset FinishedAt,
    DateTime MinimumAgeCutoff,
    IReadOnlyList<CleanupCategoryRunResult> Categories,
    int DeletedFiles,
    long FreedBytes,
    int SkippedFiles,
    int ErrorCount,
    string HistoryPath);

public sealed class AdvancedSystemCleanupService
{
    private sealed record CategoryDefinition(
        CleanupCategoryId Id,
        string Name,
        string Description,
        bool IsCleanable,
        Func<IReadOnlyList<DirectoryTarget>> GetTargets,
        string? ProcessName = null,
        string? ReadOnlyStatus = null);

    private sealed record DirectoryTarget(string Path, string Pattern = "*");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly string _historyPath;
    private readonly SemaphoreSlim _historyLock = new(1, 1);
    private CleanupScanReport? _lastScan;

    public AdvancedSystemCleanupService(string? historyPath = null)
    {
        _historyPath = Path.GetFullPath(historyPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EvoFpsOptimizer",
            "cleanup-history.jsonl"));
    }

    public Task<CleanupScanReport> ScanAsync(
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null) =>
        Task.Run(() => Scan(cancellationToken, progress), cancellationToken);

    public Task<CleanupRunReport> CleanAsync(
        IEnumerable<CleanupCategoryId> selectedCategories,
        TimeSpan minimumAge,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(selectedCategories);
        if (minimumAge < TimeSpan.FromDays(1) || minimumAge > TimeSpan.FromDays(365))
            throw new ArgumentOutOfRangeException(nameof(minimumAge));

        CleanupCategoryId[] selected = selectedCategories.Distinct().ToArray();
        if (selected.Length == 0)
            throw new ArgumentException("Select at least one cleanable category.", nameof(selectedCategories));

        return Task.Run(
            () => Clean(selected, minimumAge, cancellationToken, progress),
            cancellationToken);
    }

    public async Task<IReadOnlyList<CleanupHistoryEntry>> ReadLatestHistoryAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(limit));

        if (!File.Exists(_historyPath))
            return Array.Empty<CleanupHistoryEntry>();

        var entries = new List<CleanupHistoryEntry>();
        await foreach (string line in File.ReadLinesAsync(_historyPath, cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            CleanupHistoryEntry? entry = JsonSerializer.Deserialize<CleanupHistoryEntry>(line, JsonOptions);
            if (entry is null)
                throw new InvalidDataException($"Invalid cleanup history entry in {_historyPath}.");

            entries.Add(entry);
        }

        return entries
            .OrderByDescending(entry => entry.Timestamp)
            .Take(limit)
            .ToArray();
    }

    public static bool IsPathWithinRoot(string candidatePath, string rootPath)
    {
        if (string.IsNullOrWhiteSpace(candidatePath) || string.IsNullOrWhiteSpace(rootPath))
            return false;

        string candidate;
        string root;
        try
        {
            candidate = Path.GetFullPath(candidatePath);
            root = Path.GetFullPath(rootPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException)
        {
            return false;
        }

        string rootPrefix = root + Path.DirectorySeparatorChar;
        return candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private CleanupScanReport Scan(
        CancellationToken cancellationToken,
        IProgress<string>? progress)
    {
        EnsureWindows();

        var definitions = CreateDefinitions();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<CleanupCategoryScanResult>(definitions.Count);

        foreach (CategoryDefinition definition in definitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report($"Analizando: {definition.Name}");

            if (definition.Id == CleanupCategoryId.RecycleBin)
            {
                results.Add(ScanRecycleBin(definition));
                continue;
            }

            if (definition.Id == CleanupCategoryId.RegistryDiagnostics)
            {
                results.Add(new CleanupCategoryScanResult(
                    definition.Id,
                    definition.Name,
                    definition.Description,
                    "Registro de Windows · solo lectura",
                    false,
                    false,
                    0,
                    0,
                    0,
                    Array.Empty<CleanupFileCandidate>(),
                    definition.ReadOnlyStatus!));
                continue;
            }

            var files = new List<CleanupFileCandidate>();
            int errors = 0;
            IReadOnlyList<DirectoryTarget> targets;
            try
            {
                targets = definition.GetTargets();
            }
            catch (Exception ex) when (IsFileSystemError(ex))
            {
                results.Add(Unavailable(definition, ex.Message));
                continue;
            }

            var existingTargets = new List<string>();
            foreach (DirectoryTarget target in targets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!Directory.Exists(target.Path))
                    continue;

                existingTargets.Add(Path.GetFullPath(target.Path));
                EnumerateTarget(target, files, seenPaths, ref errors, cancellationToken);
            }

            bool available = existingTargets.Count > 0;
            string status = !available
                ? "No se encontró una ubicación compatible en este equipo."
                : errors > 0
                    ? $"Análisis parcial: {errors} ubicaciones o archivos no pudieron leerse."
                    : definition.ProcessName is not null && IsProcessRunning(definition.ProcessName)
                        ? "Caché identificada; cierra el navegador antes de limpiarla."
                        : !definition.IsCleanable
                            ? definition.ReadOnlyStatus ?? "Categoría disponible en modo de solo lectura."
                        : files.Count == 0
                            ? "No se encontraron archivos en las ubicaciones autorizadas."
                            : "Análisis completado; la limpieza volverá a validar cada archivo.";

            results.Add(new CleanupCategoryScanResult(
                definition.Id,
                definition.Name,
                definition.Description,
                string.Join(Environment.NewLine, existingTargets),
                definition.IsCleanable,
                available,
                files.Count,
                files.Sum(file => file.Length),
                errors,
                files,
                status));
        }

        var report = new CleanupScanReport(
            DateTimeOffset.Now,
            results,
            results.Sum(result => result.FileCount),
            results.Sum(result => result.EstimatedBytes),
            results.Sum(result => result.AccessErrorCount));
        _lastScan = report;
        return report;
    }

    private CleanupRunReport Clean(
        IReadOnlyCollection<CleanupCategoryId> selected,
        TimeSpan minimumAge,
        CancellationToken cancellationToken,
        IProgress<string>? progress)
    {
        EnsureWindows();

        CleanupScanReport scan = _lastScan
            ?? throw new InvalidOperationException("Run a fresh analysis before cleaning.");
        var definitions = CreateDefinitions().ToDictionary(definition => definition.Id);
        DateTime cutoff = DateTime.UtcNow - minimumAge;

        CleanupCategoryScanResult[] categories = scan.Categories
            .Where(category => selected.Contains(category.Id))
            .ToArray();
        if (categories.Length != selected.Count)
            throw new InvalidOperationException("The selected categories are not part of the latest analysis.");
        if (categories.Any(category => !category.IsCleanable))
            throw new InvalidOperationException("Read-only categories cannot be cleaned.");

        Directory.CreateDirectory(Path.GetDirectoryName(_historyPath)!);
        using (new FileStream(_historyPath, FileMode.Append, FileAccess.Write, FileShare.Read))
        {
        }

        var categoryResults = new List<CleanupCategoryRunResult>(categories.Length);
        int deletedTotal = 0;
        long bytesTotal = 0;
        int skippedTotal = 0;
        int errorsTotal = 0;
        bool stopAfterLogError = false;

        foreach (CleanupCategoryScanResult category in categories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report($"Limpiando: {category.Name}");
            CategoryDefinition definition = definitions[category.Id];
            int deleted = 0;
            long freed = 0;
            int skipped = 0;
            int errors = 0;
            bool processRunning = definition.ProcessName is not null &&
                                  IsProcessRunning(definition.ProcessName);
            IReadOnlyList<DirectoryTarget> authorizedTargets = definition.GetTargets();

            foreach (CleanupFileCandidate candidate in category.Files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string status;
                string detail;
                long actualBytes = 0;

                if (processRunning)
                {
                    status = "Skipped";
                    detail = "La aplicación asociada está en ejecución; cierra y vuelve a analizar.";
                    skipped++;
                }
                else if (candidate.LastWriteTimeUtc > cutoff)
                {
                    status = "Skipped";
                    detail = "El archivo es más reciente que la antigüedad seleccionada.";
                    skipped++;
                }
                else if (!TryDeleteCandidate(candidate, authorizedTargets, cutoff, out actualBytes, out detail))
                {
                    status = detail.StartsWith("Error:", StringComparison.Ordinal)
                        ? "Error"
                        : "Skipped";
                    if (status == "Error")
                        errors++;
                    else
                        skipped++;
                }
                else
                {
                    status = "Deleted";
                    detail = "Archivo eliminado tras validar ubicación, antigüedad y acceso exclusivo.";
                    deleted++;
                    freed += actualBytes;
                }

                var entry = new CleanupHistoryEntry(
                    DateTimeOffset.Now,
                    category.Name,
                    candidate.FullPath,
                    status,
                    actualBytes,
                    detail);
                try
                {
                    AppendHistory(entry);
                }
                catch (Exception ex) when (IsFileSystemError(ex))
                {
                    errors++;
                    stopAfterLogError = true;
                    break;
                }
            }

            categoryResults.Add(new CleanupCategoryRunResult(
                category.Name,
                deleted,
                freed,
                skipped,
                errors));
            deletedTotal += deleted;
            bytesTotal += freed;
            skippedTotal += skipped;
            errorsTotal += errors;

            if (stopAfterLogError)
                break;
        }

        return new CleanupRunReport(
            DateTimeOffset.Now,
            cutoff,
            categoryResults,
            deletedTotal,
            bytesTotal,
            skippedTotal,
            errorsTotal,
            _historyPath);
    }

    private static bool TryDeleteCandidate(
        CleanupFileCandidate candidate,
        IReadOnlyList<DirectoryTarget> targets,
        DateTime cutoff,
        out long actualBytes,
        out string detail)
    {
        actualBytes = 0;
        detail = "Archivo omitido.";

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(candidate.FullPath);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException)
        {
            detail = $"Error: ruta inválida ({ex.Message})";
            return false;
        }

        if (!targets.Any(target => IsPathWithinRoot(fullPath, target.Path)))
        {
            detail = "Error: la ruta ya no está dentro de una ubicación autorizada.";
            return false;
        }

        try
        {
            DirectoryTarget? target = targets.FirstOrDefault(candidateTarget =>
                IsPathWithinRoot(fullPath, candidateTarget.Path));
            if (target is null || !HasSafeDirectoryChain(fullPath, target.Path))
            {
                detail = "Error: una carpeta padre salió de la ubicación autorizada o es un punto de reanálisis.";
                return false;
            }

            if (!File.Exists(fullPath))
            {
                detail = "El archivo ya no existe.";
                return false;
            }

            FileAttributes attributes = File.GetAttributes(fullPath);
            if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.System | FileAttributes.ReadOnly)) != 0)
            {
                detail = "El archivo tiene atributos protegidos o es un punto de reanálisis.";
                return false;
            }

            var current = new FileInfo(fullPath);
            current.Refresh();
            if (current.LastWriteTimeUtc > cutoff)
            {
                detail = "El archivo fue actualizado después del análisis o es demasiado reciente.";
                return false;
            }

            using var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None);
            if (!GetFileInformationByHandle(stream.SafeFileHandle, out ByHandleFileInformation fileInfo))
            {
                detail = $"Error: no se pudo consultar la asignación del archivo (Win32 {Marshal.GetLastWin32Error()}).";
                return false;
            }

            if (fileInfo.NumberOfLinks > 1)
            {
                detail = "El archivo tiene enlaces duros; su eliminación no garantiza liberar espacio.";
                return false;
            }

            uint allocatedLow = GetCompressedFileSizeW(fullPath, out uint allocatedHigh);
            if (allocatedLow == uint.MaxValue && Marshal.GetLastWin32Error() != 0)
            {
                detail = $"Error: no se pudo medir el espacio asignado (Win32 {Marshal.GetLastWin32Error()}).";
                return false;
            }

            long allocatedBytes = ((long)allocatedHigh << 32) | allocatedLow;
            stream.Dispose();
            File.Delete(fullPath);
            if (File.Exists(fullPath))
            {
                detail = "Error: Windows no confirmó la eliminación.";
                return false;
            }

            actualBytes = allocatedBytes;
            return true;
        }
        catch (Exception ex) when (IsFileSystemError(ex))
        {
            detail = $"Error: {ex.Message}";
            return false;
        }
    }

    private static void EnumerateTarget(
        DirectoryTarget target,
        ICollection<CleanupFileCandidate> files,
        ISet<string> seenPaths,
        ref int errors,
        CancellationToken cancellationToken)
    {
        string root;
        try
        {
            root = Path.GetFullPath(target.Path);
            if (!Directory.Exists(root) ||
                (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
            {
                return;
            }

            EnumerateDirectory(root, root, target.Pattern, files, seenPaths, ref errors, cancellationToken);
        }
        catch (Exception ex) when (IsFileSystemError(ex))
        {
            errors++;
        }
    }

    private static void EnumerateDirectory(
        string root,
        string directory,
        string pattern,
        ICollection<CleanupFileCandidate> files,
        ISet<string> seenPaths,
        ref int errors,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            foreach (string filePath in Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    string fullPath = Path.GetFullPath(filePath);
                    if (!IsPathWithinRoot(fullPath, root) ||
                        !seenPaths.Add(fullPath))
                    {
                        continue;
                    }

                    FileAttributes attributes = File.GetAttributes(fullPath);
                    if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.System)) != 0)
                        continue;

                    var file = new FileInfo(fullPath);
                    file.Refresh();
                    files.Add(new CleanupFileCandidate(
                        fullPath,
                        file.Length,
                        file.LastWriteTimeUtc));
                }
                catch (Exception ex) when (IsFileSystemError(ex))
                {
                    errors++;
                }
            }

            foreach (string childDirectory in Directory.EnumerateDirectories(
                         directory,
                         "*",
                         SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if ((File.GetAttributes(childDirectory) & FileAttributes.ReparsePoint) == 0)
                        EnumerateDirectory(root, childDirectory, pattern, files, seenPaths, ref errors, cancellationToken);
                }
                catch (Exception ex) when (IsFileSystemError(ex))
                {
                    errors++;
                }
            }
        }
        catch (Exception ex) when (IsFileSystemError(ex))
        {
            errors++;
        }
    }

    private static CleanupCategoryScanResult ScanRecycleBin(CategoryDefinition definition)
    {
        var info = new RecycleBinInfo { Size = (uint)Marshal.SizeOf<RecycleBinInfo>() };
        int result = SHQueryRecycleBinW(null, ref info);
        bool succeeded = result >= 0;
        return new CleanupCategoryScanResult(
            definition.Id,
            definition.Name,
            definition.Description,
            "Papelera de reciclaje · API Shell",
            false,
            succeeded,
            succeeded ? checked((int)Math.Min(info.ItemCount, int.MaxValue)) : 0,
            succeeded ? info.Bytes : 0,
            succeeded ? 0 : 1,
            Array.Empty<CleanupFileCandidate>(),
            succeeded
                ? "Conteo agregado obtenido de Windows; los elementos no se exponen individualmente y no se limpian desde este flujo."
                : "Windows no permitió consultar el conteo agregado de la Papelera.");
    }

    private static CleanupCategoryScanResult Unavailable(
        CategoryDefinition definition,
        string error) =>
        new(
            definition.Id,
            definition.Name,
            definition.Description,
            "Ubicación no disponible",
            definition.IsCleanable,
            false,
            0,
            0,
            1,
            Array.Empty<CleanupFileCandidate>(),
            $"No se pudo analizar la ubicación: {error}");

    private void AppendHistory(CleanupHistoryEntry entry)
    {
        _historyLock.Wait();
        try
        {
            string line = JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine;
            File.AppendAllText(_historyPath, line);
        }
        finally
        {
            _historyLock.Release();
        }
    }

    private static IReadOnlyList<CategoryDefinition> CreateDefinitions()
    {
        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string common = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        return
        [
            Cleanable(CleanupCategoryId.UserTemporaryFiles, "Temporales del usuario",
                "Archivos temporales antiguos del perfil actual. No se eliminan carpetas ni datos fuera de TEMP.",
                () => [new(Path.GetTempPath())]),
            Cleanable(CleanupCategoryId.WindowsTemporaryFiles, "Temporales de Windows",
                "Archivos antiguos en Windows\\Temp. Los elementos inaccesibles o protegidos se omiten.",
                () => [new(Path.Combine(windows, "Temp"))]),
            Cleanable(CleanupCategoryId.WindowsErrorReports, "Informes de errores",
                "Informes WER archivados o en cola; los archivos recientes se conservan según la antigüedad elegida.",
                () =>
                [
                    new(Path.Combine(common, "Microsoft", "Windows", "WER", "ReportQueue")),
                    new(Path.Combine(common, "Microsoft", "Windows", "WER", "ReportArchive")),
                    new(Path.Combine(common, "Microsoft", "Windows", "WER", "Temp")),
                    new(Path.Combine(local, "Microsoft", "Windows", "WER", "ReportQueue")),
                    new(Path.Combine(local, "Microsoft", "Windows", "WER", "ReportArchive")),
                    new(Path.Combine(local, "Microsoft", "Windows", "WER", "Temp"))
                ]),
            Cleanable(CleanupCategoryId.OldCrashDumps, "Volcados de memoria antiguos",
                "Solo archivos .dmp antiguos en las ubicaciones conocidas de volcados.",
                () =>
                [
                    new(Path.Combine(windows, "Minidump"), "*.dmp"),
                    new(Path.Combine(local, "CrashDumps"), "*.dmp")
                ]),
            Cleanable(CleanupCategoryId.ThumbnailCache, "Caché de miniaturas",
                "Solo bases thumbcache_*.db del Explorador; no se elimina contenido personal.",
                () => [new(Path.Combine(local, "Microsoft", "Windows", "Explorer"), "thumbcache_*.db")]),
            Cleanable(CleanupCategoryId.ShaderCaches, "Cachés de shaders",
                "Cachés DirectX y de GPU identificadas en el perfil; pueden reconstruirse y causar recompilación inicial.",
                () =>
                [
                    new(Path.Combine(local, "D3DSCache")),
                    new(Path.Combine(local, "NVIDIA", "DXCache")),
                    new(Path.Combine(local, "NVIDIA", "GLCache")),
                    new(Path.Combine(local, "AMD", "DxCache")),
                    new(Path.Combine(local, "AMD", "GLCache")),
                    new(Path.Combine(local, "Intel", "ShaderCache"))
                ]),
            Browser(CleanupCategoryId.EdgeCache, "Caché de Microsoft Edge", "msedge",
                () => GetChromiumCacheTargets(Path.Combine(local, "Microsoft", "Edge", "User Data"))),
            Browser(CleanupCategoryId.ChromeCache, "Caché de Google Chrome", "chrome",
                () => GetChromiumCacheTargets(Path.Combine(local, "Google", "Chrome", "User Data"))),
            Browser(CleanupCategoryId.FirefoxCache, "Caché de Mozilla Firefox", "firefox",
                () => GetFirefoxCacheTargets(Path.Combine(local, "Mozilla", "Firefox", "Profiles"))),
            ReadOnly(CleanupCategoryId.WindowsUpdateCache, "Descargas de Windows Update",
                "Se analiza la caché conocida; no se limpia porque requiere coordinación con el servicio de mantenimiento de Windows.",
                () => [new(Path.Combine(windows, "SoftwareDistribution", "Download"))]),
            ReadOnly(CleanupCategoryId.DeliveryOptimizationCache, "Caché de optimización de distribución",
                "Se analiza la ubicación conocida; no se limpia desde EVO ni se detiene el servicio.",
                () => [new(Path.Combine(common, "Microsoft", "Windows", "DeliveryOptimization", "Cache"))]),
            ReadOnly(CleanupCategoryId.Prefetch, "Prefetch",
                "Solo lectura. Windows administra esta caché; el borrado indiscriminado puede ralentizar el inicio de aplicaciones.",
                () => [new(Path.Combine(windows, "Prefetch"))]),
            new(CleanupCategoryId.RecycleBin, "Papelera de reciclaje",
                "Conteo agregado; Windows no permite seleccionar elementos y filtrar por antigüedad desde este flujo.",
                false, static () => [], ReadOnlyStatus:
                "Análisis agregado solamente; no se vacía la Papelera."),
            new(CleanupCategoryId.RegistryDiagnostics, "Diagnóstico del registro",
                "Solo lectura. No se usan heurísticas ni se eliminan claves; los ajustes compatibles siguen en Tweaks Lab.",
                false, static () => [], ReadOnlyStatus:
                "No existe un verificador basado en evidencia de claves obsoletas conectado. No se inventan hallazgos.")
        ];
    }

    private static CategoryDefinition Cleanable(
        CleanupCategoryId id,
        string name,
        string description,
        Func<IReadOnlyList<DirectoryTarget>> getTargets) =>
        new(id, name, description, true, getTargets);

    private static CategoryDefinition ReadOnly(
        CleanupCategoryId id,
        string name,
        string status,
        Func<IReadOnlyList<DirectoryTarget>> getTargets) =>
        new(id, name, status, false, getTargets, ReadOnlyStatus: status);

    private static CategoryDefinition Browser(
        CleanupCategoryId id,
        string name,
        string processName,
        Func<IReadOnlyList<DirectoryTarget>> getTargets) =>
        new(
            id,
            name,
            $"Solo cachés temporales identificadas; no se tocan cookies, sesiones, contraseñas, historial ni perfiles. Cierra {name.Replace("Caché de ", string.Empty, StringComparison.Ordinal)} antes de limpiar.",
            true,
            getTargets,
            processName);

    private static IReadOnlyList<DirectoryTarget> GetChromiumCacheTargets(string userDataPath)
    {
        if (!Directory.Exists(userDataPath))
            return Array.Empty<DirectoryTarget>();

        var targets = new List<DirectoryTarget>();
        foreach (string profile in Directory.EnumerateDirectories(userDataPath, "*", SearchOption.TopDirectoryOnly))
        {
            foreach (string subPath in new[] { Path.Combine("Cache", "Cache_Data"), "Code Cache", "GPUCache" })
            {
                string path = Path.Combine(profile, subPath);
                if (Directory.Exists(path))
                    targets.Add(new DirectoryTarget(path));
            }
        }
        return targets;
    }

    private static IReadOnlyList<DirectoryTarget> GetFirefoxCacheTargets(string profilesPath)
    {
        if (!Directory.Exists(profilesPath))
            return Array.Empty<DirectoryTarget>();

        return Directory.EnumerateDirectories(profilesPath, "*", SearchOption.TopDirectoryOnly)
            .Select(profile => Path.Combine(profile, "cache2", "entries"))
            .Where(Directory.Exists)
            .Select(path => new DirectoryTarget(path))
            .ToArray();
    }

    private static bool IsProcessRunning(string processName)
    {
        try
        {
            Process[] processes = Process.GetProcessesByName(processName);
            try
            {
                return processes.Length > 0;
            }
            finally
            {
                foreach (Process process in processes)
                    process.Dispose();
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return true;
        }
    }

    private static bool IsFileSystemError(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or ArgumentException or
            NotSupportedException or System.Security.SecurityException;

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Advanced system cleanup is available only on Windows.");
    }

    private static bool HasSafeDirectoryChain(string filePath, string rootPath)
    {
        string root = Path.GetFullPath(rootPath);
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
            return false;

        string? directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        while (directory is not null &&
               IsPathWithinRoot(directory + Path.DirectorySeparatorChar, root))
        {
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                return false;
            if (string.Equals(
                    Path.TrimEndingDirectorySeparator(directory),
                    Path.TrimEndingDirectorySeparator(root),
                    StringComparison.OrdinalIgnoreCase))
                return true;
            directory = Path.GetDirectoryName(directory);
        }

        return false;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RecycleBinInfo
    {
        public uint Size;
        public long Bytes;
        public long ItemCount;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int SHQueryRecycleBinW(string? rootPath, ref RecycleBinInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct ByHandleFileInformation
    {
        public uint FileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(
        SafeFileHandle file,
        out ByHandleFileInformation information);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    private static extern uint GetCompressedFileSizeW(string fileName, out uint fileSizeHigh);
}
