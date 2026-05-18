using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotSmoke;

public static class WorkStartReadOnlySmoke
{
    public static async ValueTask<PilotSmokeReadOnlyResult> RunAsync(
        IWorkStartPlcOperations operations,
        PilotSmokeReadLayout layout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operations);
        ArgumentNullException.ThrowIfNull(layout);

        try
        {
            await operations.EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return PilotSmokeReadOnlyResult.ConnectionFailed(ex.Message);
        }

        WorkStartReadBlockOperationResult readResult;
        try
        {
            readResult = await operations.ReadWorkStartBlockAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return PilotSmokeReadOnlyResult.ReadFailed(ex.Message);
        }

        if (readResult.Status != WorkStartReadBlockOperationStatus.Success || readResult.Data is null)
        {
            return PilotSmokeReadOnlyResult.ReadFailed(readResult.Message ?? "WorkStart read failed.");
        }

        var data = readResult.Data;
        var lotId1 = WorkStartReadBlockInterpreter.ExtractLotId(
            data,
            layout.LotId1WordOffset,
            layout.LotIdWordLength);
        var lotId2 = WorkStartReadBlockInterpreter.ExtractLotId(
            data,
            layout.LotId2WordOffset,
            layout.LotIdWordLength);
        var selectedLotId = WorkStartReadBlockInterpreter
            .SelectLotId(lotId1.LotId, lotId2.LotId)
            .SelectedLotId;

        return PilotSmokeReadOnlyResult.Success(
            WorkStartReadBlockInterpreter.IsStartSignalActive(data, layout.StartSignalWordIndex),
            lotId1.LotId,
            lotId2.LotId,
            selectedLotId,
            data.Length);
    }
}

public sealed record PilotSmokeReadOnlyResult(
    bool ConnectionSucceeded,
    bool ReadSucceeded,
    bool StartRequestActive,
    string? LotId1,
    string? LotId2,
    string? SelectedLotId,
    int RawLength,
    string? Message)
{
    public static PilotSmokeReadOnlyResult ConnectionFailed(string message) =>
        new(
            ConnectionSucceeded: false,
            ReadSucceeded: false,
            StartRequestActive: false,
            LotId1: null,
            LotId2: null,
            SelectedLotId: null,
            RawLength: 0,
            Message: message);

    public static PilotSmokeReadOnlyResult ReadFailed(string message) =>
        new(
            ConnectionSucceeded: true,
            ReadSucceeded: false,
            StartRequestActive: false,
            LotId1: null,
            LotId2: null,
            SelectedLotId: null,
            RawLength: 0,
            Message: message);

    public static PilotSmokeReadOnlyResult Success(
        bool startRequestActive,
        string lotId1,
        string lotId2,
        string? selectedLotId,
        int rawLength) =>
        new(
            ConnectionSucceeded: true,
            ReadSucceeded: true,
            StartRequestActive: startRequestActive,
            LotId1: lotId1,
            LotId2: lotId2,
            SelectedLotId: selectedLotId,
            RawLength: rawLength,
            Message: null);
}
