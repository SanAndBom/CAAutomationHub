namespace CAAutomationHub.PilotApp.Polling;

public sealed record PilotPlcCardStatus
{
    public static PilotPlcCardStatus Unknown { get; } = CreateInitial("-", "-");

    public required string TargetId { get; init; }

    public required string TargetLabel { get; init; }

    public required string DisplayName { get; init; }

    public required string LineName { get; init; }

    public required string HostPort { get; init; }

    public required PilotPlcConnectionStatus ConnectionStatus { get; init; }

    public required string PollingStatus { get; init; }

    public string? LastReadResultStatus { get; init; }

    public required WorkRequestKind LastRequestKind { get; init; }

    public string? SelectedLotId { get; init; }

    public bool StartRequestActive { get; init; }

    public bool CompleteRequestActive { get; init; }

    public bool? StartAckState { get; init; }

    public bool? CompleteAckState { get; init; }

    public string? LastResultStatus { get; init; }

    public string? LastErrorCode { get; init; }

    public DateTimeOffset? LastUpdatedAt { get; init; }

    public static PilotPlcCardStatus CreateInitial(
        string targetId,
        string targetLabel,
        string? displayName = null,
        string? lineName = null,
        string? hostPort = null) => new()
    {
        TargetId = targetId,
        TargetLabel = targetLabel,
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? targetId : displayName,
        LineName = string.IsNullOrWhiteSpace(lineName) ? targetId : lineName,
        HostPort = string.IsNullOrWhiteSpace(hostPort) ? targetLabel : hostPort,
        ConnectionStatus = PilotPlcConnectionStatus.Unknown,
        PollingStatus = PilotPollingStatus.Stopped.ToString(),
        LastRequestKind = WorkRequestKind.None
    };
}
