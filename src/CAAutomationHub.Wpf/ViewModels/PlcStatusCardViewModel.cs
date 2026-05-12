using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.ViewModels;

public sealed class PlcStatusCardViewModel
{
    public PlcStatusCardViewModel(PlcCardSnapshot snapshot) => Snapshot = snapshot;
    public PlcCardSnapshot Snapshot { get; }
    public string PlcName => Snapshot.PlcName;
    public string LineName => Snapshot.LineName;
    public string ConnectionText => Snapshot.ConnectionState.ToString();
}
