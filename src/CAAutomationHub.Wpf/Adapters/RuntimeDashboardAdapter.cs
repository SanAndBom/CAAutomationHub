using CAAutomationHub.Wpf.Mappers;
using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Adapters;

public sealed class RuntimeDashboardAdapter : IRuntimeDashboardAdapter, IAsyncRuntimeDashboardAdapter
{
    private readonly IRuntimeSnapshotProvider _provider;

    public RuntimeDashboardAdapter() : this(new EmptyRuntimeSnapshotProvider())
    {
    }

    public RuntimeDashboardAdapter(IRuntimeSnapshotProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public DashboardSnapshot GetSnapshot()
    {
        var runtimeSnapshot = _provider.GetSnapshot();
        return RuntimeDashboardSnapshotMapper.Map(runtimeSnapshot);
    }

    public Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetSnapshot());
    }
}
