using CAAutomationHub.Wpf.Adapters;
using CAAutomationHub.Wpf.Composition;
using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Tests.Composition;

public sealed class DashboardRuntimeCompositionFactoryTests
{
    [Fact]
    public void Constructor_ThrowsArgumentNullExceptionWhenCreateFakeAdapterIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DashboardRuntimeCompositionFactory(null!));
    }

    [Fact]
    public void Create_FakeUsesFakeAdapterDelegate()
    {
        var adapter = new StubRuntimeDashboardAdapter();
        var callCount = 0;
        var factory = new DashboardRuntimeCompositionFactory(() =>
        {
            callCount++;
            return adapter;
        });

        using DashboardRuntimeComposition composition = factory.Create(DashboardRuntimeMode.Fake);

        Assert.Equal(1, callCount);
        Assert.Equal(DashboardRuntimeMode.Fake, composition.Mode);
        Assert.Same(adapter, composition.Adapter);
        Assert.Same(DashboardRuntimeCapabilities.Editable, composition.Capabilities);
        Assert.Null(composition.Lifecycle);
    }

    [Fact]
    public void DashboardRuntimeCapabilities_EditableAllowsConfigurationEditing()
    {
        DashboardRuntimeCapabilities capabilities = DashboardRuntimeCapabilities.Editable;

        Assert.True(capabilities.CanAddPlc);
        Assert.True(capabilities.CanEditPlc);
        Assert.True(capabilities.CanDeletePlc);
        Assert.True(capabilities.CanEditConfiguration);
    }

    [Fact]
    public void DashboardRuntimeCapabilities_ReadOnlyDisallowsConfigurationEditing()
    {
        DashboardRuntimeCapabilities capabilities = DashboardRuntimeCapabilities.ReadOnly;

        Assert.False(capabilities.CanAddPlc);
        Assert.False(capabilities.CanEditPlc);
        Assert.False(capabilities.CanDeletePlc);
        Assert.False(capabilities.CanEditConfiguration);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void DashboardRuntimeCapabilities_CanEditConfigurationIsTrueWhenAnyEditCapabilityIsTrue(
        bool canAddPlc,
        bool canEditPlc,
        bool canDeletePlc)
    {
        var capabilities = new DashboardRuntimeCapabilities(
            canAddPlc,
            canEditPlc,
            canDeletePlc);

        Assert.True(capabilities.CanEditConfiguration);
    }

    [Fact]
    public void DashboardRuntimeCapabilities_CanEditConfigurationIsFalseWhenAllEditCapabilitiesAreFalse()
    {
        var capabilities = new DashboardRuntimeCapabilities(
            canAddPlc: false,
            canEditPlc: false,
            canDeletePlc: false);

        Assert.False(capabilities.CanEditConfiguration);
    }

    [Fact]
    public void Create_FakeThrowsArgumentNullExceptionWhenDelegateReturnsNull()
    {
        var factory = new DashboardRuntimeCompositionFactory(() => null!);

        Assert.Throws<ArgumentNullException>(
            () => factory.Create(DashboardRuntimeMode.Fake));
    }

    [Fact]
    public void Create_InMemoryRuntimeReturnsRuntimeRailComposition()
    {
        var factory = new DashboardRuntimeCompositionFactory(() => new StubRuntimeDashboardAdapter());

        using DashboardRuntimeComposition composition = factory.Create(DashboardRuntimeMode.InMemoryRuntime);

        Assert.Equal(DashboardRuntimeMode.InMemoryRuntime, composition.Mode);
        Assert.IsType<RuntimeDashboardAdapter>(composition.Adapter);
        Assert.Same(DashboardRuntimeCapabilities.ReadOnly, composition.Capabilities);
        Assert.IsType<SupervisorRuntimeDashboardLifecycle>(composition.Lifecycle);
    }

    [Fact]
    public void Create_InMemoryRuntimeDoesNotStartOrRefreshRuntimeRail()
    {
        var factory = new DashboardRuntimeCompositionFactory(() => new StubRuntimeDashboardAdapter());

        using DashboardRuntimeComposition composition = factory.Create(DashboardRuntimeMode.InMemoryRuntime);

        var snapshot = composition.Adapter.GetSnapshot();

        Assert.Equal(DateTimeOffset.UnixEpoch, snapshot.Health.SnapshotTime);
        Assert.Equal(0, snapshot.Health.TotalPlcs);
    }

    [Fact]
    public void Create_ThrowsArgumentOutOfRangeExceptionWhenModeIsUnknown()
    {
        var factory = new DashboardRuntimeCompositionFactory(() => new StubRuntimeDashboardAdapter());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => factory.Create((DashboardRuntimeMode)99));
    }

    [Fact]
    public void DashboardRuntimeComposition_RequiresAdapter()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DashboardRuntimeComposition(
                DashboardRuntimeMode.Fake,
                adapter: null!,
                DashboardRuntimeCapabilities.Editable));
    }

    [Fact]
    public void DashboardRuntimeComposition_RequiresCapabilities()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DashboardRuntimeComposition(
                DashboardRuntimeMode.Fake,
                new StubRuntimeDashboardAdapter(),
                capabilities: null!));
    }

    [Fact]
    public void DashboardRuntimeComposition_DisposeDisposesOnlyInnerDisposable()
    {
        var adapter = new StubRuntimeDashboardAdapter();
        var lifecycle = new StubRuntimeDashboardLifecycle();
        var disposable = new StubDisposable();
        var composition = new DashboardRuntimeComposition(
            DashboardRuntimeMode.InMemoryRuntime,
            adapter,
            DashboardRuntimeCapabilities.ReadOnly,
            lifecycle,
            disposable);

        composition.Dispose();
        composition.Dispose();

        Assert.Equal(1, disposable.DisposeCallCount);
        Assert.Equal(0, lifecycle.StopAsyncCallCount);
    }

    private sealed class StubRuntimeDashboardAdapter : IRuntimeDashboardAdapter
    {
        public DashboardSnapshot GetSnapshot()
            => new(
                new RuntimeHealthSnapshot(
                    TotalPlcs: 0,
                    HealthyCount: 0,
                    WarningCount: 0,
                    CongestedCount: 0,
                    ErrorCount: 0,
                    SnapshotTime: DateTimeOffset.UnixEpoch),
                plcCards: []);
    }

    private sealed class StubRuntimeDashboardLifecycle : IRuntimeDashboardLifecycle
    {
        public int StopAsyncCallCount { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StopAsyncCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubDisposable : IDisposable
    {
        public int DisposeCallCount { get; private set; }

        public void Dispose()
        {
            DisposeCallCount++;
        }
    }
}
