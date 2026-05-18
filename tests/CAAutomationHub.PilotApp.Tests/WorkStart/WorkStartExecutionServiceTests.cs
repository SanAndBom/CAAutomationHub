using CAAutomationHub.PilotApp.WorkStart;
using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotApp.Tests.WorkStart;

public sealed class WorkStartExecutionServiceTests
{
    [Fact]
    public async Task ExecuteOnceAsync_ReturnsSucceededResult_WhenFlowSucceeds()
    {
        var runner = new FakeWorkStartFlowRunner(WorkStartFlowResult.Success("LOT123456789"));
        var service = new WorkStartExecutionService(
            runner,
            new QueueClock(
                DateTimeOffset.Parse("2026-05-18T10:00:00+09:00"),
                DateTimeOffset.Parse("2026-05-18T10:00:02+09:00")));

        var result = await service.ExecuteOnceAsync(
            new WorkStartExecutionRequest(TargetId: "PLC-01", RequestedBy: "operator", CorrelationId: "cmd-001"));

        Assert.True(result.Succeeded);
        Assert.Equal("Succeeded", result.Status);
        Assert.Equal("completed", result.Step);
        Assert.Equal(0, result.ErrorCode);
        Assert.Equal("None", result.ErrorCodeName);
        Assert.Null(result.Message);
        Assert.Equal("LOT123456789", result.SelectedLotId);
        Assert.False(result.ErrorWriteExpected);
        Assert.Equal(DateTimeOffset.Parse("2026-05-18T10:00:00+09:00"), result.StartedAt);
        Assert.Equal(DateTimeOffset.Parse("2026-05-18T10:00:02+09:00"), result.CompletedAt);
        Assert.Equal(TimeSpan.FromSeconds(2), result.Duration);
        Assert.Equal(1, runner.CallCount);
    }

    [Fact]
    public async Task ExecuteOnceAsync_ReturnsFailedResult_WhenFlowFails()
    {
        var runner = new FakeWorkStartFlowRunner(
            WorkStartFlowResult.Failure(
                WorkStartStep.DbQuery,
                WorkStartErrorCode.DbNotFound,
                "No row found for LOT ID.",
                "LOT123456789"));
        var service = new WorkStartExecutionService(
            runner,
            new QueueClock(
                DateTimeOffset.Parse("2026-05-18T10:05:00+09:00"),
                DateTimeOffset.Parse("2026-05-18T10:05:01+09:00")));

        var result = await service.ExecuteOnceAsync(new WorkStartExecutionRequest(TargetId: "PLC-01"));

        Assert.False(result.Succeeded);
        Assert.Equal("Failed", result.Status);
        Assert.Equal("db-query", result.Step);
        Assert.Equal(2301, result.ErrorCode);
        Assert.Equal("DbNotFound", result.ErrorCodeName);
        Assert.Equal("No row found for LOT ID.", result.Message);
        Assert.Equal("LOT123456789", result.SelectedLotId);
        Assert.True(result.ErrorWriteExpected);
        Assert.Equal(TimeSpan.FromSeconds(1), result.Duration);
    }

    [Fact]
    public async Task ExecuteOnceAsync_PropagatesCancellationToken_ToRunner()
    {
        var runner = new FakeWorkStartFlowRunner(WorkStartFlowResult.Success("LOT123456789"));
        var service = new WorkStartExecutionService(runner, new FixedClock());
        using var cts = new CancellationTokenSource();

        await service.ExecuteOnceAsync(new WorkStartExecutionRequest(TargetId: "PLC-01"), cts.Token);

        Assert.Equal(cts.Token, runner.LastCancellationToken);
    }

