using System.Buffers.Binary;
using System.Text;
using CAAutomationHub.PilotFlows.WorkStart;
using CAAutomationHub.PilotSmoke;

namespace CAAutomationHub.PilotSmoke.Tests;

public sealed class WorkStartReadOnlySmokeTests
{
    [Fact]
    public async Task RunAsync_ReadsWorkStartBlockAndDoesNotCallWriteOperations()
    {
        var data = new byte[40];
        WriteAscii(data, wordOffset: 0, wordLength: 6, "LOT-A");
        WriteAscii(data, wordOffset: 10, wordLength: 6, "LOT-B");
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(36, 2), 1);
        var operations = new TrackingWorkStartPlcOperations(
            WorkStartReadBlockOperationResult.Success(data));
        var layout = new PilotSmokeReadLayout(
            StartSignalWordIndex: 18,
            LotId1WordOffset: 0,
            LotId2WordOffset: 10,
            LotIdWordLength: 6);

        var result = await WorkStartReadOnlySmoke.RunAsync(operations, layout);

        Assert.True(result.ConnectionSucceeded);
        Assert.True(result.ReadSucceeded);
        Assert.True(result.StartRequestActive);
        Assert.Equal("LOT-A", result.LotId1);
        Assert.Equal("LOT-B", result.LotId2);
        Assert.Equal("LOT-A", result.SelectedLotId);
        Assert.Equal(40, result.RawLength);
        Assert.Equal(1, operations.EnsureConnectedCallCount);
        Assert.Equal(1, operations.ReadCallCount);
        Assert.Equal(0, operations.WriteProcessPayloadCallCount);
        Assert.Equal(0, operations.WriteStartAckCallCount);
        Assert.Equal(0, operations.WriteErrorCodeCallCount);
    }

    [Fact]
    public async Task RunAsync_ReportsReadFailureWithoutCallingWriteOperations()
    {
        var operations = new TrackingWorkStartPlcOperations(
            WorkStartReadBlockOperationResult.OperationFailed("read failed"));
        var layout = new PilotSmokeReadLayout(
            StartSignalWordIndex: 15,
            LotId1WordOffset: 0,
            LotId2WordOffset: 10,
            LotIdWordLength: 6);

        var result = await WorkStartReadOnlySmoke.RunAsync(operations, layout);

        Assert.True(result.ConnectionSucceeded);
        Assert.False(result.ReadSucceeded);
        Assert.Contains("read failed", result.Message);
        Assert.Equal(1, operations.EnsureConnectedCallCount);
        Assert.Equal(1, operations.ReadCallCount);
        Assert.Equal(0, operations.WriteProcessPayloadCallCount);
        Assert.Equal(0, operations.WriteStartAckCallCount);
        Assert.Equal(0, operations.WriteErrorCodeCallCount);
    }

    private static void WriteAscii(byte[] data, int wordOffset, int wordLength, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        bytes.CopyTo(data.AsSpan(wordOffset * 2, Math.Min(bytes.Length, wordLength * 2)));
    }

    private sealed class TrackingWorkStartPlcOperations : IWorkStartPlcOperations
    {
        private readonly WorkStartReadBlockOperationResult _readResult;

        public TrackingWorkStartPlcOperations(WorkStartReadBlockOperationResult readResult)
        {
            _readResult = readResult;
        }

        public int EnsureConnectedCallCount { get; private set; }

        public int ReadCallCount { get; private set; }

        public int WriteProcessPayloadCallCount { get; private set; }

        public int WriteStartAckCallCount { get; private set; }

        public int WriteErrorCodeCallCount { get; private set; }

        public ValueTask EnsureConnectedAsync(CancellationToken cancellationToken = default)
        {
            EnsureConnectedCallCount++;
            return ValueTask.CompletedTask;
        }

        public ValueTask<WorkStartReadBlockOperationResult> ReadWorkStartBlockAsync(
            CancellationToken cancellationToken = default)
        {
            ReadCallCount++;
            return ValueTask.FromResult(_readResult);
        }

        public ValueTask<bool> WriteProcessPayloadAsync(byte[] payload, CancellationToken cancellationToken = default)
        {
            WriteProcessPayloadCallCount++;
            return ValueTask.FromResult(true);
        }

        public ValueTask<bool> WriteStartAckAsync(CancellationToken cancellationToken = default)
        {
            WriteStartAckCallCount++;
            return ValueTask.FromResult(true);
        }

        public ValueTask WriteErrorCodeBestEffortAsync(
            WorkStartErrorCode errorCode,
            CancellationToken cancellationToken = default)
        {
            WriteErrorCodeCallCount++;
            return ValueTask.CompletedTask;
        }
    }
}
