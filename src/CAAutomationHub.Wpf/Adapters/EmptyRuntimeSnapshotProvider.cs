using CAAutomationHub.Contracts.Runtime;

namespace CAAutomationHub.Wpf.Adapters;

public sealed class EmptyRuntimeSnapshotProvider : IRuntimeSnapshotProvider
{
    public RuntimeSnapshot GetSnapshot()
    {
        // Skeleton provider only: this is not a fake dashboard simulator or a Runtime connection.
        return RuntimeSnapshot.Empty;
    }
}