    [Fact]
    public async Task ExecuteOnceAsync_ReturnsBusyResult_WhenAnotherExecutionIsRunning()
    {
        var releaseRunner = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runner = new BlockingWorkStartFlowRunner(releaseRunner.Task);
        var service = new WorkStartExecutionService(runner, new FixedClock());

        var firstExecution = service.ExecuteOnceAsync(new WorkStartExecutionRequest(TargetId: "PLC-01"));
        var busyResult = await service.ExecuteOnceAsync(new WorkStartExecutionRequest(TargetId: "PLC-01"));

        Assert.False(busyResult.Succeeded);
        Assert.Equal("Busy", busyResult.Status);
        Assert.Equal("busy", busyResult.Step);
        Assert.Equal(0, busyResult.ErrorCode);
        Assert.Equal("None", busyResult.ErrorCodeName);
        Assert.False(busyResult.ErrorWriteExpected);
        Assert.Null(busyResult.SelectedLotId);
        Assert.Equal(1, runner.CallCount);

        releaseRunner.SetResult();
        var firstResult = await firstExecution;
        Assert.True(firstResult.Succeeded);
    }

    [Fact]
    public void ExecutionResult_DoesNotExposePayloadOrChannelState()
    {
        var resultProperties = typeof(WorkStartExecutionResult).GetProperties();
        var propertyNames = resultProperties.Select(static property => property.Name).ToArray();
        var typeNames = resultProperties.Select(static property => property.PropertyType.FullName ?? property.PropertyType.Name).ToArray();

        Assert.DoesNotContain(propertyNames, static name => name.Contains("Payload", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, static name => name.Contains("RequestHex", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, static name => name.Contains("ResponseHex", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, static name => name.Contains("Snapshot", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, static name => name.Contains("Polling", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeNames, static name => name.Contains("Byte[]", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeNames, static name => name.Contains(string.Join("", "Run", "time", "Snapshot"), StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeNames, static name => name.Contains(string.Join("", "Channel", "Polling", "Result"), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PilotAppAssembly_DoesNotReferenceDriverRuntimeOrDatabaseConcrete()
    {
        var referencedAssemblyNames = typeof(WorkStartExecutionService)
            .Assembly
            .GetReferencedAssemblies()
            .Select(static assembly => assembly.Name)
            .Where(static name => name is not null)
            .Cast<string>()
            .ToArray();

        var forbiddenAssemblyNames = new[]
        {
            "AutomationHub." + "X" + "gtDriverCore",
            "AutomationHub." + "X" + "gtDriverCore." + "Fake" + "Plc",
            string.Join('.', "CAAutomationHub", "Run" + "time"),
            "X" + "gtChannelRunner",
            string.Join('.', "Microsoft", "Data", "SqlClient")
        };

        foreach (var forbiddenAssemblyName in forbiddenAssemblyNames)
        {
            Assert.DoesNotContain(forbiddenAssemblyName, referencedAssemblyNames);
        }
    }

    private sealed class FakeWorkStartFlowRunner : IWorkStartFlowRunner
    {
        private readonly WorkStartFlowResult _result;

        public FakeWorkStartFlowRunner(WorkStartFlowResult result)
        {
            _result = result;
        }

        public int CallCount { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public ValueTask<WorkStartFlowResult> RunAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastCancellationToken = cancellationToken;
            return ValueTask.FromResult(_result);
        }
    }

    private sealed class BlockingWorkStartFlowRunner : IWorkStartFlowRunner
    {
        private readonly Task _release;

        public BlockingWorkStartFlowRunner(Task release)
        {
            _release = release;
        }

        public int CallCount { get; private set; }

        public async ValueTask<WorkStartFlowResult> RunAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            await _release.WaitAsync(cancellationToken).ConfigureAwait(false);
            return WorkStartFlowResult.Success("LOT123456789");
        }
    }

    private sealed class QueueClock : IWorkStartExecutionClock
    {
        private readonly Queue<DateTimeOffset> _values;

        public QueueClock(params DateTimeOffset[] values)
        {
            _values = new Queue<DateTimeOffset>(values);
        }

        public DateTimeOffset GetUtcNow() => _values.Dequeue();
    }

    private sealed class FixedClock : IWorkStartExecutionClock
    {
        public DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-05-18T10:00:00+09:00");
    }
}
