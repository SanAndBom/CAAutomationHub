namespace CAAutomationHub.Contracts.Runtime;

public enum PlcPollingState
{
    Idle,
    Polling,
    Delayed,
    Suspended,
    Resetting
}
