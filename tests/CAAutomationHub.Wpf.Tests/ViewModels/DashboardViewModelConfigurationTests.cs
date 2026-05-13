using CAAutomationHub.Wpf.Adapters;
using CAAutomationHub.Wpf.Models.Dashboard;
using CAAutomationHub.Wpf.ViewModels;

namespace CAAutomationHub.Wpf.Tests.ViewModels;

public sealed class DashboardViewModelConfigurationTests
{
    [Fact]
    public void Constructor_LoadsFiveDefaultFakePlcCardsAndOverviewSeries()
    {
        var adapter = new FakeDashboardRuntimeAdapter();
        using var viewModel = new DashboardViewModel(adapter, adapter);

        Assert.Equal(5, viewModel.PlcCards.Count);
        Assert.Equal(5, viewModel.TotalCount);
        Assert.Equal(1, viewModel.InactiveCount);
        Assert.Equal(
            viewModel.TotalCount,
            viewModel.HealthyCount + viewModel.WarningCount + viewModel.CongestedCount + viewModel.ErrorCount + viewModel.InactiveCount);
        Assert.Equal(5, viewModel.CurrentCommunicationTrend.Series.Count);
        Assert.Equal(["PLC-01", "PLC-02", "PLC-03", "PLC-04", "PLC-05"], viewModel.PlcCards.Select(card => card.PlcId).ToArray());
    }

    [Fact]
    public void FakeDashboardRuntimeAdapter_HealthIncludesInactiveCount()
    {
        var snapshot = new FakeDashboardRuntimeAdapter().GetSnapshot();

        Assert.Equal(1, snapshot.Health.InactiveCount);
        Assert.Equal(
            snapshot.Health.TotalPlcs,
            snapshot.Health.HealthyCount
            + snapshot.Health.WarningCount
            + snapshot.Health.CongestedCount
            + snapshot.Health.ErrorCount
            + snapshot.Health.InactiveCount);
    }

    [Fact]
    public void CreateDefaultPlcConfiguration_UsesNextPlcIdAndIp()
    {
        var adapter = new FakeDashboardRuntimeAdapter();

        var configuration = adapter.CreateDefaultPlcConfiguration();

        Assert.Equal("PLC-06", configuration.PlcId);
        Assert.Equal("PLC 06", configuration.PlcName);
        Assert.Equal("192.168.0.26", configuration.IpAddress);
        Assert.Equal(2004, configuration.Port);
        Assert.Equal(1000, configuration.PollingIntervalMs);
        Assert.True(configuration.IsEnabled);
    }

    [Fact]
    public void AddPlc_IncreasesConfigurationCountAndAvoidsDuplicateIds()
    {
        var adapter = new FakeDashboardRuntimeAdapter();
        var first = adapter.CreateDefaultPlcConfiguration();
        var addedFirst = adapter.AddPlc(first);
        var duplicateInput = first with { PlcName = "Duplicate requested ID" };

        var addedSecond = adapter.AddPlc(duplicateInput);

        var configurations = adapter.GetPlcConfigurations();
        Assert.Equal(7, configurations.Count);
        Assert.Equal("PLC-06", addedFirst.PlcId);
        Assert.Equal("PLC-07", addedSecond.PlcId);
        Assert.Equal(configurations.Count, configurations.Select(configuration => configuration.PlcId).Distinct().Count());
    }

    [Fact]
    public void AddPlcCommand_AddsCardSelectsNewPlcAndUpdatesSummaryAndTrend()
    {
        var adapter = new FakeDashboardRuntimeAdapter();
        using var viewModel = new DashboardViewModel(adapter, adapter);
        var configuration = adapter.CreateDefaultPlcConfiguration() with
        {
            PlcName = "New Press PLC",
            LineName = "Line-New"
        };

        viewModel.AddPlcCommand.Execute(configuration);

        Assert.Equal(6, viewModel.PlcCards.Count);
        Assert.Equal(6, viewModel.TotalCount);
        Assert.Equal(1, viewModel.InactiveCount);
        Assert.Equal(
            viewModel.TotalCount,
            viewModel.HealthyCount + viewModel.WarningCount + viewModel.CongestedCount + viewModel.ErrorCount + viewModel.InactiveCount);
        Assert.NotNull(viewModel.SelectedPlc);
        Assert.Equal("PLC-06", viewModel.SelectedPlc.PlcId);
        Assert.Equal("New Press PLC", viewModel.SelectedPlc.PlcName);
        Assert.Equal("Line-New", viewModel.SelectedPlc.LineName);
        Assert.True(viewModel.SelectedPlc.IsSelected);
        Assert.True(viewModel.IsDetailPaneOpen);
        Assert.Equal("PLC-06", viewModel.CurrentCommunicationTrend.TargetId);
        Assert.Equal("New Press PLC", viewModel.CurrentCommunicationTrend.TargetName);
        Assert.Contains(adapter.GetSnapshot().CommunicationTrend.Overview.Series, series => series.TargetId == "PLC-06");
    }

