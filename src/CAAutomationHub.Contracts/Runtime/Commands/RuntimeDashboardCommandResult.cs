namespace CAAutomationHub.Contracts.Runtime;

public sealed record RuntimeDashboardCommandResult(
    string CommandId,
    bool Success,
    string Status,
    string Message,
    string? PlcId,
    string? ErrorCode,
    DateTimeOffset CompletedAt);
