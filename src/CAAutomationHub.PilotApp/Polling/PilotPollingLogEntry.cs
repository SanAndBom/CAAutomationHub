namespace CAAutomationHub.PilotApp.Polling;

public sealed record PilotPollingLogEntry(
    DateTimeOffset OccurredAt,
    WorkRequestKind RequestKind,
    string Status,
    string? Message);
