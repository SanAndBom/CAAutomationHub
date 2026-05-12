using System.Collections.Specialized;
using System.Windows;
using System.Windows.Threading;
using CAAutomationHub.Wpf.ViewModels;

namespace CAAutomationHub.Wpf.Dialogs;

public partial class RealtimeEventLogWindow : Window
{
    public RealtimeEventLogWindow(string initialPlcFilter = "")
    {
        InitializeComponent();
        var viewModel = new RealtimeEventLogViewModel(initialPlcFilter: initialPlcFilter);
        DataContext = viewModel;
        viewModel.Events.CollectionChanged += OnEventsCollectionChanged;
        Closed += OnClosed;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnEventsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || e.NewStartingIndex != 0) return;
        if (DataContext is not RealtimeEventLogViewModel viewModel || !viewModel.IsAutoScrollEnabled) return;

        Dispatcher.BeginInvoke(() => EventListScrollViewer.ScrollToTop(), DispatcherPriority.Background);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Closed -= OnClosed;
        if (DataContext is RealtimeEventLogViewModel viewModel)
        {
            viewModel.Events.CollectionChanged -= OnEventsCollectionChanged;
            viewModel.Dispose();
        }
    }
}
