namespace CAAutomationHub.PilotFlows.WorkStart;

public enum WorkStartStep
{
    Completed,
    GroupRead,
    GroupReadParse,
    StartSignal,
    LotId,
    DbQuery,
    PayloadBuild,
    BulkWrite,
    AckWrite,
    Exception
}
