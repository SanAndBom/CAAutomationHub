using CAAutomationHub.Wpf.Adapters;
using CAAutomationHub.Wpf.Models.Dashboard;
using CAAutomationHub.Wpf.ViewModels;

namespace CAAutomationHub.Wpf.Tests.ViewModels;

public sealed class DashboardRuntimeSignalTests
{
    [Fact]
    public void FakeDashboardRuntimeAdapter_ProvidesRuntimeSignalForEveryCard()
    {
        var snapshot = new FakeDashboardRuntimeAdapter().GetSnapshot();

        Assert.All(snapshot.PlcCards, card =>
        {
            Assert.NotNull(card.RuntimeSignal);
            Assert.False(string.IsNullOrWhiteSpace(card.RuntimeSignal.CurrentSequenceName));
            Assert.Equal(10, card.RuntimeSignal.ResponseLatencyBuckets.Count);
            Assert.All(card.RuntimeSignal.ResponseLatencyBuckets, bucket =>
            {
                Assert.Equal(TimeSpan.FromSeconds(30), bucket.BucketDuration);
                Assert.True(bucket.StartResponseMs >= 0);
                Assert.True(bucket.CompletionResponseMs >= 0);
            });
        });
    }

    [Theory]
    [InlineData("PLC-01", RuntimeSequenceStatus.Completed)]
    [InlineData("PLC-02", RuntimeSequenceStatus.Delayed)]
    [InlineData("PLC-03", RuntimeSequenceStatus.Completed)]
    [InlineData("PLC-04", RuntimeSequenceStatus.Failed)]
    [InlineData("PLC-05", RuntimeSequenceStatus.Idle)]
    public void FakeDashboardRuntimeAdapter_MapsConnectionStateToSequenceStatus(string plcId, RuntimeSequenceStatus expectedStatus)
    {
        var snapshot = new FakeDashboardRuntimeAdapter().GetSnapshot();

        var card = Assert.Single(snapshot.PlcCards, item => item.PlcId == plcId);

        Assert.Equal(expectedStatus, card.RuntimeSignal.CurrentSequenceStatus);
    }

    [Fact]
    public void FakeDashboardRuntimeAdapter_MapsCongestedPhaseToWaitingSequenceStatus()
    {
        var adapter = new FakeDashboardRuntimeAdapter();
        DashboardSnapshot snapshot = adapter.GetSnapshot();

        for (var i = 0; i < 8; i++)
        {
            snapshot = adapter.GetSnapshot();
        }

        var card = Assert.Single(snapshot.PlcCards, item => item.PlcId == "PLC-03");

        Assert.Equal(PlcConnectionState.Congested, card.ConnectionState);
        Assert.Equal(RuntimeSequenceStatus.Waiting, card.RuntimeSignal.CurrentSequenceStatus);
    }

    [Fact]
    public void PlcStatusCardViewModel_CurrentSequenceText_UsesStatusAndElapsed()
    {
        var first = CreateCard(
            "PLC-01",
            new PlcRuntimeSignalSnapshot(
                "폴링",
                RuntimeSequenceStatus.Completed,
                TimeSpan.Zero,
                "정상 폴링",
                []));
        var second = CreateCard(
            "PLC-01",
            new PlcRuntimeSignalSnapshot(
                "DB조회",
                RuntimeSequenceStatus.Delayed,
                TimeSpan.FromSeconds(12),
                "응답 지연",
                []));
        var viewModel = new PlcStatusCardViewModel(first);

        viewModel.UpdateSnapshot(second);

        Assert.Equal("현재: DB조회 · 지연 12s", viewModel.CurrentSequenceText);
        Assert.Equal("지연", viewModel.CurrentSequenceStatusText);
        Assert.Equal("12s", viewModel.CurrentSequenceElapsedText);
    }

    [Fact]
    public void AddPlcCommand_CreatesRuntimeSignalForNewCard()
    {
        var adapter = new FakeDashboardRuntimeAdapter();
        using var viewModel = new DashboardViewModel(adapter, adapter);
        var configuration = adapter.CreateDefaultPlcConfiguration();

        viewModel.AddPlcCommand.Execute(configuration);

        Assert.NotNull(viewModel.SelectedPlc);
        Assert.Equal("PLC-06", viewModel.SelectedPlc.PlcId);
        Assert.NotNull(viewModel.SelectedPlc.Snapshot.RuntimeSignal);
        Assert.Equal(10, viewModel.SelectedPlc.SequenceResponseLatencyBuckets.Count);
        Assert.False(string.IsNullOrWhiteSpace(viewModel.SelectedPlc.CurrentSequenceText));
    }

