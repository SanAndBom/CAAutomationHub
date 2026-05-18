namespace CAAutomationHub.PilotFlows.WorkComplete;

public enum WorkCompleteAckStatus
{
    AckOnWritten,
    AckOffWritten,
    WaitingRequestOn,
    WaitingRequestOff,
    ReadFailed,
    AckWriteFailed
}
