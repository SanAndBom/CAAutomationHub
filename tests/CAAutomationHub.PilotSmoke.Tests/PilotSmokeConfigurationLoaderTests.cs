using CAAutomationHub.PilotSmoke;

namespace CAAutomationHub.PilotSmoke.Tests;

public sealed class PilotSmokeConfigurationLoaderTests
{
    [Fact]
    public void Load_WithoutExecuteFlag_SkipsActualRead()
    {
        var configuration = PilotSmokeConfigurationLoader.Load(
            Array.Empty<string>(),
            _ => null);

        Assert.False(configuration.ShouldExecuteRead);
        Assert.Contains("--execute-read-only", configuration.SkipReason);
    }

    [Fact]
    public void Load_WithCommandLineValues_UsesExplicitReadOnlyConfiguration()
    {
        var configuration = PilotSmokeConfigurationLoader.Load(
            new[]
            {
                "--execute-read-only",
                "--host", "192.0.2.10",
                "--port", "2004",
                "--read-start", "%DB10000",
                "--read-word-count", "90",
                "--start-signal-word-index", "80",
                "--lot-id1-word-offset", "0",
                "--lot-id2-word-offset", "10",
                "--lot-id-word-length", "6"
            },
            _ => null);

        Assert.True(configuration.ShouldExecuteRead);
        Assert.Null(configuration.SkipReason);
        Assert.Equal("192.0.2.10", configuration.Connection.Host);
        Assert.Equal(2004, configuration.Connection.Port);
        Assert.Equal("%DB10000", configuration.Read.ReadStartVariable);
        Assert.Equal(90, configuration.Read.ReadWordCount);
        Assert.Equal(80, configuration.Layout.StartSignalWordIndex);
        Assert.Equal(0, configuration.Layout.LotId1WordOffset);
        Assert.Equal(10, configuration.Layout.LotId2WordOffset);
        Assert.Equal(6, configuration.Layout.LotIdWordLength);
    }

    [Fact]
    public void Load_MasksHostForReporting()
    {
        var configuration = PilotSmokeConfigurationLoader.Load(
            new[]
            {
                "--execute-read-only",
                "--host", "192.0.2.10",
                "--port", "2004",
                "--read-start", "%DB10000",
                "--read-word-count", "90",
                "--start-signal-word-index", "80",
                "--lot-id1-word-offset", "0",
                "--lot-id2-word-offset", "10",
                "--lot-id-word-length", "6"
            },
            _ => null);

        Assert.Equal("192.0.2.x", configuration.MaskedHost);
    }
}
