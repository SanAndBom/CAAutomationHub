using CAAutomationHub.PilotApp.Polling;
using CAAutomationHub.PilotApp.WorkStart;
using CAAutomationHub.PilotFlows.WorkComplete;
using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotApp.Tests.Polling;

public sealed class PilotPollingServiceTests
{
    [Fact]
    public async Task StartStopAsync_UpdatesRunningStateAndAppendsLog()
    {
        var port = new FakePilotPollingFlowPort();
        var service = new PilotPollingService(
            port,
            new PilotPollingOptions { TargetId = "PLC-01" },
            new QueueClock(
                DateTimeOffset.Parse("2026-05-18T09:00:00+09:00"),
                DateTimeOffset.Parse("2026-05-18T09:01:00+09:00")));

        await service.StartAsync();
        await service.StopAsync();

        Assert.False(service.CurrentSnapshot.IsRunning);
        Assert.Equal(PilotPollingStatus.Stopped, service.CurrentSnapshot.Status);
        Assert.Equal(2, service.CurrentSnapshot.LogEntries.Count);
        Assert.Equal("Polling started.", service.CurrentSnapshot.LogEntries[0].Message);
        Assert.Equal("Polling stopped.", service.CurrentSnapshot.LogEntries[1].Message);
    }

    [Fact]
    public async Task PollOnceAsync_ProcessesWorkStartRequest_WhenStartRequestOn()
    {
        var port = new FakePilotPollingFlowPort
        {
            RequestState = new PilotPollingRequestState
            {
                StartRequestActive = true,
                CompleteRequestActive = true,
                StartLotId = "LOT-START-01"
            },
            WorkStartResult = CreateWorkStartResult(selectedLotId: "LOT-START-01")
        };
        var service = CreateService(port);

        await service.StartAsync();
        var snapshot = await service.PollOnceAsync();

        Assert.Equal(1, port.ExecuteWorkStartCallCount);
        Assert.Equal(0, port.WriteWorkCompleteAckOnCallCount);
        Assert.Equal(WorkRequestKind.WorkStart, snapshot.LastRequestKind);
        Assert.Equal("LOT-START-01", snapshot.LastSelectedLotId);
        Assert.True(snapshot.LastStartRequestActive);
        Assert.True(snapshot.LastCompleteRequestActive);
        Assert.True(snapshot.LastStartAckState);
        Assert.Null(snapshot.LastCompleteAckState);
        Assert.Equal(PilotPollingStatus.WorkStartProcessed, snapshot.Status);
        Assert.Equal("Succeeded", snapshot.LastResultStatus);
        Assert.Equal("None", snapshot.LastErrorCode);
        Assert.Equal("PLC-01", snapshot.PlcCardStatus.TargetId);
        Assert.Equal(PilotPlcConnectionStatus.Connected, snapshot.PlcCardStatus.ConnectionStatus);
        Assert.Equal("Succeeded", snapshot.PlcCardStatus.LastReadResultStatus);
    }

