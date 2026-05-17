namespace CAAutomationHub.PilotFlows.WorkStart;

public sealed record WorkStartDataQueryResult
{
    public required WorkStartDataQueryStatus Status { get; init; }

    public bool Succeeded => Status == WorkStartDataQueryStatus.Succeeded;

    public required string LotId { get; init; }

    public WorkStartProcessData? ProcessData { get; init; }

    public string? Message { get; init; }

    public static WorkStartDataQueryResult Success(WorkStartProcessData processData)
    {
        ArgumentNullException.ThrowIfNull(processData);

        return new WorkStartDataQueryResult
        {
            Status = WorkStartDataQueryStatus.Succeeded,
            LotId = processData.LotId ?? string.Empty,
            ProcessData = processData,
            Message = null
        };
    }

    public static WorkStartDataQueryResult NotFound(string lotId) =>
        Failure(WorkStartDataQueryStatus.NotFound, lotId, "No row found for LOT ID.");

    public static WorkStartDataQueryResult MultipleRows(string lotId) =>
        Failure(WorkStartDataQueryStatus.MultipleRows, lotId, "Multiple rows found for LOT ID.");

    public static WorkStartDataQueryResult Failed(string lotId, string? message) =>
        Failure(WorkStartDataQueryStatus.Failed, lotId, message ?? "DB query failed.");

    public static WorkStartDataQueryResult DbException(string lotId, string? message) =>
        Failure(WorkStartDataQueryStatus.Exception, lotId, message ?? "DB query exception.");

    private static WorkStartDataQueryResult Failure(
        WorkStartDataQueryStatus status,
        string lotId,
        string message) =>
        new()
        {
            Status = status,
            LotId = lotId,
            ProcessData = null,
            Message = message
        };
}
