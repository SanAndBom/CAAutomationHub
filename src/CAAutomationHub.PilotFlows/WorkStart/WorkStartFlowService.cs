namespace CAAutomationHub.PilotFlows.WorkStart;

public sealed class WorkStartFlowService
{
    private readonly IWorkStartPlcOperations _plcOperations;
    private readonly IWorkStartDataQuery _dataQuery;
    private readonly WorkStartFlowOptions _options;

    public WorkStartFlowService(
        IWorkStartPlcOperations plcOperations,
        IWorkStartDataQuery dataQuery,
        WorkStartFlowOptions? options = null)
    {
        _plcOperations = plcOperations ?? throw new ArgumentNullException(nameof(plcOperations));
        _dataQuery = dataQuery ?? throw new ArgumentNullException(nameof(dataQuery));
        _options = options ?? WorkStartFlowOptions.Default;
    }

    public async ValueTask<WorkStartFlowResult> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _plcOperations.EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

            byte[] readBlockBytes;
            try
            {
                readBlockBytes = await _plcOperations
                    .ReadWorkStartBlockAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return await FailAsync(
                    WorkStartStep.GroupRead,
                    WorkStartErrorCode.ReadFailed,
                    ex.Message,
                    selectedLotId: null,
                    cancellationToken).ConfigureAwait(false);
            }

            WorkStartLotIdSelectionResult lotIdSelection;
            try
            {
                if (!WorkStartReadBlockInterpreter.IsStartSignalActive(readBlockBytes, _options.StartSignalWordIndex))
                {
                    return await FailAsync(
                        WorkStartStep.StartSignal,
                        WorkStartErrorCode.StartSignalInactive,
                        "Work start signal is not active.",
                        selectedLotId: null,
                        cancellationToken).ConfigureAwait(false);
                }

                var lotId1 = WorkStartReadBlockInterpreter.ExtractLotId(
                    readBlockBytes,
                    _options.LotId1WordOffset,
                    _options.LotIdWordLength);
                var lotId2 = WorkStartReadBlockInterpreter.ExtractLotId(
                    readBlockBytes,
                    _options.LotId2WordOffset,
                    _options.LotIdWordLength);

                lotIdSelection = WorkStartReadBlockInterpreter.SelectLotId(lotId1.LotId, lotId2.LotId);
            }
            catch (Exception ex)
            {
                return await FailAsync(
                    WorkStartStep.GroupReadParse,
                    WorkStartErrorCode.ReadParseFailed,
                    ex.Message,
                    selectedLotId: null,
                    cancellationToken).ConfigureAwait(false);
            }

            if (!lotIdSelection.HasSelection)
            {
                return await FailAsync(
                    WorkStartStep.LotId,
                    WorkStartErrorCode.LotIdEmpty,
                    "Both LOT IDs are empty.",
                    selectedLotId: null,
                    cancellationToken).ConfigureAwait(false);
            }

            var selectedLotId = lotIdSelection.SelectedLotId!;
            WorkStartDataQueryResult queryResult;
            try
            {
                queryResult = await _dataQuery.QueryAsync(selectedLotId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return await FailAsync(
                    WorkStartStep.DbQuery,
                    WorkStartErrorCode.DbException,
                    ex.Message,
                    selectedLotId,
                    cancellationToken).ConfigureAwait(false);
            }

            var queryFailure = MapQueryFailure(queryResult);
            if (queryFailure is not null)
            {
                return await FailAsync(
                    WorkStartStep.DbQuery,
                    queryFailure.Value.ErrorCode,
                    queryResult.Message ?? queryFailure.Value.Message,
                    selectedLotId,
                    cancellationToken).ConfigureAwait(false);
            }

            WorkStartPayloadBuildResult payloadBuildResult;
            try
            {
                var processData = queryResult.ProcessData! with { LotId = selectedLotId };
                payloadBuildResult = WorkStartProcessDataPayloadBuilder.Build(
                    processData,
                    _options.PayloadBuildOptions);
            }
            catch (Exception ex)
            {
                return await FailAsync(
                    WorkStartStep.PayloadBuild,
                    WorkStartErrorCode.PayloadBuildFailed,
                    ex.Message,
                    selectedLotId,
                    cancellationToken).ConfigureAwait(false);
            }

            bool payloadWritten;
            try
            {
                payloadWritten = await _plcOperations
                    .WriteProcessPayloadAsync(payloadBuildResult.PayloadBytes, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return await FailAsync(
                    WorkStartStep.BulkWrite,
                    WorkStartErrorCode.BulkWriteFailed,
                    ex.Message,
                    selectedLotId,
                    cancellationToken).ConfigureAwait(false);
            }

            if (!payloadWritten)
            {
                return await FailAsync(
                    WorkStartStep.BulkWrite,
                    WorkStartErrorCode.BulkWriteFailed,
                    "PLC bulk write failed.",
                    selectedLotId,
                    cancellationToken).ConfigureAwait(false);
            }

            bool ackWritten;
            try
            {
                ackWritten = await _plcOperations.WriteStartAckAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return await FailAsync(
                    WorkStartStep.AckWrite,
                    WorkStartErrorCode.AckWriteFailed,
                    ex.Message,
                    selectedLotId,
                    cancellationToken).ConfigureAwait(false);
            }

            if (!ackWritten)
            {
                return await FailAsync(
                    WorkStartStep.AckWrite,
                    WorkStartErrorCode.AckWriteFailed,
                    "ACK write failed.",
                    selectedLotId,
                    cancellationToken).ConfigureAwait(false);
            }

            return WorkStartFlowResult.Success(selectedLotId);
        }
        catch (Exception ex)
        {
            return await FailAsync(
                WorkStartStep.Exception,
                WorkStartErrorCode.UnexpectedException,
                ex.Message,
                selectedLotId: null,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask<WorkStartFlowResult> FailAsync(
        WorkStartStep step,
        WorkStartErrorCode errorCode,
        string? message,
        string? selectedLotId,
        CancellationToken cancellationToken)
    {
        var result = WorkStartFlowResult.Failure(step, errorCode, message, selectedLotId);
        if (!result.ErrorWriteExpected)
        {
            return result;
        }

        try
        {
            await _plcOperations.WriteErrorCodeBestEffortAsync(errorCode, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Best effort only. Preserve the primary WorkStart failure result.
        }

        return result;
    }

    private static QueryFailure? MapQueryFailure(WorkStartDataQueryResult queryResult)
    {
        if (queryResult.Succeeded && queryResult.ProcessData is not null)
        {
            return null;
        }

        return queryResult.Status switch
        {
            WorkStartDataQueryStatus.NotFound => new QueryFailure(
                WorkStartErrorCode.DbNotFound,
                "No row found for LOT ID."),
            WorkStartDataQueryStatus.MultipleRows => new QueryFailure(
                WorkStartErrorCode.DbMultipleRows,
                "Multiple rows found for LOT ID."),
            WorkStartDataQueryStatus.Exception => new QueryFailure(
                WorkStartErrorCode.DbException,
                "DB query exception."),
            _ => new QueryFailure(
                WorkStartErrorCode.DbFailed,
                "DB query failed.")
        };
    }

    private readonly record struct QueryFailure(WorkStartErrorCode ErrorCode, string Message);
}
