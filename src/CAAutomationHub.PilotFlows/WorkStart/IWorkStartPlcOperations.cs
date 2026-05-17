namespace CAAutomationHub.PilotFlows.WorkStart;

public interface IWorkStartPlcOperations
{
    ValueTask EnsureConnectedAsync(CancellationToken cancellationToken = default);

    ValueTask<byte[]> ReadWorkStartBlockAsync(CancellationToken cancellationToken = default);

    ValueTask<bool> WriteProcessPayloadAsync(
        byte[] payload,
        CancellationToken cancellationToken = default);

    ValueTask<bool> WriteStartAckAsync(CancellationToken cancellationToken = default);

    ValueTask WriteErrorCodeBestEffortAsync(
        WorkStartErrorCode errorCode,
        CancellationToken cancellationToken = default);
}
