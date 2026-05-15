namespace CAAutomationHub.Runtime.Channels;

/// <summary>
/// Optional Runtime channel boundary for components that can replace their
/// internal state without publishing a Runtime snapshot.
/// </summary>
/// <remarks>
/// This contract does not mean every Runtime channel is writable. Callers that
/// need writable behavior should pattern-match an <see cref="IRuntimePlcChannel"/>
/// to this interface and keep snapshot publishing as a separate operation.
/// </remarks>
public interface IWritableRuntimePlcChannel : IRuntimePlcChannel
{
    /// <summary>
    /// Replaces the channel's Runtime-local state.
    /// </summary>
    /// <param name="state">The replacement Runtime-local channel state.</param>
    /// <remarks>
    /// This method only updates channel state. It must not publish a snapshot,
    /// raise SnapshotChanged, or call supervisor refresh APIs.
    /// </remarks>
    void ReplaceState(RuntimePlcChannelState state);
}
