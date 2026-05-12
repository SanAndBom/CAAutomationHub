using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.ViewModels;

public sealed class PlcStatusCardViewModel : ViewModelBase
{
    public PlcStatusCardViewModel(PlcCardSnapshot snapshot) => Snapshot = snapshot;

    public PlcCardSnapshot Snapshot { get; private set; }
    public string PlcId => Snapshot.PlcId;
    public string PlcName => Snapshot.PlcName;
    public string LineName => Snapshot.LineName;
    public string IpAddress => Snapshot.IpAddress;
    public int Port => Snapshot.Port;
    public string EndpointText => $"{Snapshot.IpAddress}:{Snapshot.Port}";
    public int PollingIntervalMs => Snapshot.PollingIntervalMs;
    public string PollingIntervalText => $"{Snapshot.PollingIntervalMs}ms";
    public int LastResponseMs => Snapshot.LastResponseMs;
    public string LastResponseText => Snapshot.LastResponseMs > 0 ? $"{Snapshot.LastResponseMs}ms" : "-";
    public int TxPerMinute => Snapshot.TxPerMinute;
    public int RxPerMinute => Snapshot.RxPerMinute;
    public string TxRxText => $"{Snapshot.TxPerMinute} / {Snapshot.RxPerMinute}";
    public int ErrorCount => Snapshot.ErrorCount;
    public PlcConnectionState ConnectionState => Snapshot.ConnectionState;
    public string ConnectionText => Snapshot.ConnectionState.ToString();
    public string StatusText => Snapshot.ConnectionState switch
    {
        PlcConnectionState.Healthy => "정상",
        PlcConnectionState.Warning => "주의",
        PlcConnectionState.Congested => "정체",
        PlcConnectionState.Error => "오류",
        PlcConnectionState.Inactive => "비활성",
        _ => Snapshot.ConnectionState.ToString()
    };

    public void UpdateSnapshot(PlcCardSnapshot snapshot)
    {
        if (Snapshot == snapshot) return;

        Snapshot = snapshot;
        RaiseSnapshotPropertiesChanged();
    }

    private void RaiseSnapshotPropertiesChanged()
    {
        RaisePropertyChanged(nameof(Snapshot));
        RaisePropertyChanged(nameof(PlcId));
        RaisePropertyChanged(nameof(PlcName));
        RaisePropertyChanged(nameof(LineName));
        RaisePropertyChanged(nameof(IpAddress));
        RaisePropertyChanged(nameof(Port));
        RaisePropertyChanged(nameof(EndpointText));
        RaisePropertyChanged(nameof(PollingIntervalMs));
        RaisePropertyChanged(nameof(PollingIntervalText));
        RaisePropertyChanged(nameof(LastResponseMs));
        RaisePropertyChanged(nameof(LastResponseText));
        RaisePropertyChanged(nameof(TxPerMinute));
        RaisePropertyChanged(nameof(RxPerMinute));
        RaisePropertyChanged(nameof(TxRxText));
        RaisePropertyChanged(nameof(ErrorCount));
        RaisePropertyChanged(nameof(ConnectionState));
        RaisePropertyChanged(nameof(ConnectionText));
        RaisePropertyChanged(nameof(StatusText));
    }
}
