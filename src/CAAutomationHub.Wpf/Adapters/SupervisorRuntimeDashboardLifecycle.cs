using CAAutomationHub.Runtime;

namespace CAAutomationHub.Wpf.Adapters;

public sealed class SupervisorRuntimeDashboardLifecycle : IRuntimeDashboardLifecycle
{
    private readonly IAutomationHubSupervisor _supervisor;
    private readonly SupervisorRuntimeSnapshotProvider _snapshotProvider;

    public SupervisorRuntimeDashboardLifecycle(
        IAutomationHubSupervisor supervisor,
        SupervisorRuntimeSnapshotProvider snapshotProvider)
    {
        _supervisor = supervisor ?? throw new ArgumentNullException(nameof(supervisor));
        _snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _supervisor.StartAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await _snapshotProvider.RefreshAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // AH-RUNTIME-04 keeps app startup tolerant; failure reporting is deferred.
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => _supervisor.StopAsync(cancellationToken);
}
