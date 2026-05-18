using System.Windows;
using CAAutomationHub.Wpf.Views.Pilot;
using CAAutomationHub.Wpf.ViewModels;

namespace CAAutomationHub.Wpf;

public partial class MainWindow : Window
{
    private PilotMonitorWindow? _pilotMonitorWindow;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.CreateDefaultPilotLocal();
    }

    private void OnOpenPilotMonitorClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel) return;

        if (_pilotMonitorWindow is { IsVisible: true })
        {
            _pilotMonitorWindow.Activate();
            return;
        }

        _pilotMonitorWindow = new PilotMonitorWindow(viewModel.PilotPolling)
        {
            Owner = this
        };
        _pilotMonitorWindow.Closed += (_, _) => _pilotMonitorWindow = null;
        _pilotMonitorWindow.Show();
    }
}
