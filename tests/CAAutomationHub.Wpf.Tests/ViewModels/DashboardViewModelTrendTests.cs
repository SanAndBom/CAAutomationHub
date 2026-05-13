using CAAutomationHub.Wpf.Adapters;
using CAAutomationHub.Wpf.Models.Dashboard;
using CAAutomationHub.Wpf.ViewModels;

namespace CAAutomationHub.Wpf.Tests.ViewModels;

public sealed class DashboardViewModelTrendTests
{
    [Fact]
    public void Constructor_SelectsOverviewTrendWhenNoPlcIsSelected()
    {
        var snapshot = CreateSnapshot(
            overview: CreateTrend("overview", "전체 Overview", true, 10, 20),
            plcTrends:
            [
                CreateTrend("PLC-01", "PLC 01", false, 30)
            ]);

        using var viewModel = new DashboardViewModel(new StaticDashboardAdapter(snapshot));

        Assert.Same(snapshot.CommunicationTrend.Overview, viewModel.CurrentCommunicationTrend);
        Assert.Equal("전체 Overview", viewModel.CurrentCommunicationTrend.TargetName);
    }

    [Fact]
    public void SelectPlcCommand_SelectsMatchingPlcTrend()
    {
        var plcTrend = CreateTrend("PLC-02", "PLC 02", false, 30, 40);
        var snapshot = CreateSnapshot(
            overview: CreateTrend("overview", "전체 Overview", true, 10, 20),
            plcTrends:
            [
                CreateTrend("PLC-01", "PLC 01", false, 25),
                plcTrend
            ]);

        using var viewModel = new DashboardViewModel(new StaticDashboardAdapter(snapshot));

        viewModel.SelectPlcCommand.Execute(viewModel.PlcCards[1]);

        Assert.Same(plcTrend, viewModel.CurrentCommunicationTrend);
    }

    [Fact]
    public void SelectPlcCommand_WhenSamePlcIsClickedAgain_ClearsSelectionAndReturnsToOverviewTrend()
    {
        var overview = CreateTrend("overview", "전체 PLC 통신 품질", true, 10, 20);
        var snapshot = CreateSnapshot(
            overview: overview,
            plcTrends:
            [
                CreateTrend("PLC-01", "PLC 01", false, 25),
                CreateTrend("PLC-02", "PLC 02", false, 30)
            ]);

        using var viewModel = new DashboardViewModel(new StaticDashboardAdapter(snapshot));
        var selectedCard = viewModel.PlcCards[1];

        viewModel.SelectPlcCommand.Execute(selectedCard);
        viewModel.SelectPlcCommand.Execute(selectedCard);

        Assert.Null(viewModel.SelectedPlc);
        Assert.False(selectedCard.IsSelected);
        Assert.All(viewModel.PlcCards, card => Assert.False(card.IsSelected));
        Assert.False(viewModel.IsDetailPaneOpen);
        Assert.Same(overview, viewModel.CurrentCommunicationTrend);
    }

    [Fact]
    public void RefreshCommand_PreservesOverviewTrendAfterSelectionIsCleared()
    {
        var first = CreateSnapshot(
            overview: CreateTrend("overview", "Overview A", true, 10),
            plcTrends:
            [
                CreateTrend("PLC-01", "PLC 01 A", false, 20),
                CreateTrend("PLC-02", "PLC 02 A", false, 30)
            ]);
        var refreshedOverview = CreateTrend("overview", "Overview B", true, 12);
        var second = CreateSnapshot(
            overview: refreshedOverview,
            plcTrends:
            [
                CreateTrend("PLC-01", "PLC 01 B", false, 22),
                CreateTrend("PLC-02", "PLC 02 B", false, 32)
            ]);

        using var viewModel = new DashboardViewModel(new SequencedDashboardAdapter(first, second));
        var selectedCard = viewModel.PlcCards[1];

        viewModel.SelectPlcCommand.Execute(selectedCard);
        viewModel.SelectPlcCommand.Execute(selectedCard);
        viewModel.RefreshCommand.Execute(null);

        Assert.Null(viewModel.SelectedPlc);
        Assert.False(viewModel.IsDetailPaneOpen);
        Assert.Same(refreshedOverview, viewModel.CurrentCommunicationTrend);
        Assert.All(viewModel.PlcCards, card => Assert.False(card.IsSelected));
    }

