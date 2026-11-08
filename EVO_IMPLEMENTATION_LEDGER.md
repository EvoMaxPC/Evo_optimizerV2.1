# EVO_IMPLEMENTATION_LEDGER.md
> Inventario acumulado de implementaciones. Última actualización: 2026-11-07T20:05:00-03:00

---

## Convención de estados

| Estado | Significado |
| :--- | :--- |
| `PLANNED` | Identificado, no iniciado |
| `IMPLEMENTED` | Código escrito, sin integrar |
| `INTEGRATED` | Conectado a la arquitectura real |
| `BUILD_VERIFIED` | Compila sin errores ni advertencias |
| `TEST_VERIFIED` | Pruebas unitarias pasadas |
| `FUNCTIONALLY_VERIFIED` | Verificado en ejecución real (manual o automatizada) |

> [!IMPORTANT]
> **Nunca marcar como FUNCTIONALLY_VERIFIED algo que solo compila.**

## TANDA 12 — Limpieza avanzada del sistema (primer tramo seguro)

### T12-001 · Cleaner integrado con análisis real y limpieza por antigüedad
| Campo | Valor |
| :--- | :--- |
| **Estado** | `INTEGRATED` + `TEST_VERIFIED`; recorrido interactivo y limpieza real pendientes |
| **Archivos** | `src/EvoOptimizer.Infrastructure/Cleanup/AdvancedSystemCleanupService.cs`, `src/EvoOptimizer.App/ViewModels/AdvancedSystemCleanupViewModel.cs`, `src/EvoOptimizer.App/Views/EvoSystemCleanerPanel.xaml(.cs)`, `src/EvoOptimizer.App/Navigation/EvoNavigationCatalog.cs`, `src/EvoOptimizer.App/Views/EvoDashboardShell.xaml(.cs)`, `src/EvoOptimizer.App/MainWindow.xaml.cs`, `tests/EvoOptimizer.Core.Tests/AdvancedSystemCleanupServiceTests.cs` |
| **Alcance** | Integración de `Cleaner` en WINDOWS con categorías reales, filtros configurables, validación de rutas e historial local |

**Implementación:**
- El análisis recorre solo ubicaciones conocidas: TEMP del usuario y Windows, WER, volcados `.dmp`, miniaturas, cachés de shaders, cachés temporales de Edge/Chrome/Firefox, Windows Update, Delivery Optimization y Prefetch. Evita seguir puntos de reanálisis y deduplica las rutas físicas encontradas.
- Se pueden seleccionar categorías limpiables y antigüedad mínima de 1, 3, 7, 14, 30 o 90 días. La limpieza vuelve a comprobar la ruta autorizada, la fecha, los atributos, los enlaces duros y el acceso exclusivo; no eleva privilegios, no borra carpetas y omite archivos inaccesibles o bloqueados. No procesa cachés de navegador mientras el navegador correspondiente esté abierto.
- Solo son limpiables temporales, WER, volcados antiguos, miniaturas, cachés identificadas de shaders y cachés temporales de navegador. Cookies, contraseñas, sesiones, historial, perfiles, juegos, partidas y configuraciones no forman parte de las rutas.
- El historial JSONL local registra resultado por archivo y solo atribuye bytes liberados a archivos eliminados cuya asignación física se midió; conserva por separado estimaciones del análisis.
- Windows Update, Delivery Optimization, Prefetch y Papelera son informativos/de solo lectura. El registro no se modifica ni se declara analizado: todavía no existe un verificador conectado que identifique claves obsoletas con evidencia suficiente.

**Validación:** `dotnet build EVO-FPS-OPTIMIZER.slnx -c Debug --no-restore` compiló la solución en salida aislada mientras la aplicación abierta bloqueaba el destino Debug normal: 0 errores y una advertencia xUnit2012 preexistente en `UnitTest1.cs:1030`. `dotnet test tests/EvoOptimizer.Core.Tests/EvoOptimizer.Core.Tests.csproj -c Debug` pasó 131/131, incluida una exploración real de solo lectura de las categorías y pruebas de límites de rutas/antigüedad. La aplicación abrió con el Dashboard, pero la usuaria estaba interactuando físicamente con EVO; Computer Use devolvió `interrupted` dos veces y no se insistió. Por ello, la navegación al panel y el flujo de limpieza desde UI no quedan certificados; tampoco se borró ningún archivo del equipo.

## TANDA 11 — Navegación modular y catálogo de estados

### T11-001 · Sidebar de ocho categorías conectada a módulos existentes
| Campo | Valor |
| :--- | :--- |
| **Estado** | `BUILD_VERIFIED` + `TEST_VERIFIED`; recorrido visual de todas las rutas pendiente |
| **Archivos** | `src/EvoOptimizer.App/Navigation/EvoNavigationCatalog.cs`, `src/EvoOptimizer.App/Views/EvoDashboardShell.xaml(.cs)`, `src/EvoOptimizer.App/Views/EvoControlCenterWindow.xaml.cs`, `src/EvoOptimizer.App/Views/EvoOptimizationHubPanel.xaml.cs`, `src/EvoOptimizer.App/Views/EvoTweakPanel.xaml(.cs)`, `EVO_IMPLEMENTATION_LEDGER.md` |
| **Alcance** | IDs de navegación únicos, agrupación por categoría, estados veraces y rutas al UI/servicios que ya existen |

**Mapa de componentes verificado antes del cambio:**
- Inicio: `EvoDashboardShell`/`DashboardViewModel`; `EvoControlCenterWindow`/`EvoControlCenterViewModel`; alertas presentes dentro del ViewModel, aún sin bandeja propia.
- Diagnóstico: `EvoTweakPanel` Scanner; `WindowsSystemAuditor` + `AdaptiveDiagnosticEngine` + `LiveTelemetryDiagnosticEvaluator`; proveedor de hardware/diagnóstico del Hub; sin motor de compatibilidad independiente.
- Optimización: `AdaptiveOptimizationEngine`, recomendaciones en el ViewModel, catálogo único en Tweaks Lab y perfiles existentes; sin planner ni editor de perfiles separado.
- Gaming: Live Monitor y captura/benchmark parcialmente disponibles; configuración limitada de CS2 existente; `T9-001 Game Optimizer` queda en backlog.
- Hardware: fuentes parciales para CPU/RAM, GPU, almacenamiento y energía en Dashboard/diagnóstico; sin laboratorio de latencia de entrada.
- Windows: procesos, servicios, startup y registry están distribuidos entre Control Center/Scanner/Tweaks; Debloat tiene pantalla propia; Cleaner, Network y Privacy no están conectados.
- Laboratorio: servicios de PresentMon, medición, anomalías y hallazgos existen parcialmente; no hay pantallas independientes para los laboratorios ni centro unificado de informes.
- Administración: servicios de historial/recuperación existen parcialmente; no hay vistas de historial, Update Guardian o mantenimiento; Settings se limita a objetivos del Dashboard.
- EVO AI: pestaña transversal del Hub, enlazada aparte de las ocho categorías para no presentarla como motor de acción autónomo.

**Implementación:** catálogo central con 38 módulos listados en el adjunto (el documento los llama “26”, discrepancia pendiente de aclaración documental) y un enlace transversal a EVO AI; IDs únicos validados, categorías desplegables con iconos, estados `DISPONIBLE`/`PARCIAL`/`PENDIENTE`/`BACKLOG`/`BLOQUEADO` y marca de ruta activa. Las rutas implementadas abren las pantallas actuales; los módulos sin pantalla propia muestran una ficha que explica estado, componente disponible y límites sin acciones simuladas. El catálogo de Tweaks solo se abre desde Tweaks Lab/Registry; las demás rutas no lo duplican.

**Validación:** builds y tests Debug/Release de la solución correctos en salida aislada; 124/124 tests en ambas configuraciones; diff-check limpio. La compilación inicial completa reportó la advertencia xUnit2012 preexistente en `tests/EvoOptimizer.Core.Tests/UnitTest1.cs:1030`; la última compilación incremental terminó con 0 advertencias y 0 errores. La navegación aún no se ha recorrido en UI tras este cambio, por lo que no se declara `FUNCTIONALLY_VERIFIED`. No se inició T9-001, no se movieron proyectos/clases ni se editaron el auditor protegido, sus backups o `DIST/`.

## TANDA 10 — Dashboard inspirado en la referencia EVO

### T10-001 · UX: nuevo shell visual sin sustituir las funciones existentes
| Campo | Valor |
| :--- | :--- |
| **Estado** | `FUNCTIONALLY_VERIFIED` + `BUILD_VERIFIED` + `TEST_VERIFIED` |
| **Archivos** | `src/EvoOptimizer.App/MainWindow.xaml`, `src/EvoOptimizer.App/MainWindow.xaml.cs`, nuevos `src/EvoOptimizer.App/Views/EvoDashboardShell.xaml`, `src/EvoOptimizer.App/Views/EvoDashboardShell.xaml.cs`, `src/EvoOptimizer.App/ViewModels/DashboardViewModel.cs`, `EVO_IMPLEMENTATION_LEDGER.md` |
| **Alcance** | Presentación y navegación hacia funciones ya integradas |

