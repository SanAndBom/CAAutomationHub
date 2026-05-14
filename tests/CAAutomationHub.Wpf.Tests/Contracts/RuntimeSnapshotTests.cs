using CAAutomationHub.Contracts.Runtime;

namespace CAAutomationHub.Wpf.Tests.Contracts;

public sealed class RuntimeSnapshotTests
{
    [Fact]
    public void Empty_ProvidesNullSafeRuntimeSnapshot()
    {
        var snapshot = RuntimeSnapshot.Empty;

        Assert.Same(RuntimeHealthState.Empty, snapshot.Health);
        Assert.Empty(snapshot.Channels);
        Assert.Empty(snapshot.RecentEvents);
    }

    [Fact]
    public void Constructor_ReplacesNullCollectionsAndHealthWithEmptyValues()
    {
        var snapshot = new RuntimeSnapshot(
            DateTimeOffset.UnixEpoch,
            health: null,
            channels: null,
            recentEvents: null);

        Assert.Equal(DateTimeOffset.UnixEpoch, snapshot.CapturedAt);
        Assert.Same(RuntimeHealthState.Empty, snapshot.Health);
        Assert.Empty(snapshot.Channels);
        Assert.Empty(snapshot.RecentEvents);
    }
}
