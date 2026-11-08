using System.Windows;
using System.ComponentModel;
using EvoOptimizer.App.ViewModels;
using EvoOptimizer.Infrastructure.Hardware;
using EvoOptimizer.Infrastructure.Telemetry;
using EvoOptimizer.Infrastructure.Audit;
using EvoOptimizer.Infrastructure.Cleanup;
using EvoOptimizer.Core.Diagnosis;

namespace EvoOptimizer.App;


public partial class MainWindow : Window
{
    private readonly DashboardViewModel _viewModel;
    private readonly AdvancedSystemCleanupViewModel _cleanupViewModel;
    private readonly WindowsSystemAuditor _auditor = new();
    private readonly AdaptiveDiagnosticEngine _diagnosticEngine = new();
    private bool _shutdownStarted;
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();

        // Composition root. Fases futuras: sustituir por DI y pasar un IDecisionEngine real.
        var hardware = new RegistryHardwareProfileProvider().Capture();
        _viewModel = new DashboardViewModel(new WindowsTelemetryProvider(), hardware);
        DataContext = _viewModel;
        DashboardShell.DataContext = _viewModel;
        _cleanupViewModel = new AdvancedSystemCleanupViewModel(new AdvancedSystemCleanupService());
        DashboardShell.SetCleanerViewModel(_cleanupViewModel);
        DashboardShell.AnalyzeRequested += AnalyzeSystem_Click;

        Closing += HandleClosing;
    }

    private async void HandleClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
            return;

        e.Cancel = true;
        if (_shutdownStarted)
            return;

        _shutdownStarted = true;
        IsEnabled = false;

        await DashboardShell.DisposeAsync();
        await _cleanupViewModel.DisposeAsync();
        _viewModel.Dispose();
        _allowClose = true;
        _ = Dispatcher.BeginInvoke(new Action(Close));
    }

    private async void AnalyzeSystem_Click(object sender, RoutedEventArgs e)
    {
        DashboardShell.SetAnalysisBusy(true);
        DashboardShell.SetAnalysisStatus("Recopilando información del sistema...");

        try
        {
            var audit = await _auditor.CaptureAsync();

            var report = _diagnosticEngine.Analyze(audit);
            var liveFindings = LiveTelemetryDiagnosticEvaluator.Evaluate(
                _viewModel.Hardware,
                _viewModel.CaptureTelemetryHistory());
            report = report with
            {
                Findings = report.Findings.Concat(liveFindings).ToArray()
            };

            DashboardShell.SetDiagnosticItems(report.Findings);

            var exported = await WindowsSystemAuditor.ExportAsync(audit);

            DashboardShell.SetAnalysisStatus(
                $"Análisis completado: {report.Findings.Count} hallazgos. " +
                $"Advertencias de auditoría: {audit.Warnings.Count}. " +
                $"Informe: {exported.TextPath}");
        }
        catch (Exception ex)
        {
            DashboardShell.SetAnalysisStatus("Error durante el análisis.");

            MessageBox.Show(
                ex.Message,
                "EVO FPS OPTIMIZER",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            DashboardShell.SetAnalysisBusy(false);
        }
    }
}
