using System.Buffers.Binary;
using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotFlows.Tests.WorkStart;

public sealed class WorkStartAckOffServiceTests
{
    private const int StartSignalWordIndex = 83;

    [Fact]
    public async Task AckOffAsync_WritesAckOff_WhenStartRequestIsOff()
    {
        var operations = new FakeWorkStartPlcOperations
        {
            ReadResult = WorkStartReadBlockOperationResult.Success(CreateReadBlock(startSignalActive: false))
        };
        var service = new WorkStartAckOffService(
            operations,
            new WorkStartAckOffOptions { StartSignalWordIndex = StartSignalWordIndex });

        var result = await service.AckOffAsync();

        Assert.Equal(WorkStartAckOffStatus.AckOffWritten, result.Status);
        Assert.True(result.Succeeded);
        Assert.Equal(1, operations.WriteStartAckCallCount);
    }

    [Fact]
    public async Task AckOffAsync_DoesNotWriteAckOff_WhenStartRequestStillOn()
    {
        var operations = new FakeWorkStartPlcOperations
        {
            ReadResult = WorkStartReadBlockOperationResult.Success(CreateReadBlock(startSignalActive: true))
        };
        var service = new WorkStartAckOffService(
            operations,
            new WorkStartAckOffOptions { StartSignalWordIndex = StartSignalWordIndex });

        var result = await service.AckOffAsync();

        Assert.Equal(WorkStartAckOffStatus.WaitingRequestOff, result.Status);
        Assert.False(result.Succeeded);
        Assert.Equal(0, operations.WriteStartAckCallCount);
    }

    private static byte[] CreateReadBlock(bool startSignalActive)
    {
        var readBlock = new byte[WorkStartReadBlockLayout.DefaultReadWordCount * 2];
        BinaryPrimitives.WriteUInt16LittleEndian(
            readBlock.AsSpan(StartSignalWordIndex * 2, 2),
            startSignalActive ? (ushort)1 : (ushort)0);
        return readBlock;
    }

    private sealed class FakeWorkStartPlcOperations : IWorkStartPlcOperations
    {
        public WorkStartReadBlockOperationResult ReadResult { get; init; } =
            WorkStartReadBlockOperationResult.Success([]);

        public int WriteStartAckCallCount { get; private set; }

        public ValueTask EnsureConnectedAsync(CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask<WorkStartReadBlockOperationResult> ReadWorkStartBlockAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(ReadResult);

        public ValueTask<bool> WriteProcessPayloadAsync(
            byte[] payload,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(true);

        public ValueTask<bool> WriteStartAckAsync(CancellationToken cancellationToken = default)
        {
            WriteStartAckCallCount++;
            return ValueTask.FromResult(true);
        }

        public ValueTask WriteErrorCodeBestEffortAsync(
            WorkStartErrorCode errorCode,
            CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }
}
