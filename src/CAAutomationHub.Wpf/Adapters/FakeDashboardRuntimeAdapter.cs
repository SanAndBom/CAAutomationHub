using CAAutomationHub.Wpf.Models.Dashboard;
using CAAutomationHub.Wpf.Services;

namespace CAAutomationHub.Wpf.Adapters;

public sealed class FakeDashboardRuntimeAdapter : IRuntimeDashboardAdapter, IPlcDashboardConfigurationService
{
    private readonly object _syncRoot = new();
    private readonly List<PlcDashboardConfiguration> _configurations;
    private readonly IPlcDashboardConfigurationStore? _configurationStore;
    private int _snapshotIndex;

    public FakeDashboardRuntimeAdapter()
        => _configurations = DefaultPlcDashboardConfigurations.Create();

    public FakeDashboardRuntimeAdapter(IPlcDashboardConfigurationStore configurationStore)
    {
        _configurationStore = configurationStore ?? throw new ArgumentNullException(nameof(configurationStore));
        _configurations = _configurationStore.Load().ToList();
    }

    public DashboardSnapshot GetSnapshot()
    {
        var tick = _snapshotIndex++;
        var jitter = (tick % 7) - 3;
        var congestionPhase = tick % 12;
        List<PlcDashboardConfiguration> configurations;

        lock (_syncRoot)
        {
            configurations = _configurations.ToList();
        }

        var cards = configurations
            .Select((configuration, index) => CreateCard(configuration, index + 1, congestionPhase, jitter, tick))
            .ToArray();

        var health = new RuntimeHealthSnapshot(
            cards.Length,
            cards.Count(c => c.ConnectionState == PlcConnectionState.Healthy),
            cards.Count(c => c.ConnectionState == PlcConnectionState.Warning),
            cards.Count(c => c.ConnectionState == PlcConnectionState.Congested),
            cards.Count(c => c.ConnectionState == PlcConnectionState.Error),
            DateTimeOffset.UtcNow,
            cards.Count(c => c.ConnectionState == PlcConnectionState.Inactive));

        var trend = FakeCommunicationTrendFactory.Create(cards, tick);

        return new DashboardSnapshot(health, cards, trend);
    }

    public IReadOnlyList<PlcDashboardConfiguration> GetPlcConfigurations()
    {
        lock (_syncRoot)
        {
            return _configurations.ToArray();
        }
    }

    public PlcDashboardConfiguration? GetPlcConfiguration(string plcId)
    {
        lock (_syncRoot)
        {
            return _configurations.FirstOrDefault(configuration => configuration.PlcId == plcId);
        }
    }

    public PlcDashboardConfiguration CreateDefaultPlcConfiguration()
    {
        lock (_syncRoot)
        {
            var nextIndex = DefaultPlcDashboardConfigurations.GetNextConfigurationIndex(_configurations);
            return DefaultPlcDashboardConfigurations.CreateDefaultConfiguration(nextIndex);
        }
    }

    public PlcDashboardConfiguration AddPlc(PlcDashboardConfiguration configuration)
    {
        lock (_syncRoot)
        {
            var nextIndex = DefaultPlcDashboardConfigurations.GetNextConfigurationIndex(_configurations);
            var normalized = configuration with
            {
                PlcId = $"PLC-{nextIndex:00}",
                IpAddress = string.IsNullOrWhiteSpace(configuration.IpAddress)
                    ? $"192.168.0.{20 + nextIndex}"
                    : configuration.IpAddress
            };

            _configurations.Add(normalized);
            SaveConfigurations();
            return normalized;
        }
    }

    public void UpdatePlc(PlcDashboardConfiguration configuration)
    {
        lock (_syncRoot)
        {
            var index = _configurations.FindIndex(item => item.PlcId == configuration.PlcId);
            if (index < 0) return;

            _configurations[index] = configuration;
            SaveConfigurations();
        }
    }

    public void DeletePlc(string plcId)
    {
        lock (_syncRoot)
        {
            _configurations.RemoveAll(configuration => configuration.PlcId == plcId);
            SaveConfigurations();
        }
    }

    private static PlcConnectionState GetCyclingState(int phase)
    {
        if (phase < 4) return PlcConnectionState.Healthy;
        if (phase < 8) return PlcConnectionState.Warning;
        return PlcConnectionState.Congested;
    }

    private static PlcCardSnapshot CreateCard(
        PlcDashboardConfiguration configuration,
        int fallbackIndex,
        int congestionPhase,
        int jitter,
        int tick)
    {
        var index = DefaultPlcDashboardConfigurations.GetStableIndex(configuration.PlcId, fallbackIndex);
        var state = configuration.IsEnabled ? GetState(index, congestionPhase) : PlcConnectionState.Inactive;
        var lastResponseMs = state switch
        {
            PlcConnectionState.Healthy => index switch
            {
                1 => 38 + jitter,
                6 => 62 + (jitter * 4),
                7 => 71 + (jitter * 5),
                10 => 55 + (jitter * 4),
                _ => 48 + (index * 4) + jitter
            },
            PlcConnectionState.Warning => index == 2 ? 512 + (jitter * 9) : 248 + (jitter * 8),
            PlcConnectionState.Congested => index == 9 ? 430 + (jitter * 10) : 94 + (jitter * 6),
            PlcConnectionState.Error => 880 + (jitter * 12),
            _ => 0
        };
        var txPerMinute = state switch
        {
            PlcConnectionState.Inactive => 0,
            PlcConnectionState.Error => 46 + tick,
            PlcConnectionState.Congested => index == 9 ? 88 + tick : 148 + tick,
            PlcConnectionState.Warning => index == 2 ? 182 + tick : 116 + tick,
            _ => 120 + (index * 6) + tick
        };
        var rxPerMinute = state == PlcConnectionState.Inactive ? 0 : Math.Max(0, txPerMinute - (index % 6));
        var errorCount = state switch
        {
            PlcConnectionState.Error => 5 + (tick / 9),
            PlcConnectionState.Warning => index == 2 && tick % 10 == 0 ? 2 : index == 2 ? 1 : 2,
            PlcConnectionState.Congested => index == 3 && congestionPhase >= 8 ? 1 : index == 9 ? 3 : 0,
            _ => 0
        };
        var runtimeSignal = FakeRuntimeSignalFactory.Create(state, index, tick);

        return new PlcCardSnapshot(
            configuration.PlcId,
            configuration.PlcName,
            configuration.LineName,
            state,
            configuration.IpAddress,
            configuration.Port,
            configuration.PollingIntervalMs,
            Math.Max(0, lastResponseMs),
            txPerMinute,
            rxPerMinute,
            errorCount,
            runtimeSignal);
    }

    private static PlcConnectionState GetState(int index, int congestionPhase)
        => index switch
        {
            2 or 8 => PlcConnectionState.Warning,
            3 => GetCyclingState(congestionPhase),
            4 => PlcConnectionState.Error,
            5 => PlcConnectionState.Inactive,
            9 => PlcConnectionState.Congested,
            _ => PlcConnectionState.Healthy
        };

    private void SaveConfigurations()
        => _configurationStore?.Save(_configurations.ToArray());
}
