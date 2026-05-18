namespace CAAutomationHub.PilotApp.WorkStart;

public sealed record WorkStartExecutionResult
{
    public required bool Succeeded { get; init; }

    public required string Status { get; init; }

    public required string Step { get; init; }

    public required int ErrorCode { get; init; }

    public required string? ErrorCodeName { get; init; }

    public string? Message { get; init; }

    public string? SelectedLotId { get; init; }

    public required bool ErrorWriteExpected { get; init; }

    public required DateTimeOffset StartedAt { get; init; }

    public required DateTimeOffset CompletedAt { get; init; }

    public required TimeSpan Duration { get; init; }
}
