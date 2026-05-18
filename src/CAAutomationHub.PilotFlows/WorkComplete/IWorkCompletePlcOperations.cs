namespace CAAutomationHub.PilotFlows.WorkComplete;

public interface IWorkCompletePlcOperations
{
    ValueTask EnsureConnectedAsync(CancellationToken cancellationToken = default);

    ValueTask<WorkCompleteReadBlockOperationResult> ReadWorkCompleteBlockAsync(
        CancellationToken cancellationToken = default);

    ValueTask<bool> WriteCompleteAckAsync(
        ushort value,
        CancellationToken cancellationToken = default);
}
