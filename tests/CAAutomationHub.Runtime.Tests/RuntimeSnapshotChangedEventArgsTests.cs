using CAAutomationHub.Contracts.Runtime;

namespace CAAutomationHub.Runtime.Tests;

public sealed class RuntimeSnapshotChangedEventArgsTests
{
    [Fact]
    public void Constructor_PreservesSnapshotOccurredAtAndRevision()
    {
        var snapshot = RuntimeSnapshot.Empty;
        var occurredAt = new DateTimeOffset(2026, 5, 14, 21, 30, 0, TimeSpan.FromHours(9));

        var args = new RuntimeSnapshotChangedEventArgs(snapshot, occurredAt, revision: 42);

        Assert.Same(snapshot, args.Snapshot);
        Assert.Equal(occurredAt, args.OccurredAt);
        Assert.Equal(42, args.Revision);
    }

    [Fact]
    public void Constructor_AllowsMissingRevision()
    {
        var args = new RuntimeSnapshotChangedEventArgs(
            RuntimeSnapshot.Empty,
            DateTimeOffset.UnixEpoch);

        Assert.Null(args.Revision);
    }

    [Fact]
    public void Constructor_ThrowsWhenSnapshotIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new RuntimeSnapshotChangedEventArgs(
                snapshot: null!,
                occurredAt: DateTimeOffset.UnixEpoch));

        Assert.Equal("snapshot", exception.ParamName);
    }
}
