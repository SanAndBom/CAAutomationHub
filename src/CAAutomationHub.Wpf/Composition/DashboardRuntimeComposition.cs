using CAAutomationHub.Wpf.Adapters;

namespace CAAutomationHub.Wpf.Composition;

public sealed class DashboardRuntimeComposition : IDisposable
{
    private readonly IDisposable? _disposable;
    private bool _disposed;

    public DashboardRuntimeComposition(
        DashboardRuntimeMode mode,
        IRuntimeDashboardAdapter adapter,
        IRuntimeDashboardLifecycle? lifecycle = null,
        IDisposable? disposable = null)
    {
        Mode = mode;
        Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        Lifecycle = lifecycle;
        _disposable = disposable;
    }

    public DashboardRuntimeMode Mode { get; }

    public IRuntimeDashboardAdapter Adapter { get; }

    public IRuntimeDashboardLifecycle? Lifecycle { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposable?.Dispose();
        _disposed = true;
    }
}
