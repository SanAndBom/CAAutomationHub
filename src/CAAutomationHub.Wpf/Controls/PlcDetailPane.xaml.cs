using System.Windows;
using System.Windows.Controls;
using CAAutomationHub.Wpf.Dialogs;
using CAAutomationHub.Wpf.ViewModels;

namespace CAAutomationHub.Wpf.Controls;

public partial class PlcDetailPane : UserControl
{
    public static readonly RoutedEvent EditRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(EditRequested),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(PlcDetailPane));

    public static readonly RoutedEvent DeleteRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(DeleteRequested),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(PlcDetailPane));

    public PlcDetailPane() => InitializeComponent();

    public event RoutedEventHandler EditRequested
    {
        add => AddHandler(EditRequestedEvent, value);
        remove => RemoveHandler(EditRequestedEvent, value);
    }

    public event RoutedEventHandler DeleteRequested
    {
        add => AddHandler(DeleteRequestedEvent, value);
        remove => RemoveHandler(DeleteRequestedEvent, value);
    }

    private void OnEditClick(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(EditRequestedEvent, this));
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(DeleteRequestedEvent, this));
    }

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
