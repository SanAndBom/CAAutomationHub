using System.Windows.Controls;
using CAAutomationHub.Wpf.ViewModels;

namespace CAAutomationHub.Wpf.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        // Current shell owns this view for its lifetime. If future navigation reuses DashboardView,
        // move this disposal boundary to the navigation/view-model owner instead.
        if (DataContext is DashboardViewModel viewModel) viewModel.Dispose();
    }
}
