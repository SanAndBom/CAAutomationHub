using CAAutomationHub.PilotApp.WorkStart;
using CAAutomationHub.PilotFlows.WorkComplete;
using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotApp.Polling;

public interface IPilotPollingFlowPort
{
    ValueTask<PilotPollingRequestState> ReadRequestStateAsync(CancellationToken cancellationToken = default);

    ValueTask<WorkStartExecutionResult> ExecuteWorkStartAsync(
        WorkStartExecutionRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<WorkStartAckOffResult> ClearWorkStartAckAsync(CancellationToken cancellationToken = default);

    ValueTask<WorkCompleteAckResult> WriteWorkCompleteAckOnAsync(CancellationToken cancellationToken = default);

    ValueTask<WorkCompleteAckResult> ClearWorkCompleteAckAsync(CancellationToken cancellationToken = default);
}
