using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Services;

public sealed class DashboardRefreshOrchestrator
{
    private readonly IUiDispatcher _dispatcher;
    private readonly Action<DashboardSnapshot> _applySnapshot;
    private readonly object _syncRoot = new();
    private DashboardSnapshot? _pendingSnapshot;
    private DateTimeOffset? _latestAcceptedSnapshotTime;
    private bool _isApplyScheduled;
    private bool _isApplyingSnapshot;

    public DashboardRefreshOrchestrator(IUiDispatcher dispatcher, Action<DashboardSnapshot> applySnapshot)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _applySnapshot = applySnapshot ?? throw new ArgumentNullException(nameof(applySnapshot));
    }

    public void SubmitSnapshot(DashboardSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var shouldPost = false;
        lock (_syncRoot)
        {
            if (IsOlderThanLatestAccepted(snapshot))
            {
                return;
            }

            _latestAcceptedSnapshotTime = snapshot.Health.SnapshotTime;
            _pendingSnapshot = snapshot;

            if (!_isApplyScheduled && !_isApplyingSnapshot)
            {
                _isApplyScheduled = true;
                shouldPost = true;
            }
        }

        if (shouldPost)
        {
            _dispatcher.Post(ApplyPendingSnapshot);
        }
    }

    private void ApplyPendingSnapshot()
    {
        DashboardSnapshot? snapshot;
        lock (_syncRoot)
        {
            _isApplyScheduled = false;
            snapshot = _pendingSnapshot;
            _pendingSnapshot = null;

            if (snapshot is null)
            {
                return;
            }

            _isApplyingSnapshot = true;
        }

        try
        {
            _applySnapshot(snapshot);
        }
        finally
        {
            SchedulePendingSnapshotIfNeeded();
        }
    }

    private void SchedulePendingSnapshotIfNeeded()
    {
        var shouldPost = false;
        lock (_syncRoot)
        {
            _isApplyingSnapshot = false;
            if (_pendingSnapshot is not null)
            {
                _isApplyScheduled = true;
                shouldPost = true;
            }
        }

        if (shouldPost)
        {
            _dispatcher.Post(ApplyPendingSnapshot);
        }
    }

    private bool IsOlderThanLatestAccepted(DashboardSnapshot snapshot)
        => _latestAcceptedSnapshotTime is DateTimeOffset latestAcceptedSnapshotTime &&
           snapshot.Health.SnapshotTime < latestAcceptedSnapshotTime;
}
