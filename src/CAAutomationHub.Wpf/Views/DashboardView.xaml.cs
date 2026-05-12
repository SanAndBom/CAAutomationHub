using System.Windows.Controls;
using CAAutomationHub.Wpf.Dialogs;
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

    private void OnAddPlcClick(object sender, System.Windows.RoutedEventArgs e)
    {
        // AH-WPF-03 keeps this prototype in code-behind. Move dialog creation to an
        // IDialogService or ViewModel command when the add/edit workflow becomes real.
        var dialog = new PlcEditorDialogWindow
        {
            Owner = System.Windows.Window.GetWindow(this)
        };

        dialog.ShowDialog();
    }
}
