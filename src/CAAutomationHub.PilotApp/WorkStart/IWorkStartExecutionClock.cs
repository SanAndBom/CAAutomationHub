namespace CAAutomationHub.PilotApp.WorkStart;

public interface IWorkStartExecutionClock
{
    DateTimeOffset GetUtcNow();
}
