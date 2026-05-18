namespace CAAutomationHub.PilotComposition.WorkStart;

public sealed record WorkStartDemoOptions(
    string SimulatedLotId = "DEMO-LOT-0001",
    bool ShouldSucceed = true,
    DateTimeOffset? StartedAt = null,
    string? FailureMessage = null);
