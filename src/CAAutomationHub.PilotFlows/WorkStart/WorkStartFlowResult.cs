namespace CAAutomationHub.PilotFlows.WorkStart;

public sealed record WorkStartFlowResult
{
    public required WorkStartFlowStatus Status { get; init; }

    public bool Succeeded => Status == WorkStartFlowStatus.Succeeded;

    public required WorkStartStep Step { get; init; }

    public required WorkStartErrorCode ErrorCode { get; init; }

    public string? Message { get; init; }

    public string? SelectedLotId { get; init; }

    public bool ErrorWriteExpected => WorkStartErrorWritePolicy.ShouldWriteErrorCode(ErrorCode);

    public static WorkStartFlowResult Success(string? selectedLotId) =>
        new()
        {
            Status = WorkStartFlowStatus.Succeeded,
            Step = WorkStartStep.Completed,
            ErrorCode = WorkStartErrorCode.None,
            Message = null,
            SelectedLotId = selectedLotId
        };

    public static WorkStartFlowResult Failure(
        WorkStartStep step,
        WorkStartErrorCode errorCode,
        string? message,
        string? selectedLotId) =>
        new()
        {
            Status = WorkStartFlowStatus.Failed,
            Step = step,
            ErrorCode = errorCode,
            Message = message,
            SelectedLotId = selectedLotId
        };
}