    [Fact]
    public void RefreshCommand_PreservesAddedPlcDuringFakeLiveUpdate()
    {
        var adapter = new FakeDashboardRuntimeAdapter();
        using var viewModel = new DashboardViewModel(adapter, adapter);
        var configuration = adapter.CreateDefaultPlcConfiguration();

        viewModel.AddPlcCommand.Execute(configuration);
        viewModel.RefreshCommand.Execute(null);

        Assert.Contains(viewModel.PlcCards, card => card.PlcId == "PLC-06");
        Assert.NotNull(viewModel.SelectedPlc);
        Assert.Equal("PLC-06", viewModel.SelectedPlc.PlcId);
        Assert.Equal("PLC-06", viewModel.CurrentCommunicationTrend.TargetId);
    }

    [Fact]
    public void EditSelectedPlcCommand_UpdatesSelectedCardDetailAndTrendByPlcId()
    {
        var adapter = new FakeDashboardRuntimeAdapter();
        using var viewModel = new DashboardViewModel(adapter, adapter);

        viewModel.SelectPlcCommand.Execute(viewModel.PlcCards[1]);
        var selected = viewModel.SelectedPlc;
        Assert.NotNull(selected);
        var selectedId = selected!.PlcId;
        var original = adapter.GetPlcConfiguration(selectedId);
        Assert.NotNull(original);
        var edited = original! with
        {
            PlcName = "Edited Press PLC",
            LineName = "Edited-Line",
            IpAddress = "10.10.0.42",
            Port = 2404,
            PollingIntervalMs = 750
        };

        viewModel.EditSelectedPlcCommand.Execute(edited);

        Assert.NotNull(viewModel.SelectedPlc);
        Assert.Equal(selectedId, viewModel.SelectedPlc.PlcId);
        Assert.Equal("Edited Press PLC", viewModel.SelectedPlc.PlcName);
        Assert.Equal("Edited-Line", viewModel.SelectedPlc.LineName);
        Assert.Equal("10.10.0.42", viewModel.SelectedPlc.IpAddress);
        Assert.Equal(2404, viewModel.SelectedPlc.Port);
        Assert.Equal(750, viewModel.SelectedPlc.PollingIntervalMs);
        Assert.Equal("Edited Press PLC", viewModel.CurrentCommunicationTrend.TargetName);
    }

    [Fact]
    public void RefreshCommand_PreservesEditedConfigurationDuringFakeLiveUpdate()
    {
        var adapter = new FakeDashboardRuntimeAdapter();
        using var viewModel = new DashboardViewModel(adapter, adapter);
        var plcId = viewModel.PlcCards[0].PlcId;
        var original = adapter.GetPlcConfiguration(plcId);
        Assert.NotNull(original);
        var edited = original! with
        {
            PlcName = "Persistent PLC",
            IpAddress = "10.20.30.40",
            Port = 3304,
            PollingIntervalMs = 900
        };

        viewModel.EditSelectedPlcCommand.Execute(edited);
        viewModel.RefreshCommand.Execute(null);

        var card = Assert.Single(viewModel.PlcCards, plc => plc.PlcId == plcId);
        Assert.Equal("Persistent PLC", card.PlcName);
        Assert.Equal("10.20.30.40:3304", card.EndpointText);
        Assert.Equal("900ms", card.PollingIntervalText);
    }

    [Fact]
    public void DeleteSelectedPlcCommand_RemovesCardClearsSelectionAndReturnsToOverview()
    {
        var adapter = new FakeDashboardRuntimeAdapter();
        using var viewModel = new DashboardViewModel(adapter, adapter);

        viewModel.SelectPlcCommand.Execute(viewModel.PlcCards[1]);
        var selected = viewModel.SelectedPlc;
        Assert.NotNull(selected);
        var deletedId = selected!.PlcId;
        var expectedCount = viewModel.PlcCards.Count - 1;

        viewModel.DeleteSelectedPlcCommand.Execute(deletedId);

        Assert.DoesNotContain(viewModel.PlcCards, plc => plc.PlcId == deletedId);
        Assert.Null(viewModel.SelectedPlc);
        Assert.False(viewModel.IsDetailPaneOpen);
        Assert.Equal("overview", viewModel.CurrentCommunicationTrend.TargetId);
        Assert.Equal(expectedCount, viewModel.TotalCount);
        Assert.DoesNotContain(viewModel.CurrentCommunicationTrend.Series, series => series.TargetId == deletedId);
        Assert.All(viewModel.PlcCards, card => Assert.False(card.IsSelected));
    }