**Qué se hizo:**
- Dashboard de grafito/azul inspirado en la imagen: barra de producto con controles de ventana, navegación lateral, tarjetas de CPU/GPU/RAM/almacenamiento/Windows, gráficas livianas, accesos existentes, resumen de CS2 y panel de detalles diagnósticos.
- Los botones de navegación abren Dashboard, diagnóstico, Control Center, Scanner/Tweaks/Debloat, Live Monitor y el tab CS2 Config real. Se conservan el analizador, exportación, métricas y paneles existentes.
- Las superficies sin fuente actual (uso de GPU, temperatura, capacidad de disco, actualizaciones Windows, FPS, lows, frametime, frecuencia del monitor) se marcan indisponibles/no medidas. No se copian valores ilustrativos de la maqueta.
- La hora de cabecera es la hora local del equipo; las curvas siguen usando las muestras reales CPU/RAM ya capturadas.

**Validación funcional:** la aplicación Debug compilada abrió con la nueva ventana y la UI mostró CPU/RAM obtenidas en vivo, hardware detectado y capacidades no soportadas marcadas como no disponibles. Se ejecutó el análisis desde el botón; mostró 22 hallazgos (incluidos hechos CIM y las observaciones `LIVE_TELEMETRY_COVERAGE`, `LIVE_CPU_LOAD` y `LIVE_MEMORY_LOAD`), 2 advertencias y la ruta del informe exportado. Se confirmó que el TXT existe y contiene la auditoría de solo lectura. No se aplicaron cambios al sistema.

**Incidencia corregida durante la validación:** el primer arranque falló porque `ProgressBar.Value` usaba el modo TwoWay predeterminado contra `MemoryUsagePercentValue`, una propiedad de solo lectura. Los bindings `CpuUsagePercentValue` y `MemoryUsagePercentValue` del Dashboard ahora especifican `Mode=OneWay`; tras rebuild, el inicio y el análisis completaron correctamente.

**Validación automatizada:** `dotnet build EVO-FPS-OPTIMIZER.slnx -c Debug --no-restore` y `-c Release --no-restore` → 0 advertencias, 0 errores; `dotnet test EVO-FPS-OPTIMIZER.slnx -c Debug --no-build` y `-c Release --no-build` → 124/124 en ambas configuraciones; `git diff --check` limpio (solo advertencias informativas de normalización LF/CRLF).

## TANDA 8 — Diagnóstico con telemetría real del Dashboard

### T8-001 · Evidencia observacional de hardware, CPU y memoria
| Campo | Valor |
| :--- | :--- |
| **Estado** | `FUNCTIONALLY_VERIFIED` + `TEST_VERIFIED` |
| **Archivos** | `src/EvoOptimizer.Core/Diagnosis/LiveTelemetryDiagnosticEvaluator.cs`, `src/EvoOptimizer.App/ViewModels/DashboardViewModel.cs`, `src/EvoOptimizer.App/MainWindow.xaml.cs`, `tests/EvoOptimizer.Core.Tests/LiveTelemetryDiagnosticEvaluatorTests.cs` |
| **Alcance** | Enriquecer el análisis manual existente; solo lectura, no aplica optimizaciones |

**Qué se hizo:**
- El botón de análisis combina el diagnóstico existente de la auditoría Windows/CIM con CPU global y memoria física que el Dashboard ya muestrea con `GetSystemTimes` y `GlobalMemoryStatusEx`.
- El informe incluye perfil real de hardware, cobertura/ventana y promedio/máximo de carga. Señala carga alta solo con al menos cinco muestras y umbral elevado en al menos el 80% de ellas; lo califica como señal observacional, no como cuello de botella confirmado.
- Ausencia o insuficiencia de mediciones queda explícita; no se sustituye por cero. No se diagnostica temperatura, GPU, procesos, FPS ni frametime sin fuentes disponibles.
- Se reutilizó el flujo de `AnalyzeSystem_Click`, sus controles y exportación de auditoría. Sin escrituras del sistema.

**Pruebas añadidas:** insuficiencia de muestras, medición normal CPU/RAM, CPU elevada sostenida, picos no sostenidos, presión de memoria sostenida y perfil de hardware.

**Validación funcional:** desde el Dashboard se ejecutó el botón de análisis y se observaron en el mismo listado hallazgos de auditoría Windows/CIM junto con CPU/RAM medidas durante 30 muestras (ventana de 29 s en el hallazgo de cobertura); el informe terminó con 22 hallazgos y 2 advertencias y se exportó a `C:\Users\ZuKA\AppData\Local\EvoFpsOptimizer\Audits\EVO_Audit_20261107-071150.txt`. La exportación existe y contiene auditoría de solo lectura.

**Pruebas:** 124/124 en Debug y 124/124 en Release; builds de la solución Debug/Release con 0 advertencias y 0 errores; `git diff --check` limpio. No se modificó Windows.

## TANDA 7 — Integración de la navegación en la ventana principal

### T7-001 · UX: Dashboard y módulos existentes dentro de un único shell
| Campo | Valor |
| :--- | :--- |
| **Estado** | `FUNCTIONALLY_VERIFIED` |
| **Prioridad** | ALTA |
| **Archivos** | `src/EvoOptimizer.App/MainWindow.xaml`, `src/EvoOptimizer.App/MainWindow.xaml.cs`, `src/EvoOptimizer.App/Views/EvoControlCenterWindow.xaml`, `src/EvoOptimizer.App/Views/EvoControlCenterWindow.xaml.cs`, eliminación de `src/EvoOptimizer.App/Navigation/EvoControlCenterLauncher.cs` |
| **Alcance** | Integración del Dashboard, Hub, Scanner/Tweaks/Debloat y Live Monitor ya existentes |

**Qué se hizo:**
- Se incorporó una sidebar en la ventana principal con acceso al Dashboard y al Control Center; el contenido de este último conserva sus tres pestañas funcionales.
- `EvoControlCenterWindow` ahora es un `UserControl` reutilizable; sus paneles y su ViewModel se integran en MainWindow. Se retiró el launcher que abría una segunda ventana.
- El cierre de MainWindow espera la liberación asíncrona del ViewModel del Control Center y luego del Dashboard.
- No se añadieron herramientas para las áreas del nuevo brief que todavía no tienen implementaciones conectadas.

**Validación:** build Debug exitoso; 107/107 pruebas; `git diff --check` limpio. Build Release exitoso y se generó `src/EvoOptimizer.App/bin/Release/net10.0-windows/EvoOptimizer.App.exe` (una advertencia xUnit2012 preexistente en `tests/EvoOptimizer.Core.Tests/UnitTest1.cs:1030`). Se abrió la aplicación compilada y se navegó entre Dashboard, Control Center, Scanner/Tweaks/Debloat y Live Monitor con Computer Use.

## TANDA 6 — Rediseño visual premium de superficies existentes

### T6-001 · UX: sistema visual compartido y jerarquía del Dashboard / Control Center
| Campo | Valor |
| :--- | :--- |
| **Estado** | `TEST_VERIFIED` |
| **Prioridad** | ALTA |
| **Archivos** | `src/EvoOptimizer.App/App.xaml`, `src/EvoOptimizer.App/MainWindow.xaml`, `src/EvoOptimizer.App/Themes/EvoDark.xaml`, `src/EvoOptimizer.App/Views/EvoControlCenterWindow.xaml`, `src/EvoOptimizer.App/Views/EvoOptimizationHubPanel.xaml`, `src/EvoOptimizer.App/Views/EvoTweakPanel.xaml`, `src/EvoOptimizer.App/Views/EvoLiveMonitorPanel.xaml` |
| **Alcance** | Presentación XAML; no se modificaron ViewModels, comandos ni servicios |

**Qué se hizo:**
- Se estableció un tema global grafito con superficies y bordes diferenciados, azul eléctrico y colores semánticos; botones con estados hover, pressed y disabled, y paneles con radios/espaciado coherentes.
- El Dashboard presenta la identidad «PERFORMANCE ENGINEERING SYSTEM» y objetivos de uso; sus binding names, análisis y datos reales se conservaron.
- El Control Center adopta una navegación por pestañas estilizadas. Se mantienen únicamente sus tres vistas conectadas: Control Center, Scanner/Tweaks/Debloat y Live Monitor.
- Se migró la paleta de las vistas existentes sin añadir módulos decorativos ni datos simulados.

**Validación:** la compilación, las pruebas y el recorrido de UI en ejecución se registran en T7-001.

---

## TANDA 1 — Estabilidad del Control Center e integración del motor adaptativo

### T1-001 · Fix: Crash por binding TwoWay en EvoControlCenterWindow
| Campo | Valor |
| :--- | :--- |
| **Estado** | `BUILD_VERIFIED` |
| **Prioridad** | CRÍTICA |
| **Archivos** | `src/EvoOptimizer.App/Views/EvoControlCenterWindow.xaml.cs`, `src/EvoOptimizer.App/Views/EvoControlCenterWindow.xaml` |
| **Dependencias** | `EvoControlCenterViewModel`, `EvoLiveMonitorPanel` |

**Problema resuelto:**  
`System.InvalidOperationException: TwoWay o OneWayToSource no puede funcionar en la propiedad de solo lectura "MemoryUsagePercent"`.  
Causa: WPF heredaba el DataContext del propietario (`MainWindow` → `DashboardViewModel`) durante el primer layout pass antes de que code-behind asignara el ViewModel correcto.

**Solución:**  
- El `EvoControlCenterViewModel` se instancia **antes** de `InitializeComponent()`.
- Se asigna `DataContext` tanto en la ventana como en el panel explícitamente, eliminando herencia del árbol visual del Owner.
- Todos los bindings XAML del panel tienen `Mode=OneWay` explícito.

---

