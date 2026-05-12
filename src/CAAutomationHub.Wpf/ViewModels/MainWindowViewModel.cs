namespace CAAutomationHub.Wpf.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    public DashboardViewModel Dashboard { get; } = new();
}
