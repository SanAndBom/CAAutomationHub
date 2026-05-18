using CAAutomationHub.PilotComposition.Configuration;

namespace CAAutomationHub.PilotComposition.Tests.Configuration;

public sealed class PilotLocalConfigurationLoaderTests
{
    [Fact]
    public void Load_ReadsSampleConfigurationWithoutSecretValue()
    {
        var path = FindRepositoryFile("config", "pilot.sample.json");

        var configuration = PilotLocalConfigurationLoader.Load(path);

        Assert.Equal(PilotProfileKind.FakePlcLocal, configuration.Profile);
        Assert.Equal("fakeplc-local", configuration.Plc.TargetId);
        Assert.Equal("localhost", configuration.Plc.Host);
        Assert.Equal(2004, configuration.Plc.Port);
        Assert.Equal("%DB10000", configuration.Plc.ReadStartVariable);
        Assert.Equal(90, configuration.Plc.ReadWordCount);
        Assert.Equal(83, configuration.Plc.StartSignalWordIndex);
        Assert.Equal(84, configuration.Plc.CompleteSignalWordIndex);
        Assert.Equal(0, configuration.Plc.LotId1WordOffset);
        Assert.Equal(10, configuration.Plc.LotId2WordOffset);
        Assert.Equal(6, configuration.Plc.LotIdWordLength);
        Assert.Equal(PilotDatabaseMode.Fake, configuration.Db.Mode);
        Assert.Equal("CAAH_WORKSTART_DB_CONNECTION_STRING", configuration.Db.ConnectionEnvironmentVariable);
    }

    [Fact]
    public void Load_ThrowsFileNotFoundException_WhenFileIsMissing()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "pilot.local.json");

        Assert.Throws<FileNotFoundException>(() => PilotLocalConfigurationLoader.Load(missingPath));
    }

    [Fact]
    public void Load_UsesExplicitLocalConfigurationPath()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("caah-pilot-config-");
        var localPath = Path.Combine(tempDirectory.FullName, "pilot.local.json");
        File.WriteAllText(
            localPath,
            """
            {
              "profile": "Fake",
              "plc": {
                "targetId": "fake-profile",
                "host": "localhost",
                "port": 2004,
                "readStartVariable": "%DB10000",
                "readWordCount": 90,
                "startSignalWordIndex": 83,
                "completeSignalWordIndex": 84,
                "lotId1WordOffset": 0,
                "lotId2WordOffset": 10,
                "lotIdWordLength": 6
              },
              "db": {
                "mode": "Fake",
                "connectionEnvironmentVariable": "CAAH_WORKSTART_DB_CONNECTION_STRING"
              }
            }
            """);

        var configuration = PilotLocalConfigurationLoader.Load(localPath);

        Assert.Equal(PilotProfileKind.Fake, configuration.Profile);
        Assert.Equal("fake-profile", configuration.Plc.TargetId);
    }

    [Fact]
    public void DatabaseConfiguration_DoesNotExposeSecretValueProperty()
    {
        var propertyNames = typeof(PilotDatabaseConfiguration)
            .GetProperties()
            .Select(static property => property.Name)
            .ToArray();

        Assert.DoesNotContain(propertyNames, static name => name.Equals("Value", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, static name => name.Contains("Secret", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nameof(PilotDatabaseConfiguration.ConnectionEnvironmentVariable), propertyNames);
    }

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
}
