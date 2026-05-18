namespace CAAutomationHub.PilotFlows.WorkComplete;

public sealed record WorkCompleteReadBlockOperationResult
{
    public required WorkCompleteReadBlockOperationStatus Status { get; init; }

    public byte[]? Data { get; init; }

    public string? Message { get; init; }

    public static WorkCompleteReadBlockOperationResult Success(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new()
        {
            Status = WorkCompleteReadBlockOperationStatus.Success,
            Data = data.ToArray()
        };
    }

    public static WorkCompleteReadBlockOperationResult OperationFailed(string? message) =>
        new()
        {
            Status = WorkCompleteReadBlockOperationStatus.OperationFailed,
            Message = message
        };

    public static WorkCompleteReadBlockOperationResult ParseFailed(string? message) =>
        new()
        {
            Status = WorkCompleteReadBlockOperationStatus.ParseFailed,
            Message = message
        };
}
