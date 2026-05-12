using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.ViewModels;

public sealed class PlcStatusCardViewModel : ViewModelBase
{
    public PlcStatusCardViewModel(PlcCardSnapshot snapshot) => Snapshot = snapshot;

    public PlcCardSnapshot Snapshot { get; private set; }
    public string PlcName => Snapshot.PlcName;
    public string LineName => Snapshot.LineName;
    public string ConnectionText => Snapshot.ConnectionState.ToString();

    public void UpdateSnapshot(PlcCardSnapshot snapshot)
    {
        if (Snapshot == snapshot) return;

        Snapshot = snapshot;
        RaisePropertyChanged(nameof(Snapshot));
        RaisePropertyChanged(nameof(PlcName));
        RaisePropertyChanged(nameof(LineName));
        RaisePropertyChanged(nameof(ConnectionText));
    }
}
