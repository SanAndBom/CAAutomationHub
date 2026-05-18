namespace CAAutomationHub.PilotApp.Polling;

public interface IPilotPollingRequestStateReader
{
    ValueTask<PilotPollingRequestState> ReadAsync(CancellationToken cancellationToken = default);
}
