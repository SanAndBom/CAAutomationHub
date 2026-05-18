namespace CAAutomationHub.PilotApp.Polling;

public enum PilotPollingStatus
{
    Stopped,
    Running,
    Idle,
    WorkStartProcessed,
    WorkStartAckOffWritten,
    WorkCompleteAckOnWritten,
    WorkCompleteAckOffWritten,
    Failed
}
