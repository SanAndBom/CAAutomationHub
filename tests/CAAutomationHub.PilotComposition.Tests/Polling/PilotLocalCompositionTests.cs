using CAAutomationHub.PilotApp.Polling;
using CAAutomationHub.PilotComposition.Configuration;
using CAAutomationHub.PilotComposition.Polling;

namespace CAAutomationHub.PilotComposition.Tests.Polling;

public sealed class PilotLocalCompositionTests
{
    [Fact]
    public async Task CreatePollingService_WithFakeProfile_ReturnsObservablePollingService()
    {
        var configuration = CreateConfiguration(PilotProfileKind.Fake);

        var composition = PilotLocalComposition.Create(configuration);

        Assert.Contains("Fake", composition.StatusMessage, StringComparison.OrdinalIgnoreCase);
        await composition.PollingService.StartAsync();
        var snapshot = await composition.PollingService.PollOnceAsync();

        Assert.True(snapshot.IsRunning);
        Assert.Equal(PilotPollingStatus.WorkStartProcessed, snapshot.Status);
        Assert.Equal(WorkRequestKind.WorkStart, snapshot.LastRequestKind);
        Assert.Equal("PILOT-FAKE-LOT", snapshot.LastSelectedLotId);
        Assert.True(snapshot.LastStartRequestActive);
        Assert.True(snapshot.LastStartAckState);
    }

    [Fact]
    public void CreatePollingService_WithFakePlcLocalProfile_AllowsLoopbackTarget()
    {
        var configuration = CreateConfiguration(PilotProfileKind.FakePlcLocal);

        var composition = PilotLocalComposition.Create(configuration);

        Assert.NotNull(composition.PollingService);
        Assert.Contains("localhost:2004", composition.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreatePollingService_MapsConfigDisplayFieldsToInitialPilotCard()
    {
        var configuration = CreateConfiguration(PilotProfileKind.FakePlcLocal) with
        {
            Plc = CreatePlcConfiguration() with
            {
                DisplayName = "Fake PLC Local",
                LineName = "Local Test"
            }
        };

        var composition = PilotLocalComposition.Create(configuration);
        var card = composition.PollingService.CurrentSnapshot.PlcCardStatus;

        Assert.Equal("fakeplc-local", card.TargetId);
        Assert.Equal("Fake PLC Local", card.DisplayName);
        Assert.Equal("Local Test", card.LineName);
        Assert.Equal("localhost:2004", card.HostPort);
    }

    [Fact]
    public void CreatePollingService_WithFakePlcLocalAndSqlServerMode_RequiresConnectionEnvironmentVariable()
    {
        var configuration = CreateConfiguration(PilotProfileKind.FakePlcLocal) with
        {
            Db = new PilotDatabaseConfiguration
            {
                Mode = PilotDatabaseMode.SqlServer,
                ConnectionStringEnvironmentVariable = "CAAH_WORKSTART_DB_CONNECTION_STRING"
            }
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            PilotLocalComposition.Create(configuration, static _ => null));

        Assert.Contains("CAAH_WORKSTART_DB_CONNECTION_STRING", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("placeholder-connection-token", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreatePollingService_WithFakePlcLocalAndSqlServerMode_ComposesWithoutOpeningDatabase()
    {
        var configuration = CreateConfiguration(PilotProfileKind.FakePlcLocal) with
        {
            Db = new PilotDatabaseConfiguration
            {
                Mode = PilotDatabaseMode.SqlServer,
                ConnectionStringEnvironmentVariable = "CAAH_WORKSTART_DB_CONNECTION_STRING"
            }
        };

        var composition = PilotLocalComposition.Create(
            configuration,
            static name => name == "CAAH_WORKSTART_DB_CONNECTION_STRING"
                ? "placeholder-connection-token"
                : null);

        Assert.NotNull(composition.PollingService);
        Assert.Contains("SqlServer", composition.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("localhost:2004", composition.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PollOnce_WithFakePlcLocalProfileAndNoListener_ReturnsFailedSnapshot()
    {
        var configuration = CreateConfiguration(PilotProfileKind.FakePlcLocal) with
        {
            Plc = CreatePlcConfiguration() with { Host = "127.0.0.1", Port = 1 }
        };
        var composition = PilotLocalComposition.Create(configuration);

        var snapshot = await composition.PollingService.PollOnceAsync();

        Assert.Equal(PilotPollingStatus.Failed, snapshot.Status);
        Assert.Equal("ReadFailed", snapshot.LastResultStatus);
        Assert.Equal("ReadFailed", snapshot.LastErrorCode);
        Assert.Contains("Polling request read failed", snapshot.LastMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreatePollingService_WithFakePlcLocalProfile_RejectsNonLoopbackTarget()
    {
        var configuration = CreateConfiguration(PilotProfileKind.FakePlcLocal) with
        {
            Plc = CreatePlcConfiguration() with { Host = "192.0.2.10" }
        };

        var error = Assert.Throws<InvalidOperationException>(() => PilotLocalComposition.Create(configuration));

        Assert.Contains("loopback", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(PilotProfileKind.RealReadOnly)]
    [InlineData(PilotProfileKind.RealPilot)]
    public void CreatePollingService_WithRealProfiles_IsBlockedForPilotLiveFastTrack(PilotProfileKind profile)
    {
        var configuration = CreateConfiguration(profile);

        var error = Assert.Throws<NotSupportedException>(() => PilotLocalComposition.Create(configuration));

        Assert.Contains("not enabled", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static PilotLocalConfiguration CreateConfiguration(PilotProfileKind profile) =>
        new()
        {
            Profile = profile,
            Plc = CreatePlcConfiguration(),
            Db = new PilotDatabaseConfiguration
            {
                Mode = PilotDatabaseMode.Fake,
                ConnectionStringEnvironmentVariable = "CAAH_WORKSTART_DB_CONNECTION_STRING"
            }
        };

    private static PilotPlcTargetConfiguration CreatePlcConfiguration() =>
        new()
        {
            TargetId = "fakeplc-local",
            DisplayName = "Fake PLC Local",
            LineName = "Local Test",
            Host = "localhost",
            Port = 2004,
            ReadStartVariable = "%DB10000",
            ReadWordCount = 90,
            StartSignalWordIndex = 83,
            CompleteSignalWordIndex = 84,
            LotId1WordOffset = 0,
            LotId2WordOffset = 10,
            LotIdWordLength = 6
        };
}
