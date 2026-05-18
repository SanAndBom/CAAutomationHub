using CAAutomationHub.PilotApp.WorkStart;

namespace CAAutomationHub.PilotApp.Polling;

public sealed class PilotPollingService : IPilotPollingService
{
    private readonly IPilotPollingFlowPort _flowPort;
    private readonly PilotPollingOptions _options;
    private readonly IWorkStartExecutionClock _clock;

    public PilotPollingService(
        IPilotPollingFlowPort flowPort,
        PilotPollingOptions options,
        IWorkStartExecutionClock? clock = null)
    {
        _flowPort = flowPort ?? throw new ArgumentNullException(nameof(flowPort));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(_options.TargetId))
        {
            throw new ArgumentException("Polling target id is required.", nameof(options));
        }

        _clock = clock ?? SystemWorkStartExecutionClock.Instance;
        CurrentSnapshot = PilotPollingSnapshot.Initial;
    }

    public event EventHandler<PilotPollingSnapshotChangedEventArgs>? SnapshotChanged;

    public PilotPollingSnapshot CurrentSnapshot { get; private set; }

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PublishLifecycleSnapshot(PilotPollingStatus.Running, "Polling started.", isRunning: true);
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PublishLifecycleSnapshot(PilotPollingStatus.Stopped, "Polling stopped.", isRunning: false);
        return ValueTask.CompletedTask;
    }

    public async ValueTask<PilotPollingSnapshot> PollOnceAsync(CancellationToken cancellationToken = default)
    {
        var requestState = await _flowPort
            .ReadRequestStateAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!requestState.ReadSucceeded)
        {
            Publish(
                CreateSnapshot(
                    CurrentSnapshot,
                    WorkRequestKind.None,
                    PilotPollingStatus.Failed,
                    resultStatus: "ReadFailed",
                    errorCode: "ReadFailed",
                    message: requestState.Message ?? "Polling request read failed.",
                    requestState: requestState));
            return CurrentSnapshot;
        }

        if (requestState.StartRequestActive)
        {
            if (CurrentSnapshot.LastStartAckState == true)
            {
                PublishIdleWaitingForRequestOff(requestState, WorkRequestKind.WorkStart);
                return CurrentSnapshot;
            }

            var result = await _flowPort
                .ExecuteWorkStartAsync(new WorkStartExecutionRequest(TargetId: _options.TargetId), cancellationToken)
                .ConfigureAwait(false);

            Publish(
                CreateSnapshot(
                    CurrentSnapshot,
                    WorkRequestKind.WorkStart,
                    result.Succeeded ? PilotPollingStatus.WorkStartProcessed : PilotPollingStatus.Failed,
                    result.Status,
                    result.ErrorCodeName,
                    result.Message ?? "WorkStart processed.",
                    requestState,
                    selectedLotId: result.SelectedLotId ?? requestState.StartLotId,
                    startAckState: result.Succeeded ? true : CurrentSnapshot.LastStartAckState));
            return CurrentSnapshot;
        }

        if (CurrentSnapshot.LastStartAckState == true)
        {
            var result = await _flowPort
                .ClearWorkStartAckAsync(cancellationToken)
                .ConfigureAwait(false);

            Publish(
                CreateSnapshot(
                    CurrentSnapshot,
                    WorkRequestKind.WorkStart,
                    result.Succeeded ? PilotPollingStatus.WorkStartAckOffWritten : PilotPollingStatus.Failed,
                    result.Status.ToString(),
                    result.Succeeded ? null : result.Status.ToString(),
                    result.Message ?? "WorkStart ACK OFF processed.",
                    requestState,
                    startAckState: result.Succeeded ? false : CurrentSnapshot.LastStartAckState));
            return CurrentSnapshot;
        }

        if (requestState.CompleteRequestActive)
        {
            if (CurrentSnapshot.LastCompleteAckState == true)
            {
                PublishIdleWaitingForRequestOff(requestState, WorkRequestKind.WorkComplete);
                return CurrentSnapshot;
            }

            var result = await _flowPort
                .WriteWorkCompleteAckOnAsync(cancellationToken)
                .ConfigureAwait(false);

            Publish(
                CreateSnapshot(
                    CurrentSnapshot,
                    WorkRequestKind.WorkComplete,
                    result.Succeeded ? PilotPollingStatus.WorkCompleteAckOnWritten : PilotPollingStatus.Failed,
                    result.Status.ToString(),
                    result.Succeeded ? null : result.Status.ToString(),
                    result.Message ?? "WorkComplete ACK ON processed.",
                    requestState,
                    completeAckState: result.Succeeded ? true : CurrentSnapshot.LastCompleteAckState));
            return CurrentSnapshot;
        }

        if (CurrentSnapshot.LastCompleteAckState == true)
        {
            var result = await _flowPort
                .ClearWorkCompleteAckAsync(cancellationToken)
                .ConfigureAwait(false);

            Publish(
                CreateSnapshot(
                    CurrentSnapshot,
                    WorkRequestKind.WorkComplete,
                    result.Succeeded ? PilotPollingStatus.WorkCompleteAckOffWritten : PilotPollingStatus.Failed,
                    result.Status.ToString(),
                    result.Succeeded ? null : result.Status.ToString(),
                    result.Message ?? "WorkComplete ACK OFF processed.",
                    requestState,
                    completeAckState: result.Succeeded ? false : CurrentSnapshot.LastCompleteAckState));
            return CurrentSnapshot;
        }

        Publish(
            CreateSnapshot(
                CurrentSnapshot,
                WorkRequestKind.None,
                PilotPollingStatus.Idle,
                resultStatus: PilotPollingStatus.Idle.ToString(),
                errorCode: null,
                message: "No active pilot request.",
                requestState: requestState));
        return CurrentSnapshot;
    }

    private void PublishIdleWaitingForRequestOff(
        PilotPollingRequestState requestState,
        WorkRequestKind requestKind)
    {
        Publish(
            CreateSnapshot(
                CurrentSnapshot,
                requestKind,
                PilotPollingStatus.Idle,
                resultStatus: "WaitingRequestOff",
                errorCode: null,
                message: "ACK is already ON; waiting for request OFF.",
                requestState: requestState));
    }

    private PilotPollingSnapshot CreateSnapshot(
        PilotPollingSnapshot previous,
        WorkRequestKind requestKind,
        PilotPollingStatus status,
        string? resultStatus,
        string? errorCode,
        string? message,
        PilotPollingRequestState? requestState = null,
        bool? isRunning = null,
        string? selectedLotId = null,
        bool? startAckState = null,
        bool? completeAckState = null)
    {
        var now = _clock.GetUtcNow();
        return previous with
        {
            IsRunning = isRunning ?? previous.IsRunning,
            Status = status,
            LastRequestKind = requestKind,
            LastSelectedLotId = selectedLotId ?? requestState?.StartLotId ?? previous.LastSelectedLotId,
            LastStartRequestActive = requestState?.StartRequestActive ?? previous.LastStartRequestActive,
            LastCompleteRequestActive = requestState?.CompleteRequestActive ?? previous.LastCompleteRequestActive,
            LastStartAckState = startAckState ?? previous.LastStartAckState,
            LastCompleteAckState = completeAckState ?? previous.LastCompleteAckState,
            LastResultStatus = resultStatus,
            LastErrorCode = errorCode,
            LastMessage = message,
            LastUpdatedAt = now,
            LogEntries = AppendLog(previous.LogEntries, now, requestKind, status, message)
        };
    }

    private void PublishLifecycleSnapshot(PilotPollingStatus status, string message, bool isRunning)
    {
        var now = _clock.GetUtcNow();
        Publish(CurrentSnapshot with
        {
            IsRunning = isRunning,
            Status = status,
            LastUpdatedAt = now,
            LogEntries = AppendLog(CurrentSnapshot.LogEntries, now, WorkRequestKind.None, status, message)
        });
    }

    private IReadOnlyList<PilotPollingLogEntry> AppendLog(
        IReadOnlyList<PilotPollingLogEntry> previous,
        DateTimeOffset now,
        WorkRequestKind requestKind,
        PilotPollingStatus status,
        string? message)
    {
        var entries = previous
            .Append(new PilotPollingLogEntry(now, requestKind, status.ToString(), message))
            .ToArray();

        return entries.Length <= _options.MaxLogEntries
            ? entries
            : entries[^_options.MaxLogEntries..];
    }

    private void Publish(PilotPollingSnapshot snapshot)
    {
        CurrentSnapshot = snapshot;
        SnapshotChanged?.Invoke(this, new PilotPollingSnapshotChangedEventArgs(snapshot));
    }
}
