using System.Buffers.Binary;
using AutomationHub.XgtDriverCore.Client;
using AutomationHub.XgtDriverCore.Protocol;
using CAAutomationHub.PilotFlows.WorkComplete;

namespace CAAutomationHub.PilotFlows.Xgt.WorkComplete;

public sealed class WorkCompleteXgtPlcOperations : IWorkCompletePlcOperations
{
    private readonly IXgtSession _session;
    private readonly XgtReadRequest _readRequest;
    private readonly WorkCompleteXgtWriteOptions _writeOptions;

    public WorkCompleteXgtPlcOperations(IXgtSession session)
        : this(session, WorkCompleteXgtReadOptions.Default)
    {
    }

    public WorkCompleteXgtPlcOperations(
        IXgtSession session,
        WorkCompleteXgtReadOptions readOptions)
        : this(session, readOptions, WorkCompleteXgtWriteOptions.Default)
    {
    }

    public WorkCompleteXgtPlcOperations(
        IXgtSession session,
        WorkCompleteXgtReadOptions readOptions,
        WorkCompleteXgtWriteOptions writeOptions)
        : this(session, CreateReadRequest(readOptions), writeOptions)
    {
    }

    public WorkCompleteXgtPlcOperations(
        IXgtSession session,
        XgtReadRequest readRequest,
        WorkCompleteXgtWriteOptions writeOptions)
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

    public async ValueTask<WorkCompleteReadBlockOperationResult> ReadWorkCompleteBlockAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _session.ReadAsync(_readRequest, cancellationToken).ConfigureAwait(false);
            return MapReadResponse(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return WorkCompleteReadBlockOperationResult.OperationFailed(
                $"XGT read operation failed: {ex.Message}");
        }
    }

    public async ValueTask<bool> WriteCompleteAckAsync(
        ushort value,
        CancellationToken cancellationToken = default)
    {
        var payload = new byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, value);

        var request = new XgtWriteRequest(
            XgtDataType.Continuous,
            new[] { new XgtVariableBlock(_writeOptions.CompleteAckWriteVariable, payload) });

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

    private static WorkCompleteReadBlockOperationResult MapReadResponse(XgtReadResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.Status.IsNak)
        {
            return WorkCompleteReadBlockOperationResult.OperationFailed("XGT read returned NAK.");
        }

        if (response.VariableBlocks.Count == 0)
        {
            return WorkCompleteReadBlockOperationResult.ParseFailed(
                "XGT ACK read response did not include a data block.");
        }

        var data = response.VariableBlocks[0].Data;
        if (data is null || data.Length == 0)
        {
            return WorkCompleteReadBlockOperationResult.ParseFailed(
                "XGT ACK read response did not include readable data block bytes.");
        }

        return WorkCompleteReadBlockOperationResult.Success(data);
    }

    private static XgtReadRequest CreateReadRequest(WorkCompleteXgtReadOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var continuousByteLength = checked((ushort)(options.ReadWordCount * 2));

        return new XgtReadRequest(
            XgtDataType.Continuous,
            new[] { new XgtVariableBlock(options.ReadStartVariable) },
            continuousByteLength);
    }
}