### T1-002 · Integración: AdaptiveOptimizationEngine → EvoControlCenterViewModel
| Campo | Valor |
| :--- | :--- |
| **Estado** | `BUILD_VERIFIED` |
| **Prioridad** | ALTA |
| **Archivos** | `src/EvoOptimizer.App/ViewModels/EvoControlCenterViewModel.cs` |
| **Dependencias** | `EvoOptimizer.Core.Optimization.AdaptiveOptimizationEngine`, `LiveAnomalyDetector.OptimizationSignals` |

**Qué se hizo:**  
- El ViewModel integra `AdaptiveOptimizationEngine` (Core) como dependencia inyectable.  
- En cada ciclo de telemetría, el detector de anomalías genera señales que se pasan al motor adaptativo.  
- El resultado (`AdaptiveOptimizationReport`) se publica en la colección observable `OptimizationRecommendations` que se renderiza en la UI.

---

### T1-003 · UI: Panel de Decisiones Adaptativas en EvoLiveMonitorPanel
| Campo | Valor |
| :--- | :--- |
| **Estado** | `BUILD_VERIFIED` |
| **Prioridad** | MEDIA |
| **Archivos** | `src/EvoOptimizer.App/Views/EvoLiveMonitorPanel.xaml` |
| **Dependencias** | `EvoControlCenterViewModel.OptimizationRecommendations` |

**Qué se hizo:**  
- Panel "DECISIONES ADAPTATIVAS" en `EvoLiveMonitorPanel`.  
- Muestra: título, prioridad, riesgo, explicación, confianza, reversibilidad y si requiere aprobación.

---

### T1-004 · Tests: AdaptiveOptimizationEngine y RegressionGuard
| Campo | Valor |
| :--- | :--- |
| **Estado** | `TEST_VERIFIED` |
| **Prioridad** | ALTA |
| **Archivos** | `tests/EvoOptimizer.Core.Tests/UnitTest1.cs` |
| **Dependencias** | `AdaptiveOptimizationEngine`, `RegressionGuard` |
**Resultado: 13/13 ✅**

---

## TANDA 2 — Inteligencia de Hardware Específica (Intel Alder Lake i3-1220P + Intel UHD + Windows 11)

### T2-001 · Detección y Modelado de Arquitectura Híbrida Intel (P-Cores vs E-Cores)
| Campo | Valor |
| :--- | :--- |
| **Estado** | `TEST_VERIFIED` |
| **Prioridad** | CRÍTICA |
| **Archivos** | `src/EvoOptimizer.Core/Hardware/CpuTopologyInfo.cs`, `src/EvoOptimizer.Infrastructure/Hardware/IntelHybridTopologyProvider.cs`, `src/EvoOptimizer.Infrastructure/Native/NativeMethods.cs` |
| **Dependencias** | Win32 `GetLogicalProcessorInformationEx` (`RelationProcessorCore`) |

**Qué se implementó:**  
- Descubrimiento de topología en tiempo de ejecución: clasifica núcleos físicos por `EfficiencyClass` (1 = Performance / P-Core, 0 = Efficient / E-Core) y detecta HyperThreading (`LtpPcSmt`).
- Para el Intel Core i3-1220P detecta exactamente: 10 núcleos físicos, 2 P-Cores con HT (4 hilos, máscara `0x00F`), 8 E-Cores sin HT (8 hilos, máscara `0xFF0`), total 12 hilos lógicos (`0xFFF`).
- Cálculo de máscaras de afinidad para gaming (`PerformanceCoresOnly`, `PerformanceAndLightEfficiency`, `ExcludeCore0`) para evitar saltos de hilos de CS2 hacia E-cores de mayor latencia.

---

### T2-002 · Telemetría en Tiempo Real de GPU Intel UHD mediante PDH
| Campo | Valor |
| :--- | :--- |
| **Estado** | `BUILD_VERIFIED` |
| **Prioridad** | ALTA |
| **Archivos** | `src/EvoOptimizer.Infrastructure/Telemetry/WindowsGpuReader.cs`, `src/EvoOptimizer.Infrastructure/Telemetry/LivePerformanceMonitor.cs` |
| **Dependencias** | `pdh.dll` (`PdhOpenQuery`, `PdhAddEnglishCounter`, `PdhCollectQueryData`) |

**Qué se implementó:**  
- Lector nativo en C# utilizando la API Win32 de PDH sin dependencias externas NuGet.
- Muestrea la utilización del motor 3D de GPU (`\GPU Engine(*engtype_3D*)\Utilization Percentage`).
- Muestrea memoria VRAM dinámica compartida (`\GPU Adapter Memory(*)\Shared Usage`).

---

### T2-003 · Detección de Configuración de Windows Gaming y Señales del Sistema
| Campo | Valor |
| :--- | :--- |
| **Estado** | `BUILD_VERIFIED` |
| **Prioridad** | ALTA |
| **Archivos** | `src/EvoOptimizer.Infrastructure/Hardware/WindowsGamingConfigService.cs` |
| **Dependencias** | Registro de Windows (`HKCU\GameDVR`, `HKCU\GameBar`, `HKLM\GraphicsDrivers`), `powercfg` |

**Qué se implementó:**  
- Inspección de GameDVR (`AppCaptureEnabled`), GameMode (`AllowAutoGameMode`), HAGS (`HwSchMode`) y Plan de Energía Activo.
- Generación de señales automáticas `GAMEDVR_CAPTURE_ENABLED` y `GAME_MODE_DISABLED`.

---

### T2-004 · Telemetría de Procesos Competidores y Memoria Commit
| Campo | Valor |
| :--- | :--- |
| **Estado** | `BUILD_VERIFIED` |
| **Prioridad** | ALTA |
| **Archivos** | `src/EvoOptimizer.Infrastructure/Telemetry/LivePerformanceMonitor.cs` |
| **Dependencias** | `GlobalMemoryStatusEx`, `Process.TotalProcessorTime` |

**Qué se implementó:**  
- Cálculo delta de CPU para todos los procesos activos en cada intervalo de muestreo.
- Lista en vivo de los principales procesos competidores (nombre, PID, % CPU y MB de RAM).
- Medición de memoria comprometida (Commit Used y Commit Limit).

---

### T2-005 · Integración de HardwareBottleneckAnalyzer al Control Center
| Campo | Valor |
| :--- | :--- |
| **Estado** | `TEST_VERIFIED` |
| **Prioridad** | CRÍTICA |
| **Archivos** | `src/EvoOptimizer.App/ViewModels/EvoControlCenterViewModel.cs`, `src/EvoOptimizer.Core/Analysis/HardwareBottleneckAnalyzer.cs` |
| **Dependencias** | `HardwarePerformanceSample`, `LivePerformanceMonitor` |

**Qué se implementó:**  
- Conexión del historial de telemetría hacia `HardwareBottleneckAnalyzer`.
- Identificación de cuello de botella primario (`GPU`, `CPU`, `MEMORIA`, `ALMACENAMIENTO` o `EQUILIBRADO`).

---

## TANDA 3 — Optimizador Adaptativo de Rendimiento para CS2 y Sistema A/B

### T3-001 · Optimizador de Afinidad de CPU para CS2 (Cs2AffinityOptimizer)
| Campo | Valor |
| :--- | :--- |
| **Estado** | `BUILD_VERIFIED` |
| **Prioridad** | CRÍTICA |
| **Archivos** | `src/EvoOptimizer.Infrastructure/Optimization/Cs2AffinityOptimizer.cs`, `src/EvoOptimizer.Infrastructure/Native/NativeMethods.cs` |
| **Dependencias** | Win32 `SetProcessAffinityMask`, `GetProcessAffinityMask`, `CpuTopologyInfo` |

**Qué se implementó:**  
- Ajuste adaptativo y seguro de afinidad exclusivo para `cs2.exe`.
- Soporta modos:
  - `PerformanceCoresOnly` (Máscara `0x00F` en i3-1220P): elimina los saltos a E-cores que provocan stutter y caídas de 1% low.
  - `PerformanceAndLightEfficiency` (Máscara `0x03F`): 2 P-Cores + 2 E-Cores auxiliares.
- Verificación criptográfica de identidad de proceso por PID y `StartTimeUtcTicks` para evitar aplicar cambios a PIDs reciclados.
- Token de reversión completo y función `Rollback(Guid tokenId)` que restaura la máscara original.

---

### T3-002 · Orquestador de Experimentación y Evaluación Comparativa A/B (Cs2ExperimentOrchestrator)
| Campo | Valor |
| :--- | :--- |
| **Estado** | `TEST_VERIFIED` |
| **Prioridad** | CRÍTICA |
| **Archivos** | `src/EvoOptimizer.Infrastructure/Benchmark/Cs2ExperimentOrchestrator.cs` |
| **Dependencias** | `RegressionGuard`, `RegressionMeasurement`, `RegressionWatch` |

**Qué se implementó:**  
- Flujo experimental riguroso:
  - Configuración A: Línea Base con configuración stock.
  - Configuración B: Candidato optimizado con una única variable (Prioridad o Afinidad).