    [Fact]
    public async Task PollOnceAsync_ReturnsFailedSnapshot_WhenRequestStateReadThrows()
    {
        var port = new ThrowingPilotPollingFlowPort(new InvalidOperationException("connect failed"));
        var service = CreateService(port);

        var snapshot = await service.PollOnceAsync();

        Assert.Equal(PilotPollingStatus.Failed, snapshot.Status);
        Assert.Equal("ReadFailed", snapshot.LastResultStatus);
        Assert.Equal("ReadFailed", snapshot.LastErrorCode);
        Assert.Contains("connect failed", snapshot.LastMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(PilotPlcConnectionStatus.Failed, snapshot.PlcCardStatus.ConnectionStatus);
        Assert.Equal("ReadFailed", snapshot.PlcCardStatus.LastReadResultStatus);
    }

    [Fact]
    public async Task PollOnceAsync_ClearsStartAck_WhenStartRequestOffAfterAckOn()
    {
        var port = new FakePilotPollingFlowPort
        {
            RequestState = new PilotPollingRequestState
            {
                StartRequestActive = true,
                StartLotId = "LOT-START-01"
            },
            WorkStartResult = CreateWorkStartResult(selectedLotId: "LOT-START-01"),
            StartAckOffResult = WorkStartAckOffResult.AckOffWritten()
        };
        var service = CreateService(port);

        await service.StartAsync();
        await service.PollOnceAsync();
        port.RequestState = new PilotPollingRequestState { StartRequestActive = false };

        var snapshot = await service.PollOnceAsync();

        Assert.Equal(1, port.ClearWorkStartAckCallCount);
        Assert.Equal(WorkRequestKind.WorkStart, snapshot.LastRequestKind);
        Assert.False(snapshot.LastStartRequestActive);
        Assert.False(snapshot.LastStartAckState);
        Assert.Equal(PilotPollingStatus.WorkStartAckOffWritten, snapshot.Status);
        Assert.Equal("AckOffWritten", snapshot.LastResultStatus);
    }

    [Fact]
    public async Task PollOnceAsync_ProcessesWorkCompleteRequest_WhenCompleteRequestOn()
    {
        var port = new FakePilotPollingFlowPort
        {
            RequestState = new PilotPollingRequestState { CompleteRequestActive = true },
            CompleteAckOnResult = WorkCompleteAckResult.AckOnWritten()
        };
        var service = CreateService(port);

        await service.StartAsync();
        var snapshot = await service.PollOnceAsync();

        Assert.Equal(1, port.WriteWorkCompleteAckOnCallCount);
        Assert.Equal(0, port.ExecuteWorkStartCallCount);
        Assert.Equal(WorkRequestKind.WorkComplete, snapshot.LastRequestKind);
        Assert.True(snapshot.LastCompleteRequestActive);
        Assert.True(snapshot.LastCompleteAckState);
        Assert.Equal(PilotPollingStatus.WorkCompleteAckOnWritten, snapshot.Status);
        Assert.Equal("AckOnWritten", snapshot.LastResultStatus);
    }

    [Fact]
    public async Task PollOnceAsync_ClearsCompleteAck_WhenCompleteRequestOffAfterAckOn()
    {
        var port = new FakePilotPollingFlowPort
        {
            RequestState = new PilotPollingRequestState { CompleteRequestActive = true },
            CompleteAckOnResult = WorkCompleteAckResult.AckOnWritten(),
            CompleteAckOffResult = WorkCompleteAckResult.AckOffWritten()
        };
        var service = CreateService(port);

        await service.StartAsync();
        await service.PollOnceAsync();
        port.RequestState = new PilotPollingRequestState { CompleteRequestActive = false };

        var snapshot = await service.PollOnceAsync();

        Assert.Equal(1, port.ClearWorkCompleteAckCallCount);
        Assert.Equal(WorkRequestKind.WorkComplete, snapshot.LastRequestKind);
        Assert.False(snapshot.LastCompleteRequestActive);
        Assert.False(snapshot.LastCompleteAckState);
        Assert.Equal(PilotPollingStatus.WorkCompleteAckOffWritten, snapshot.Status);
        Assert.Equal("AckOffWritten", snapshot.LastResultStatus);
    }

    [Fact]
    public async Task PollOnceAsync_AppendsTrendPoint()
    {
        var port = new FakePilotPollingFlowPort
        {
            RequestState = new PilotPollingRequestState
            {
                StartRequestActive = true,
                StartLotId = "LOT-START-01"
            },
            WorkStartResult = CreateWorkStartResult(selectedLotId: "LOT-START-01")
        };
        var service = CreateService(port);

        var snapshot = await service.PollOnceAsync();

        var trendPoint = Assert.Single(snapshot.TrendPoints);
        Assert.Equal(1, trendPoint.SequenceNo);
        Assert.True(trendPoint.IsSuccess);
        Assert.Equal(WorkRequestKind.WorkStart, trendPoint.RequestKind);
        Assert.Equal("LOT-START-01", trendPoint.SelectedLotId);
        Assert.Equal("Succeeded", trendPoint.ResultStatus);
        Assert.Equal("None", trendPoint.ErrorCode);
    }

    [Fact]
    public async Task PollOnceAsync_TrimsTrendPointsToMaxCount()
    {
        var port = new FakePilotPollingFlowPort();
        var service = CreateService(port, maxTrendPoints: 2);

        await service.PollOnceAsync();
        await service.PollOnceAsync();
        var snapshot = await service.PollOnceAsync();

        Assert.Equal(2, snapshot.TrendPoints.Count);
        Assert.Equal([2, 3], snapshot.TrendPoints.Select(static point => point.SequenceNo).ToArray());
    }

    [Fact]
    public void PollingSnapshot_DoesNotExposeRuntimeOrChannelPollingDetails()
    {
        var propertyNames = typeof(PilotPollingSnapshot)
            .GetProperties()
            .Select(static property => property.Name)
            .ToArray();
        var typeNames = typeof(PilotPollingSnapshot)
            .GetProperties()
            .Select(static property => property.PropertyType.FullName ?? property.PropertyType.Name)
            .ToArray();

        Assert.DoesNotContain(propertyNames, static name => name.Contains("Payload", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, static name => name.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeNames, static name => name.Contains(string.Join("", "Run", "time", "Snapshot"), StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeNames, static name => name.Contains(string.Join("", "Channel", "Polling", "Result"), StringComparison.OrdinalIgnoreCase));
    }

    private static PilotPollingService CreateService(IPilotPollingFlowPort port, int maxTrendPoints = 50)
        => new(
            port,
            new PilotPollingOptions { TargetId = "PLC-01", MaxTrendPoints = maxTrendPoints },
            new FixedClock());

    private static WorkStartExecutionResult CreateWorkStartResult(string selectedLotId)
        => new()
        {
            Succeeded = true,
            Status = "Succeeded",
            Step = "completed",
            ErrorCode = 0,
            ErrorCodeName = "None",
            Message = null,
            SelectedLotId = selectedLotId,
            ErrorWriteExpected = false,
            StartedAt = DateTimeOffset.Parse("2026-05-18T10:00:00+09:00"),
            CompletedAt = DateTimeOffset.Parse("2026-05-18T10:00:01+09:00"),
            Duration = TimeSpan.FromSeconds(1)
        };

    private sealed class FakePilotPollingFlowPort : IPilotPollingFlowPort
    {
        public PilotPollingRequestState RequestState { get; set; } = new();

        public WorkStartExecutionResult WorkStartResult { get; init; } =
            CreateWorkStartResult("LOT-START-01");

        public WorkStartAckOffResult StartAckOffResult { get; init; } =
            WorkStartAckOffResult.AckOffWritten();

        public WorkCompleteAckResult CompleteAckOnResult { get; init; } =
            WorkCompleteAckResult.AckOnWritten();

        public WorkCompleteAckResult CompleteAckOffResult { get; init; } =
            WorkCompleteAckResult.AckOffWritten();

        public int ExecuteWorkStartCallCount { get; private set; }

        public int ClearWorkStartAckCallCount { get; private set; }

        public int WriteWorkCompleteAckOnCallCount { get; private set; }

        public int ClearWorkCompleteAckCallCount { get; private set; }

        public ValueTask<PilotPollingRequestState> ReadRequestStateAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(RequestState);

        public ValueTask<WorkStartExecutionResult> ExecuteWorkStartAsync(
            WorkStartExecutionRequest request,
            CancellationToken cancellationToken = default)
        {
            ExecuteWorkStartCallCount++;
            return ValueTask.FromResult(WorkStartResult);
        }

        public ValueTask<WorkStartAckOffResult> ClearWorkStartAckAsync(
            CancellationToken cancellationToken = default)
        {
            ClearWorkStartAckCallCount++;
            return ValueTask.FromResult(StartAckOffResult);
        }

        public ValueTask<WorkCompleteAckResult> WriteWorkCompleteAckOnAsync(
            CancellationToken cancellationToken = default)
        {
            WriteWorkCompleteAckOnCallCount++;
            return ValueTask.FromResult(CompleteAckOnResult);
        }

        public ValueTask<WorkCompleteAckResult> ClearWorkCompleteAckAsync(
            CancellationToken cancellationToken = default)
        {
            ClearWorkCompleteAckCallCount++;
            return ValueTask.FromResult(CompleteAckOffResult);
        }
    }

    private sealed class ThrowingPilotPollingFlowPort : IPilotPollingFlowPort
    {
        private readonly Exception _exception;

        public ThrowingPilotPollingFlowPort(Exception exception)
        {
            _exception = exception;
        }

        public ValueTask<PilotPollingRequestState> ReadRequestStateAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromException<PilotPollingRequestState>(_exception);

        public ValueTask<WorkStartExecutionResult> ExecuteWorkStartAsync(
            WorkStartExecutionRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask<WorkStartAckOffResult> ClearWorkStartAckAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask<WorkCompleteAckResult> WriteWorkCompleteAckOnAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask<WorkCompleteAckResult> ClearWorkCompleteAckAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FixedClock : IWorkStartExecutionClock
    {
        public DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-05-18T10:00:00+09:00");
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
}
