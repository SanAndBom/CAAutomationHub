using System.Windows;
using CAAutomationHub.Wpf.ViewModels.Pilot;

namespace CAAutomationHub.Wpf.Views.Pilot;

public partial class PilotMonitorWindow : Window
{
    public PilotMonitorWindow()
    {
        InitializeComponent();
    }

    public PilotMonitorWindow(PilotPollingViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
