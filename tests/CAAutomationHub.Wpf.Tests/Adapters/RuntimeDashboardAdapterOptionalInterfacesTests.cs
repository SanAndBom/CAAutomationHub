using System.Reflection;
using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Wpf.Adapters;
using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Tests.Adapters;

public sealed class RuntimeDashboardAdapterOptionalInterfacesTests
{
    private static readonly DateTimeOffset CapturedAt = new(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RuntimeDashboardAdapterContract_OnlyDeclaresGetSnapshot()
    {
        MethodInfo[] methods = typeof(IRuntimeDashboardAdapter)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        MethodInfo method = Assert.Single(methods);
        Assert.Equal(nameof(IRuntimeDashboardAdapter.GetSnapshot), method.Name);
        Assert.Equal(typeof(DashboardSnapshot), method.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void RuntimeDashboardAdapter_ImplementsAsyncOptionalInterface()
    {
        var adapter = new RuntimeDashboardAdapter();

        Assert.IsAssignableFrom<IAsyncRuntimeDashboardAdapter>(adapter);
    }

    [Fact]
    public async Task GetSnapshotAsync_ReturnsMappedDashboardSnapshot()
    {
        var provider = new CountingRuntimeSnapshotProvider(new RuntimeSnapshot(
            CapturedAt,
            new RuntimeHealthState(
                TotalPlcs: 3,
                OnlineCount: 2,
                ReconnectingCount: 1,
                HealthyCount: 1,
                WarningCount: 1,
                CongestedCount: 0,
                ErrorCount: 1,
                InactiveCount: 0,
                CapturedAt: CapturedAt),
            channels: [],
            recentEvents: []));
        var adapter = (IAsyncRuntimeDashboardAdapter)new RuntimeDashboardAdapter(provider);

        DashboardSnapshot snapshot = await adapter.GetSnapshotAsync(CancellationToken.None);

        Assert.Equal(1, provider.CallCount);
        Assert.Equal(3, snapshot.Health.TotalPlcs);
        Assert.Equal(1, snapshot.Health.HealthyCount);
        Assert.Equal(1, snapshot.Health.WarningCount);
        Assert.Equal(1, snapshot.Health.ErrorCount);
        Assert.Equal(CapturedAt, snapshot.Health.SnapshotTime);
    }

    [Fact]
    public void GetSnapshotAsync_ThrowsWhenTokenIsAlreadyCancelled()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var provider = new CountingRuntimeSnapshotProvider(RuntimeSnapshot.Empty);
        var adapter = (IAsyncRuntimeDashboardAdapter)new RuntimeDashboardAdapter(provider);
        Action action = () => adapter.GetSnapshotAsync(cancellation.Token);

        Assert.Throws<OperationCanceledException>(action);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public void DashboardSnapshotChangedEventArgs_PreservesSnapshotAndOccurredAt()
    {
        var snapshot = new DashboardSnapshot(
            new RuntimeHealthSnapshot(0, 0, 0, 0, 0, CapturedAt),
            []);
        var occurredAt = new DateTimeOffset(2026, 5, 14, 12, 30, 0, TimeSpan.Zero);

        var args = new DashboardSnapshotChangedEventArgs(snapshot, occurredAt);

        Assert.Same(snapshot, args.Snapshot);
        Assert.Equal(occurredAt, args.OccurredAt);
    }

    [Fact]
    public void DashboardSnapshotChangedEventArgs_ThrowsWhenSnapshotIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new DashboardSnapshotChangedEventArgs(null!, CapturedAt));
    }

    [Fact]
    public void RuntimeDashboardLifecycleContract_DeclaresStartAndStopAsync()
    {
        MethodInfo start = GetRequiredMethod(
            typeof(IRuntimeDashboardLifecycle),
            nameof(IRuntimeDashboardLifecycle.StartAsync));
        MethodInfo stop = GetRequiredMethod(
            typeof(IRuntimeDashboardLifecycle),
            nameof(IRuntimeDashboardLifecycle.StopAsync));

        AssertTaskWithCancellationTokenSignature(start);
        AssertTaskWithCancellationTokenSignature(stop);
    }

    [Fact]
    public void RuntimeDashboardEventSourceContract_DeclaresSnapshotAndEventEvents()
    {
        EventInfo snapshotChanged = Assert.Single(
            typeof(IRuntimeDashboardEventSource).GetEvents(),
            item => item.Name == nameof(IRuntimeDashboardEventSource.SnapshotChanged));
        EventInfo eventReceived = Assert.Single(
            typeof(IRuntimeDashboardEventSource).GetEvents(),
            item => item.Name == nameof(IRuntimeDashboardEventSource.EventReceived));

        Assert.Equal(typeof(EventHandler<DashboardSnapshotChangedEventArgs>), snapshotChanged.EventHandlerType);
        Assert.Equal(typeof(EventHandler<RuntimeDashboardEvent>), eventReceived.EventHandlerType);
    }

    [Fact]
    public void FakeDashboardRuntimeAdapter_IsNotForcedToImplementOptionalInterfaces()
    {
        object adapter = new FakeDashboardRuntimeAdapter();

        Assert.False(adapter is IAsyncRuntimeDashboardAdapter);
        Assert.False(adapter is IRuntimeDashboardLifecycle);
        Assert.False(adapter is IRuntimeDashboardEventSource);
    }

    private static MethodInfo GetRequiredMethod(Type type, string name)
        => type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
           ?? throw new InvalidOperationException($"{type.Name}.{name} was not found.");

    private static void AssertTaskWithCancellationTokenSignature(MethodInfo method)
    {
        Assert.Equal(typeof(Task), method.ReturnType);
        ParameterInfo parameter = Assert.Single(method.GetParameters());
        Assert.Equal(typeof(CancellationToken), parameter.ParameterType);
        Assert.Equal("cancellationToken", parameter.Name);
    }

    private sealed class CountingRuntimeSnapshotProvider : IRuntimeSnapshotProvider
    {
        private readonly RuntimeSnapshot _snapshot;

        public CountingRuntimeSnapshotProvider(RuntimeSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public int CallCount { get; private set; }

        public RuntimeSnapshot GetSnapshot()
        {
            CallCount++;
            return _snapshot;
        }
    }
}