    [Fact]
    public void DeleteUnselectedPlcCommand_RemovesCardAndPreservesCurrentSelection()
    {
        var adapter = new FakeDashboardRuntimeAdapter();
        using var viewModel = new DashboardViewModel(adapter, adapter);
        var selectedId = viewModel.PlcCards[0].PlcId;
        var deletedId = viewModel.PlcCards[2].PlcId;

        viewModel.SelectPlcCommand.Execute(viewModel.PlcCards[0]);
        viewModel.DeleteSelectedPlcCommand.Execute(deletedId);

        Assert.DoesNotContain(viewModel.PlcCards, plc => plc.PlcId == deletedId);
        Assert.NotNull(viewModel.SelectedPlc);
        Assert.Equal(selectedId, viewModel.SelectedPlc.PlcId);
        Assert.True(viewModel.IsDetailPaneOpen);
        Assert.Equal(selectedId, viewModel.CurrentCommunicationTrend.TargetId);
    }

    [Fact]
    public void RefreshCommand_RemovesCardsMissingFromNextSnapshot()
    {
        var first = CreateSnapshot(["PLC-01", "PLC-02"]);
        var second = CreateSnapshot(["PLC-01"]);
        using var viewModel = new DashboardViewModel(new SequencedDashboardAdapter(first, second));

        viewModel.SelectPlcCommand.Execute(viewModel.PlcCards[1]);
        viewModel.RefreshCommand.Execute(null);

        Assert.Single(viewModel.PlcCards);
        Assert.Equal("PLC-01", viewModel.PlcCards[0].PlcId);
        Assert.Null(viewModel.SelectedPlc);
        Assert.False(viewModel.IsDetailPaneOpen);
        Assert.Equal("overview", viewModel.CurrentCommunicationTrend.TargetId);
    }

    [Fact]
    public void RefreshCommand_PreservesDeletedConfigurationDuringFakeLiveUpdate()
    {
        var adapter = new FakeDashboardRuntimeAdapter();
        using var viewModel = new DashboardViewModel(adapter, adapter);
        var deletedId = viewModel.PlcCards[3].PlcId;

        viewModel.DeleteSelectedPlcCommand.Execute(deletedId);
        viewModel.RefreshCommand.Execute(null);

        Assert.DoesNotContain(viewModel.PlcCards, plc => plc.PlcId == deletedId);
        Assert.DoesNotContain(viewModel.CurrentCommunicationTrend.Series, series => series.TargetId == deletedId);
        Assert.Null(adapter.GetPlcConfiguration(deletedId));
    }

    private static DashboardSnapshot CreateSnapshot(IReadOnlyList<string> plcIds)
    {
        var cards = plcIds.Select(CreateCard).ToArray();
        var health = new RuntimeHealthSnapshot(
            cards.Length,
            cards.Length,
            WarningCount: 0,
            CongestedCount: 0,
            ErrorCount: 0,
            DateTimeOffset.UtcNow);
        var overview = new CommunicationTrendSnapshot(
            "overview",
            "Overview",
            isOverview: true,
            WarningThresholdMs: 100,
            ErrorThresholdMs: 500,
            [],
            series: cards.Select(card => new CommunicationTrendSeries(
                card.PlcId,
                card.PlcName,
                card.ConnectionState,
                IsWorst: false,
                [])).ToArray());
        var trends = cards
            .Select(card => new CommunicationTrendSnapshot(
                card.PlcId,
                card.PlcName,
                isOverview: false,
                WarningThresholdMs: 100,
                ErrorThresholdMs: 500,
                []))
            .ToArray();

        return new DashboardSnapshot(health, cards, new CommunicationTrendSetSnapshot(overview, trends));
    }

    private static PlcCardSnapshot CreateCard(string plcId)
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
            0);

    private sealed class SequencedDashboardAdapter : IRuntimeDashboardAdapter
    {
        private readonly Queue<DashboardSnapshot> _snapshots;

        public SequencedDashboardAdapter(params DashboardSnapshot[] snapshots)
            => _snapshots = new Queue<DashboardSnapshot>(snapshots);

        public DashboardSnapshot GetSnapshot()
            => _snapshots.Count > 1 ? _snapshots.Dequeue() : _snapshots.Peek();
    }
}
