using CAAutomationHub.Runtime;
using CAAutomationHub.Wpf.Adapters;

namespace CAAutomationHub.Wpf.Composition;

public sealed class DashboardRuntimeCompositionFactory
{
    private readonly Func<IRuntimeDashboardAdapter> _createFakeAdapter;

    public DashboardRuntimeCompositionFactory(Func<IRuntimeDashboardAdapter> createFakeAdapter)
    {
        _createFakeAdapter = createFakeAdapter ?? throw new ArgumentNullException(nameof(createFakeAdapter));
    }

    public DashboardRuntimeComposition Create(DashboardRuntimeMode mode)
    {
        return mode switch
        {
            DashboardRuntimeMode.Fake => CreateFake(),
            DashboardRuntimeMode.InMemoryRuntime => CreateInMemoryRuntime(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported dashboard runtime mode.")
        };
    }

    private DashboardRuntimeComposition CreateFake()
    {
        IRuntimeDashboardAdapter adapter = _createFakeAdapter()
            ?? throw new ArgumentNullException(nameof(_createFakeAdapter), "Fake adapter factory returned null.");

        return new DashboardRuntimeComposition(
            DashboardRuntimeMode.Fake,
            adapter);
    }

    private static DashboardRuntimeComposition CreateInMemoryRuntime()
    {
        var supervisor = new InMemoryAutomationHubSupervisor();
        var provider = new SupervisorRuntimeSnapshotProvider(supervisor);
        var adapter = new RuntimeDashboardAdapter(provider);
        var lifecycle = new SupervisorRuntimeDashboardLifecycle(supervisor, provider);

        return new DashboardRuntimeComposition(
            DashboardRuntimeMode.InMemoryRuntime,
            adapter,
            lifecycle,
            provider);
    }
}
