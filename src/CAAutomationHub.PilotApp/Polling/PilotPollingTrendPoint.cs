namespace CAAutomationHub.PilotApp.Polling;

public sealed record PilotPollingTrendPoint(
    int SequenceNo,
    DateTimeOffset Timestamp,
    bool IsSuccess,
    WorkRequestKind RequestKind,
    long DurationMs,
    string? SelectedLotId,
    string? ResultStatus,
    string? ErrorCode);
