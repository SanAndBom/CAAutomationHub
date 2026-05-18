using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Services;

public sealed class JsonPlcDashboardConfigurationStore : IPlcDashboardConfigurationStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _configurationPath;

    public JsonPlcDashboardConfigurationStore(string configurationPath)
    {
        if (string.IsNullOrWhiteSpace(configurationPath))
        {
            throw new ArgumentException("Configuration path is required.", nameof(configurationPath));
        }

        _configurationPath = configurationPath;
    }

    public static JsonPlcDashboardConfigurationStore CreateDefault()
        => new(Path.Combine(FindRepositoryRoot()?.FullName ?? AppContext.BaseDirectory, "config", "plc-cards.local.json"));

    public IReadOnlyList<PlcDashboardConfiguration> Load()
    {
        if (!File.Exists(_configurationPath))
        {
            return DefaultPlcDashboardConfigurations.Create();
        }

        using var stream = File.OpenRead(_configurationPath);
        var document = JsonSerializer.Deserialize<PlcCardConfigurationDocument>(stream, SerializerOptions);
        return MapDocument(document);
    }

    public async Task<IReadOnlyList<PlcDashboardConfiguration>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_configurationPath))
        {
            return DefaultPlcDashboardConfigurations.Create();
        }

        await using var stream = File.OpenRead(_configurationPath);
        var document = await JsonSerializer.DeserializeAsync<PlcCardConfigurationDocument>(stream, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);
        return MapDocument(document);
    }

    public void Save(IReadOnlyList<PlcDashboardConfiguration> configurations)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_configurationPath) ?? ".");
        using var stream = File.Create(_configurationPath);
        JsonSerializer.Serialize(stream, MapConfigurations(configurations), SerializerOptions);
    }

    public async Task SaveAsync(IReadOnlyList<PlcDashboardConfiguration> configurations, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_configurationPath) ?? ".");
        await using var stream = File.Create(_configurationPath);
        await JsonSerializer.SerializeAsync(stream, MapConfigurations(configurations), SerializerOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    private static IReadOnlyList<PlcDashboardConfiguration> MapDocument(PlcCardConfigurationDocument? document)
    {
        if (document?.Plcs is null || document.Plcs.Count == 0)
        {
            return DefaultPlcDashboardConfigurations.Create();
        }

        return document.Plcs
            .Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .Select(MapItem)
            .ToArray();
    }

    private static PlcDashboardConfiguration MapItem(PlcCardConfigurationItem item)
        => new(
            item.Id.Trim(),
            string.IsNullOrWhiteSpace(item.DisplayName) ? item.Id.Trim() : item.DisplayName.Trim(),
            string.IsNullOrWhiteSpace(item.LineName) ? "Line-1" : item.LineName.Trim(),
            item.Description?.Trim() ?? string.Empty,
            string.IsNullOrWhiteSpace(item.Host) ? "localhost" : item.Host.Trim(),
            item.Port <= 0 ? 2004 : item.Port,
            item.PollingIntervalMs <= 0 ? 1000 : item.PollingIntervalMs,
            item.TimeoutMs <= 0 ? 800 : item.TimeoutMs,
            item.ReconnectIntervalSeconds <= 0 ? 5 : item.ReconnectIntervalSeconds,
            item.MaxRetryCount <= 0 ? 5 : item.MaxRetryCount,
            item.AutoReconnect ?? true,
            item.ConnectOnStartup ?? true,
            item.IsEnabled ?? true);

    private static PlcCardConfigurationDocument MapConfigurations(IReadOnlyList<PlcDashboardConfiguration> configurations)
        => new(configurations.Select(configuration => new PlcCardConfigurationItem
        {
            Id = configuration.PlcId,
            DisplayName = configuration.PlcName,
            LineName = configuration.LineName,
            Description = configuration.Description,
            Host = configuration.IpAddress,
            Port = configuration.Port,
            Status = "Unknown",
            PollingIntervalMs = configuration.PollingIntervalMs,
            TimeoutMs = configuration.TimeoutMs,
            ReconnectIntervalSeconds = configuration.ReconnectIntervalSeconds,
            MaxRetryCount = configuration.MaxRetryCount,
            AutoReconnect = configuration.AutoReconnect,
            ConnectOnStartup = configuration.ConnectOnStartup,
            IsEnabled = configuration.IsEnabled
        }).ToArray());

    private static DirectoryInfo? FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CAAutomationHub.sln")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private sealed record PlcCardConfigurationDocument(
        [property: JsonPropertyName("plcs")] IReadOnlyList<PlcCardConfigurationItem> Plcs);

    private sealed class PlcCardConfigurationItem
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; init; } = string.Empty;

        [JsonPropertyName("lineName")]
        public string LineName { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("host")]
        public string Host { get; init; } = string.Empty;

        [JsonPropertyName("port")]
        public int Port { get; init; }

        [JsonPropertyName("status")]
        public string Status { get; init; } = "Unknown";

        [JsonPropertyName("pollingIntervalMs")]
        public int PollingIntervalMs { get; init; }

        [JsonPropertyName("timeoutMs")]
        public int TimeoutMs { get; init; }

        [JsonPropertyName("reconnectIntervalSeconds")]
        public int ReconnectIntervalSeconds { get; init; }

        [JsonPropertyName("maxRetryCount")]
        public int MaxRetryCount { get; init; }

        [JsonPropertyName("autoReconnect")]
        public bool? AutoReconnect { get; init; }

        [JsonPropertyName("connectOnStartup")]
        public bool? ConnectOnStartup { get; init; }

        [JsonPropertyName("isEnabled")]
        public bool? IsEnabled { get; init; }
    }
}
