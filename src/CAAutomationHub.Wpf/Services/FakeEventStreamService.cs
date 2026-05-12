using System.Windows.Threading;
using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Services;

public sealed class FakeEventStreamService : IEventStreamService
{
    private static readonly TimeSpan[] Intervals =
    [
        TimeSpan.FromMilliseconds(800),
        TimeSpan.FromMilliseconds(1000),
        TimeSpan.FromMilliseconds(1200),
        TimeSpan.FromMilliseconds(1500)
    ];

    private static readonly FakeEventTemplate[] Templates =
    [
        new(EventSeverity.Info, "PLC-01", "Connection", "Heartbeat received within expected latency.", "Live"),
        new(EventSeverity.Warning, "PLC-02", "Latency", "Response time exceeded warning threshold.", "Watch"),
        new(EventSeverity.Info, "PLC-03", "Polling", "Polling cycle completed successfully.", "Live"),
        new(EventSeverity.Critical, "PLC-04", "Connection", "Communication timeout detected.", "Open"),
        new(EventSeverity.Warning, "PLC-09", "Traffic", "Packet congestion pattern detected.", "Watch"),
        new(EventSeverity.Info, "PLC-06", "Recovery", "Connection state returned to healthy.", "Closed"),
        new(EventSeverity.Warning, "PLC-08", "ErrorCount", "Retry count increased during polling.", "Watch"),
        new(EventSeverity.Info, "PLC-10", "Runtime", "Snapshot refresh completed.", "Live")
    ];

    private readonly DispatcherTimer _timer;
    private int _eventIndex;
    private bool _isDisposed;

    public FakeEventStreamService()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = Intervals[0]
        };
        _timer.Tick += OnTick;
    }

    public event EventHandler<RuntimeEventLogItem>? EventReceived;

    public void Start()
    {
        if (_isDisposed || _timer.IsEnabled) return;
        _timer.Start();
    }

    public void Stop()
    {
        if (_timer.IsEnabled) _timer.Stop();
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        Stop();
        _timer.Tick -= OnTick;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var template = Templates[_eventIndex % Templates.Length];
        var item = new RuntimeEventLogItem(
            DateTimeOffset.Now,
            template.Severity,
            template.PlcName,
            template.Category,
            $"{template.Message} #{_eventIndex + 1:0000}",
            template.Status);

        _eventIndex++;
        _timer.Interval = Intervals[_eventIndex % Intervals.Length];
        EventReceived?.Invoke(this, item);
    }

    private sealed record FakeEventTemplate(
        EventSeverity Severity,
        string PlcName,
        string Category,
        string Message,
        string Status);
}
