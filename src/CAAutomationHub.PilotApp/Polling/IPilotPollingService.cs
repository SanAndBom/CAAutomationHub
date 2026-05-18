namespace CAAutomationHub.PilotApp.Polling;

public interface IPilotPollingService
{
    event EventHandler<PilotPollingSnapshotChangedEventArgs>? SnapshotChanged;

    PilotPollingSnapshot CurrentSnapshot { get; }

    ValueTask StartAsync(CancellationToken cancellationToken = default);

    ValueTask StopAsync(CancellationToken cancellationToken = default);

    ValueTask<PilotPollingSnapshot> PollOnceAsync(CancellationToken cancellationToken = default);
}
