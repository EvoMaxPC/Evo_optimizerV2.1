using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EvoOptimizer.App.Navigation;

public enum EvoModuleStatus
{
    Available,
    Partial,
    Planned,
    Backlog,
    Blocked
}

public enum EvoModuleRoute
{
    Dashboard,
    Diagnosis,
    ControlCenter,
    Scanner,
    LiveMonitor,
    Tweaks,
    Debloat,
    HardwareDiagnostics,
    OptimizationCatalog,
    Cs2Configuration,
    Benchmark,
    EvoAi,
    SystemCleaner,
    Placeholder
}

public sealed record EvoNavigationCategory(
    string Number,
    string Name,
    string Icon,
    bool InitiallyExpanded,
    IReadOnlyList<EvoNavigationModule> Modules);

public sealed class EvoNavigationModule(
    string id,
    string name,
    string description,
    EvoModuleStatus status,
    string existingComponent,
    string statusDetails,
    EvoModuleRoute route) : INotifyPropertyChanged
{
    private bool _isSelected;

    public string Id { get; } = id;
    public string Name { get; } = name;
    public string Description { get; } = description;
    public EvoModuleStatus Status { get; } = status;
    public string ExistingComponent { get; } = existingComponent;
    public string StatusDetails { get; } = statusDetails;
    public EvoModuleRoute Route { get; } = route;

    public string StatusLabel => Status switch
    {
        EvoModuleStatus.Available => "DISPONIBLE",
        EvoModuleStatus.Partial => "PARCIAL",
        EvoModuleStatus.Planned => "PENDIENTE",
        EvoModuleStatus.Backlog => "BACKLOG",
        EvoModuleStatus.Blocked => "BLOQUEADO",
        _ => throw new InvalidOperationException($"Unknown module status: {Status}")
    };

    public string StatusColor => Status switch
    {
        EvoModuleStatus.Available => "#42D392",
        EvoModuleStatus.Partial => "#F5B84B",
        EvoModuleStatus.Planned => "#93A0AF",
        EvoModuleStatus.Backlog => "#FF8B72",
        EvoModuleStatus.Blocked => "#FF6670",
        _ => "#93A0AF"
    };

    public bool IsSelected
    {
        get => _isSelected;
        internal set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public static class EvoNavigationCatalog
{
    public static IReadOnlyList<EvoNavigationModule> CrosscuttingModules { get; } =
    [
        M("crosscutting.evo-ai", "EVO AI", "Inteligencia transversal conectada al diagnóstico y las recomendaciones.", EvoModuleStatus.Partial, "EvoAiDecisions + EvoControlCenterViewModel + pestaña EVO AI", "La pestaña actual presenta decisiones y advertencias; no sustituye autorización ni ejecuta cambios por sí sola.", EvoModuleRoute.EvoAi)
    ];

    public static IReadOnlyList<EvoNavigationCategory> Categories { get; } =
    [
        new("01", "DASHBOARD", "\uE80F", true,
        [
            M("home.dashboard", "Dashboard", "Resumen del equipo y telemetría disponible.", EvoModuleStatus.Available, "EvoDashboardShell + DashboardViewModel", "Dashboard integrado con datos reales de CPU/RAM y disponibilidad explícita para las métricas sin fuente.", EvoModuleRoute.Dashboard)
        ]),
        new("02", "TELEMETRÍA Y MONITOR", "\uE7F4", false,
        [
            M("gaming.gaming-monitor", "Live Monitor", "Monitorización disponible durante una sesión.", EvoModuleStatus.Partial, "EvoLiveMonitorPanel", "La telemetría existente no incluye FPS/frametime de todos los juegos.", EvoModuleRoute.LiveMonitor),
            M("diagnosis.smart-scan", "Smart Scan", "Escaneo general de hardware y configuración.", EvoModuleStatus.Available, "EvoTweakPanel · Scanner", "Abre el escáner existente, de solo lectura.", EvoModuleRoute.Scanner),
            M("home.notifications", "Notifications", "Alertas y anomalías generadas por los componentes existentes.", EvoModuleStatus.Partial, "Alerts + Anomalies en EvoLiveMonitorPanel", "Abre el monitor existente donde se muestran alertas; no hay una bandeja de notificaciones independiente.", EvoModuleRoute.LiveMonitor),
            M("lab.benchmark-lab", "Benchmark Lab", "Captura y persistencia de evidencia de benchmark.", EvoModuleStatus.Partial, "PresentMonCaptureService + benchmark persistence", "Abre Live Monitor; el laboratorio independiente aún no existe.", EvoModuleRoute.Benchmark),
            M("lab.ab-testing", "A/B Testing", "Comparación de mediciones antes/después.", EvoModuleStatus.Partial, "OptimizationMeasurementService + benchmark services", "Usa flujos existentes; no se crea una nueva sesión de comparación desde aquí.", EvoModuleRoute.Benchmark),
            M("lab.stability-lab", "Stability Lab", "Anomalías y estabilidad observada.", EvoModuleStatus.Partial, "LiveAnomalyDetector + EvoLiveMonitorPanel", "Abre Live Monitor; no ejecuta pruebas de estrés nuevas.", EvoModuleRoute.LiveMonitor),
            M("gaming.gaming-benchmarks", "Gaming Benchmarks", "Captura y evidencia de benchmark de juego.", EvoModuleStatus.Partial, "PresentMon capture/benchmark services", "Abre Live Monitor; las capturas dependen de las fuentes y flujos existentes.", EvoModuleRoute.Benchmark)
        ]),
        new("03", "DIAGNÓSTICO DE HARDWARE", "\uE9D9", false,
        [
            M("diagnosis.system-diagnostics", "Diagnostics", "Hallazgos explicables de Windows, hardware y telemetría.", EvoModuleStatus.Available, "WindowsSystemAuditor + AdaptiveDiagnosticEngine + LiveTelemetryDiagnosticEvaluator", "Ejecuta el análisis existente y exporta el informe.", EvoModuleRoute.Diagnosis),
            M("diagnosis.hardware-intelligence", "Hardware Intelligence", "Identificación y evidencia de hardware.", EvoModuleStatus.Partial, "RegistryHardwareProfileProvider + pestaña Diagnóstico de hardware", "Se muestran los datos que entregan las fuentes existentes; no predice rendimiento.", EvoModuleRoute.HardwareDiagnostics),
            M("diagnosis.compatibility-engine", "Compatibility Engine", "Compatibilidad entre hardware, drivers, aplicaciones y ajustes.", EvoModuleStatus.Planned, "Sin motor independiente conectado", "No se genera un resultado de compatibilidad hasta integrar y validar sus fuentes.", EvoModuleRoute.Placeholder),
            M("hardware.cpu-memory", "CPU & Memory", "Identificación y métricas disponibles de CPU/RAM.", EvoModuleStatus.Partial, "Dashboard telemetry + Hardware Diagnostics", "CPU global y memoria física medidas; frecuencia efectiva y sensores térmicos no se presumen.", EvoModuleRoute.HardwareDiagnostics),
            M("hardware.gpu-graphics", "GPU & Graphics", "Identificación de GPU y evidencia gráfica disponible.", EvoModuleStatus.Partial, "AdaptiveDiagnosticEngine + hardware audit", "La identificación puede estar disponible; uso, VRAM y temperatura pueden seguir sin fuente.", EvoModuleRoute.HardwareDiagnostics),
            M("hardware.storage-health", "Storage Health", "Información de unidades y salud, cuando existe evidencia.", EvoModuleStatus.Partial, "AdaptiveDiagnosticEngine + Windows audit", "Abre hallazgos de diagnóstico; SMART/NVMe no se inventan si la fuente no devuelve datos.", EvoModuleRoute.HardwareDiagnostics),
            M("hardware.power-intelligence", "Power Intelligence", "Evidencia de energía y plan activo.", EvoModuleStatus.Partial, "Hardware Diagnostics + Windows optimization services", "Consulta los hallazgos existentes; no cambia el plan desde esta ruta.", EvoModuleRoute.HardwareDiagnostics),
            M("hardware.input-latency", "Input Latency Lab", "Periféricos y diagnóstico de latencia de entrada.", EvoModuleStatus.Planned, "Sin proveedor de latencia conectado", "No se mide polling rate ni latencia de entrada todavía.", EvoModuleRoute.Placeholder),
            M("lab.bottleneck-predictor", "Bottleneck Predictor", "Hallazgos basados en hardware y evidencia medida.", EvoModuleStatus.Partial, "AdaptiveDiagnosticEngine + BottleneckFindings", "Consulta los hallazgos existentes; no afirma un cuello de botella sin evidencia.", EvoModuleRoute.HardwareDiagnostics)
        ]),
        new("04", "OPTIMIZADOR Y TWEAKS", "\uE713", false,
        [
            M("home.control-center", "Control Center", "Centro de mando para diagnóstico, decisiones y operaciones existentes.", EvoModuleStatus.Available, "EvoControlCenterWindow + EvoControlCenterViewModel", "Reutiliza el centro actual; no duplica el catálogo de Tweaks Lab.", EvoModuleRoute.ControlCenter),
            M("optimization.automatic-optimizer", "Automatic Optimizer", "Coordinación de optimizaciones sujetas al flujo de autorización.", EvoModuleStatus.Partial, "AdaptiveOptimizationEngine + EvoControlCenterViewModel", "Las operaciones disponibles están en el Control Center y conservan sus confirmaciones.", EvoModuleRoute.ControlCenter),
            M("optimization.optimization-planner", "Optimization Planner", "Recomendaciones del motor según evidencia disponible.", EvoModuleStatus.Partial, "OptimizationRecommendations en EvoControlCenterViewModel", "Las recomendaciones aparecen en el Control Center; no hay una pantalla de planificación propia.", EvoModuleRoute.ControlCenter),
            M("optimization.tweaks-lab", "Tweaks Lab", "Catálogo central de ajustes de Windows.", EvoModuleStatus.Available, "EvoTweakPanel · Tweaks", "Esta es la única ruta al catálogo de tweaks.", EvoModuleRoute.Tweaks),
            M("optimization.profile-studio", "Profile Studio", "Perfiles de uso y operaciones existentes.", EvoModuleStatus.Partial, "ApplyProfileAsync + objetivos del Dashboard", "Se reutilizan los perfiles existentes; no hay editor independiente de perfiles.", EvoModuleRoute.ControlCenter),
            M("windows.processes", "Processes", "Procesos y gobierno seguro de procesos.", EvoModuleStatus.Partial, "TopProcesses + ProcessGovernanceService", "Los controles existentes viven en el Control Center; los procesos críticos permanecen protegidos.", EvoModuleRoute.ControlCenter),
            M("windows.registry", "Registry", "Ajustes de registro compatibles con el catálogo central.", EvoModuleStatus.Partial, "EvoTweakPanel · Tweaks + RegistryTweakService", "Abre Tweaks Lab; no crea un segundo catálogo.", EvoModuleRoute.Tweaks),
            M("windows.debloat", "Debloat", "Gestión selectiva y restaurable de paquetes UWP.", EvoModuleStatus.Available, "EvoTweakPanel · Debloat", "Abre el flujo existente con confirmación y restauración.", EvoModuleRoute.Debloat),
            M("gaming.game-optimizer", "Game Optimizer", "Motor universal de optimización específica por juego.", EvoModuleStatus.Backlog, "T9-001 en backlog", "No iniciar en esta entrega; el módulo universal sigue pendiente.", EvoModuleRoute.Placeholder),
            M("gaming.game-profiles", "Game Profiles", "Configuración específica actualmente soportada.", EvoModuleStatus.Partial, "CS2 Config · Cs2AutoexecConfigService", "Abre la gestión limitada y reversible de autoexec.cfg para CS2.", EvoModuleRoute.Cs2Configuration)
        ]),
        new("05", "MANTENIMIENTO Y LIMPIEZA", "\uE74D", false,
        [
            M("windows.cleaner", "Cleaner", "Análisis y limpieza selectiva de temporales y cachés identificadas.", EvoModuleStatus.Partial, "AdvancedSystemCleanupService + AdvancedSystemCleanupViewModel", "Limpieza real con antigüedad seleccionable, exclusiones y registro por archivo; Windows Update, Prefetch, Papelera y registro son solo lectura.", EvoModuleRoute.SystemCleaner),
            M("windows.services", "Services", "Estado y evidencia de servicios de Windows.", EvoModuleStatus.Partial, "WindowsSystemAuditor + catalog services", "Los resultados existentes están integrados al diagnóstico y catálogo, no en una pantalla independiente.", EvoModuleRoute.OptimizationCatalog),
            M("windows.startup-manager", "Startup Manager", "Aplicaciones de inicio detectadas y acciones existentes.", EvoModuleStatus.Partial, "WindowsStartupCandidate + Scanner/Control Center", "La lista depende de la auditoría; no se ejecutan cambios desde esta ficha.", EvoModuleRoute.Scanner),
            M("lab.performance-reports", "Performance Reports", "Informes existentes y resultados de mediciones.", EvoModuleStatus.Partial, "Audit export + HistoryDashboardService", "La exportación de auditoría existe; no hay un centro unificado de informes.", EvoModuleRoute.Placeholder),
            M("admin.history", "History", "Historial de operaciones y mediciones.", EvoModuleStatus.Partial, "HistoryDashboardService + ChangeJournal", "La persistencia existe parcialmente; todavía no hay una pantalla de historial integrada.", EvoModuleRoute.Placeholder),
            M("admin.update-guardian", "Update Guardian", "Seguimiento de regresiones relacionadas con actualizaciones.", EvoModuleStatus.Partial, "RegressionRecoveryService", "El servicio existe; no hay una pantalla/flujo de guardian integrado.", EvoModuleRoute.Placeholder),
            M("admin.smart-maintenance", "Smart Maintenance", "Mantenimiento programado y controlado.", EvoModuleStatus.Planned, "Sin coordinador de mantenimiento conectado", "No se crean tareas ni se programa mantenimiento desde EVO.", EvoModuleRoute.Placeholder),
            M("admin.driver-advisor", "Driver Advisor", "Asesoramiento de drivers con fuentes verificables.", EvoModuleStatus.Planned, "Sin módulo de asesoramiento de drivers", "No descarga ni recomienda drivers en esta versión.", EvoModuleRoute.Placeholder)
        ]),
        new("06", "EVO AI & AJUSTES", "\uE99A", false,
        [
            M("windows.network", "Network", "Adaptadores, conectividad y latencia de red.", EvoModuleStatus.Planned, "Sin pantalla/proveedor de diagnóstico de red", "No se mide ni modifica la red desde esta ruta.", EvoModuleRoute.Placeholder),
            M("windows.privacy", "Privacy", "Inspección y ajustes de privacidad de Windows.", EvoModuleStatus.Planned, "Sin módulo de privacidad conectado", "No se muestran ni aplican ajustes de privacidad todavía.", EvoModuleRoute.Placeholder),
            M("admin.settings", "Settings", "Configuración general y objetivos de uso.", EvoModuleStatus.Partial, "Objetivos de uso en DashboardViewModel", "Los objetivos están en la barra lateral; no existe aún una pantalla general de ajustes.", EvoModuleRoute.Placeholder),
            CrosscuttingModules[0]
        ])
    ];

    private static readonly IReadOnlyDictionary<string, EvoNavigationModule> ModulesById =
        BuildModuleIndex();

    static EvoNavigationCatalog() => Select("home.dashboard");

    public static EvoNavigationModule? Find(string id) =>
        ModulesById.GetValueOrDefault(id);

    public static void Select(string id)
    {
        foreach (var module in ModulesById.Values)
            module.IsSelected = module.Id == id;
    }

    private static IReadOnlyDictionary<string, EvoNavigationModule> BuildModuleIndex()
    {
        var modules = Categories.SelectMany(category => category.Modules)
            .ToArray();
        var duplicate = modules
            .GroupBy(module => module.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Duplicate EVO navigation ID: {duplicate.Key}");

        return modules.ToDictionary(module => module.Id, StringComparer.Ordinal);
    }

    private static EvoNavigationModule M(
        string id,
        string name,
        string description,
        EvoModuleStatus status,
        string existingComponent,
        string statusDetails,
        EvoModuleRoute route) =>
        new(id, name, description, status, existingComponent, statusDetails, route);
}
