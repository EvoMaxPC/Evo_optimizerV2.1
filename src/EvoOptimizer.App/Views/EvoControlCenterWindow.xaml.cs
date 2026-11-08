using System.Windows;
using System.Windows.Controls;
using EvoOptimizer.App.ViewModels;

namespace EvoOptimizer.App.Views;

public partial class EvoControlCenterWindow : UserControl
{
    private readonly EvoControlCenterViewModel _viewModel;
    private bool _disposed;
    private Task? _initializationTask;

    public EvoControlCenterWindow()
    {
        // Crear el ViewModel ANTES de InitializeComponent para que el panel
        // reciba el DataContext correcto desde el primer layout pass.
        _viewModel = new EvoControlCenterViewModel();

        InitializeComponent();

        // Asignar DataContext explícito en la ventana Y en el panel para
        // romper cualquier herencia del DataContext de la ventana propietaria.
        DataContext = _viewModel;
        HubPanel.DataContext = _viewModel;
        LiveMonitorPanel.DataContext = _viewModel;
        TweakPanel.DataContext = _viewModel;

        Loaded += HandleLoaded;
    }

    public void SelectControlCenter() => ControlCenterSections.SelectedIndex = 0;

    public void SelectOptimizationCatalog()
    {
        ControlCenterSections.SelectedIndex = 0;
        HubPanel.SelectOptimizationCatalogTab();
    }

    public void SelectHardwareDiagnostics()
    {
        ControlCenterSections.SelectedIndex = 0;
        HubPanel.SelectHardwareDiagnosticsTab();
    }

    public void SelectEvoAi()
    {
        ControlCenterSections.SelectedIndex = 0;
        HubPanel.SelectEvoAiTab();
    }

    public void SelectScannerTweaksDebloat() => ControlCenterSections.SelectedIndex = 1;

    public void SelectScanner()
    {
        ControlCenterSections.SelectedIndex = 1;
        TweakPanel.SelectScannerTab();
    }

    public void SelectTweaks()
    {
        ControlCenterSections.SelectedIndex = 1;
        TweakPanel.SelectTweaksTab();
    }

    public void SelectDebloat()
    {
        ControlCenterSections.SelectedIndex = 1;
        TweakPanel.SelectDebloatTab();
    }

    public void SelectLiveMonitor() => ControlCenterSections.SelectedIndex = 2;

    public void SelectCs2Config()
    {
        ControlCenterSections.SelectedIndex = 0;
        HubPanel.SelectCs2ConfigTab();
    }

    private async void HandleLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= HandleLoaded;
        _initializationTask = InitializeAsync();
        await _initializationTask;
    }

    private async Task InitializeAsync()
    {
        try
        {
            await _viewModel.StartAsync();
            await _viewModel.RefreshWindowsOptimizationAsync();
            await _viewModel.StartOptimizationHubAsync();
            _viewModel.InitializeTweaksState();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                Window.GetWindow(this),
                ex.ToString(),
                "EVO - Error iniciando monitor",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            if (_initializationTask is not null)
                await _initializationTask;

            await _viewModel.DisposeAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                Window.GetWindow(this),
                ex.Message,
                "EVO - Error de cierre",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
