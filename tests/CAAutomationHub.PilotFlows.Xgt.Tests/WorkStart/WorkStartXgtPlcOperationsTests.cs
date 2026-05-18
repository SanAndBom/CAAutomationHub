using AutomationHub.XgtDriverCore.Client;
using AutomationHub.XgtDriverCore.Protocol;
using CAAutomationHub.PilotFlows.WorkStart;
using CAAutomationHub.PilotFlows.Xgt.WorkStart;

namespace CAAutomationHub.PilotFlows.Xgt.Tests.WorkStart;

public sealed class WorkStartXgtPlcOperationsTests
{
    [Fact]
    public void WorkStartXgtReadOptions_Default_UsesPilotBaseline()
    {
        var options = WorkStartXgtReadOptions.Default;

        Assert.Equal("%DB10000", options.ReadStartVariable);
        Assert.Equal(90, options.ReadWordCount);
    }

    [Fact]
    public void WorkStartXgtWriteOptions_Default_UsesPilotBaseline()
    {
        var options = WorkStartXgtWriteOptions.Default;

        Assert.Equal("%DB11000", options.ProcessPayloadWriteVariable);
        Assert.Equal("%DB11416", options.StartAckWriteVariable);
        Assert.Equal((ushort)1, options.StartAckValue);
        Assert.Equal("%DB11410", options.ErrorCodeWriteVariable);
    }

    [Theory]
    [InlineData("", "%DB11416", "%DB11410")]
    [InlineData(" ", "%DB11416", "%DB11410")]
    [InlineData("%DB11000", "", "%DB11410")]
    [InlineData("%DB11000", " ", "%DB11410")]
    [InlineData("%DB11000", "%DB11416", "")]
    [InlineData("%DB11000", "%DB11416", " ")]
    public void WorkStartXgtWriteOptions_RejectsEmptyVariables(
        string processPayloadWriteVariable,
        string startAckWriteVariable,
        string errorCodeWriteVariable)
    {
        Assert.Throws<ArgumentException>(
            () => new WorkStartXgtWriteOptions(
                processPayloadWriteVariable,
                startAckWriteVariable,
                startAckValue: 1,
                errorCodeWriteVariable));
    }

