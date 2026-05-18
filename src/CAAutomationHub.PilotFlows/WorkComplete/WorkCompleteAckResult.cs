namespace CAAutomationHub.PilotFlows.WorkComplete;

public sealed record WorkCompleteAckResult
{
    public required WorkCompleteAckStatus Status { get; init; }

    public bool Succeeded =>
        Status is WorkCompleteAckStatus.AckOnWritten or WorkCompleteAckStatus.AckOffWritten;

    public string? Message { get; init; }

    public static WorkCompleteAckResult AckOnWritten() =>
        new()
        {
            Status = WorkCompleteAckStatus.AckOnWritten
        };

    public static WorkCompleteAckResult AckOffWritten() =>
        new()
        {
            Status = WorkCompleteAckStatus.AckOffWritten
        };

    public static WorkCompleteAckResult WaitingRequestOn() =>
        new()
        {
            Status = WorkCompleteAckStatus.WaitingRequestOn,
            Message = "Work complete request is not active."
        };

    public static WorkCompleteAckResult WaitingRequestOff() =>
        new()
        {
            Status = WorkCompleteAckStatus.WaitingRequestOff,
            Message = "Work complete request is still active."
        };

    public static WorkCompleteAckResult ReadFailed(string? message) =>
        new()
        {
            Status = WorkCompleteAckStatus.ReadFailed,
            Message = message ?? "Work complete request signal read failed."
        };

    public static WorkCompleteAckResult AckWriteFailed(string? message) =>
        new()
        {
            Status = WorkCompleteAckStatus.AckWriteFailed,
            Message = message ?? "Work complete ACK write failed."
        };
}
