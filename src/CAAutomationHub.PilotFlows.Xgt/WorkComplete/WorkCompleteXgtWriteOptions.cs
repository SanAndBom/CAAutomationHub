namespace CAAutomationHub.PilotFlows.Xgt.WorkComplete;

public sealed class WorkCompleteXgtWriteOptions
{
    public const string DefaultCompleteAckWriteVariable = "%DB11418";

    public static WorkCompleteXgtWriteOptions Default { get; } =
        new(DefaultCompleteAckWriteVariable);

    public WorkCompleteXgtWriteOptions(string completeAckWriteVariable)
    {
        if (string.IsNullOrWhiteSpace(completeAckWriteVariable))
        {
            throw new ArgumentException(
                "Complete ACK write variable is required.",
                nameof(completeAckWriteVariable));
        }

        CompleteAckWriteVariable = completeAckWriteVariable;
    }

    public string CompleteAckWriteVariable { get; }
}
