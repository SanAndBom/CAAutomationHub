namespace CAAutomationHub.Contracts.Runtime;

public enum RuntimeDashboardCommandKind
{
    TestConnection,
    AddOrUpdatePlc,
    DeletePlc,
    StartPlc,
    StopPlc,
    ResetConnection,
    ManualReconnect
}
