using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotApp.WorkStart;

public sealed class WorkStartFlowServiceRunner : IWorkStartFlowRunner
{
    private readonly WorkStartFlowService _service;

    public WorkStartFlowServiceRunner(WorkStartFlowService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public ValueTask<WorkStartFlowResult> RunAsync(CancellationToken cancellationToken = default) =>
        _service.RunAsync(cancellationToken);
}
