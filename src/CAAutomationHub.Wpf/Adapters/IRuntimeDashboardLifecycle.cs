namespace CAAutomationHub.Wpf.Adapters;

public interface IRuntimeDashboardLifecycle
{
    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}
