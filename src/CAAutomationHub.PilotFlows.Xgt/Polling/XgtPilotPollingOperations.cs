using CAAutomationHub.PilotFlows.WorkComplete;
using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotFlows.Xgt.Polling;

public sealed record XgtPilotPollingOperations(
    IWorkStartPlcOperations StartAckOnOperations,
    IWorkStartPlcOperations StartAckOffOperations,
    IWorkCompletePlcOperations CompleteOperations);
