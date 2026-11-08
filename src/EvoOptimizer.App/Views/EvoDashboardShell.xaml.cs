using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EvoOptimizer.App.Navigation;
using EvoOptimizer.App.ViewModels;

namespace EvoOptimizer.App.Views;

public partial class EvoDashboardShell : UserControl
{
    public event RoutedEventHandler? AnalyzeRequested;

    public EvoDashboardShell()
    {
        InitializeComponent();
    }

    public async ValueTask DisposeAsync()
    {
        await ControlCenterPanel.DisposeAsync();
    }

    public void SetAnalysisBusy(bool isBusy)
    {
        AnalyzeSystemButton.IsEnabled = !isBusy;
        AnalyzeSystemDetailButton.IsEnabled = !isBusy;
    }

    public void SetAnalysisStatus(string status) =>
        AnalyzeSystemStatus.Text = status;

    public void SetDiagnosticItems(IEnumerable findings) =>
        SystemDiagnosticItems.ItemsSource = findings;

    public void SetCleanerViewModel(AdvancedSystemCleanupViewModel viewModel) =>
        SystemCleanerPanel.DataContext = viewModel;

    private void NavigateModule_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: EvoNavigationModule module })
            return;

        EvoNavigationCatalog.Select(module.Id);

        switch (module.Route)
        {
            case EvoModuleRoute.Dashboard:
                MainPages.SelectedIndex = 0;
                DashboardScroll.ScrollToTop();
                return;
            case EvoModuleRoute.Diagnosis:
                NavigateDiagnosis_Click(sender, e);
                return;
            case EvoModuleRoute.ControlCenter:
            case EvoModuleRoute.OptimizationCatalog:
                MainPages.SelectedIndex = 1;
                ControlCenterPanel.SelectControlCenter();
                if (module.Route == EvoModuleRoute.OptimizationCatalog)
                    ControlCenterPanel.SelectOptimizationCatalog();
                return;
            case EvoModuleRoute.Scanner:
                MainPages.SelectedIndex = 1;
                ControlCenterPanel.SelectScanner();
                return;
            case EvoModuleRoute.Tweaks:
                MainPages.SelectedIndex = 1;
                ControlCenterPanel.SelectTweaks();
                return;
            case EvoModuleRoute.Debloat:
                MainPages.SelectedIndex = 1;
                ControlCenterPanel.SelectDebloat();
                return;
            case EvoModuleRoute.LiveMonitor:
                MainPages.SelectedIndex = 1;
                ControlCenterPanel.SelectLiveMonitor();
                return;
            case EvoModuleRoute.HardwareDiagnostics:
                MainPages.SelectedIndex = 1;
                ControlCenterPanel.SelectHardwareDiagnostics();
                return;
            case EvoModuleRoute.Cs2Configuration:
                NavigateGameOptimizer_Click(sender, e);
                return;
            case EvoModuleRoute.Benchmark:
                MainPages.SelectedIndex = 1;
                ControlCenterPanel.SelectLiveMonitor();
                return;
            case EvoModuleRoute.EvoAi:
                NavigateEvoAi_Click(sender, e);
                return;
            case EvoModuleRoute.SystemCleaner:
                MainPages.SelectedIndex = 3;
                return;
            case EvoModuleRoute.Placeholder:
                ShowModuleStatus(module);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(module.Route), module.Route, "Unknown EVO navigation route.");
        }
    }

    private void NavigateEvoAi_Click(object sender, RoutedEventArgs e)
    {
        EvoNavigationCatalog.Select("crosscutting.evo-ai");
        MainPages.SelectedIndex = 1;
        ControlCenterPanel.SelectEvoAi();
    }

    private void ShowModuleStatus(EvoNavigationModule module)
    {
        ModuleInfoPage.PageTitle = module.Name;
        ModuleInfoPage.Subtitle = module.Description;
        ModuleInfoPage.ModuleStatus = module.StatusLabel;
        ModuleInfoPage.ModuleStatusBrush = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter()
            .ConvertFromString(module.StatusColor)!;
        ModuleInfoPage.ModuleStatusDetails = module.StatusDetails;
        ModuleInfoPage.ModuleComponent = module.ExistingComponent;
        MainPages.SelectedIndex = 2;
    }

    private void AnalyzeRequested_Click(object sender, RoutedEventArgs e) =>
        AnalyzeRequested?.Invoke(sender, e);

    private void NavigateDiagnosis_Click(object sender, RoutedEventArgs e)
    {
        EvoNavigationCatalog.Select("diagnosis.system-diagnostics");
        MainPages.SelectedIndex = 0;
        DiagnosticDetails.IsExpanded = true;
        AnalyzeRequested?.Invoke(sender, e);
    }

    private void NavigateControlCenter_Click(object sender, RoutedEventArgs e)
    {
        EvoNavigationCatalog.Select("home.control-center");
        MainPages.SelectedIndex = 1;
        ControlCenterPanel.SelectControlCenter();
    }

    private void NavigateLiveMonitor_Click(object sender, RoutedEventArgs e)
    {
        EvoNavigationCatalog.Select("gaming.gaming-monitor");
        MainPages.SelectedIndex = 1;
        ControlCenterPanel.SelectLiveMonitor();
    }

    private void NavigateGameOptimizer_Click(object sender, RoutedEventArgs e)
    {
        EvoNavigationCatalog.Select("gaming.game-profiles");
        MainPages.SelectedIndex = 1;
        ControlCenterPanel.SelectCs2Config();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleWindowState();
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed &&
            Window.GetWindow(this) is { } window)
        {
            window.DragMove();
        }
    }

    private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is { } window)
            window.WindowState = WindowState.Minimized;
    }

    private void ToggleMaximizeWindow_Click(object sender, RoutedEventArgs e) =>
        ToggleWindowState();

    private void CloseWindow_Click(object sender, RoutedEventArgs e) =>
        Window.GetWindow(this)?.Close();

    private void ToggleWindowState()
    {
        if (Window.GetWindow(this) is not { } window)
            return;

        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}
