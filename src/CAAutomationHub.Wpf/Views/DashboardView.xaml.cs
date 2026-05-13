using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using CAAutomationHub.Wpf.Dialogs;
using CAAutomationHub.Wpf.ViewModels;

namespace CAAutomationHub.Wpf.Views;

public partial class DashboardView : UserControl
{
    private Point _dragStartPoint;
    private double _dragStartHorizontalOffset;
    private bool _isTrackingCardDrag;
    private bool _isDraggingCards;

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

    private void OnPlcCardScrollViewerPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsFromScrollBar(e.OriginalSource as DependencyObject)) return;

        _dragStartPoint = e.GetPosition(PlcCardScrollViewer);
        _dragStartHorizontalOffset = PlcCardScrollViewer.HorizontalOffset;
        _isTrackingCardDrag = true;
        _isDraggingCards = false;
    }

    private void OnPlcCardScrollViewerPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isTrackingCardDrag) return;
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            ResetCardDragState();
            return;
        }

        var currentPoint = e.GetPosition(PlcCardScrollViewer);
        var deltaX = currentPoint.X - _dragStartPoint.X;

        if (!_isDraggingCards && Math.Abs(deltaX) < SystemParameters.MinimumHorizontalDragDistance) return;

        if (!_isDraggingCards)
        {
            _isDraggingCards = true;
            PlcCardScrollViewer.CaptureMouse();
            PlcCardScrollViewer.Cursor = Cursors.SizeWE;
        }

        PlcCardScrollViewer.ScrollToHorizontalOffset(_dragStartHorizontalOffset - deltaX);
        e.Handled = true;
    }

    private void OnPlcCardScrollViewerPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isTrackingCardDrag) return;

        var wasDragging = _isDraggingCards;
        ResetCardDragState();
        e.Handled = wasDragging;
    }

    private void OnPlcCardScrollViewerPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift) return;

        PlcCardScrollViewer.ScrollToHorizontalOffset(PlcCardScrollViewer.HorizontalOffset - e.Delta);
        e.Handled = true;
    }

    private void ResetCardDragState()
    {
        if (PlcCardScrollViewer.IsMouseCaptured) PlcCardScrollViewer.ReleaseMouseCapture();

        PlcCardScrollViewer.Cursor = null;
        _isTrackingCardDrag = false;
        _isDraggingCards = false;
    }

    private static bool IsFromScrollBar(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is ScrollBar) return true;
            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }
}