- Cálculo de variaciones porcentuales: $\Delta$ FPS promedio, $\Delta$ 1% Low, $\Delta$ 0.1% Low, $\Delta$ P99 frametime, $\Delta$ tasa de stuttering.
- Evaluación automática de regresión mediante `RegressionGuard`:
  - Si 1% low cae $\ge 5\%$ o latencia P99 sube $\ge 10\%$, emite veredicto `RegressionDetected` con `RequiresRollback = true`.
  - Si 1% low mejora $\ge 3\%$ sin regresiones, emite veredicto `ImprovementVerified`.
  - Correlación con Intel UHD: Si la GPU está al $92\%+$, emite `GpuBottleneckDominant`, explicando que la ganancia no provendrá de FPS promedio sino de consistencia del frametime.
  - Diferencias menores a $\pm 2\%$ se clasifican como `Inconclusive` (ruido de medición).
- 4 pruebas unitarias específicas añadidas y superadas (`Cs2ExperimentOrchestratorTests`).

---

### T3-003 · Controles Interactivos de Benchmark y Optimización en UI
| Campo | Valor |
| :--- | :--- |
| **Estado** | `BUILD_VERIFIED` |
| **Prioridad** | ALTA |
| **Archivos** | `src/EvoOptimizer.App/Views/EvoLiveMonitorPanel.xaml`, `src/EvoOptimizer.App/Views/EvoLiveMonitorPanel.xaml.cs`, `src/EvoOptimizer.App/ViewModels/EvoControlCenterViewModel.cs` |
| **Dependencias** | `Cs2ProcessPriorityOptimizer`, `Cs2AffinityOptimizer`, `Cs2ExperimentOrchestrator` |

**Qué se implementó:**  
- **Panel de Optimización Adaptativa de CS2:**
  - Visualización del estado actual de prioridad y afinidad.
  - Botón "PRIORIDAD ABOVENORMAL" (seguro, nunca High ni Realtime).
  - Botón "AFINIDAD 2 P-CORES (4 HILOS)" (Máscara `0x00F`).
  - Botón "AFINIDAD BALANCEADA (2P + 2E)" (Máscara `0x03F`).
  - Botón "REVERTIR TODO" para restaurar inmediatamente configuración stock.
- **Panel de Laboratorio de Experimentación A/B:**
  - Botones "1. CAPTURAR LÍNEA BASE (A)" y "2. CAPTURAR CANDIDATO (B)".
  - Barra de progreso y cronómetro de sesión de benchmark.
  - Tarjeta de resultado comparativo con deltas ($\Delta$ 1% Low, $\Delta$ P99, $\Delta$ Avg FPS) y veredicto explicativo.

---

### T3-004 · Batería de Pruebas Unitarias Ampliada
| Campo | Valor |
| :--- | :--- |
| **Estado** | `TEST_VERIFIED` |
| **Archivos** | `tests/EvoOptimizer.Core.Tests/UnitTest1.cs`, `tests/EvoOptimizer.Core.Tests/EvoOptimizer.Core.Tests.csproj` |
**Resultado: 21/21 tests superados (0 errores, 0 fallos) ✅**

---

## CORRECCIÓN PUNTUAL — Cálculo Dinámico de Máscaras y Validación de Topología en CS2 Affinity Optimizer

### CP-001 · Desacoplamiento de Máscaras Fijas y Validación Estricta de Topología
| Campo | Valor |
| :--- | :--- |
| **Estado** | `TEST_VERIFIED` |
| **Prioridad** | CRÍTICA |
| **Archivos** | `src/EvoOptimizer.Core/Hardware/CpuTopologyInfo.cs`, `src/EvoOptimizer.Infrastructure/Optimization/Cs2AffinityOptimizer.cs`, `src/EvoOptimizer.App/ViewModels/EvoControlCenterViewModel.cs`, `src/EvoOptimizer.App/Views/EvoLiveMonitorPanel.xaml`, `tests/EvoOptimizer.Core.Tests/UnitTest1.cs` |
| **Dependencias** | `CpuTopologyInfo`, `Cs2AffinityOptimizer`, `IntelHybridTopologyProvider` |

**Qué se corrigió:**
1. **Cálculo Dinámico en `CpuTopologyInfo.GetAffinityMask()`:**
   - Eliminados todos los supuestos de índices o máscaras fijas (`0x00F`, `0x03F`).
   - `PerformanceCoresOnly`: itera dinámicamente sobre la lista de núcleos descubiertos agregando los bits de afinidad de los clasificados como `CoreType.Performance`.
   - `PerformanceAndLightEfficiency`: toma la máscara de P-Cores calculada dinámicamente y agrega hasta 2 núcleos marcados como `CoreType.Efficient`.
   - `ExcludeCore0`: excluye la máscara completa del primer núcleo físico descubierto (`Cores[0].AffinityMask`), contemplando correctamente núcleos con HyperThreading (SMT).
2. **Validación de Confiabilidad (`IsTopologyReliable`):**
   - Agregada propiedad de validación que verifica: `TotalPhysicalCores > 0`, `LogicalProcessors > 0`, `AllCoresMask != 0`, `Cores != null`, y para procesadores híbridos `PerformanceCores > 0 && PerformanceCoresMask != 0`.
   - `Cs2AffinityOptimizer.Apply()` valida `IsTopologyReliable` antes de invocar Win32 `SetProcessAffinityMask`. Si la topología no es confiable, aborta de forma segura reportando `AffinityOptimizationStatus.Failed`.
   - Se valida además que la máscara objetivo no sea cero y sea un subconjunto estricto de `AllCoresMask` (`(targetMask & topology.AllCoresMask) == targetMask`).
3. **UI Dinámica en Control Center:**
   - Los botones de afinidad en `EvoLiveMonitorPanel.xaml` ahora se vinculan dinámicamente a las propiedades `PcoresButtonContent` y `BalancedButtonContent` expuestas por `EvoControlCenterViewModel`.
   - La descripción del resultado y el resumen de topología se generan dinámicamente usando `PerformanceLogicalProcessors` y el valor real de máscara en hexadecimal.
4. **Verificación y Pruebas:**
   - Agregadas pruebas para topologías híbridas heterogéneas arbitrarias, validación de topologías corruptas/incompletas, y rechazo seguro de optimización ante topologías no confiables.
   - Batería de pruebas: **24/24 superadas (0 advertencias, 0 errores) ✅**.

---

### CP-002 · Reconstrucción y Endurecimiento de `CpuTopologyInfo`
| Campo | Valor |
| :--- | :--- |
| **Estado** | `TEST_VERIFIED` |
| **Prioridad** | CRÍTICA |
| **Archivos** | `src/EvoOptimizer.Core/Hardware/CpuTopologyInfo.cs`, `tests/EvoOptimizer.Core.Tests/UnitTest1.cs` |
| **Dependencias** | `CoreDescription`, `IntelHybridTopologyProvider`, `Cs2AffinityOptimizer`, `EvoControlCenterViewModel` |

**Problema resuelto:**
El archivo había quedado incompleto (cuerpo de `Default()` y de `GetAffinityMask()` truncado/sintácticamente inválido), lo que impedía compilar `EvoOptimizer.Core` y bloqueaba a todos sus consumidores.

**Qué se corrigió:**
1. **Definiciones restauradas sin romper firmas públicas:**
   - `CoreType` (`Standard`, `Performance`, `Efficient`), `CoreDescription`, `CpuTopologyInfo` (mismos parámetros posicionales), `Default(int, string?)` y `GetAffinityMask(GamingAffinityMode)`.
   - Se conservan `PerformanceLogicalProcessors`, `EfficientLogicalProcessors` e `IsTopologyReliable` usados por la UI y el optimizador.
2. **`Default()` robusto:**
   - Clampea la cantidad de hilos a `[0, 64]` (límite de la máscara de 64 bits).
   - Con `0` devuelve una topología vacía y **no confiable** en lugar de una máscara inválida.
   - No inventa P-Cores/E-Cores: todos los núcleos quedan como `CoreType.Standard` con `IsHybrid = false`.
   - `PerformanceLogicalProcessors` ahora cuenta todos los hilos cuando no hay P-Cores declarados (antes devolvía `0` en CPU estándar y la UI mostraba "0 hilos").
3. **`GetAffinityMask()` 100 % derivado de la topología detectada:**
   - `PerformanceCoresOnly`: unión dinámica de las máscaras `CoreType.Performance`; respaldo en `PerformanceCoresMask` sólo si es subconjunto válido de `AllCoresMask`.
   - `PerformanceAndLightEfficiency`: P-Cores + hasta 2 E-Cores ordenados por `CoreIndex` (política `LightEfficiencyCoreCount`, sin máscaras fijas).
   - `ExcludeCore0`: excluye el **primer núcleo físico descubierto** (con todos sus hilos SMT); si no hay lista, usa el bit más bajo de `AllCoresMask` (antes usaba el literal `1UL`).
   - Toda máscara devuelta se recorta con `AllCoresMask`; nunca se devuelve `ulong.MaxValue` como respaldo.
   - En topologías híbridas sin P-Cores válidos devuelve `0` (el consumidor rechaza la operación) en lugar de usar todos los núcleos.
4. **Validación de integridad (`IsTopologyReliable`):**
   - Índices no negativos y únicos; máscaras no nulas, contenidas en `AllCoresMask`, disjuntas y con `popcount == LogicalProcessorCount`.
   - `Cores.Count == TotalPhysicalCores`, unión de máscaras `== AllCoresMask`, suma de hilos `== popcount(AllCoresMask)`.
   - `PerformanceCoresMask`/`EfficientCoresMask` disjuntas y con unión igual a `AllCoresMask`.
   - Híbridas: exige P-Cores y E-Cores reales y que los agregados coincidan exactamente con la lista de núcleos (no se asume topología híbrida sin datos).
   - `LogicalProcessors` limitado a 64 (afinidad de un solo grupo de procesadores).
