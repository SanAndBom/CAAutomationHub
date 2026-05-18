namespace CAAutomationHub.PilotApp.Polling;

public sealed class PilotPollingSnapshotChangedEventArgs : EventArgs
{
    public PilotPollingSnapshotChangedEventArgs(PilotPollingSnapshot snapshot)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public PilotPollingSnapshot Snapshot { get; }
}
