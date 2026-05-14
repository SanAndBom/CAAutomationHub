namespace CAAutomationHub.Wpf.Models.Dashboard;

public sealed class DashboardSnapshotChangedEventArgs : EventArgs
{
    public DashboardSnapshotChangedEventArgs(DashboardSnapshot snapshot, DateTimeOffset occurredAt)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        OccurredAt = occurredAt;
    }

    public DashboardSnapshot Snapshot { get; }

    public DateTimeOffset OccurredAt { get; }
}
