using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotApp.WorkStart;

public sealed class WorkStartExecutionService : IWorkStartExecutionService
{
    private readonly IWorkStartFlowRunner _runner;
    private readonly IWorkStartExecutionClock _clock;
    private readonly SemaphoreSlim _singleExecutionGate = new(1, 1);

    public WorkStartExecutionService(
        IWorkStartFlowRunner runner,
        IWorkStartExecutionClock? clock = null)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _clock = clock ?? SystemWorkStartExecutionClock.Instance;
    }

    public async ValueTask<WorkStartExecutionResult> ExecuteOnceAsync(
        WorkStartExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var startedAt = _clock.GetUtcNow();
        if (!await _singleExecutionGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            var busyCompletedAt = _clock.GetUtcNow();
            return CreateBusyResult(startedAt, busyCompletedAt);
        }

        try
        {
            var flowResult = await _runner.RunAsync(cancellationToken).ConfigureAwait(false);
            var completedAt = _clock.GetUtcNow();
            return Map(flowResult, startedAt, completedAt);
        }
        finally
        {
            _singleExecutionGate.Release();
        }
    }

    private static WorkStartExecutionResult Map(
        WorkStartFlowResult flowResult,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt) =>
        new()
        {
            Succeeded = flowResult.Succeeded,
            Status = flowResult.Status.ToString(),
            Step = flowResult.Step.ToCode(),
            ErrorCode = (int)flowResult.ErrorCode,
            ErrorCodeName = flowResult.ErrorCode.ToString(),
            Message = flowResult.Message,
            SelectedLotId = flowResult.SelectedLotId,
            ErrorWriteExpected = flowResult.ErrorWriteExpected,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            Duration = completedAt - startedAt
        };

    private static WorkStartExecutionResult CreateBusyResult(
        DateTimeOffset startedAt,
        DateTimeOffset completedAt) =>
        new()
        {
            Succeeded = false,
            Status = "Busy",
            Step = "busy",
            ErrorCode = (int)WorkStartErrorCode.None,
            ErrorCodeName = WorkStartErrorCode.None.ToString(),
            Message = "A WorkStart execution is already running.",
            SelectedLotId = null,
            ErrorWriteExpected = false,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            Duration = completedAt - startedAt
        };
}
