namespace CAAutomationHub.PilotComposition.Configuration;

public sealed record PilotPlcTargetConfiguration
{
    public required string TargetId { get; init; }

    public string? DisplayName { get; init; }

    public string? LineName { get; init; }

    public required string Host { get; init; }

    public required int Port { get; init; }

    public required string ReadStartVariable { get; init; }

    public required int ReadWordCount { get; init; }

    public required int StartSignalWordIndex { get; init; }

    public required int CompleteSignalWordIndex { get; init; }

    public required int LotId1WordOffset { get; init; }

    public required int LotId2WordOffset { get; init; }

    public required int LotIdWordLength { get; init; }
}
