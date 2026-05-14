namespace CAAutomationHub.Contracts.Runtime;

public sealed record RuntimeEvent(
    string EventId,
    DateTimeOffset OccurredAt,
    string? PlcId,
    RuntimeEventSeverity Severity,
    RuntimeEventCategory Category,
    string Message,
    string? Status,
    string? Detail);