    [Fact]
    public void WorkStartXgtWriteOptions_RejectsNullVariables()
    {
        Assert.Throws<ArgumentException>(
            () => new WorkStartXgtWriteOptions(
                null!,
                "%DB11416",
                startAckValue: 1,
                "%DB11410"));
        Assert.Throws<ArgumentException>(
            () => new WorkStartXgtWriteOptions(
                "%DB11000",
                null!,
                startAckValue: 1,
                "%DB11410"));
        Assert.Throws<ArgumentException>(
            () => new WorkStartXgtWriteOptions(
                "%DB11000",
                "%DB11416",
                startAckValue: 1,
                null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void WorkStartXgtReadOptions_RejectsEmptyReadStartVariable(string readStartVariable)
    {
        Assert.Throws<ArgumentException>(
            () => new WorkStartXgtReadOptions(readStartVariable, readWordCount: 90));
    }

    [Fact]
    public void WorkStartXgtReadOptions_RejectsNullReadStartVariable()
    {
        Assert.Throws<ArgumentException>(
            () => new WorkStartXgtReadOptions(null!, readWordCount: 90));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void WorkStartXgtReadOptions_RejectsNonPositiveReadWordCount(int readWordCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new WorkStartXgtReadOptions("%DB10000", readWordCount));
    }

    [Fact]
    public async Task EnsureConnectedAsync_ConnectsDisconnectedSession()
    {
        var session = new FakeXgtSession
        {
            IsConnectedValue = false,
            ReadResponse = AckWithData(1, 2, 3)
        };
        var operations = CreateOperations(session);

        await operations.EnsureConnectedAsync();

        Assert.Equal(1, session.ConnectCallCount);
    }

    [Fact]
    public async Task ReadWorkStartBlockAsync_MapsAckReadWithDataToSuccess()
    {
        var source = new byte[] { 1, 2, 3 };
        var session = new FakeXgtSession
        {
            IsConnectedValue = true,
            ReadResponse = AckWithData(source)
        };
        var operations = CreateOperations(session);

        var result = await operations.ReadWorkStartBlockAsync();
        source[0] = 9;

        Assert.Equal(WorkStartReadBlockOperationStatus.Success, result.Status);
        Assert.Equal(new byte[] { 1, 2, 3 }, result.Data);
        Assert.Null(result.Message);
        Assert.Same(DefaultReadRequest, session.LastReadRequest);
    }

    [Fact]
    public async Task ReadWorkStartBlockAsync_UsesOptionsToBuildReadRequest()
    {
        var session = new FakeXgtSession
        {
            IsConnectedValue = true,
            ReadResponse = AckWithData(1, 2, 3)
        };
        var options = new WorkStartXgtReadOptions("%DB12000", readWordCount: 7);
        var operations = new WorkStartXgtPlcOperations(session, options);

        await operations.ReadWorkStartBlockAsync();

        var request = Assert.IsType<XgtReadRequest>(session.LastReadRequest);
        Assert.Equal(XgtDataType.Continuous, request.DataType);
        Assert.Equal((ushort)14, request.ContinuousByteLength);
        var block = Assert.Single(request.VariableBlocks);
        Assert.Equal("%DB12000", block.VariableName);
    }

    [Fact]
    public async Task ReadWorkStartBlockAsync_MapsNakToOperationFailed()
    {
        var session = new FakeXgtSession
        {
            IsConnectedValue = true,
            ReadResponse = new XgtReadResponse(
                XgtDataType.Continuous,
                XgtErrorStatus.Nak(0x0001, 0x1234),
                Array.Empty<XgtVariableBlock>())
        };
        var operations = CreateOperations(session);

        var result = await operations.ReadWorkStartBlockAsync();

        Assert.Equal(WorkStartReadBlockOperationStatus.OperationFailed, result.Status);
        Assert.Null(result.Data);
        Assert.Contains("XGT read returned NAK", result.Message);
    }

    [Fact]
    public async Task ReadWorkStartBlockAsync_MapsReadExceptionToOperationFailed()
    {
        var session = new FakeXgtSession
        {
            IsConnectedValue = true,
            ReadException = new InvalidOperationException("forced read failure")
        };
        var operations = CreateOperations(session);

        var result = await operations.ReadWorkStartBlockAsync();

        Assert.Equal(WorkStartReadBlockOperationStatus.OperationFailed, result.Status);
        Assert.Null(result.Data);
        Assert.Contains("forced read failure", result.Message);
    }

    [Fact]
    public async Task ReadWorkStartBlockAsync_MapsAckWithoutDataToParseFailed()
    {
        var session = new FakeXgtSession
        {
            IsConnectedValue = true,
            ReadResponse = new XgtReadResponse(
                XgtDataType.Continuous,
                XgtErrorStatus.Ack(),
                new[] { new XgtVariableBlock("%MB100") })
        };
        var operations = CreateOperations(session);

        var result = await operations.ReadWorkStartBlockAsync();

        Assert.Equal(WorkStartReadBlockOperationStatus.ParseFailed, result.Status);
        Assert.Null(result.Data);
        Assert.Contains("data block", result.Message);
    }

    [Fact]
    public void WorkStartReadBlockOperationResult_DoesNotExposeXgtClassification()
    {
        var publicPropertyTypes = typeof(WorkStartReadBlockOperationResult)
            .GetProperties()
            .Select(property => property.PropertyType);

        Assert.DoesNotContain(
            publicPropertyTypes,
            type => type.FullName?.StartsWith("AutomationHub.XgtDriverCore", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task WriteProcessPayloadAsync_UsesProcessPayloadWriteVariable()
    {
        var payload = new byte[] { 0x10, 0x20, 0x30 };
        var session = new FakeXgtSession { IsConnectedValue = true };
        var writeOptions = new WorkStartXgtWriteOptions(
            "%DB12000",
            "%DB11416",
            startAckValue: 1,
            "%DB11410");
        var operations = CreateOperations(session, writeOptions);

        var result = await operations.WriteProcessPayloadAsync(payload);

        Assert.True(result);
        var request = Assert.IsType<XgtWriteRequest>(session.LastWriteRequest);
        Assert.Equal(XgtDataType.Continuous, request.DataType);
        var block = Assert.Single(request.VariableBlocks);
        Assert.Equal("%DB12000", block.VariableName);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30 }, block.Data);
    }

    [Fact]
    public async Task WriteProcessPayloadAsync_RejectsNullPayload()
    {
        var operations = CreateOperations(new FakeXgtSession { IsConnectedValue = true });

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await operations.WriteProcessPayloadAsync(null!));
    }

    [Fact]
    public async Task WriteStartAckAsync_WritesAckValueAsUShortLittleEndian()
    {
        var session = new FakeXgtSession { IsConnectedValue = true };
        var writeOptions = new WorkStartXgtWriteOptions(
            "%DB11000",
            "%DB12016",
            startAckValue: 0x1234,
            "%DB11410");
        var operations = CreateOperations(session, writeOptions);

        var result = await operations.WriteStartAckAsync();

        Assert.True(result);
        var request = Assert.IsType<XgtWriteRequest>(session.LastWriteRequest);
        Assert.Equal(XgtDataType.Continuous, request.DataType);
        var block = Assert.Single(request.VariableBlocks);
        Assert.Equal("%DB12016", block.VariableName);
        Assert.Equal(new byte[] { 0x34, 0x12 }, block.Data);
    }

    [Fact]
    public async Task WriteErrorCodeBestEffortAsync_WritesErrorCodeAsUShortLittleEndian()
    {
        var session = new FakeXgtSession { IsConnectedValue = true };
        var writeOptions = new WorkStartXgtWriteOptions(
            "%DB11000",
            "%DB11416",
            startAckValue: 1,
            "%DB12010");
        var operations = CreateOperations(session, writeOptions);

        await operations.WriteErrorCodeBestEffortAsync(WorkStartErrorCode.BulkWriteFailed);

        var request = Assert.IsType<XgtWriteRequest>(session.LastWriteRequest);
        Assert.Equal(XgtDataType.Continuous, request.DataType);
        var block = Assert.Single(request.VariableBlocks);
        Assert.Equal("%DB12010", block.VariableName);
        Assert.Equal(new byte[] { 0xC5, 0x09 }, block.Data);
    }

    [Fact]
    public async Task WriteProcessPayloadAsync_ReturnsFalse_WhenSessionWriteFails()
    {
        var session = new FakeXgtSession
        {
            IsConnectedValue = true,
            WriteResponse = new XgtWriteResponse(
                XgtDataType.Continuous,
                XgtErrorStatus.Nak(0x0001, 0x1234))
        };
        var operations = CreateOperations(session);

        var result = await operations.WriteProcessPayloadAsync(new byte[] { 1 });

        Assert.False(result);
    }

    [Fact]
    public async Task WriteStartAckAsync_ReturnsFalse_WhenSessionWriteFails()
    {
        var session = new FakeXgtSession
        {
            IsConnectedValue = true,
            WriteException = new InvalidOperationException("forced write failure")
        };
        var operations = CreateOperations(session);

        var result = await operations.WriteStartAckAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task WriteErrorCodeBestEffortAsync_DoesNotThrow_WhenSessionWriteFails()
    {
        var session = new FakeXgtSession
        {
            IsConnectedValue = true,
            WriteException = new InvalidOperationException("forced write failure")
        };
        var operations = CreateOperations(session);

        await operations.WriteErrorCodeBestEffortAsync(WorkStartErrorCode.AckWriteFailed);
    }

    private static readonly XgtReadRequest DefaultReadRequest = new(
        XgtDataType.Continuous,
        new[] { new XgtVariableBlock("%MB100") },
        continuousByteLength: 16);

    private static WorkStartXgtPlcOperations CreateOperations(FakeXgtSession session) =>
        new(session, DefaultReadRequest);

    private static WorkStartXgtPlcOperations CreateOperations(
        FakeXgtSession session,
        WorkStartXgtWriteOptions writeOptions) =>
        new(session, DefaultReadRequest, writeOptions);

    private static XgtReadResponse AckWithData(params byte[] data) =>
        new(
            XgtDataType.Continuous,
            XgtErrorStatus.Ack(),
            new[] { new XgtVariableBlock("%MB100", data) });

    private sealed class FakeXgtSession : IXgtSession
    {
        public bool IsConnectedValue { get; init; }

        public XgtReadResponse? ReadResponse { get; init; }

        public Exception? ReadException { get; init; }

        public XgtWriteResponse? WriteResponse { get; init; }

        public Exception? WriteException { get; init; }

        public int ConnectCallCount { get; private set; }

        public XgtReadRequest? LastReadRequest { get; private set; }

        public XgtWriteRequest? LastWriteRequest { get; private set; }

        public bool IsConnected => IsConnectedValue || ConnectCallCount > 0;

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            ConnectCallCount++;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<XgtReadResponse> ReadAsync(XgtReadRequest request, CancellationToken cancellationToken)
        {
            LastReadRequest = request;

            if (ReadException is not null)
            {
                throw ReadException;
            }

            return Task.FromResult(ReadResponse ?? AckWithData());
        }

        public Task<XgtWriteResponse> WriteAsync(XgtWriteRequest request, CancellationToken cancellationToken)
        {
            LastWriteRequest = request;

            if (WriteException is not null)
            {
                throw WriteException;
            }

            return Task.FromResult(
                WriteResponse ?? new XgtWriteResponse(XgtDataType.Continuous, XgtErrorStatus.Ack()));
        }

        public Task<XgtStatusResponse> ReadStatusAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<byte[]> ExchangeRawAsync(byte[] requestFrame, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
