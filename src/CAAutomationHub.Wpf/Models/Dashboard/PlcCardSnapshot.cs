namespace CAAutomationHub.Wpf.Models.Dashboard;

public sealed record PlcCardSnapshot
{
    public PlcCardSnapshot(
        string plcId,
        string plcName,
        string lineName,
        PlcConnectionState connectionState,
        string ipAddress,
        int port,
        int pollingIntervalMs,
        int lastResponseMs,
        int txPerMinute,
        int rxPerMinute,
        int errorCount,
        PlcRuntimeSignalSnapshot? runtimeSignal = null)
    {
        PlcId = plcId;
        PlcName = plcName;
        LineName = lineName;
        ConnectionState = connectionState;
        IpAddress = ipAddress;
        Port = port;
        PollingIntervalMs = pollingIntervalMs;
        LastResponseMs = lastResponseMs;
        TxPerMinute = txPerMinute;
        RxPerMinute = rxPerMinute;
        ErrorCount = errorCount;
        RuntimeSignal = runtimeSignal ?? PlcRuntimeSignalSnapshot.Empty;
    }

    public string PlcId { get; init; }
    public string PlcName { get; init; }
    public string LineName { get; init; }
    public PlcConnectionState ConnectionState { get; init; }
    public string IpAddress { get; init; }
    public int Port { get; init; }
    public int PollingIntervalMs { get; init; }
    public int LastResponseMs { get; init; }
    public int TxPerMinute { get; init; }
    public int RxPerMinute { get; init; }
    public int ErrorCount { get; init; }
    public PlcRuntimeSignalSnapshot RuntimeSignal { get; init; }
}
