namespace CAAutomationHub.PilotFlows.WorkComplete;

public sealed class WorkCompleteAckOptions
{
    public static WorkCompleteAckOptions Default { get; } = new();

    public int CompleteSignalWordIndex { get; init; } =
        WorkCompleteReadBlockLayout.DefaultCompleteSignalWordIndex;

    public ushort AckOnValue { get; init; } = 1;

    public ushort AckOffValue { get; init; }
}
