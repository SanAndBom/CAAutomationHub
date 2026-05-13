using CAAutomationHub.Wpf.Adapters;
using CAAutomationHub.Wpf.Models.Dashboard;
using CAAutomationHub.Wpf.ViewModels;

namespace CAAutomationHub.Wpf.Tests.ViewModels;

public sealed class DashboardViewModelSelectionTests
{
    [Fact]
    public void SelectPlcCommand_SetsOnlySelectedCardAsSelected()
    {
        using var viewModel = new DashboardViewModel(new StaticDashboardAdapter(CreateSnapshot(10)));

        var first = viewModel.PlcCards[0];
        var second = viewModel.PlcCards[1];

        viewModel.SelectPlcCommand.Execute(first);
        viewModel.SelectPlcCommand.Execute(second);

        Assert.False(first.IsSelected);
        Assert.True(second.IsSelected);
        Assert.Same(second, viewModel.SelectedPlc);
    }

    [Fact]
    public void RefreshCommand_PreservesSelectedCardHighlightForSamePlcId()
    {
        var adapter = new SequencedDashboardAdapter(CreateSnapshot(10), CreateSnapshot(20));
        using var viewModel = new DashboardViewModel(adapter);

        var selected = viewModel.PlcCards[1];

        viewModel.SelectPlcCommand.Execute(selected);
        viewModel.RefreshCommand.Execute(null);

        Assert.False(viewModel.PlcCards[0].IsSelected);
        Assert.True(viewModel.PlcCards[1].IsSelected);
        Assert.Same(viewModel.PlcCards[1], viewModel.SelectedPlc);
        Assert.NotNull(viewModel.SelectedPlc);
        Assert.Equal(20, viewModel.SelectedPlc.LastResponseMs);
    }

    private static DashboardSnapshot CreateSnapshot(int plc02LastResponseMs)
    {
        var cards = new[]
        {
            CreateCard("PLC-01", 11),
            CreateCard("PLC-02", plc02LastResponseMs)
        };

        var health = new RuntimeHealthSnapshot(
            TotalPlcs: cards.Length,
            HealthyCount: cards.Length,
            WarningCount: 0,
            CongestedCount: 0,
            ErrorCount: 0,
            SnapshotTime: DateTimeOffset.UtcNow);

        return new DashboardSnapshot(health, cards);
    }

    private static PlcCardSnapshot CreateCard(string plcId, int lastResponseMs)
        => new(
            plcId,
            $"{plcId} Name",
            "Line-1",
            PlcConnectionState.Healthy,
            "192.168.0.10",
            2004,
            500,
            lastResponseMs,
            100,
            98,
            0);

    private sealed class StaticDashboardAdapter : IRuntimeDashboardAdapter
    {
        private readonly DashboardSnapshot _snapshot;

        public StaticDashboardAdapter(DashboardSnapshot snapshot) => _snapshot = snapshot;

        public DashboardSnapshot GetSnapshot() => _snapshot;
    }

    private sealed class SequencedDashboardAdapter : IRuntimeDashboardAdapter
    {
        private readonly Queue<DashboardSnapshot> _snapshots;

        public SequencedDashboardAdapter(params DashboardSnapshot[] snapshots)
            => _snapshots = new Queue<DashboardSnapshot>(snapshots);

        public DashboardSnapshot GetSnapshot()
            => _snapshots.Count > 1 ? _snapshots.Dequeue() : _snapshots.Peek();
    }
}
