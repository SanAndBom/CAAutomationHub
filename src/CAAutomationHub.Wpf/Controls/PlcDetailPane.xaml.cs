using System.Windows;
using System.Windows.Controls;
using CAAutomationHub.Wpf.Dialogs;
using CAAutomationHub.Wpf.ViewModels;

namespace CAAutomationHub.Wpf.Controls;

public partial class PlcDetailPane : UserControl
{
    public PlcDetailPane() => InitializeComponent();

    private void OnOpenEventLogClick(object sender, RoutedEventArgs e)
    {
        var plcFilter = DataContext is PlcStatusCardViewModel selectedPlc ? selectedPlc.PlcId : string.Empty;

        // AH-WPF-04 prototype: opened from code-behind. Later move to IDialogService or ViewModel command.
        var window = new RealtimeEventLogWindow(plcFilter)
        {
            Owner = Window.GetWindow(this)
        };

        window.ShowDialog();
    }
}
