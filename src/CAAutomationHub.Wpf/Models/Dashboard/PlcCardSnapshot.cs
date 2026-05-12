namespace CAAutomationHub.Wpf.Models.Dashboard;

public sealed record PlcCardSnapshot(
    string PlcId,
    string PlcName,
    string LineName,
    PlcConnectionState ConnectionState,
    string IpAddress,
    int Port,
    int PollingIntervalMs,
    int LastResponseMs,
    int TxPerMinute,
    int RxPerMinute,
    int ErrorCount);
