using CAAutomationHub.Contracts.Runtime;

namespace CAAutomationHub.Wpf.Adapters;

public interface IRuntimeSnapshotProvider
{
    RuntimeSnapshot GetSnapshot();
}
