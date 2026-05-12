using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Services;

public interface IEventStreamService : IDisposable
{
    event EventHandler<RuntimeEventLogItem>? EventReceived;

    void Start();
    void Stop();
}
