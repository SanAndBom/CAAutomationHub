using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotApp.WorkStart;

public interface IWorkStartFlowRunner
{
    ValueTask<WorkStartFlowResult> RunAsync(CancellationToken cancellationToken = default);
}
