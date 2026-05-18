using CAAutomationHub.Wpf.Models.Dashboard;
using CAAutomationHub.Wpf.Services;

namespace CAAutomationHub.Wpf.Tests.Services;

public sealed class JsonPlcDashboardConfigurationStoreTests
{
    [Fact]
    public void MissingLocalFileUsesDefaultConfigurations()
    {
        using var temp = new TemporaryDirectory();
        var store = new JsonPlcDashboardConfigurationStore(Path.Combine(temp.Path, "plc-cards.local.json"));

        var configurations = store.Load();

        Assert.Equal(5, configurations.Count);
        Assert.Contains(configurations, configuration => configuration.PlcId == "PLC-01");
        Assert.False(File.Exists(Path.Combine(temp.Path, "plc-cards.local.json")));
    }

    [Fact]
    public void LoadsCardsFromJson()
    {
        using var temp = new TemporaryDirectory();
        var path = Path.Combine(temp.Path, "plc-cards.local.json");
        File.WriteAllText(
            path,
            """
            {
              "plcs": [
                {
                  "id": "fakeplc-local",
                  "displayName": "Fake PLC Local",
                  "lineName": "Local Test",
                  "host": "localhost",
                  "port": 2004,
                  "pollingIntervalMs": 500
                }
              ]
            }
            """);
        var store = new JsonPlcDashboardConfigurationStore(path);

        var configuration = Assert.Single(store.Load());

        Assert.Equal("fakeplc-local", configuration.PlcId);
        Assert.Equal("Fake PLC Local", configuration.PlcName);
        Assert.Equal("Local Test", configuration.LineName);
        Assert.Equal("localhost", configuration.IpAddress);
        Assert.Equal(2004, configuration.Port);
        Assert.Equal(500, configuration.PollingIntervalMs);
        Assert.True(configuration.IsEnabled);
    }

    [Fact]
    public void SavesEditedCardToJson()
    {
        using var temp = new TemporaryDirectory();
        var path = Path.Combine(temp.Path, "plc-cards.local.json");
        var store = new JsonPlcDashboardConfigurationStore(path);
        var edited = CreateConfiguration() with
        {
            PlcName = "Edited Fake PLC",
            IpAddress = "localhost",
            Port = 2404,
            PollingIntervalMs = 750
        };

        store.Save([edited]);

        var reloaded = Assert.Single(new JsonPlcDashboardConfigurationStore(path).Load());
        Assert.Equal("Edited Fake PLC", reloaded.PlcName);
        Assert.Equal("localhost", reloaded.IpAddress);
        Assert.Equal(2404, reloaded.Port);
        Assert.Equal(750, reloaded.PollingIntervalMs);
    }

    [Fact]
    public void SampleDoesNotContainActualFieldEndpoint()
    {
        var samplePath = FindRepositoryFile("config", "plc-cards.sample.json");

        var sample = File.ReadAllText(samplePath);

        Assert.DoesNotContain("192.168.0.21", sample);
        Assert.DoesNotContain("Password=", sample, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Pwd=", sample, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("localhost", sample);
    }

    private static PlcDashboardConfiguration CreateConfiguration()
        => new(
            "fakeplc-local",
            "Fake PLC Local",
            "Local Test",
            "Local fake PLC card",
            "localhost",
            2004,
            500,
            800,
            5,
            5,
            AutoReconnect: true,
            ConnectOnStartup: true,
            IsEnabled: true);

    private static string FindRepositoryFile(params string[] relativePathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(relativePathParts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not find repository file.", Path.Combine(relativePathParts));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"caah-plc-cards-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
