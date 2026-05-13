namespace CAAutomationHub.Wpf.Models.Dashboard;

public sealed record PlcRuntimeSignalSnapshot(
    string CurrentSequenceName,
    RuntimeSequenceStatus CurrentSequenceStatus,
    TimeSpan CurrentSequenceElapsed,
    string? LastSequenceMessage,
    IReadOnlyList<SequenceResponseLatencyBucket> ResponseLatencyBuckets)
{
    public static PlcRuntimeSignalSnapshot Empty { get; } = new(
        "대기",
        RuntimeSequenceStatus.Idle,
        TimeSpan.Zero,
        null,
        Array.Empty<SequenceResponseLatencyBucket>());
}