    [Fact]
    public void RefreshCommand_PreservesSelectedPlcTrendForSamePlcId()
    {
        var first = CreateSnapshot(
            overview: CreateTrend("overview", "Overview A", true, 10),
            plcTrends:
            [
                CreateTrend("PLC-01", "PLC 01 A", false, 20),
                CreateTrend("PLC-02", "PLC 02 A", false, 30)
            ]);
        var refreshedPlcTrend = CreateTrend("PLC-02", "PLC 02 B", false, 55);
        var second = CreateSnapshot(
            overview: CreateTrend("overview", "Overview B", true, 11),
            plcTrends:
            [
                CreateTrend("PLC-01", "PLC 01 B", false, 21),
                refreshedPlcTrend
            ]);

        using var viewModel = new DashboardViewModel(new SequencedDashboardAdapter(first, second));

        viewModel.SelectPlcCommand.Execute(viewModel.PlcCards[1]);
        viewModel.RefreshCommand.Execute(null);

        Assert.Same(refreshedPlcTrend, viewModel.CurrentCommunicationTrend);
        Assert.Equal("PLC-02", viewModel.CurrentCommunicationTrend.TargetId);
    }

    [Fact]
    public void RefreshCommand_FallsBackToOverviewWhenSelectedPlcTrendIsMissing()
    {
        var first = CreateSnapshot(
            overview: CreateTrend("overview", "Overview A", true, 10),
            plcTrends:
            [
                CreateTrend("PLC-01", "PLC 01 A", false, 20),
                CreateTrend("PLC-02", "PLC 02 A", false, 30)
            ]);
        var fallbackOverview = CreateTrend("overview", "Overview B", true, 11);
        var second = CreateSnapshot(
            overview: fallbackOverview,
            plcTrends:
            [
                CreateTrend("PLC-01", "PLC 01 B", false, 21)
            ]);

        using var viewModel = new DashboardViewModel(new SequencedDashboardAdapter(first, second));

        viewModel.SelectPlcCommand.Execute(viewModel.PlcCards[1]);
        viewModel.RefreshCommand.Execute(null);

        Assert.Same(fallbackOverview, viewModel.CurrentCommunicationTrend);
    }

    [Fact]
    public void SummaryMetrics_CalculateFromCurrentTrendPoints()
    {
        var snapshot = CreateSnapshot(
            overview: new CommunicationTrendSnapshot(
                "overview",
                "전체 Overview",
                true,
                100,
                500,
                [
                    new TrendPoint(DateTimeOffset.UtcNow.AddSeconds(-10), 10, false),
                    new TrendPoint(DateTimeOffset.UtcNow.AddSeconds(-5), 20, true, TrendMarkerKind.Error, "Error"),
                    new TrendPoint(DateTimeOffset.UtcNow, 30, false)
                ]),
            plcTrends: []);

        using var viewModel = new DashboardViewModel(new StaticDashboardAdapter(snapshot));

        Assert.Equal(20, viewModel.TrendAverageResponseMs);
        Assert.Equal(30, viewModel.TrendMaxResponseMs);
        Assert.Equal(1, viewModel.TrendErrorCount);
        Assert.Equal(3, viewModel.TrendPointCount);
    }

    [Fact]
    public void SummaryMetrics_AreSafeForEmptyTrendPoints()
    {
        var snapshot = CreateSnapshot(
            overview: CommunicationTrendSnapshot.CreateEmpty("overview", "전체 Overview", true),
            plcTrends: []);

        using var viewModel = new DashboardViewModel(new StaticDashboardAdapter(snapshot));

        Assert.Equal(0, viewModel.TrendAverageResponseMs);
        Assert.Equal(0, viewModel.TrendMaxResponseMs);
        Assert.Equal(0, viewModel.TrendErrorCount);
        Assert.Equal(0, viewModel.TrendPointCount);
    }

    [Fact]
    public void FakeDashboardRuntimeAdapter_CreatesWarningAndErrorMarkers()
    {
        var snapshot = new FakeDashboardRuntimeAdapter().GetSnapshot();

        var points = snapshot.CommunicationTrend.PlcTrends
            .SelectMany(trend => trend.Points)
            .ToArray();

        Assert.Contains(points, point => point.MarkerKind == TrendMarkerKind.Warning);
        Assert.Contains(points, point => point.MarkerKind == TrendMarkerKind.Error);
        Assert.All(points.Where(point => point.HasError), point => Assert.Equal(TrendMarkerKind.Error, point.MarkerKind));
    }

    [Fact]
    public void FakeDashboardRuntimeAdapter_UsesClearOverviewTargetName()
    {
        var snapshot = new FakeDashboardRuntimeAdapter().GetSnapshot();

        Assert.Equal("전체 PLC 통신 품질", snapshot.CommunicationTrend.Overview.TargetName);
    }

