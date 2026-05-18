namespace CAAutomationHub.PilotApp.WorkStart;

public sealed record WorkStartExecutionRequest(
    string? TargetId = null,
    string? RequestedBy = null,
    string? CorrelationId = null);
