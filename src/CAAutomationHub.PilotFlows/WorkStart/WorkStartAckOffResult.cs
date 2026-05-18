namespace CAAutomationHub.PilotFlows.WorkStart;

public sealed record WorkStartAckOffResult
{
    public required WorkStartAckOffStatus Status { get; init; }

    public bool Succeeded => Status == WorkStartAckOffStatus.AckOffWritten;

    public string? Message { get; init; }

    public static WorkStartAckOffResult AckOffWritten() =>
        new()
        {
            Status = WorkStartAckOffStatus.AckOffWritten
        };

    public static WorkStartAckOffResult WaitingRequestOff() =>
        new()
        {
            Status = WorkStartAckOffStatus.WaitingRequestOff,
            Message = "Work start request is still active."
        };

    public static WorkStartAckOffResult ReadFailed(string? message) =>
        new()
        {
            Status = WorkStartAckOffStatus.ReadFailed,
            Message = message ?? "Work start request signal read failed."
        };

    public static WorkStartAckOffResult AckOffWriteFailed(string? message) =>
        new()
        {
            Status = WorkStartAckOffStatus.AckOffWriteFailed,
            Message = message ?? "Work start ACK OFF write failed."
        };
}
