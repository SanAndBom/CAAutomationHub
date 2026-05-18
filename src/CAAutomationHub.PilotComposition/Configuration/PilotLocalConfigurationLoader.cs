using System.Text.Json;
using System.Text.Json.Serialization;

namespace CAAutomationHub.PilotComposition.Configuration;

public static class PilotLocalConfigurationLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static PilotLocalConfiguration Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Pilot configuration path is required.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Pilot local configuration file was not found.", path);
        }

        var json = File.ReadAllText(path);
        var configuration = JsonSerializer.Deserialize<PilotLocalConfiguration>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Pilot local configuration could not be deserialized.");

        Validate(configuration);
        return configuration;
    }

    public static void Validate(PilotLocalConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configuration.Plc);
        ArgumentNullException.ThrowIfNull(configuration.Db);

        if (string.IsNullOrWhiteSpace(configuration.Plc.TargetId))
        {
            throw new InvalidOperationException("Pilot PLC target id is required.");
        }

        if (string.IsNullOrWhiteSpace(configuration.Plc.Host))
        {
            throw new InvalidOperationException("Pilot PLC host is required.");
        }

        if (configuration.Plc.Port <= 0)
        {
            throw new InvalidOperationException("Pilot PLC port must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(configuration.Plc.ReadStartVariable))
        {
            throw new InvalidOperationException("Pilot PLC read start variable is required.");
        }

        if (configuration.Plc.ReadWordCount <= 0)
        {
            throw new InvalidOperationException("Pilot PLC read word count must be greater than zero.");
        }

        if (configuration.Plc.LotIdWordLength <= 0)
        {
            throw new InvalidOperationException("Pilot LOT ID word length must be greater than zero.");
        }
    }
}