5. **Pruebas:**
   - Nuevas: CPU estándar vía `Default()`, clamp 0/128 hilos, máscaras inválidas (fuera de rango, duplicadas, popcount incoherente, conteo de núcleos incorrecto) y verificación de que toda máscara queda dentro de `AllCoresMask`.
   - Se mantienen las pruebas de topología híbrida Alder Lake i3-1220P y de topologías corruptas.
   - Batería completa: **28/28 superadas**; `dotnet build .\EVO-FPS-OPTIMIZER.slnx -c Debug` → 0 errores, 0 advertencias.

---

### CP-003 — Primer bloque funcional de optimización de Windows (Modo Juego, Game DVR, energía, inicio)
| Campo | Valor |
| :--- | :--- |
| **Estado** | `TEST_VERIFIED` |
| **Prioridad** | ALTA |
| **Archivos** | `src/EvoOptimizer.Core/Optimization/WindowsOptimizationModels.cs`, `src/EvoOptimizer.Infrastructure/Optimization/WindowsChangeJournal.cs`, `src/EvoOptimizer.Infrastructure/Optimization/WindowsOptimizationService.cs`, `src/EvoOptimizer.Infrastructure/Hardware/WindowsGamingConfigService.cs`, `src/EvoOptimizer.App/ViewModels/EvoControlCenterViewModel.cs`, `src/EvoOptimizer.App/Views/EvoLiveMonitorPanel.xaml`, `src/EvoOptimizer.App/Views/EvoLiveMonitorPanel.xaml.cs`, `src/EvoOptimizer.App/Views/EvoControlCenterWindow.xaml.cs`, `tests/EvoOptimizer.Core.Tests/UnitTest1.cs` |
| **Dependencias** | `WindowsGamingConfigService`, `StartupBackgroundAnalyzer`, `EvoHistoryStore` (SQLite), `EvoControlCenterViewModel` |

**Alcance implementado:**
1. **Modelo y planificador puro (`WindowsOptimizationModels.cs`):**
   - `WindowsOptimizationPlanner.Build(snapshot)` genera los cuatro ajustes en orden de prioridad: Modo Juego, Game DVR/capturas, plan de energía y aplicaciones de inicio.
   - Cada `WindowsOptimizationItem` expone estado actual, cambio propuesto, beneficio esperado, riesgo, nivel de riesgo y motivo de bloqueo. Los textos describen el efecto técnico conocido y **no prometen mejoras de FPS**.
   - `WindowsChangeOperation` modela operaciones atómicas: `RegistryDword`, `RegistryString`, `RegistryDelete`, `PowerPlan`, `StartupValue`.
2. **Diario de cambios persistente (`WindowsChangeJournal.cs`):**
   - JSON con escritura atómica (`.tmp` + `File.Move`), mutex asíncrono y enums serializados como texto.
   - Registro *write-ahead*: el estado original se persiste **antes** de tocar Windows; `GetActiveAsync` devuelve `Pending`, `Applied` o `Verified`, de modo que un cierre inesperado durante la aplicación también puede revertirse.
   - Ruta por defecto: `%LOCALAPPDATA%\EvoFpsOptimizer\windows-changes.json`.
3. **Servicio de aplicación y rollback (`WindowsOptimizationService.cs`):**
   - `ApplyAsync` exige `userAuthorized: true`; sin autorización devuelve `NotAuthorized` y no escribe nada.
   - Aplica **un ajuste por vez** (mutex), guarda el estado original, ejecuta la operación, **relee y verifica** el resultado; si la verificación falla, intenta restaurar el estado original y registra el error.
   - `ApplyStartupAsync` desactiva una aplicación de inicio individual de `HKCU\...\Run`; `RollbackAsync`/`RollbackAllAsync` restauran en orden inverso y verifican.
   - Sólo escribe en HKCU (`GameBar`, `GameDVR`, `CurrentVersion\Run`). **No toca HKLM, Defender, firewall ni servicios críticos.**
   - Plan de energía: lee el GUID activo y busca el plan "Alto rendimiento"/"High performance" con `powercfg /list`; si no existe, el ajuste queda no aplicable (no se inventan GUIDs). La extracción de GUID usa regex porque la salida de `powercfg` está localizada.
   - Historial append-only en SQLite con categoría `"WindowsOptimization"` en `%LOCALAPPDATA%\EvoFpsOptimizer\history.db` (el diario JSON sigue siendo la fuente de rollback; si SQLite falla, el ajuste no se bloquea).
4. **Integración real en el Control Center:**
   - Nueva sección **"OPTIMIZACIÓN DE WINDOWS (CAMBIOS REVERSIBLES)"** en `EvoLiveMonitorPanel.xaml` con estado actual, cambio propuesto, beneficio y riesgo por ajuste, lista de aplicaciones de inicio y cambios activos.
   - Cada botón APLICAR/DESACTIVAR abre un `MessageBox` de confirmación con el estado actual → propuesto, beneficio y riesgo antes de invocar al servicio.
   - Botón REVERTIR por cambio y REVERTIR TODOS LOS CAMBIOS DE WINDOWS. El resumen de configuración (`GamingConfigSummary`) se actualiza tras aplicar o revertir.
5. **Pruebas:**
   - `WindowsOptimizationPlannerTests` (9): orden de prioridad, aplicabilidad de cada ajuste, riesgos, estados vacíos y bloqueos.
   - `WindowsChangeJournalTests` (5): persistencia entre instancias, enums como texto, actualización, Id desconocido y filtro de cambios activos.
   - Batería completa: **42/42 superadas**; `dotnet build .\EVO-FPS-OPTIMIZER.slnx -c Debug` → 0 errores, 0 advertencias.

**Límites deliberados:**
- No se modificaron `RestoreCenter` ni su allow-list de tres valores DWORD; el diario nuevo cubre plan de energía y valores de inicio que ese mecanismo no soporta.
- No se automatiza la desactivación masiva de inicio: cada aplicación requiere autorización y confirmación individual.
- Las mejoras de rendimiento no se declaran sin mediciones A/B reales.

---

## TANDA 4 — Diagnóstico ASUS ExpertCenter + expansión integral de optimizaciones

> Alcance: integración de las consultas del script de diagnóstico ASUS dentro de
> los motores existentes (sin módulos duplicados), catálogo amplio de
> optimizaciones ejecutables/verificables/reversibles, gobierno de procesos,
> coordinador EVO AI y rediseño del Control Center.
>
> **No se declaró FUNCTIONALLY_VERIFIED ninguna aplicación/rollback real sobre
> hardware.** El circuito de medición sigue siendo obligatorio antes de conservar
> un cambio.

### T4-001 · Diagnóstico real y continuo de hardware (CPU, P/E, RAM, GPU, NVMe, SMART, TRIM, energía, servicios, inicio, procesos)
| Campo | Valor |
| :--- | :--- |
| **Estado** | `TEST_VERIFIED` |
| **Prioridad** | CRÍTICA |
| **Archivos** | `src/EvoOptimizer.Core/Diagnosis/HardwareEvidenceEvaluator.cs` (nuevo), `src/EvoOptimizer.Core/Diagnosis/AdaptiveDiagnosticEngine.cs`, `src/EvoOptimizer.Infrastructure/Audit/WindowsSystemAuditor.cs`, `src/EvoOptimizer.App/ViewModels/EvoControlCenterViewModel.Hub.cs` (nuevo), `src/EvoOptimizer.App/Views/EvoOptimizationHubPanel.xaml` (nuevo) |
| **Dependencias** | `WindowsSystemAuditor` (PowerShell 5.1 + CIM), `AdaptiveDiagnosticEngine`, `SystemAuditReport` |

**Implementado:**
1. **Auditoría ampliada de solo lectura** (`WindowsSystemAuditor`): payload PowerShell 5.1 codificado en base64 que añade CPU (`LoadPercentage`, `ProcessorId`, `SocketDesignation`), GPU (`AdapterCompatibility`, `VideoProcessor`, `PNPDeviceID`), RAM (`DataWidth`, `TotalWidth`, `FormFactor`, `SMBIOSMemoryType`, `SerialNumber`), matriz de memoria, almacenamiento y contadores de confiabilidad con `try/catch`, TRIM (`fsutil`), plan de energía (`powercfg`), servicios automáticos, inicio (`Win32_StartupCommand`) y dos muestras de procesos separadas 1 s con `elapsedSeconds`/`logicalProcessors`.
2. **Evaluador puro** (`HardwareEvidenceEvaluator`): convierte hechos CIM en evidencia tipada. Dos módulos **no** confirman Dual Channel: sólo se verifica con tokens explícitos de canal (`ChannelA`, `DIMM_A1`); localizadores ambiguos (`DIMM 0`) devuelven `Unknown`. Capacidades/velocidades distintas generan advertencia de *flex mode*.
3. **SMART por cobertura**: `ReliabilityCounters` > `FailurePrediction` > `HealthStatusOnly` > `Unavailable`. `HealthStatus` se rotula explícitamente como **NO equivale a SMART completo**.
4. **CPU instantánea real**: `LoadPercentage` es utilización informada por Windows; la potencia en watts queda `UNKNOWN` sin RAPL/PDH (`PowerConfidence.Unknown`). La CPU por proceso se calcula por **delta de `TotalProcessorTime`** entre las dos muestras; nunca se usa la CPU acumulada como consumo instantáneo.
5. **Diagnóstico EVO AI**: `AdaptiveDiagnosticEngine` agrega hallazgos `MEMORY_DUAL_CHANNEL`, `MEMORY_TOPOLOGY_LIMITATION`, `CPU_POWER_EVIDENCE`, `STORAGE_HEALTH_*`, `STORAGE_TRIM`, `POWER_PLAN_ACTIVE`, `SERVICES_AUTO_STATUS`, `STARTUP_APPS_SNAPSHOT`, `PROCESS_INSTANTANEOUS_CPU`.
6. **Control Center**: pestaña **DIAGNÓSTICO DE HARDWARE** con tabla `Categoría · Dato · Valor · Origen · Confianza · Medido`, hallazgos con severidad y fecha de medición. Refresco automático cada 5 minutos y botón manual `VOLVER A MEDIR`. Todo dato ausente se muestra `UNKNOWN`.

