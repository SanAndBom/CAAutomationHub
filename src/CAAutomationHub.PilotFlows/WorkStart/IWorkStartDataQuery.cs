namespace CAAutomationHub.PilotFlows.WorkStart;

public interface IWorkStartDataQuery
{
    ValueTask<WorkStartDataQueryResult> QueryAsync(
        string lotId,
        CancellationToken cancellationToken = default);
}