    [Fact]
    public void EditSelectedPlcCommand_PreservesRuntimeSignalForSamePlcId()
    {
        var adapter = new FakeDashboardRuntimeAdapter();
        using var viewModel = new DashboardViewModel(adapter, adapter);

        viewModel.SelectPlcCommand.Execute(viewModel.PlcCards[0]);
        var selectedId = viewModel.SelectedPlc!.PlcId;
        var originalSignal = viewModel.SelectedPlc.RuntimeSignal;
        var configuration = adapter.GetPlcConfiguration(selectedId);
        Assert.NotNull(configuration);

        viewModel.EditSelectedPlcCommand.Execute(configuration! with { PlcName = "Edited Runtime PLC" });

        Assert.NotNull(viewModel.SelectedPlc);
        Assert.Equal(selectedId, viewModel.SelectedPlc.PlcId);
        Assert.Equal("Edited Runtime PLC", viewModel.SelectedPlc.PlcName);
        Assert.NotNull(viewModel.SelectedPlc.RuntimeSignal);
        Assert.Equal(originalSignal.CurrentSequenceStatus, viewModel.SelectedPlc.RuntimeSignal.CurrentSequenceStatus);
        Assert.Equal(10, viewModel.SelectedPlc.SequenceResponseLatencyBuckets.Count);
    }

    [Fact]
    public void DeleteSelectedPlcCommand_RemovesRuntimeSignalWithDeletedCard()
    {
        var adapter = new FakeDashboardRuntimeAdapter();
        using var viewModel = new DashboardViewModel(adapter, adapter);
        var deletedId = viewModel.PlcCards[1].PlcId;

        viewModel.DeleteSelectedPlcCommand.Execute(deletedId);

        Assert.DoesNotContain(viewModel.PlcCards, card => card.PlcId == deletedId);
        Assert.DoesNotContain(adapter.GetSnapshot().PlcCards, card => card.PlcId == deletedId);
    }

    [Fact]
    public void RefreshCommand_UpdatesRuntimeSignalForSamePlcId()
    {
        var first = CreateSnapshot(CreateCard(
            "PLC-01",
            new PlcRuntimeSignalSnapshot(
                "폴링",
                RuntimeSequenceStatus.Running,
                TimeSpan.FromSeconds(1),
                "폴링 중",
                [])));
        var second = CreateSnapshot(CreateCard(
            "PLC-01",
            new PlcRuntimeSignalSnapshot(
                "DB조회",
                RuntimeSequenceStatus.Delayed,
                TimeSpan.FromSeconds(12),
                "응답 지연",
                [])));
        using var viewModel = new DashboardViewModel(new SequencedDashboardAdapter(first, second));

        viewModel.RefreshCommand.Execute(null);

        Assert.Equal("현재: DB조회 · 지연 12s", viewModel.PlcCards[0].CurrentSequenceText);
    }

    private static DashboardSnapshot CreateSnapshot(PlcCardSnapshot card)
    {
        var health = new RuntimeHealthSnapshot(
            TotalPlcs: 1,
            HealthyCount: 1,
            WarningCount: 0,
            CongestedCount: 0,
            ErrorCount: 0,
            SnapshotTime: DateTimeOffset.UtcNow);

        return new DashboardSnapshot(health, [card]);
    }

    private static PlcCardSnapshot CreateCard(string plcId, PlcRuntimeSignalSnapshot runtimeSignal)
        => new(
            plcId,
            $"{plcId} Name",
            "Line-1",
            PlcConnectionState.Healthy,
            "192.168.0.10",
            2004,
            500,
            20,
            100,
            98,
            0,
            runtimeSignal);

    private sealed class SequencedDashboardAdapter : IRuntimeDashboardAdapter
    {
        private readonly Queue<DashboardSnapshot> _snapshots;

        public SequencedDashboardAdapter(params DashboardSnapshot[] snapshots)
            => _snapshots = new Queue<DashboardSnapshot>(snapshots);

        public DashboardSnapshot GetSnapshot()
            => _snapshots.Count > 1 ? _snapshots.Dequeue() : _snapshots.Peek();
    }
}
