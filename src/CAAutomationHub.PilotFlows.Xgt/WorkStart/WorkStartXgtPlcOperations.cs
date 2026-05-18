using AutomationHub.XgtDriverCore.Client;
using AutomationHub.XgtDriverCore.Protocol;
using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotFlows.Xgt.WorkStart;

public sealed class WorkStartXgtPlcOperations : IWorkStartPlcOperations
{
    private readonly IXgtSession _session;
    private readonly XgtReadRequest _readRequest;

    public WorkStartXgtPlcOperations(IXgtSession session, XgtReadRequest readRequest)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _readRequest = readRequest ?? throw new ArgumentNullException(nameof(readRequest));
    }

    public async ValueTask EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        if (_session.IsConnected)
        {
            return;
        }

        await _session.ConnectAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<WorkStartReadBlockOperationResult> ReadWorkStartBlockAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _session.ReadAsync(_readRequest, cancellationToken).ConfigureAwait(false);
            return WorkStartXgtReadResultMapper.Map(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return WorkStartReadBlockOperationResult.OperationFailed(
                $"XGT read operation failed: {ex.Message}");
        }
    }

    public ValueTask<bool> WriteProcessPayloadAsync(
        byte[] payload,
        CancellationToken cancellationToken = default)
    {
        throw CreateReadSkeletonException();
    }

    public ValueTask<bool> WriteStartAckAsync(CancellationToken cancellationToken = default)
    {
        throw CreateReadSkeletonException();
    }

    public ValueTask WriteErrorCodeBestEffortAsync(
        WorkStartErrorCode errorCode,
        CancellationToken cancellationToken = default)
    {
        throw CreateReadSkeletonException();
    }

    private static NotSupportedException CreateReadSkeletonException() =>
        new("WorkStart XGT adapter is read-only in AH-PILOT-12. Write, ACK, and error writer paths are not implemented.");
}
