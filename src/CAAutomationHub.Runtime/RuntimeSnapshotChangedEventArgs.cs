using CAAutomationHub.Contracts.Runtime;

namespace CAAutomationHub.Runtime;

/// <summary>
/// Provides data for a Runtime snapshot publication.
/// </summary>
/// <remarks>
/// The payload remains the shared <see cref="RuntimeSnapshot"/> contract. Optional
/// <see cref="Revision"/> can be used by later Runtime implementations without adding
/// revision data to the snapshot DTO in AH-RUNTIME-01.
/// </remarks>
public sealed class RuntimeSnapshotChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeSnapshotChangedEventArgs"/> class.
    /// </summary>
    /// <param name="snapshot">The published Runtime snapshot.</param>
    /// <param name="occurredAt">The event occurrence or snapshot publication time.</param>
    /// <param name="revision">An optional monotonically increasing snapshot revision.</param>
    public RuntimeSnapshotChangedEventArgs(
        RuntimeSnapshot snapshot,
        DateTimeOffset occurredAt,
        long? revision = null)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        OccurredAt = occurredAt;
        Revision = revision;
    }

    /// <summary>
    /// Gets the published Runtime snapshot.
    /// </summary>
    /// <remarks>
    /// Runtime publishers should use a single capturedAt value when creating this snapshot:
    /// <see cref="RuntimeSnapshot.CapturedAt"/> and
    /// <see cref="RuntimeSnapshot.Health"/>.<see cref="RuntimeHealthState.CapturedAt"/>
    /// should match so downstream dashboard stale-snapshot checks remain stable.
    /// </remarks>
    public RuntimeSnapshot Snapshot { get; }

    /// <summary>
    /// Gets the event occurrence or snapshot publication time.
    /// </summary>
    public DateTimeOffset OccurredAt { get; }

    /// <summary>
    /// Gets an optional snapshot revision supplied by the Runtime publisher.
    /// </summary>
    public long? Revision { get; }
}
