namespace CAAutomationHub.PilotFlows.Xgt.WorkComplete;

public sealed class WorkCompleteXgtReadOptions
{
    public const string DefaultReadStartVariable = "%DB10000";

    public const int DefaultReadWordCount = 90;

    public static WorkCompleteXgtReadOptions Default { get; } =
        new(DefaultReadStartVariable, DefaultReadWordCount);

    public WorkCompleteXgtReadOptions(string readStartVariable, int readWordCount)
    {
        if (string.IsNullOrWhiteSpace(readStartVariable))
        {
            throw new ArgumentException(
                "WorkComplete read start variable is required.",
                nameof(readStartVariable));
        }

        if (readWordCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(readWordCount),
                readWordCount,
                "WorkComplete read word count must be positive.");
        }

        ReadStartVariable = readStartVariable;
        ReadWordCount = readWordCount;
    }

    public string ReadStartVariable { get; }

    public int ReadWordCount { get; }
}
