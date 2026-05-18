using CAAutomationHub.PilotFlows.WorkComplete;
using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotApp.Polling;

public sealed class PilotPollingRequestStateReader : IPilotPollingRequestStateReader
{
    private readonly IWorkStartPlcOperations _workStartOperations;
    private readonly IWorkCompletePlcOperations _workCompleteOperations;
    private readonly WorkStartFlowOptions _workStartOptions;
    private readonly WorkCompleteAckOptions _workCompleteOptions;

    public PilotPollingRequestStateReader(
        IWorkStartPlcOperations workStartOperations,
        IWorkCompletePlcOperations workCompleteOperations,
        WorkStartFlowOptions? workStartOptions = null,
        WorkCompleteAckOptions? workCompleteOptions = null)
    {
        _workStartOperations = workStartOperations ?? throw new ArgumentNullException(nameof(workStartOperations));
        _workCompleteOperations = workCompleteOperations ?? throw new ArgumentNullException(nameof(workCompleteOperations));
        _workStartOptions = workStartOptions ?? WorkStartFlowOptions.Default;
        _workCompleteOptions = workCompleteOptions ?? WorkCompleteAckOptions.Default;
    }

    public async ValueTask<PilotPollingRequestState> ReadAsync(CancellationToken cancellationToken = default)
    {
        var startState = await ReadStartStateAsync(cancellationToken).ConfigureAwait(false);
        if (!startState.ReadSucceeded)
        {
            return startState;
        }

        var completeState = await ReadCompleteStateAsync(cancellationToken).ConfigureAwait(false);
        if (!completeState.ReadSucceeded)
        {
            return completeState;
        }

        return startState with
        {
            CompleteRequestActive = completeState.CompleteRequestActive
        };
    }

    private async ValueTask<PilotPollingRequestState> ReadStartStateAsync(CancellationToken cancellationToken)
    {
        await _workStartOperations.EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        var result = await _workStartOperations.ReadWorkStartBlockAsync(cancellationToken).ConfigureAwait(false);
        if (result.Status != WorkStartReadBlockOperationStatus.Success || result.Data is null)
        {
            return new PilotPollingRequestState
            {
                ReadSucceeded = false,
                Message = result.Message ?? "WorkStart request read failed."
            };
        }

        var lotId1 = WorkStartReadBlockInterpreter.ExtractLotId(
            result.Data,
            _workStartOptions.LotId1WordOffset,
            _workStartOptions.LotIdWordLength);
        var lotId2 = WorkStartReadBlockInterpreter.ExtractLotId(
            result.Data,
            _workStartOptions.LotId2WordOffset,
            _workStartOptions.LotIdWordLength);
        var selectedLotId = WorkStartReadBlockInterpreter
            .SelectLotId(lotId1.LotId, lotId2.LotId)
            .SelectedLotId;

        return new PilotPollingRequestState
        {
            StartRequestActive = WorkStartReadBlockInterpreter.IsStartSignalActive(
                result.Data,
                _workStartOptions.StartSignalWordIndex),
            StartLotId = selectedLotId
        };
    }

    private async ValueTask<PilotPollingRequestState> ReadCompleteStateAsync(CancellationToken cancellationToken)
    {
        await _workCompleteOperations.EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        var result = await _workCompleteOperations.ReadWorkCompleteBlockAsync(cancellationToken).ConfigureAwait(false);
        if (result.Status != WorkCompleteReadBlockOperationStatus.Success || result.Data is null)
        {
            return new PilotPollingRequestState
            {
                ReadSucceeded = false,
                Message = result.Message ?? "WorkComplete request read failed."
            };
        }

        return new PilotPollingRequestState
        {
            CompleteRequestActive = WorkCompleteReadBlockInterpreter.IsCompleteSignalActive(
                result.Data,
                _workCompleteOptions.CompleteSignalWordIndex)
        };
    }
}
