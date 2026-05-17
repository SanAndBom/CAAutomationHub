namespace CAAutomationHub.PilotFlows.WorkStart;

public enum WorkStartErrorCode
{
    None = 0,
    ReadFailed = 1101,
    ReadParseFailed = 1102,
    StartSignalInactive = 1200,
    LotIdEmpty = 2201,
    DbException = 2300,
    DbNotFound = 2301,
    DbMultipleRows = 2302,
    DbFailed = 2303,
    PayloadBuildFailed = 2400,
    BulkWriteFailed = 2501,
    AckWriteFailed = 2601,
    UnexpectedException = 2999
}