**Límites deliberados:** no se modificó ninguna configuración durante el diagnóstico; no se muestran ceros inventados; no se leen sensores de temperatura sin fuente real (quedan `UNKNOWN`).

---

### T4-002 · Optimizaciones de Windows adicionales (HKCU, reversibles)
| Campo | Valor |
| :--- | :--- |
| **Estado** | `TEST_VERIFIED` |
| **Archivos** | `src/EvoOptimizer.Core/Optimization/WindowsOptimizationModels.cs`, `src/EvoOptimizer.Infrastructure/Optimization/WindowsOptimizationService.cs` |
| **Dependencias** | `WindowsChangeJournal`, `WindowsOptimizationPlanner` |

**Implementado:**
- `WindowsOptimizationTarget` amplía a 7 objetivos: `GameMode`, `GameDvr`, `PowerPlan`, `StartupApp`, `UwpBackground`, `EdgeBackground`, `WindowsSuggestions`.
- Nuevos ajustes, todos en HKCU/bajo usuario y con rollback por diario:
  - `UwpBackground`: `BackgroundAccessApplications\GlobalUserDisabled`.
  - `EdgeBackground`: política de usuario `Software\Policies\Microsoft\Edge\BackgroundModeEnabled = 0`.
  - `WindowsSuggestions`: seis valores `SubscribedContent-*` + `SoftLandingEnabled` + `SystemPaneSuggestionsEnabled`.
- `CaptureSnapshotAsync` ahora es público para que el catálogo y el Control Center expliquen el estado medido.
- Se mantiene la invariante: escritura *write-ahead*, verificación posterior, un ajuste por vez, sin HKLM/Defender/firewall/servicios.
- Pruebas del planificador actualizadas a 7 ítems y nuevos casos para UWP/Edge/sugerencias.

---

### T4-003 · Catálogo profesional de optimizaciones (33+ entradas, 9 categorías)
| Campo | Valor |
| :--- | :--- |
| **Estado** | `TEST_VERIFIED` |
| **Archivos** | `src/EvoOptimizer.Core/Optimization/OptimizationCatalog.cs` (nuevo), `src/EvoOptimizer.App/Views/EvoOptimizationHubPanel.xaml` (nuevo) |
| **Dependencias** | `WindowsOptimizationPlanner/Service`, `HardwareDiagnosticSnapshot`, `OptimizationLiveContext`, `WindowsChangeJournal` |

**Implementado:**
- `OptimizationCatalogEntry` declara: ID, nombre, descripción, categoría, compatibilidad, estado detectado, beneficio esperado, riesgo, confianza, autorización, medición, estado del ciclo, botones aplicables, detalle, fuente de evidencia, fecha y ejecutor real (`ActionKey`).
- Estados completos: `NOT_IMPLEMENTED`, `AVAILABLE`, `APPLIED`, `ROLLBACK_AVAILABLE`, `UNTESTED`, `INSUFFICIENT_EVIDENCE`, `INCONCLUSIVE`, `IMPROVEMENT_VERIFIED`, `REGRESSION_DETECTED`, `NOT_APPLICABLE`.
- Categorías: Windows, Memory, CPU, GPU, Process, Thermal, Storage, Gaming, EVO AI. Incluye entradas ejecutables (ajustes Windows, prioridad/afinidad CS2) y entradas de política/monitoreo que **nunca** declaran `CanApply`.
- Se respetan las políticas: `MEM-SUPPRESS-STANDBY-PURGE` queda `NOT_IMPLEMENTED` (prohibida la purga indiscriminada), `WIN-OPTIONAL-SERVICES` queda `NOT_APPLICABLE` (no se desactivan servicios sin evidencia), `GPU-DISCRETE-QUEUE-ISOLATION` requiere dGPU compatible, `THERM-SAFETY-LOCK-CPU-BOOST` no aplica sin sensores.
- Los cambios del diario se mapean a estados: `Verified → APPLIED`, `PerformanceVerified → IMPROVEMENT_VERIFIED`, `PerformanceInconclusive → INCONCLUSIVE`, `Failed/RolledBack → AVAILABLE`.

---

### T4-004 · Gobierno de procesos (sólo prioridad, reversible)
| Campo | Valor |
| :--- | :--- |
| **Estado** | `TEST_VERIFIED` |
| **Archivos** | `src/EvoOptimizer.Infrastructure/Optimization/ProcessGovernanceService.cs` (nuevo) |
| **Dependencias** | `LivePerformanceMonitor.ProcessSummary`, `EvoHistoryStore` |

**Implementado:**
- Clasificación `Critical/High/Normal/Background/Unknown` con lista de procesos protegidos (system, csrss, wininit, winlogon, services, lsass, smss, svchost, dwm, explorer, etc.).
- `ThrottleAsync` exige autorización, rechaza procesos críticos y usa **BelowNormal** (nunca High/Realtime). `RestoreAsync` devuelve la clase original y verifica. **Nunca termina procesos.**
- Historial en SQLite (`ProcessGovernance`). UI en el tablero con CPU/RAM/importancia/prioridad y botones BAJAR/RESTARURAR con confirmación.

---

### T4-005 · Coordinador EVO AI (decisiones explicadas, sin causalidad falsa)
| Campo | Valor |
| :--- | :--- |
| **Estado** | `TEST_VERIFIED` |
| **Archivos** | `src/EvoOptimizer.Core/Optimization/EvoAiCoordinator.cs` (nuevo) |
| **Dependencias** | `HardwareDiagnosticSnapshot`, `OptimizationLiveContext`, historial de evidencia |

**Implementado:**
- Resume el cuello de botella dominante y genera decisiones con evidencia, confianza y acción sugerida.
- Cruza memoria (Dual Channel/UNKNOWN), almacenamiento (SMART/TRIM), sesión CS2 y resultados de optimizaciones.
- Detecta conflictos: optimizaciones de CPU con GPU como límite; presión de memoria con GPU saturada.
- Advierte: optimizaciones aplicadas sin FPS, temperaturas UNKNOWN, resultados inconclusos. Se **abstiene** sin muestras suficientes y explicita que correlación no es causalidad.
- Pestaña **EVO AI** con resumen, decisiones, conflictos y advertencias.

---

### T4-006 · Rediseño del Control Center (dashboard, catálogo, diagnóstico, EVO AI, perfiles)
| Campo | Valor |
| :--- | :--- |
| **Estado** | `BUILD_VERIFIED` + smoke de carga WPF |
| **Archivos** | `src/EvoOptimizer.App/Views/EvoControlCenterWindow.xaml(.cs)`, `src/EvoOptimizer.App/Views/EvoOptimizationHubPanel.xaml(.cs)` (nuevo), `src/EvoOptimizer.App/ViewModels/EvoControlCenterViewModel.Hub.cs` (nuevo), `EvoControlCenterViewModel.cs` |

**Implementado:**
- Ventana con pestañas **EVO CONTROL CENTER** y **TELEMETRÍA EN VIVO** (se conserva el panel original sin romper bindings).
- Tablero: EVO Performance Score transparente (CPU, GPU, RAM, commit, optimizaciones sin evidencia, diagnóstico pendiente), salud, cuello de botella, métricas CPU/GPU/RAM/commit/CS2/VRAM compartida, perfiles `EVERYDAY`/`GAMING`/`MAXPERFORMANCE`, perfil automático de CS2 y optimizaciones activas.
- Catálogo: búsqueda por texto, filtro por categoría, badges de estado, tarjetas con beneficio/riesgo/confianza/fecha y botones `MEDIR BASE`, `APLICAR`, `REVERTIR`, `MEDIR POSTERIOR` con confirmación explícita.
- `ExecuteCatalogActionAsync` despacha por `ActionKey` a los ejecutores existentes (`windows:<Target>`, `cs2:priority`, `cs2:affinity-*`, `game:auto-profile`, `process:governance`).
- Perfil automático: al detectar CS2 aplica prioridad/afinidad existentes; al salir las revierte. Desactivado por defecto; activarlo constituye la autorización.
- **Smoke WPF real:** ventana instanciada, `Show()`, `UpdateLayout()` y 14 s de ejecución con monitor + hub → `XAML_OK`, `LAYOUT_OK`, `LOADED_OK`, `APP_EXIT_OK`, sin excepciones de XAML.

---

