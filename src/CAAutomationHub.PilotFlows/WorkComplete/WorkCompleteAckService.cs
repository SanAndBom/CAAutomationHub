namespace CAAutomationHub.PilotFlows.WorkComplete;

public sealed class WorkCompleteAckService
{
    private readonly IWorkCompletePlcOperations _plcOperations;
    private readonly WorkCompleteAckOptions _options;

    public WorkCompleteAckService(
        IWorkCompletePlcOperations plcOperations,
        WorkCompleteAckOptions? options = null)
    {
        _plcOperations = plcOperations ?? throw new ArgumentNullException(nameof(plcOperations));
        _options = options ?? WorkCompleteAckOptions.Default;
    }

    public async ValueTask<WorkCompleteAckResult> AckOnAsync(
        CancellationToken cancellationToken = default)
    {
        var read = await ReadSignalAsync(cancellationToken).ConfigureAwait(false);
        if (read.Result is not null)
        {
            return read.Result;
        }

        if (!read.IsCompleteSignalActive)
        {
            return WorkCompleteAckResult.WaitingRequestOn();
        }

        return await WriteAckAsync(
            _options.AckOnValue,
            WorkCompleteAckResult.AckOnWritten,
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<WorkCompleteAckResult> AckOffAsync(
        CancellationToken cancellationToken = default)
    {
        var read = await ReadSignalAsync(cancellationToken).ConfigureAwait(false);
        if (read.Result is not null)
        {
            return read.Result;
        }

        if (read.IsCompleteSignalActive)
        {
            return WorkCompleteAckResult.WaitingRequestOff();
        }

        return await WriteAckAsync(
            _options.AckOffValue,
            WorkCompleteAckResult.AckOffWritten,
            cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<(bool IsCompleteSignalActive, WorkCompleteAckResult? Result)> ReadSignalAsync(
        CancellationToken cancellationToken)
    {
        await _plcOperations.EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        WorkCompleteReadBlockOperationResult readResult;
        try
        {
            readResult = await _plcOperations
                .ReadWorkCompleteBlockAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return (false, WorkCompleteAckResult.ReadFailed(ex.Message));
        }

        if (readResult.Status != WorkCompleteReadBlockOperationStatus.Success || readResult.Data is null)
        {
            return (false, WorkCompleteAckResult.ReadFailed(readResult.Message));
        }

        var signalActive = WorkCompleteReadBlockInterpreter.IsCompleteSignalActive(
            readResult.Data,
            _options.CompleteSignalWordIndex);
        return (signalActive, null);
    }

    private async ValueTask<WorkCompleteAckResult> WriteAckAsync(
        ushort value,
        Func<WorkCompleteAckResult> createSuccessResult,
        CancellationToken cancellationToken)
    {
        bool written;
        try
        {
            written = await _plcOperations
                .WriteCompleteAckAsync(value, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return WorkCompleteAckResult.AckWriteFailed(ex.Message);
        }

        return written
            ? createSuccessResult()
            : WorkCompleteAckResult.AckWriteFailed(null);
    }
}
