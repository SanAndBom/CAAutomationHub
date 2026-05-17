namespace CAAutomationHub.PilotFlows.WorkStart;

public sealed class WorkStartReadBlockOperationResult
{
    private readonly byte[]? _data;

    private WorkStartReadBlockOperationResult(
        WorkStartReadBlockOperationStatus status,
        byte[]? data,
        string? message)
    {
        Status = status;
        _data = data is null ? null : (byte[])data.Clone();
        Message = message;
    }

    public WorkStartReadBlockOperationStatus Status { get; }

    public byte[]? Data => _data is null ? null : (byte[])_data.Clone();

    public string? Message { get; }

    public static WorkStartReadBlockOperationResult Success(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new WorkStartReadBlockOperationResult(
            WorkStartReadBlockOperationStatus.Success,
            data,
            message: null);
    }

    public static WorkStartReadBlockOperationResult OperationFailed(string? message = null) =>
        new(
            WorkStartReadBlockOperationStatus.OperationFailed,
            data: null,
            message);

    public static WorkStartReadBlockOperationResult ParseFailed(string? message = null) =>
        new(
            WorkStartReadBlockOperationStatus.ParseFailed,
            data: null,
            message);
}
