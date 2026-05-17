namespace CAAutomationHub.PilotFlows.WorkStart;

public static class WorkStartStepExtensions
{
    public static string ToCode(this WorkStartStep step) =>
        step switch
        {
            WorkStartStep.Completed => "completed",
            WorkStartStep.GroupRead => "group-read",
            WorkStartStep.GroupReadParse => "group-read-parse",
            WorkStartStep.StartSignal => "start-signal",
            WorkStartStep.LotId => "lotid",
            WorkStartStep.DbQuery => "db-query",
            WorkStartStep.PayloadBuild => "payload-build",
            WorkStartStep.BulkWrite => "bulk-write",
            WorkStartStep.AckWrite => "ack-write",
            WorkStartStep.Exception => "exception",
            _ => throw new ArgumentOutOfRangeException(nameof(step), step, "Unknown WorkStart step.")
        };
}
