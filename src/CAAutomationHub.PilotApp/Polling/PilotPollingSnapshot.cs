namespace CAAutomationHub.PilotApp.Polling;

public sealed record PilotPollingSnapshot
{
    public static PilotPollingSnapshot Initial { get; } = new()
    {
        IsRunning = false,
        Status = PilotPollingStatus.Stopped,
        LastRequestKind = WorkRequestKind.None,
        LastUpdatedAt = null,
        LogEntries = []
    };

    public required bool IsRunning { get; init; }

    public required PilotPollingStatus Status { get; init; }

    public required WorkRequestKind LastRequestKind { get; init; }

    public string? LastSelectedLotId { get; init; }

    public bool LastStartRequestActive { get; init; }

    public bool LastCompleteRequestActive { get; init; }

    public bool? LastStartAckState { get; init; }

    public bool? LastCompleteAckState { get; init; }

    public string? LastResultStatus { get; init; }

    public string? LastErrorCode { get; init; }

    public string? LastMessage { get; init; }

    public DateTimeOffset? LastUpdatedAt { get; init; }

    public required IReadOnlyList<PilotPollingLogEntry> LogEntries { get; init; }
}
