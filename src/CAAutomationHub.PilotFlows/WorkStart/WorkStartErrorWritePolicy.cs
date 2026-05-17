namespace CAAutomationHub.PilotFlows.WorkStart;

public static class WorkStartErrorWritePolicy
{
    public static bool ShouldWriteErrorCode(WorkStartErrorCode errorCode) =>
        errorCode is WorkStartErrorCode.LotIdEmpty
            or WorkStartErrorCode.DbException
            or WorkStartErrorCode.DbNotFound
            or WorkStartErrorCode.DbMultipleRows
            or WorkStartErrorCode.DbFailed
            or WorkStartErrorCode.PayloadBuildFailed
            or WorkStartErrorCode.BulkWriteFailed
            or WorkStartErrorCode.AckWriteFailed;
}
