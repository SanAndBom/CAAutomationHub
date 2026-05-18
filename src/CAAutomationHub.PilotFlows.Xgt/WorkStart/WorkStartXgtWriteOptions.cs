namespace CAAutomationHub.PilotFlows.Xgt.WorkStart;

public sealed class WorkStartXgtWriteOptions
{
    public const string DefaultProcessPayloadWriteVariable = "%DB11000";

    public const string DefaultStartAckWriteVariable = "%DB11416";

    public const ushort DefaultStartAckValue = 1;

    public const string DefaultErrorCodeWriteVariable = "%DB11410";

    public static WorkStartXgtWriteOptions Default { get; } =
        new(
            DefaultProcessPayloadWriteVariable,
            DefaultStartAckWriteVariable,
            DefaultStartAckValue,
            DefaultErrorCodeWriteVariable);

    public WorkStartXgtWriteOptions(
        string processPayloadWriteVariable,
        string startAckWriteVariable,
        ushort startAckValue,
        string errorCodeWriteVariable)
    {
        if (string.IsNullOrWhiteSpace(processPayloadWriteVariable))
        {
            throw new ArgumentException(
                "Process payload write variable is required.",
                nameof(processPayloadWriteVariable));
        }

        if (string.IsNullOrWhiteSpace(startAckWriteVariable))
        {
            throw new ArgumentException(
                "Start ACK write variable is required.",
                nameof(startAckWriteVariable));
        }

        if (string.IsNullOrWhiteSpace(errorCodeWriteVariable))
        {
            throw new ArgumentException(
                "Error code write variable is required.",
                nameof(errorCodeWriteVariable));
        }

        ProcessPayloadWriteVariable = processPayloadWriteVariable;
        StartAckWriteVariable = startAckWriteVariable;
        StartAckValue = startAckValue;
        ErrorCodeWriteVariable = errorCodeWriteVariable;
    }

    public string ProcessPayloadWriteVariable { get; }

    public string StartAckWriteVariable { get; }

    public ushort StartAckValue { get; }

    public string ErrorCodeWriteVariable { get; }
}
