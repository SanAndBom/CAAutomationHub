namespace CAAutomationHub.Wpf.Models.Dashboard;

public sealed record SequenceResponseLatencyBucket(
    DateTimeOffset BucketStart,
    TimeSpan BucketDuration,
    int StartResponseMs,
    int CompletionResponseMs);
