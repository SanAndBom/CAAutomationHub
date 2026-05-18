using CAAutomationHub.PilotApp.WorkStart;
using CAAutomationHub.PilotFlows.WorkComplete;
using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotApp.Polling;

public sealed class PilotPollingFlowPort : IPilotPollingFlowPort
{
    private readonly IPilotPollingRequestStateReader _requestStateReader;
    private readonly IWorkStartExecutionService _workStartExecutionService;
    private readonly WorkStartAckOffService _workStartAckOffService;
    private readonly WorkCompleteAckService _workCompleteAckService;

    public PilotPollingFlowPort(
        IPilotPollingRequestStateReader requestStateReader,
        IWorkStartExecutionService workStartExecutionService,
        WorkStartAckOffService workStartAckOffService,
        WorkCompleteAckService workCompleteAckService)
    {
        _requestStateReader = requestStateReader ?? throw new ArgumentNullException(nameof(requestStateReader));
        _workStartExecutionService = workStartExecutionService ?? throw new ArgumentNullException(nameof(workStartExecutionService));
        _workStartAckOffService = workStartAckOffService ?? throw new ArgumentNullException(nameof(workStartAckOffService));
        _workCompleteAckService = workCompleteAckService ?? throw new ArgumentNullException(nameof(workCompleteAckService));
    }

    public ValueTask<PilotPollingRequestState> ReadRequestStateAsync(
        CancellationToken cancellationToken = default) =>
        _requestStateReader.ReadAsync(cancellationToken);

    public ValueTask<WorkStartExecutionResult> ExecuteWorkStartAsync(
        WorkStartExecutionRequest request,
        CancellationToken cancellationToken = default) =>
        _workStartExecutionService.ExecuteOnceAsync(request, cancellationToken);

    public ValueTask<WorkStartAckOffResult> ClearWorkStartAckAsync(CancellationToken cancellationToken = default) =>
        _workStartAckOffService.AckOffAsync(cancellationToken);

    public ValueTask<WorkCompleteAckResult> WriteWorkCompleteAckOnAsync(CancellationToken cancellationToken = default) =>
        _workCompleteAckService.AckOnAsync(cancellationToken);

    public ValueTask<WorkCompleteAckResult> ClearWorkCompleteAckAsync(CancellationToken cancellationToken = default) =>
        _workCompleteAckService.AckOffAsync(cancellationToken);
}
