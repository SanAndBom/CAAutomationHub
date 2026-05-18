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
    public async Task WriteMethods_AreExplicitlyUnsupportedInReadSkeleton()
    {
        var operations = CreateOperations(new FakeXgtSession { IsConnectedValue = true });

        await Assert.ThrowsAsync<NotSupportedException>(
            async () => await operations.WriteProcessPayloadAsync(new byte[] { 1 }));
        await Assert.ThrowsAsync<NotSupportedException>(
            async () => await operations.WriteStartAckAsync());
        await Assert.ThrowsAsync<NotSupportedException>(
            async () => await operations.WriteErrorCodeBestEffortAsync(WorkStartErrorCode.ReadFailed));
    }

    private static readonly XgtReadRequest DefaultReadRequest = new(
        XgtDataType.Continuous,
        new[] { new XgtVariableBlock("%MB100") },
        continuousByteLength: 16);

    private static WorkStartXgtPlcOperations CreateOperations(FakeXgtSession session) =>
        new(session, DefaultReadRequest);

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

        public int ConnectCallCount { get; private set; }

        public XgtReadRequest? LastReadRequest { get; private set; }

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

        public Task<XgtWriteResponse> WriteAsync(XgtWriteRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<XgtStatusResponse> ReadStatusAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<byte[]> ExchangeRawAsync(byte[] requestFrame, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
