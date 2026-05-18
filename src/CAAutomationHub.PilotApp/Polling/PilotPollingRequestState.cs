namespace CAAutomationHub.PilotApp.Polling;

public sealed record PilotPollingRequestState
{
    public bool ReadSucceeded { get; init; } = true;

    public bool StartRequestActive { get; init; }

    public bool CompleteRequestActive { get; init; }

    public string? StartLotId { get; init; }

    public string? Message { get; init; }
}