### T4-007 · Pruebas y compilación
- Nuevas pruebas (`tests/EvoOptimizer.Core.Tests/OptimizationExpansionTests.cs`): Dual Channel verificado/ambiguo/único/flex, CPU utilización≠potencia, TRIM 0/1, SMART completo vs HealthStatus, delta de procesos, ausencia de segunda muestra, catálogo (IDs únicos, 9 categorías, mapeo de estados, políticas), EVO AI (abstención, conflicto GPU, verificación, presión de memoria) y gobierno de procesos (protección de críticos, autorización).
- `WindowsOptimizationPlannerTests` actualizado a 7 ítems.
- **Batería completa: 76/76 superadas.**
- `dotnet build EVO-FPS-OPTIMIZER.slnx -c Debug` → **0 errores, 0 advertencias**.
- **No ejecutado en hardware real:** aplicar/revertir ajustes de Windows y prioridades de proceso durante el smoke (el smoke no modifica el sistema).

**Pendiente / no implementado por política o falta de evidencia:**
- `WIN-DELIVERY-OPTIMIZATION` (HKLM/elevación), `WIN-WINDOWED-GAMES` (clave sin verificar), `CPU-EPP`, `THERM-SAFETY-LOCK-CPU-BOOST` (sin sensores), `GPU-DISCRETE-QUEUE-ISOLATION` (sin dGPU), `MEM-SUPPRESS-STANDBY-PURGE` (prohibido por política).
- Lectura de temperatura/frecuencia/potencia reales y espacio libre de disco para el puntaje (quedan `UNKNOWN`).
- Persistencia de resultados A/B en SQLite y CSV de PresentMon siguen en backlog.

---

## TANDA 5 — Brief original: escáner de PC + 50+ tweaks reales (FPS/latencia, registro, debloat)

> Objetivo del brief fundacional: FPS optimizer estilo Hone para i3-1220P · Intel UHD ·
> 16 GB DDR5-5600 · 500 GB · 1080p 200 Hz, con escáner de especificaciones, tweaks de FPS y
> latencia, tweaks de registro y debloat de Windows (50+ en total) y un diseño profesional.

### T5-001 · Escáner de especificaciones del equipo (solo lectura)
| Campo | Valor |
| :--- | :--- |
| **Estado** | `TEST_VERIFIED` + `BUILD_VERIFIED` |
| **Prioridad** | ALTA |
| **Archivos** | `src/EvoOptimizer.App/ViewModels/EvoControlCenterViewModel.Tweaks.cs`, `src/EvoOptimizer.App/Views/EvoTweakPanel.xaml`, `src/EvoOptimizer.Infrastructure/Audit/WindowsSystemAuditor.cs` (reutilizado) |
| **Dependencias** | `WindowsSystemAuditor` (CIM vía PowerShell 5.1), `HardwareFactRow` |

**Alcance:**
- El escáner reutiliza el auditor CIM existente: no se duplicó ninguna consulta. `RunScannerAsync()` produce `ScannerRows` (categoría, dato, valor, origen, confiabilidad, fecha) con **18 categorías de hardware** más contadores de servicios/inicio.
- Ocho tarjetas de especificaciones derivadas de lo detectado: `ScannerCpuText`, `ScannerGpuText`, `ScannerMemoryText`, `ScannerStorageText`, `ScannerDisplayText` (resolución + tasa de refresco reales vía `Win32_VideoController`), `ScannerOsText`, `ScannerTopologyText`.
- `ScannerBriefText` contrasta lo declarado en el brief (i3-1220P, Intel UHD, 16 GB DDR5-5600, 500 GB, 1080p/200 Hz) con lo medido ahora. Si CIM no informa un dato, figura como `UNKNOWN`; nunca se rellena con el valor del brief.
- `EXPORTAR INFORME` reutiliza `WindowsSystemAuditor.ExportAsync` (JSON + TXT en `%LOCALAPPDATA%\EvoFpsOptimizer\Audits`). El escáner **no escribe en el sistema**.

### T5-002 · Motor de tweaks de registro (49 definiciones, 9 categorías)
| Campo | Valor |
| :--- | :--- |
| **Estado** | `TEST_VERIFIED` (lectura real verificada) / `BUILD_VERIFIED` |
| **Prioridad** | ALTA |
| **Archivos** | `src/EvoOptimizer.Core/Tweaks/RegistryTweakModels.cs`, `RegistryTweakDefinitions.cs`, `TweakChangeModels.cs`, `src/EvoOptimizer.Infrastructure/Tweaks/RegistryTweakService.cs`, `TweakChangeJournal.cs`, `src/EvoOptimizer.Infrastructure/Common/PowerShellScriptRunner.cs` |
| **Dependencias** | `Microsoft.Win32.Registry`, `reg.exe` elevado, `EvoHistoryStore` no involucrado |

**Alcance:**
- **49 tweaks de registro** con metadatos obligatorios por entrada: qué hace, beneficio esperado, riesgo + nota de riesgo, valor actual leído, valor propuesto, ubicación exacta (`HKCU\…\Valor`), elevación, reinicio, compatibilidad, origen de evidencia y fecha de lectura.
  - GAMING/FPS: Game DVR, captururas históricas, ModeFinality (`GameConfigStore`), DXGI FSE honor, `GameDVR` flags, Auto Game Mode, panel de Game Bar, Nexus.
  - Multimedia scheduler: `SystemResponsiveness=0`, `NetworkThrottlingIndex=4294967295`, categorías GPU/Priority/Scheduling/SFIO de la tarea `Games` en `High`.
  - INPUT: `MouseSpeed/Threshold1/Threshold2`, `KeyboardDelay/Speed`, colas `mouclass`/`kbdclass` (`MouseDataQueueSize`/`KeyboardDataQueueSize=16`), `Win32PrioritySeparation=38`.
  - RED: `TCPNoDelay=1`, `TcpAckFrequency=1`.
  - ENERGÍA: `PowerThrottlingOff=1`, `FastStartupDisabled=1`.
  - MEMORIA: `DisablePagingExecutive=1` (no desactiva la paginación, sólo evita paginar el kernel), `LargeSystemCache=1`, `ClearPageFileAtShutdown=0`.
  - ALMACENAMIENTO: `NtfsDisable8dot3NameCreation=1`, `NtfsDisableLastAccessUpdate=1`.
  - VISUAL/EFECTOS: `PerformanceOptions=2`, transparencia, animaciones de barra, `MenuShowDelay=0`, sacudida de ventana deshabilitada, `Serialize` (StartupDelayOnDesktop).
  - SISTEMA/DEBLOAT DE CONFIGURACIÓN: Bing en búsqueda, ConsentStore Cortana, Search Highlights, Advertising ID, sugerencias/CDM, rastreo de programas iniciados, `WaitToKillAppTimeout`, `HungAppTimeout`, `AutoEndTasks`, `Taskkill` de svc-host agrupado.
- **29 entradas HKCU** (escritas en proceso) y **20 entradas HKLM** (20 piden UAC; si el usuario cancela —error 1223— no se escribe nada).
- Política de seguridad hecha código: `BlockedPathFragments` (Defender, firewall/`SharedAccess`, `Safer`, BitLocker) y `BlockedValueNames` (`Start`, `ImagePath`, `DelayedAutoStart`, `FailureActions`, `Type`, `ObjectName`, `RequiredPrivileges`). Un tweak que toque esas zonas se marca `BLOQUEADO POR POLÍTICA EVO` y `CanApply=false`. La prueba `Definitions_NoneViolatesSafetyPolicy` verifica que **ninguna** de las 49 entradas las toca; las colas de `mouclass`/`kbdclass`/`Tcpip` siguen permitidas porque no son valores de control de servicios.
- Ciclo real: lectura → diario *write-ahead* con valor original → escritura (HKCU directo / HKLM por `reg.exe` con `Verb=runas`) → **relectura de verificación**. Si la relectura no coincide, se restaura el valor original y el cambio queda `Failed`.
- `RollbackAsync` restaura el valor original (o elimina el valor creado por EVO si Windows no lo tenía) y relee para confirmar. `RollbackAllAsync` revierte en orden inverso. `ApplyLowRiskAsync` aplica sólo disponibles de riesgo bajo.
- Diario: `%LOCALAPPDATA%\EvoFpsOptimizer\tweaks-changes.json` (tmp + `File.Move`, enums como texto).

### T5-003 · Motor de debloat UWP (33 apps evaluables, con restauración)
| Campo | Valor |
| :--- | :--- |
| **Estado** | `TEST_VERIFIED` (lógica pura) / `BUILD_VERIFIED` |
| **Archivos** | `src/EvoOptimizer.Core/Tweaks/DebloatModels.cs`, `DebloatCatalog.cs`, `src/EvoOptimizer.Infrastructure/Tweaks/DebloatService.cs`, `DebloatChangeJournal.cs` |

