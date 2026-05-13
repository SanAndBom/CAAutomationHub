namespace CAAutomationHub.Wpf.Models.Dashboard;

public sealed record PlcDashboardConfiguration(
    string PlcId,
    string PlcName,
    string LineName,
    string Description,
    string IpAddress,
    int Port,
    int PollingIntervalMs,
    int TimeoutMs,
    int ReconnectIntervalSeconds,
    int MaxRetryCount,
    bool AutoReconnect,
    bool ConnectOnStartup,
    bool IsEnabled);
