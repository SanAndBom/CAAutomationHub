using CAAutomationHub.Contracts.Runtime;

namespace CAAutomationHub.Runtime;

/// <summary>
/// Defines the top-level Runtime control plane boundary for lifecycle,
/// snapshot access, runtime events, and command intake.
/// </summary>
/// <remarks>
/// The supervisor is not a PLC channel implementation and must not expose WPF,
/// driver, channel runner, or fake PLC details through this boundary.
/// </remarks>
public interface IAutomationHubSupervisor
{
    /// <summary>
    /// Raised after the Runtime publishes a newer snapshot.
    /// </summary>
    event EventHandler<RuntimeSnapshotChangedEventArgs>? SnapshotChanged;

    /// <summary>
    /// Raised when the Runtime publishes a domain event.
    /// </summary>
    event EventHandler<RuntimeEvent>? RuntimeEventRaised;

    /// <summary>
    /// Starts Runtime supervision.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the start operation.</param>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops Runtime supervision.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the stop operation.</param>
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the latest Runtime snapshot.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the snapshot request.</param>
    /// <returns>The latest Runtime snapshot available to the supervisor.</returns>
    /// <remarks>
    /// A snapshot creation path must use one capturedAt value for the whole snapshot:
    /// <see cref="RuntimeSnapshot.CapturedAt"/> and
    /// <see cref="RuntimeSnapshot.Health"/>.<see cref="RuntimeHealthState.CapturedAt"/>
    /// must describe the same point in time. The health captured time is mapped by WPF
    /// to DashboardSnapshot.Health.SnapshotTime; mismatched values can make stale
    /// snapshot filtering and push/pull ordering unreliable.
    /// </remarks>
    Task<RuntimeSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Accepts a dashboard-originated Runtime command for supervisor-level dispatch.
    /// </summary>
    /// <param name="command">The dashboard command contract.</param>
    /// <param name="cancellationToken">A token used to cancel the command request.</param>
    /// <returns>The command result contract.</returns>
    /// <remarks>
    /// AH-RUNTIME-01 defines this boundary only; it does not implement command execution.
    /// </remarks>
    Task<RuntimeDashboardCommandResult> ExecuteAsync(
        RuntimeDashboardCommand command,
        CancellationToken cancellationToken);
}