**Alcance:**
- **33 aplicaciones catalogadas**: overlays y apps de Xbox (App, TCUI, GameOverlay, GameBar, SpeechToText, GamingApp), Teams, Clipchamp, Outlook, Skype, Copilot, Cortana, Mixed Reality, People, Your Phone, Get Help/Get Started, Bing News/Weather, Solitaire, Maps, Movies & TV, Groove, Feedback Hub, Advertising SDK y bloatware de terceros (Amazon, Instagram, Facebook, Spotify, Photoshop Express, Flipboard, LinkedIn, TikTok).
- **Lista protegida explícita** (`ProtectedPrefixes`): Store, DesktopAppInstaller, VCLibs, UI.Xaml, .NET Native, WindowsAppRuntime, WebView2, AAD.BrokerPlugin, AccountsControl, CredDialogHost, BioEnrollment, LockApp, SysMgr, AssignedAccessLock, CloudExperienceHost, ContentDeliveryManager, CrossDevice, Shell/StartMenu ExperienceHost, Search, QuickAssist, RemoteDesktop, Terminal, Edge, **SecHealthUI (Defender)**, XboxIdentityProvider, **GamingServices**, OOBE, Services.Store. La protección gana siempre: `RemoveAsync` se niega aunque una definición futura liste el paquete.
- Quite por usuario con `Remove-AppxPackage`, verificación por re-escaneo (`Get-AppxPackage`) y restauración con `Add-AppxPackage -Register <InstallLocation>\AppxManifest.xml -DisableDevelopmentMode`. El diario (`debloat-changes.json`) guarda `PackageFullName`, `InstallLocation`, `Version` antes de tocar nada.
- Sin escaneo exitoso el estado es `SIN ESCANEO`/`UNKNOWN`, nunca “no hay bloatware”. Un paquete no catalogado se reporta como protegido/no listable y no ofrece quite.

### T5-004 · Tercer tab del Control Center: ESCÁNER · TWEAKS · DEBLOAT
| Campo | Valor |
| :--- | :--- |
| **Estado** | `BUILD_VERIFIED` + smoke WPF |
| **Archivos** | `src/EvoOptimizer.App/Views/EvoTweakPanel.xaml(.cs)`, `EvoControlCenterWindow.xaml`, `EvoControlCenterWindow.xaml.cs`, `EvoControlCenterViewModel.Tweaks.cs` |

**Alcance:**
- Panel oscuro estilo Hone con `TabControl` interno de 3 vistas y `DataGrid` propios (badges de estado coloreados por `StateColor`, sin conversores de valor).
- SCANNER: botón ESCANEAR/EXPORTAR, 8 tarjetas de especificaciones y grilla de hechos con origen/confiabilidad/fecha.
- TWEAKS: ESCANEAR TWEAKS, APLICAR SÓLO RIESGO BAJO, REVERTIR TODOS, búsqueda TwoWay y filtro por categoría; grilla con ubicación exacta, valor actual → propuesto, elevación, reinicio, riesgo y botones APLICAR/REVERTIR por fila.
- DEBLOAT: ESCANEAR APPS UWP con grilla de paquete detectado, versión, motivo, beneficio, riesgo, hint de reinstalación y botones QUITAR/RESTAURAR.
- **Toda escritura pasa por `MessageBox` de confirmación** con estado actual → propuesto, beneficio, riesgo y alcance del rollback antes de invocar al servicio; los loteos piden confirmación separada.
- Corrección de cierre: `HandleClosing` ya no llama `Close()` de forma reentrante dentro del evento `Closing`; lo difiere con `Dispatcher.BeginInvoke` (sin ello el cierre lanzaba una excepción en cuanto `DisposeAsync` terminaba síncrono).

### T5-005 · Pruebas, compilación y verificación
- Nuevas pruebas en `tests/EvoOptimizer.Core.Tests/TweakEngineTests.cs` (31 pruebas): ≥45 tweaks únicos, **catálogo combinado >50** (49 + 33 = 82), cobertura de las 9 categorías, metadatos completos, hives coherentes con elevación, cero violaciones de la política, `IsBlockedPath/Value`, `Evaluate` (Applied/Available/Unknown/RequiresElevation/Blocked), etiquetas y colores de todos los estados, catálogo de debloat (IDs únicos, ningún eliminable protegido, protegidos reales, nombre vacío = protegido), `Match` con escaneo vacío/instalado/protegido/no instalado, quite registrado → restaurable, `EvaluateUnlisted`, round-trip de `TweakChangeJournal` (enums como texto, `Update` con Id inexistente, filtros activos), `DebloatChangeJournal` (Restored deja de estar activo) y **escaneo real de los 49 tweaks contra el registro de esta máquina (solo lectura, sin escribir en el diario)**.
- `dotnet test tests/EvoOptimizer.Core.Tests` → **107/107 superadas** (desde 76).
- `dotnet build EVO-FPS-OPTIMIZER.slnx -c Debug` → **0 errores, 0 advertencias**.
- **Smoke WPF real:** proyecto temporal `artifacts/ui-smoke` (borrado tras la verificación) construyó `EvoTweakPanel`, midió layout (`978x364`), instanció y mostró `EvoControlCenterWindow` con los tres tabs → `APP_OK`, `PANEL_OK`, `WINDOW_OK`, `LOADED_OK`, `RUN_RETURNED`, `APP_EXIT_OK`, sin excepciones de XAML ni de cierre.

**Límites y lo que NO está verificado:**
- **Ningún tweak fue aplicado ni revertido en este equipo durante la tanda**: la verificación real fue de lectura. Aplicar sigue exigiendo confirmación del usuario y, para HKLM, UAC.
- La ganancia de FPS/latencia de cualquier tweak **no se declara**: el circuito BASELINE→APPLY→MEDIR→COMPARTE del panel de optimizaciones es el que decide; sin medición A/B el resultado es `INCONCLUSO`.
- El debloat no fue escaneado ni ejecutado en esta máquina (no se invocó `Get-AppxPackage` en la batería para mantener las pruebas rápidas); su lógica pura está verificada y la verificación en vivo queda pendiente.
- Los prefijos de paquetes de terceros provienen de convenciones conocidas de `Get-AppxPackage`; si un prefijo no coincide en este equipo, la app aparece como `NO INSTALADA` (no se ofrece quite) en lugar de asumir datos.
- 20 tweaks HKLM requieren UAC y algunos piden reinicio; sin reinicio el efecto puede no notarse y EVO lo muestra en la columna REINICIO.

---

## Funcionalidades planificadas (próximos pasos en backlog)

### T4-001 · Persistencia de Resultados de Experimentos en SQLite
| Campo | Valor |
| :--- | :--- |
| **Estado** | `PLANNED` |
| **Dependencias** | `SqliteBenchmarkRepository`, `Cs2ExperimentOrchestrator` |
| **Área** | Learning & Persistence |

Guardar los resultados A/B en la base de datos local SQLite para que EVO aprenda el perfil de mejora en este equipo a lo largo del tiempo.

---

### T4-002 · Ingesta directa de archivos CSV de PresentMon durante partidas reales
| Campo | Valor |
| :--- | :--- |
| **Estado** | `PLANNED` |
| **Dependencias** | `PresentMonCsvReader`, `AdvancedFpsAnalyzer`, `Cs2ExperimentOrchestrator` |
| **Área** | Monitoring & Evidence |

Permitir arrastrar o cargar un archivo CSV generado por PresentMon para procesar automáticamente todos los frametimes y generar la comparación A/B con datos directos del motor de captura.

---

### T9-001 · EVO Game Optimizer específico por juego
| Campo | Valor |
| :--- | :--- |
| **Estado** | `PARTIAL_IMPLEMENTATION` — gestor reversible de configuración CS2 implementado; motor universal pendiente |
| **Prioridad** | CS2 primero; ampliar solo con fuentes y perfiles verificados |
| **Área** | CS2 · configuración por juego |
| **Archivos de esta fase** | `src/EvoOptimizer.Infrastructure/GameProfiles/Cs2AutoexecConfigService.cs`, `src/EvoOptimizer.App/ViewModels/EvoControlCenterViewModel.GameConfig.cs`, `src/EvoOptimizer.App/Views/EvoOptimizationHubPanel.xaml(.cs)`, `tests/EvoOptimizer.Core.Tests/Cs2AutoexecConfigServiceTests.cs` |
| **Especificación fuente** | Adjunto `EVO GAME OPTIMIZER — UNIVERSAL GAME-SPECIFIC PERFORMANCE ENGINE` (463 líneas; recibido el 2026-11-07) |
| **Validación** | Tests Debug/Release y builds Debug/Release exitosos; 124 pruebas totales. No se aplicó configuración real en este equipo. |

**Implementado en esta fase:**
- Nueva pestaña **CS2 CONFIG** dentro del Control Center integrado en la ventana principal; abre y lee únicamente `game\csgo\cfg\autoexec.cfg` elegido por la usuaria.
- Administra un bloque delimitado por EVO, conserva el texto no administrado, acepta solo variables numéricas de una lista acotada (`cl_hud_color`, `cl_showfps`, `fps_max`, `fps_max_ui`, `r_fullscreen_gamma`) y no precarga valores ni perfiles que impongan FPS.
- Toda escritura/restauración exige confirmación explícita desde la UI, rechaza operaciones mientras CS2 está abierto y verifica el archivo después de escribir. Una configuración existente se copia a un backup adyacente; el diario local permite revertir el último cambio. Si el archivo cambió externamente, se bloquea la reversión. Si no existía, revertir quita el nuevo archivo solo si sigue intacto.
- La funcionalidad solo modifica el `autoexec.cfg` elegido; no cambia Windows, Steam/launch options, anticheat ni memoria del juego. No declara mejoras de FPS ni valida que el juego actual reconozca cada variable.
- Pruebas cubren lectura sin datos, escritura/restauración exacta, creación reversible, preservación fuera del bloque EVO, detección de modificaciones externas, validación de nombres/valores y rechazo de rutas ajenas a CS2.

**Pendiente de la especificación mayor:** detección de instalaciones/biblioteca, perfiles recomendados con evidencia, diagnóstico por juego y fuentes compatibles de frametime/latencia, benchmark A/B, sesión/telemetría dedicada, perfiles para otros juegos y versionado más amplio. No se simulan ni se declaran funcionales.