    [Fact]
    public void FakeDashboardRuntimeAdapter_ProvidesThresholdsThroughTrendSnapshots()
    {
        var snapshot = new FakeDashboardRuntimeAdapter().GetSnapshot();
        var allTrends = snapshot.CommunicationTrend.PlcTrends
            .Prepend(snapshot.CommunicationTrend.Overview)
            .ToArray();

        Assert.All(allTrends, trend =>
        {
            Assert.Equal(250, trend.WarningThresholdMs);
            Assert.Equal(500, trend.CongestedThresholdMs);
            Assert.Equal(750, trend.ErrorThresholdMs);
            Assert.True(trend.WarningThresholdMs < trend.CongestedThresholdMs);
            Assert.True(trend.CongestedThresholdMs < trend.ErrorThresholdMs);
        });
    }

    [Fact]
    public void FakeDashboardRuntimeAdapter_OverviewTrendProvidesWorstPlcHint()
    {
        var snapshot = new FakeDashboardRuntimeAdapter().GetSnapshot();
        var overview = snapshot.CommunicationTrend.Overview;

        Assert.False(string.IsNullOrWhiteSpace(overview.WorstPlcId));
        Assert.False(string.IsNullOrWhiteSpace(overview.WorstPlcName));
        Assert.True(overview.WorstResponseMs > 0);
        Assert.Contains(snapshot.PlcCards, card => card.PlcId == overview.WorstPlcId);
    }

    [Fact]
    public void FakeDashboardRuntimeAdapter_OverviewTrendProvidesOneSeriesForEachPlcCard()
    {
        var snapshot = new FakeDashboardRuntimeAdapter().GetSnapshot();
        var overview = snapshot.CommunicationTrend.Overview;

        Assert.Equal(snapshot.PlcCards.Count, overview.Series.Count);
        foreach (var card in snapshot.PlcCards)
        {
            var series = Assert.Single(overview.Series, item => item.TargetId == card.PlcId);
            Assert.Equal(card.PlcName, series.TargetName);
            Assert.Equal(card.ConnectionState, series.State);
            Assert.NotEmpty(series.Points);
        }
    }

    [Fact]
    public void FakeDashboardRuntimeAdapter_OverviewTrendMarksExactlyOneWorstSeries()
    {
        var snapshot = new FakeDashboardRuntimeAdapter().GetSnapshot();
        var overview = snapshot.CommunicationTrend.Overview;

        var worstSeries = Assert.Single(overview.Series, series => series.IsWorst);

        Assert.Equal(overview.WorstPlcId, worstSeries.TargetId);
        Assert.Equal(overview.WorstPlcName, worstSeries.TargetName);
    }

    [Fact]
    public void FakeDashboardRuntimeAdapter_SelectedPlcTrendsDoNotProvideWorstPlcHint()
    {
        var snapshot = new FakeDashboardRuntimeAdapter().GetSnapshot();

        Assert.All(snapshot.CommunicationTrend.PlcTrends, trend =>
        {
            Assert.Null(trend.WorstPlcId);
            Assert.Null(trend.WorstPlcName);
            Assert.Null(trend.WorstResponseMs);
            Assert.Empty(trend.Series);
        });
    }

    private static DashboardSnapshot CreateSnapshot(
        CommunicationTrendSnapshot overview,
        IReadOnlyList<CommunicationTrendSnapshot> plcTrends)
    {
        var cards = new[]
        {
            CreateCard("PLC-01", 11),
            CreateCard("PLC-02", 22)
        };

        var health = new RuntimeHealthSnapshot(
            TotalPlcs: cards.Length,
            HealthyCount: cards.Length,
            WarningCount: 0,
            CongestedCount: 0,
            ErrorCount: 0,
            SnapshotTime: DateTimeOffset.UtcNow);

        return new DashboardSnapshot(
            health,
            cards,
            new CommunicationTrendSetSnapshot(overview, plcTrends));
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

    private static CommunicationTrendSnapshot CreateTrend(
        string targetId,
        string targetName,
        bool isOverview,
        params double[] responseMs)
        => new(
            targetId,
            targetName,
            isOverview,
            WarningThresholdMs: 100,
            ErrorThresholdMs: 500,
            responseMs.Select((response, index) => new TrendPoint(
                    DateTimeOffset.UtcNow.AddSeconds(index),
                    response,
                    HasError: false))
                .ToArray());

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
