namespace CAAutomationHub.PilotFlows.WorkStart;

public sealed class WorkStartAckOffService
{
    private readonly IWorkStartPlcOperations _plcOperations;
    private readonly WorkStartAckOffOptions _options;

    public WorkStartAckOffService(
        IWorkStartPlcOperations plcOperations,
        WorkStartAckOffOptions? options = null)
    {
        _plcOperations = plcOperations ?? throw new ArgumentNullException(nameof(plcOperations));
        _options = options ?? WorkStartAckOffOptions.Default;
    }

    public async ValueTask<WorkStartAckOffResult> AckOffAsync(
        CancellationToken cancellationToken = default)
    {
        await _plcOperations.EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        WorkStartReadBlockOperationResult readResult;
        try
        {
            readResult = await _plcOperations
                .ReadWorkStartBlockAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return WorkStartAckOffResult.ReadFailed(ex.Message);
        }

        if (readResult.Status != WorkStartReadBlockOperationStatus.Success || readResult.Data is null)
        {
            return WorkStartAckOffResult.ReadFailed(readResult.Message);
        }

        if (WorkStartReadBlockInterpreter.IsStartSignalActive(readResult.Data, _options.StartSignalWordIndex))
        {
            return WorkStartAckOffResult.WaitingRequestOff();
        }

        bool ackOffWritten;
        try
        {
            ackOffWritten = await _plcOperations
                .WriteStartAckAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return WorkStartAckOffResult.AckOffWriteFailed(ex.Message);
        }

        return ackOffWritten
            ? WorkStartAckOffResult.AckOffWritten()
            : WorkStartAckOffResult.AckOffWriteFailed(null);
    }
}
