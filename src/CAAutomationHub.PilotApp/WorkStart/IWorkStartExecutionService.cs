namespace CAAutomationHub.PilotApp.WorkStart;

public interface IWorkStartExecutionService
{
    ValueTask<WorkStartExecutionResult> ExecuteOnceAsync(
        WorkStartExecutionRequest request,
        CancellationToken cancellationToken = default);
}
