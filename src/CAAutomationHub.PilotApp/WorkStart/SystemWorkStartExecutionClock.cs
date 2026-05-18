namespace CAAutomationHub.PilotApp.WorkStart;

public sealed class SystemWorkStartExecutionClock : IWorkStartExecutionClock
{
    public static SystemWorkStartExecutionClock Instance { get; } = new();

    private SystemWorkStartExecutionClock()
    {
    }

    public DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;
}
