using System.Buffers.Binary;
using AutomationHub.XgtDriverCore.Client;
using AutomationHub.XgtDriverCore.Protocol;
using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotFlows.Xgt.WorkStart;

public sealed class WorkStartXgtPlcOperations : IWorkStartPlcOperations
{
    private readonly IXgtSession _session;
    private readonly XgtReadRequest _readRequest;
    private readonly WorkStartXgtWriteOptions _writeOptions;

    public WorkStartXgtPlcOperations(IXgtSession session)
        : this(session, WorkStartXgtReadOptions.Default)
    {
    }

    public WorkStartXgtPlcOperations(IXgtSession session, WorkStartXgtReadOptions options)
        : this(session, options, WorkStartXgtWriteOptions.Default)
    {
    }

    public WorkStartXgtPlcOperations(
        IXgtSession session,
        WorkStartXgtReadOptions readOptions,
        WorkStartXgtWriteOptions writeOptions)
        : this(session, CreateReadRequest(readOptions), writeOptions)
    {
    }

    public WorkStartXgtPlcOperations(IXgtSession session, XgtReadRequest readRequest)
        : this(session, readRequest, WorkStartXgtWriteOptions.Default)
    {
    }

    public WorkStartXgtPlcOperations(
        IXgtSession session,
        XgtReadRequest readRequest,
        WorkStartXgtWriteOptions writeOptions)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _readRequest = readRequest ?? throw new ArgumentNullException(nameof(readRequest));
        _writeOptions = writeOptions ?? throw new ArgumentNullException(nameof(writeOptions));
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

    public async ValueTask<bool> WriteProcessPayloadAsync(
        byte[] payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var request = CreateWriteRequest(
            _writeOptions.ProcessPayloadWriteVariable,
            payload.ToArray());

        return await TryWriteAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<bool> WriteStartAckAsync(CancellationToken cancellationToken = default)
    {
        var request = CreateWriteRequest(
            _writeOptions.StartAckWriteVariable,
            CreateUInt16LittleEndianPayload(_writeOptions.StartAckValue));

        return await TryWriteAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask WriteErrorCodeBestEffortAsync(
        WorkStartErrorCode errorCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateWriteRequest(
                _writeOptions.ErrorCodeWriteVariable,
                CreateUInt16LittleEndianPayload(checked((ushort)errorCode)));

            await _session.WriteAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // WorkStart error writes are best-effort and must not replace the primary failure result.
        }
    }

    private async ValueTask<bool> TryWriteAsync(
        XgtWriteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _session.WriteAsync(request, cancellationToken).ConfigureAwait(false);
            return response.Status.IsAck;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    private static XgtWriteRequest CreateWriteRequest(string variable, byte[] payload) =>
        new(
            XgtDataType.Continuous,
            new[] { new XgtVariableBlock(variable, payload) });

    private static byte[] CreateUInt16LittleEndianPayload(ushort value)
    {
        var payload = new byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, value);
        return payload;
    }

    private static XgtReadRequest CreateReadRequest(WorkStartXgtReadOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var continuousByteLength = checked((ushort)(options.ReadWordCount * 2));

        return new XgtReadRequest(
            XgtDataType.Continuous,
            new[] { new XgtVariableBlock(options.ReadStartVariable) },
            continuousByteLength);
    }
}
