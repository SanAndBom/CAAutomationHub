namespace CAAutomationHub.PilotFlows.Xgt.WorkStart;

public sealed class WorkStartXgtReadOptions
{
    public const string DefaultReadStartVariable = "%DB10000";

    public const int DefaultReadWordCount = 90;

    public static WorkStartXgtReadOptions Default { get; } =
        new(DefaultReadStartVariable, DefaultReadWordCount);

    public WorkStartXgtReadOptions(string readStartVariable, int readWordCount)
    {
        if (string.IsNullOrWhiteSpace(readStartVariable))
        {
            throw new ArgumentException("Read start variable is required.", nameof(readStartVariable));
        }

        if (readWordCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(readWordCount),
                readWordCount,
                "Read word count must be greater than zero.");
        }

        ReadStartVariable = readStartVariable;
        ReadWordCount = readWordCount;
    }

    public string ReadStartVariable { get; }

    public int ReadWordCount { get; }
}
