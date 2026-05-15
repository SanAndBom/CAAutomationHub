using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Runtime.Channels;
using CAAutomationHub.Runtime.Polling;

namespace CAAutomationHub.Runtime.Tests.Polling;

public sealed class PollingPublishCoordinatorTests
{
    [Fact]
    public void Constructor_ThrowsWhenChannelRegistryIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new PollingPublishCoordinator(null!, _ => Task.FromResult(RuntimeSnapshot.Empty)));

        Assert.Equal("channelRegistry", exception.ParamName);
    }

    [Fact]
    public void Constructor_ThrowsWhenRefreshDelegateIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new PollingPublishCoordinator(new RuntimeChannelRegistry(), null!));

        Assert.Equal("refreshSnapshotAsync", exception.ParamName);
    }

    [Fact]
    public void PollingChannelUpdate_ThrowsWhenPlcIdIsNullEmptyOrWhiteSpace()
    {
        RuntimePlcChannelState state = CreateState("PLC-01");

        Assert.Throws<ArgumentException>(() => new PollingChannelUpdate(null!, state));
        Assert.Throws<ArgumentException>(() => new PollingChannelUpdate(string.Empty, state));
        Assert.Throws<ArgumentException>(() => new PollingChannelUpdate("   ", state));
    }

    [Fact]
    public void PollingChannelUpdate_ThrowsWhenStateIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new PollingChannelUpdate("PLC-01", null!));

        Assert.Equal("state", exception.ParamName);
    }

    [Fact]
    public async Task PublishAsync_ThrowsWhenUpdatesIsNull()
    {
        var coordinator = new PollingPublishCoordinator(
            new RuntimeChannelRegistry(),
            _ => Task.FromResult(RuntimeSnapshot.Empty));

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => coordinator.PublishAsync(null!, CancellationToken.None));

        Assert.Equal("updates", exception.ParamName);
    }

    [Fact]
    public async Task PublishAsync_ThrowsWhenUpdatesContainsNullItem()
    {
        var coordinator = new PollingPublishCoordinator(
            new RuntimeChannelRegistry(),
            _ => Task.FromResult(RuntimeSnapshot.Empty));
        PollingChannelUpdate?[] updates = [null];

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => coordinator.PublishAsync(updates!, CancellationToken.None));

        Assert.Equal("updates", exception.ParamName);
    }

    [Fact]
    public async Task PublishAsync_WithEmptyUpdatesDoesNotRefresh()
    {
        var refreshCallCount = 0;
        var coordinator = new PollingPublishCoordinator(
            new RuntimeChannelRegistry(),
            _ =>
            {
                refreshCallCount++;
                return Task.FromResult(RuntimeSnapshot.Empty);
            });

        PollingPublishResult result = await coordinator.PublishAsync([], CancellationToken.None);

        Assert.Equal(0, result.RequestedCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Empty(result.MissingChannelIds);
        Assert.Empty(result.NonWritableChannelIds);
        Assert.Empty(result.UpdateFailures);
        Assert.False(result.PublishAttempted);
        Assert.False(result.PublishSucceeded);
        Assert.Null(result.PublishException);
        Assert.Equal(0, refreshCallCount);
    }

    [Fact]
    public async Task PublishAsync_UpdatesWritableChannelAndRefreshesOnce()
    {
        var registry = new RuntimeChannelRegistry();
        var channel = new InMemoryRuntimePlcChannel("PLC-01", "Original PLC");
        registry.Add(channel);
        var refreshCallCount = 0;
        var coordinator = new PollingPublishCoordinator(
            registry,
            _ =>
            {
                refreshCallCount++;
                return Task.FromResult(RuntimeSnapshot.Empty);
            });
        RuntimePlcChannelState replacement = CreateState(
            "PLC-01",
            plcName: "Updated PLC",
            linkState: PlcLinkState.Reconnecting,
            healthSeverity: PlcHealthSeverity.Warning);

        PollingPublishResult result = await coordinator.PublishAsync(
            [new PollingChannelUpdate("PLC-01", replacement)],
            CancellationToken.None);

        ChannelRuntimeState state = channel.GetState(DateTimeOffset.UtcNow);
        Assert.Equal("Updated PLC", state.PlcName);
        Assert.Equal(PlcLinkState.Reconnecting, state.LinkState);
        Assert.Equal(PlcHealthSeverity.Warning, state.HealthSeverity);
        Assert.Equal(1, result.RequestedCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.True(result.PublishAttempted);
        Assert.True(result.PublishSucceeded);
        Assert.Null(result.PublishException);
        Assert.Equal(1, refreshCallCount);
    }

    [Fact]
    public async Task PublishAsync_WithMultipleWritableUpdatesRefreshesOnceAfterAllUpdates()
    {
        var registry = new RuntimeChannelRegistry();
        var first = new InMemoryRuntimePlcChannel("PLC-01", "First PLC");
        var second = new InMemoryRuntimePlcChannel("PLC-02", "Second PLC");
        registry.Add(first);
        registry.Add(second);
        var refreshCallCount = 0;
        string? firstNameAtRefresh = null;
        string? secondNameAtRefresh = null;
        var coordinator = new PollingPublishCoordinator(
            registry,
            _ =>
            {
                refreshCallCount++;
                firstNameAtRefresh = first.GetState(DateTimeOffset.UtcNow).PlcName;
                secondNameAtRefresh = second.GetState(DateTimeOffset.UtcNow).PlcName;
                return Task.FromResult(RuntimeSnapshot.Empty);
            });

        PollingPublishResult result = await coordinator.PublishAsync(
            [
                new PollingChannelUpdate("PLC-01", CreateState("PLC-01", plcName: "Updated First")),
                new PollingChannelUpdate("PLC-02", CreateState("PLC-02", plcName: "Updated Second")),
            ],
            CancellationToken.None);

        Assert.Equal(2, result.RequestedCount);
        Assert.Equal(2, result.UpdatedCount);
        Assert.Equal(1, refreshCallCount);
        Assert.Equal("Updated First", firstNameAtRefresh);
        Assert.Equal("Updated Second", secondNameAtRefresh);
    }

    [Fact]
    public async Task PublishAsync_RecordsMissingChannelAndContinues()
    {
        var registry = new RuntimeChannelRegistry();
        var existing = new InMemoryRuntimePlcChannel("PLC-01", "Original PLC");
        registry.Add(existing);
        var refreshCallCount = 0;
        var coordinator = new PollingPublishCoordinator(
            registry,
            _ =>
            {
                refreshCallCount++;
                return Task.FromResult(RuntimeSnapshot.Empty);
            });

        PollingPublishResult result = await coordinator.PublishAsync(
            [
                new PollingChannelUpdate("PLC-MISSING", CreateState("PLC-MISSING")),
                new PollingChannelUpdate("PLC-01", CreateState("PLC-01", plcName: "Updated PLC")),
            ],
            CancellationToken.None);

        Assert.Equal(2, result.RequestedCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(["PLC-MISSING"], result.MissingChannelIds);
        Assert.Empty(result.NonWritableChannelIds);
        Assert.Empty(result.UpdateFailures);
        Assert.True(result.PublishSucceeded);
        Assert.Equal(1, refreshCallCount);
        Assert.Equal("Updated PLC", existing.GetState(DateTimeOffset.UtcNow).PlcName);
    }

    [Fact]
    public async Task PublishAsync_RecordsNonWritableChannelAndContinues()
    {
        var registry = new RuntimeChannelRegistry();
        var readOnly = new ReadOnlyRuntimePlcChannel("PLC-READONLY");
        var writable = new InMemoryRuntimePlcChannel("PLC-01", "Original PLC");
        registry.Add(readOnly);
        registry.Add(writable);
        var refreshCallCount = 0;
        var coordinator = new PollingPublishCoordinator(
            registry,
            _ =>
            {
                refreshCallCount++;
                return Task.FromResult(RuntimeSnapshot.Empty);
            });

        PollingPublishResult result = await coordinator.PublishAsync(
            [
                new PollingChannelUpdate("PLC-READONLY", CreateState("PLC-READONLY")),
                new PollingChannelUpdate("PLC-01", CreateState("PLC-01", plcName: "Updated PLC")),
            ],
            CancellationToken.None);

        Assert.Equal(2, result.RequestedCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Empty(result.MissingChannelIds);
        Assert.Equal(["PLC-READONLY"], result.NonWritableChannelIds);
        Assert.Empty(result.UpdateFailures);
        Assert.True(result.PublishSucceeded);
        Assert.Equal(1, refreshCallCount);
        Assert.Equal("Updated PLC", writable.GetState(DateTimeOffset.UtcNow).PlcName);
    }

    [Fact]
    public async Task PublishAsync_RecordsUpdateFailureAndContinues()
    {
        var registry = new RuntimeChannelRegistry();
        var failing = new ThrowingWritableRuntimePlcChannel("PLC-FAIL");
        var writable = new InMemoryRuntimePlcChannel("PLC-01", "Original PLC");
        registry.Add(failing);
        registry.Add(writable);
        var refreshCallCount = 0;
        var coordinator = new PollingPublishCoordinator(
            registry,
            _ =>
            {
                refreshCallCount++;
                return Task.FromResult(RuntimeSnapshot.Empty);
            });

        PollingPublishResult result = await coordinator.PublishAsync(
            [
                new PollingChannelUpdate("PLC-FAIL", CreateState("PLC-FAIL")),
                new PollingChannelUpdate("PLC-01", CreateState("PLC-01", plcName: "Updated PLC")),
            ],
            CancellationToken.None);

        PollingPublishUpdateFailure failure = Assert.Single(result.UpdateFailures);
        Assert.Equal("PLC-FAIL", failure.PlcId);
        Assert.IsType<InvalidOperationException>(failure.Exception);
        Assert.Equal(1, result.UpdatedCount);
        Assert.True(result.PublishSucceeded);
        Assert.Equal(1, refreshCallCount);
        Assert.Equal("Updated PLC", writable.GetState(DateTimeOffset.UtcNow).PlcName);
    }

    [Fact]
    public async Task PublishAsync_RecordsStateMismatchFailureFromWritableChannel()
    {
        var registry = new RuntimeChannelRegistry();
        registry.Add(new InMemoryRuntimePlcChannel("PLC-01", "Original PLC"));
        var coordinator = new PollingPublishCoordinator(
            registry,
            _ => Task.FromResult(RuntimeSnapshot.Empty));

        PollingPublishResult result = await coordinator.PublishAsync(
            [new PollingChannelUpdate("PLC-01", CreateState("PLC-02"))],
            CancellationToken.None);

        PollingPublishUpdateFailure failure = Assert.Single(result.UpdateFailures);
        Assert.Equal("PLC-01", failure.PlcId);
        Assert.IsType<ArgumentException>(failure.Exception);
        Assert.Equal(0, result.UpdatedCount);
        Assert.False(result.PublishAttempted);
        Assert.False(result.PublishSucceeded);
    }

    [Fact]
    public async Task PublishAsync_WhenNoUpdatesWereAppliedDoesNotRefresh()
    {
        var registry = new RuntimeChannelRegistry();
        registry.Add(new ReadOnlyRuntimePlcChannel("PLC-READONLY"));
        registry.Add(new ThrowingWritableRuntimePlcChannel("PLC-FAIL"));
        var refreshCallCount = 0;
        var coordinator = new PollingPublishCoordinator(
            registry,
            _ =>
            {
                refreshCallCount++;
                return Task.FromResult(RuntimeSnapshot.Empty);
            });

        PollingPublishResult result = await coordinator.PublishAsync(
            [
                new PollingChannelUpdate("PLC-MISSING", CreateState("PLC-MISSING")),
                new PollingChannelUpdate("PLC-READONLY", CreateState("PLC-READONLY")),
                new PollingChannelUpdate("PLC-FAIL", CreateState("PLC-FAIL")),
            ],
            CancellationToken.None);

        Assert.Equal(3, result.RequestedCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(["PLC-MISSING"], result.MissingChannelIds);
        Assert.Equal(["PLC-READONLY"], result.NonWritableChannelIds);
        Assert.Single(result.UpdateFailures);
        Assert.False(result.PublishAttempted);
        Assert.False(result.PublishSucceeded);
        Assert.Null(result.PublishException);
        Assert.Equal(0, refreshCallCount);
    }

    [Fact]
    public async Task PublishAsync_RecordsPublishFailureWithoutThrowing()
    {
        var registry = new RuntimeChannelRegistry();
        var channel = new InMemoryRuntimePlcChannel("PLC-01", "Original PLC");
        registry.Add(channel);
        var publishException = new InvalidOperationException("Snapshot refresh failed.");
        var coordinator = new PollingPublishCoordinator(
            registry,
            _ => throw publishException);

        PollingPublishResult result = await coordinator.PublishAsync(
            [new PollingChannelUpdate("PLC-01", CreateState("PLC-01", plcName: "Updated PLC"))],
            CancellationToken.None);

        Assert.Equal(1, result.UpdatedCount);
        Assert.True(result.PublishAttempted);
        Assert.False(result.PublishSucceeded);
        Assert.Same(publishException, result.PublishException);
        Assert.Equal("Updated PLC", channel.GetState(DateTimeOffset.UtcNow).PlcName);
    }

    [Fact]
    public async Task PublishAsync_ThrowsWhenCancellationRequestedBeforeStart()
    {
        var coordinator = new PollingPublishCoordinator(
            new RuntimeChannelRegistry(),
            _ => Task.FromResult(RuntimeSnapshot.Empty));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => coordinator.PublishAsync([], cts.Token));
    }

    [Fact]
    public async Task PublishAsync_PropagatesCancellationFromRefreshDelegate()
    {
        var registry = new RuntimeChannelRegistry();
        registry.Add(new InMemoryRuntimePlcChannel("PLC-01", "Original PLC"));
        using var refreshCts = new CancellationTokenSource();
        await refreshCts.CancelAsync();
        var coordinator = new PollingPublishCoordinator(
            registry,
            _ => Task.FromCanceled<RuntimeSnapshot>(refreshCts.Token));

        Exception exception = await Record.ExceptionAsync(
            () => coordinator.PublishAsync(
                [new PollingChannelUpdate("PLC-01", CreateState("PLC-01"))],
                CancellationToken.None));

        Assert.IsAssignableFrom<OperationCanceledException>(exception);
    }

    [Fact]
    public void PollingPublishCoordinator_DoesNotExposeSchedulerOrLoopMembers()
    {
        string[] memberNames = typeof(PollingPublishCoordinator)
            .GetMembers()
            .Select(member => member.Name)
            .ToArray();

        Assert.DoesNotContain(memberNames, name => name.Contains("Scheduler", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(memberNames, name => name.Contains("Timer", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(memberNames, name => name.Contains("Loop", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(memberNames, name => name.Contains("Interval", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("CAAutomationHub.Wpf")]
    [InlineData("XgtDriverCore")]
    [InlineData("XgtChannelRunner")]
    [InlineData("FakePlc")]
    public void RuntimePollingFiles_DoNotReferenceForbiddenBoundaries(string forbiddenText)
    {
        DirectoryInfo repositoryRoot = FindRepositoryRoot();
        string pollingDirectory = Path.Combine(
            repositoryRoot.FullName,
            "src",
            "CAAutomationHub.Runtime",
            "Polling");

        if (!Directory.Exists(pollingDirectory))
        {
            return;
        }

        string[] pollingFiles = Directory.GetFiles(pollingDirectory, "*.cs", SearchOption.AllDirectories);
        Assert.DoesNotContain(
            pollingFiles,
            file => File.ReadAllText(file).Contains(forbiddenText, StringComparison.OrdinalIgnoreCase));
    }

    private static RuntimePlcChannelState CreateState(
        string plcId,
        string plcName = "Updated PLC",
        PlcLinkState linkState = PlcLinkState.Online,
        PlcHealthSeverity healthSeverity = PlcHealthSeverity.Healthy)
        => new(
            PlcId: plcId,
            PlcName: plcName,
            LineName: "Line-A",
            IsEnabled: true,
            IpAddress: "127.0.0.1",
            Port: 2004,
            LinkState: linkState,
            HealthSeverity: healthSeverity,
            PollingState: PlcPollingState.Polling,
            SequenceState: RuntimeSequenceState.Idle,
            ConfiguredPollingIntervalMs: 500,
            EffectivePollingIntervalMs: 500,
            LastResponseMs: 12,
            ConsecutiveFailures: 0,
            ReconnectCount: 0,
            SuccessRate: 1.0,
            LastSuccessAt: null,
            LastFailureAt: null,
            LastError: null);

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null && !File.Exists(Path.Combine(current.FullName, "CAAutomationHub.sln")))
        {
            current = current.Parent;
        }

        return current ?? throw new InvalidOperationException("Repository root could not be found.");
    }

    private sealed class ReadOnlyRuntimePlcChannel : IRuntimePlcChannel
    {
        public ReadOnlyRuntimePlcChannel(string plcId)
        {
            PlcId = plcId;
        }

        public string PlcId { get; }

        public ChannelRuntimeState GetState(DateTimeOffset capturedAt)
            => new(
                PlcId: PlcId,
                PlcName: $"{PlcId} name",
                LineName: "Line-A",
                IsEnabled: true,
                IpAddress: "127.0.0.1",
                Port: 2004,
                LinkState: PlcLinkState.Online,
                HealthSeverity: PlcHealthSeverity.Healthy,
                PollingState: PlcPollingState.Polling,
                SequenceState: RuntimeSequenceState.Idle,
                ConfiguredPollingIntervalMs: 500,
                EffectivePollingIntervalMs: 500,
                LastResponseMs: 0,
                ConsecutiveFailures: 0,
                ReconnectCount: 0,
                SuccessRate: 1.0,
                LastSuccessAt: null,
                LastFailureAt: null,
                LastError: null);
    }

    private sealed class ThrowingWritableRuntimePlcChannel : IWritableRuntimePlcChannel
    {
        public ThrowingWritableRuntimePlcChannel(string plcId)
        {
            PlcId = plcId;
        }

        public string PlcId { get; }

        public RuntimePlcChannelState GetRuntimeState()
            => throw new InvalidOperationException($"Unable to read runtime state for {PlcId}.");

        public void ReplaceState(RuntimePlcChannelState state)
            => throw new InvalidOperationException($"Unable to replace state for {PlcId}.");

        public ChannelRuntimeState GetState(DateTimeOffset capturedAt)
            => throw new InvalidOperationException($"Unable to read state for {PlcId}.");
    }
}
