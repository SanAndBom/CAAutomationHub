using System.Buffers.Binary;
using CAAutomationHub.PilotFlows.WorkComplete;

namespace CAAutomationHub.PilotFlows.Tests.WorkComplete;

public sealed class WorkCompleteAckServiceTests
{
    private const int CompleteSignalWordIndex = 84;

    [Fact]
    public async Task AckOnAsync_WritesAckOn_WhenCompleteRequestIsOn()
    {
        var operations = new FakeWorkCompletePlcOperations
        {
            ReadResult = WorkCompleteReadBlockOperationResult.Success(CreateReadBlock(completeSignalActive: true))
        };
        var service = new WorkCompleteAckService(
            operations,
            new WorkCompleteAckOptions { CompleteSignalWordIndex = CompleteSignalWordIndex });

        var result = await service.AckOnAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(WorkCompleteAckStatus.AckOnWritten, result.Status);
        Assert.Equal(new[] { (ushort)1 }, operations.WrittenAckValues);
    }

    [Fact]
    public async Task AckOffAsync_WritesAckOff_WhenCompleteRequestIsOff()
    {
        var operations = new FakeWorkCompletePlcOperations
        {
            ReadResult = WorkCompleteReadBlockOperationResult.Success(CreateReadBlock(completeSignalActive: false))
        };
        var service = new WorkCompleteAckService(
            operations,
            new WorkCompleteAckOptions { CompleteSignalWordIndex = CompleteSignalWordIndex });

        var result = await service.AckOffAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(WorkCompleteAckStatus.AckOffWritten, result.Status);
        Assert.Equal(new[] { (ushort)0 }, operations.WrittenAckValues);
    }

    [Fact]
    public async Task AckOffAsync_DoesNotWriteAckOff_WhenCompleteRequestStillOn()
    {
        var operations = new FakeWorkCompletePlcOperations
        {
            ReadResult = WorkCompleteReadBlockOperationResult.Success(CreateReadBlock(completeSignalActive: true))
        };
        var service = new WorkCompleteAckService(
            operations,
            new WorkCompleteAckOptions { CompleteSignalWordIndex = CompleteSignalWordIndex });

        var result = await service.AckOffAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(WorkCompleteAckStatus.WaitingRequestOff, result.Status);
        Assert.Empty(operations.WrittenAckValues);
    }

    private static byte[] CreateReadBlock(bool completeSignalActive)
    {
        var readBlock = new byte[WorkCompleteReadBlockLayout.DefaultReadWordCount * 2];
        BinaryPrimitives.WriteUInt16LittleEndian(
            readBlock.AsSpan(CompleteSignalWordIndex * 2, 2),
            completeSignalActive ? (ushort)1 : (ushort)0);
        return readBlock;
    }

    private sealed class FakeWorkCompletePlcOperations : IWorkCompletePlcOperations
    {
        public WorkCompleteReadBlockOperationResult ReadResult { get; init; } =
            WorkCompleteReadBlockOperationResult.Success([]);

        public List<ushort> WrittenAckValues { get; } = [];

        public ValueTask EnsureConnectedAsync(CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask<WorkCompleteReadBlockOperationResult> ReadWorkCompleteBlockAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(ReadResult);

        public ValueTask<bool> WriteCompleteAckAsync(
            ushort value,
            CancellationToken cancellationToken = default)
        {
            WrittenAckValues.Add(value);
            return ValueTask.FromResult(true);
        }
    }
}
