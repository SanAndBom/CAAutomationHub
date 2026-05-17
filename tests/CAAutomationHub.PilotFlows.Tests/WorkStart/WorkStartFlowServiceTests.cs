using System.Buffers.Binary;
using System.Text;
using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotFlows.Tests.WorkStart;

public sealed class WorkStartFlowServiceTests
{
    [Fact]
    public async Task RunAsync_Succeeds_WhenReadQueryPayloadWriteAndAckSucceed()
    {
        var plc = new FakeWorkStartPlcOperations
        {
            ReadBlock = CreateReadBlock(lotId1: "LOT123456789")
        };
        var query = new FakeWorkStartDataQuery(
            WorkStartDataQueryResult.Success(CreateSampleData(lotId: null)));
        var service = new WorkStartFlowService(plc, query);

        var result = await service.RunAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(WorkStartStep.Completed, result.Step);
        Assert.Equal(WorkStartErrorCode.None, result.ErrorCode);
        Assert.Equal("LOT123456789", result.SelectedLotId);
        Assert.False(result.ErrorWriteExpected);
        Assert.Equal(1, plc.EnsureConnectedCallCount);
        Assert.Equal(1, plc.ReadCallCount);
        Assert.Equal(1, plc.PayloadWriteCallCount);
        Assert.Equal(1, plc.AckWriteCallCount);
        Assert.Empty(plc.WrittenErrorCodes);
        Assert.Equal(new[] { "LOT123456789" }, query.QueriedLotIds);
        Assert.NotNull(plc.LastPayload);
        Assert.Equal("LOT123456789", ReadAscii(plc.LastPayload!, 0, 12));
    }

    [Fact]
    public async Task RunAsync_ReturnsLotIdEmpty_WhenBothLotIdsAreEmpty()
    {
        var plc = new FakeWorkStartPlcOperations
        {
            ReadBlock = CreateReadBlock()
        };
        var query = new FakeWorkStartDataQuery(
            WorkStartDataQueryResult.Success(CreateSampleData(lotId: null)));
        var service = new WorkStartFlowService(plc, query);

        var result = await service.RunAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(WorkStartStep.LotId, result.Step);
        Assert.Equal(WorkStartErrorCode.LotIdEmpty, result.ErrorCode);
        Assert.Null(result.SelectedLotId);
        Assert.True(result.ErrorWriteExpected);
        Assert.Empty(query.QueriedLotIds);
        Assert.Equal(new[] { WorkStartErrorCode.LotIdEmpty }, plc.WrittenErrorCodes);
        Assert.Equal(0, plc.PayloadWriteCallCount);
        Assert.Equal(0, plc.AckWriteCallCount);
    }

    [Fact]
    public async Task RunAsync_ReturnsDbNotFound_WhenQueryReturnsNotFound()
    {
        var plc = new FakeWorkStartPlcOperations
        {
            ReadBlock = CreateReadBlock(lotId1: "LOT123456789")
        };
        var query = new FakeWorkStartDataQuery(
            WorkStartDataQueryResult.NotFound("LOT123456789"));
        var service = new WorkStartFlowService(plc, query);

        var result = await service.RunAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(WorkStartStep.DbQuery, result.Step);
        Assert.Equal(WorkStartErrorCode.DbNotFound, result.ErrorCode);
        Assert.Equal("LOT123456789", result.SelectedLotId);
        Assert.True(result.ErrorWriteExpected);
        Assert.Equal(new[] { "LOT123456789" }, query.QueriedLotIds);
        Assert.Equal(new[] { WorkStartErrorCode.DbNotFound }, plc.WrittenErrorCodes);
        Assert.Equal(0, plc.PayloadWriteCallCount);
        Assert.Equal(0, plc.AckWriteCallCount);
    }

    [Fact]
    public async Task RunAsync_ReturnsDbMultipleRows_WhenQueryReturnsMultipleRows()
    {
        var plc = new FakeWorkStartPlcOperations
        {
            ReadBlock = CreateReadBlock(lotId1: "LOT123456789")
        };
        var query = new FakeWorkStartDataQuery(
            WorkStartDataQueryResult.MultipleRows("LOT123456789"));
        var service = new WorkStartFlowService(plc, query);

        var result = await service.RunAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(WorkStartStep.DbQuery, result.Step);
        Assert.Equal(WorkStartErrorCode.DbMultipleRows, result.ErrorCode);
        Assert.Equal("LOT123456789", result.SelectedLotId);
        Assert.True(result.ErrorWriteExpected);
        Assert.Equal(new[] { "LOT123456789" }, query.QueriedLotIds);
        Assert.Equal(new[] { WorkStartErrorCode.DbMultipleRows }, plc.WrittenErrorCodes);
        Assert.Equal(0, plc.PayloadWriteCallCount);
        Assert.Equal(0, plc.AckWriteCallCount);
    }

    [Fact]
    public async Task RunAsync_ReturnsDbFailed_WhenQueryReturnsFailed()
    {
        var plc = new FakeWorkStartPlcOperations
        {
            ReadBlock = CreateReadBlock(lotId1: "LOT123456789")
        };
        var query = new FakeWorkStartDataQuery(
            WorkStartDataQueryResult.Failed("LOT123456789", "DB command failed."));
        var service = new WorkStartFlowService(plc, query);

        var result = await service.RunAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(WorkStartStep.DbQuery, result.Step);
        Assert.Equal(WorkStartErrorCode.DbFailed, result.ErrorCode);
        Assert.Equal("DB command failed.", result.Message);
        Assert.Equal("LOT123456789", result.SelectedLotId);
        Assert.True(result.ErrorWriteExpected);
        Assert.Equal(new[] { WorkStartErrorCode.DbFailed }, plc.WrittenErrorCodes);
        Assert.Equal(0, plc.PayloadWriteCallCount);
        Assert.Equal(0, plc.AckWriteCallCount);
    }

    [Fact]
    public async Task RunAsync_ReturnsDbException_WhenQueryThrows()
    {
        var plc = new FakeWorkStartPlcOperations
        {
            ReadBlock = CreateReadBlock(lotId1: "LOT123456789")
        };
        var query = new FakeWorkStartDataQuery(
            WorkStartDataQueryResult.Success(CreateSampleData(lotId: null)))
        {
            QueryException = new InvalidOperationException("DB timeout.")
        };
        var service = new WorkStartFlowService(plc, query);

        var result = await service.RunAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(WorkStartStep.DbQuery, result.Step);
        Assert.Equal(WorkStartErrorCode.DbException, result.ErrorCode);
        Assert.Equal("DB timeout.", result.Message);
        Assert.Equal("LOT123456789", result.SelectedLotId);
        Assert.True(result.ErrorWriteExpected);
        Assert.Equal(new[] { WorkStartErrorCode.DbException }, plc.WrittenErrorCodes);
        Assert.Equal(0, plc.PayloadWriteCallCount);
        Assert.Equal(0, plc.AckWriteCallCount);
    }

    [Fact]
    public async Task RunAsync_ReturnsPayloadBuildFailed_WhenPayloadOptionsAreInvalid()
    {
        var plc = new FakeWorkStartPlcOperations
        {
            ReadBlock = CreateReadBlock(lotId1: "LOT123456789")
        };
        var query = new FakeWorkStartDataQuery(
            WorkStartDataQueryResult.Success(CreateSampleData(lotId: null)));
        var service = new WorkStartFlowService(
            plc,
            query,
            new WorkStartFlowOptions
            {
                PayloadBuildOptions = new WorkStartPayloadBuildOptions { WordCount = 1 }
            });

        var result = await service.RunAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(WorkStartStep.PayloadBuild, result.Step);
        Assert.Equal(WorkStartErrorCode.PayloadBuildFailed, result.ErrorCode);
        Assert.Equal("LOT123456789", result.SelectedLotId);
        Assert.True(result.ErrorWriteExpected);
        Assert.Equal(new[] { WorkStartErrorCode.PayloadBuildFailed }, plc.WrittenErrorCodes);
        Assert.Equal(0, plc.PayloadWriteCallCount);
        Assert.Equal(0, plc.AckWriteCallCount);
    }

    [Fact]
    public async Task RunAsync_ReturnsBulkWriteFailed_WhenPayloadWriteFails()
    {
        var plc = new FakeWorkStartPlcOperations
        {
            ReadBlock = CreateReadBlock(lotId1: "LOT123456789"),
            PayloadWriteSucceeds = false
        };
        var query = new FakeWorkStartDataQuery(
            WorkStartDataQueryResult.Success(CreateSampleData(lotId: null)));
        var service = new WorkStartFlowService(plc, query);

        var result = await service.RunAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(WorkStartStep.BulkWrite, result.Step);
        Assert.Equal(WorkStartErrorCode.BulkWriteFailed, result.ErrorCode);
        Assert.Equal("LOT123456789", result.SelectedLotId);
        Assert.True(result.ErrorWriteExpected);
        Assert.Equal(new[] { WorkStartErrorCode.BulkWriteFailed }, plc.WrittenErrorCodes);
        Assert.Equal(1, plc.PayloadWriteCallCount);
        Assert.Equal(0, plc.AckWriteCallCount);
    }

    [Fact]
    public async Task RunAsync_ReturnsAckWriteFailed_WhenAckWriteFails()
    {
        var plc = new FakeWorkStartPlcOperations
        {
            ReadBlock = CreateReadBlock(lotId1: "LOT123456789"),
            AckWriteSucceeds = false
        };
        var query = new FakeWorkStartDataQuery(
            WorkStartDataQueryResult.Success(CreateSampleData(lotId: null)));
        var service = new WorkStartFlowService(plc, query);

        var result = await service.RunAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(WorkStartStep.AckWrite, result.Step);
        Assert.Equal(WorkStartErrorCode.AckWriteFailed, result.ErrorCode);
        Assert.Equal("LOT123456789", result.SelectedLotId);
        Assert.True(result.ErrorWriteExpected);
        Assert.Equal(new[] { WorkStartErrorCode.AckWriteFailed }, plc.WrittenErrorCodes);
        Assert.Equal(1, plc.PayloadWriteCallCount);
        Assert.Equal(1, plc.AckWriteCallCount);
    }

    [Fact]
    public async Task RunAsync_ReturnsReadFailed_WhenReadThrows()
    {
        var plc = new FakeWorkStartPlcOperations
        {
            ThrowOnRead = true
        };
        var query = new FakeWorkStartDataQuery(
            WorkStartDataQueryResult.Success(CreateSampleData(lotId: null)));
        var service = new WorkStartFlowService(plc, query);

        var result = await service.RunAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(WorkStartStep.GroupRead, result.Step);
        Assert.Equal(WorkStartErrorCode.ReadFailed, result.ErrorCode);
        Assert.Null(result.SelectedLotId);
        Assert.False(result.ErrorWriteExpected);
        Assert.Empty(plc.WrittenErrorCodes);
        Assert.Empty(query.QueriedLotIds);
        Assert.Equal(0, plc.PayloadWriteCallCount);
        Assert.Equal(0, plc.AckWriteCallCount);
    }

    [Fact]
    public async Task RunAsync_DoesNotWriteErrorCode_WhenStartSignalInactive()
    {
        var plc = new FakeWorkStartPlcOperations
        {
            ReadBlock = CreateReadBlock(lotId1: "LOT123456789", startSignalActive: false)
        };
        var query = new FakeWorkStartDataQuery(
            WorkStartDataQueryResult.Success(CreateSampleData(lotId: null)));
        var service = new WorkStartFlowService(plc, query);

        var result = await service.RunAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(WorkStartStep.StartSignal, result.Step);
        Assert.Equal(WorkStartErrorCode.StartSignalInactive, result.ErrorCode);
        Assert.Null(result.SelectedLotId);
        Assert.False(result.ErrorWriteExpected);
        Assert.Empty(plc.WrittenErrorCodes);
        Assert.Empty(query.QueriedLotIds);
    }

    [Fact]
    public async Task RunAsync_KeepsFailureResult_WhenBestEffortErrorWriteThrows()
    {
        var plc = new FakeWorkStartPlcOperations
        {
            ReadBlock = CreateReadBlock(),
            ThrowOnErrorWrite = true
        };
        var query = new FakeWorkStartDataQuery(
            WorkStartDataQueryResult.Success(CreateSampleData(lotId: null)));
        var service = new WorkStartFlowService(plc, query);

        var result = await service.RunAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(WorkStartStep.LotId, result.Step);
        Assert.Equal(WorkStartErrorCode.LotIdEmpty, result.ErrorCode);
        Assert.Equal(new[] { WorkStartErrorCode.LotIdEmpty }, plc.WrittenErrorCodes);
    }

    [Fact]
    public void PublicServiceTypes_DoNotExposeExternalBoundaryTypes()
    {
        var forbiddenReferenceNames = new[]
        {
            string.Join('.', "CAAutomationHub", "Run" + "time"),
            string.Join('.', "CAAutomationHub", "Flow" + "Definitions"),
            "AutomationHub." + "X" + "gtDriverCore",
            "X" + "gtChannelRunner",
            "Fake" + "Plc"
        };

        var referencedNames = typeof(WorkStartFlowService)
            .Assembly
            .GetReferencedAssemblies()
            .Select(static assembly => assembly.Name)
            .Where(static name => name is not null)
            .Cast<string>()
            .ToArray();

        foreach (var forbiddenReferenceName in forbiddenReferenceNames)
        {
            Assert.DoesNotContain(forbiddenReferenceName, referencedNames);
        }

        var exposedNames = new[]
        {
            typeof(WorkStartFlowService).FullName,
            typeof(IWorkStartPlcOperations).FullName,
            typeof(IWorkStartDataQuery).FullName,
            typeof(WorkStartDataQueryResult).FullName
        };

        Assert.DoesNotContain(string.Join("", "Run", "time", "Snapshot"), exposedNames);
        Assert.DoesNotContain(string.Join("", "Channel", "Polling", "Result"), exposedNames);
    }

    private static byte[] CreateReadBlock(
        string? lotId1 = null,
        string? lotId2 = null,
        bool startSignalActive = true)
    {
        var readBlockBytes = new byte[WorkStartReadBlockLayout.DefaultReadWordCount * 2];
        if (startSignalActive)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(
                readBlockBytes.AsSpan(WorkStartReadBlockLayout.DefaultStartSignalWordIndex * 2, 2),
                1);
        }

        WriteAscii(readBlockBytes, WorkStartReadBlockLayout.DefaultLotId1WordOffset, lotId1);
        WriteAscii(readBlockBytes, WorkStartReadBlockLayout.DefaultLotId2WordOffset, lotId2);
        return readBlockBytes;
    }

    private static void WriteAscii(byte[] readBlockBytes, int wordOffset, string? value)
    {
        if (value is null)
        {
            return;
        }

        var maxByteLength = WorkStartReadBlockLayout.DefaultLotIdWordLength * 2;
        var bytes = Encoding.ASCII.GetBytes(value);
        Buffer.BlockCopy(bytes, 0, readBlockBytes, wordOffset * 2, Math.Min(maxByteLength, bytes.Length));
    }

    private static string ReadAscii(byte[] source, int offset, int length) =>
        Encoding.ASCII.GetString(source, offset, length).Trim('\0', ' ');

    private static WorkStartProcessData CreateSampleData(string? lotId) =>
        new()
        {
            LotId = lotId,
            Profile = "PROFILE-ABC",
            Tblr = "L",
            WinType = "W",
            CutSize = 500,
            Lr = "R",
            RollerYn = "Y",
            RollerHolePos = 1234,
            RollerHoleWidth = 5678,
            RollerHoleLength = 9012,
            RollerType = "S",
            CutDegree = 90
        };

    private sealed class FakeWorkStartPlcOperations : IWorkStartPlcOperations
    {
        public byte[] ReadBlock { get; init; } = CreateReadBlock();

        public bool ThrowOnRead { get; init; }

        public bool PayloadWriteSucceeds { get; init; } = true;

        public bool AckWriteSucceeds { get; init; } = true;

        public bool ThrowOnErrorWrite { get; init; }

        public int EnsureConnectedCallCount { get; private set; }

        public int ReadCallCount { get; private set; }

        public int PayloadWriteCallCount { get; private set; }

        public int AckWriteCallCount { get; private set; }

        public byte[]? LastPayload { get; private set; }

        public List<WorkStartErrorCode> WrittenErrorCodes { get; } = [];

        public ValueTask EnsureConnectedAsync(CancellationToken cancellationToken = default)
        {
            EnsureConnectedCallCount++;
            return ValueTask.CompletedTask;
        }

        public ValueTask<byte[]> ReadWorkStartBlockAsync(CancellationToken cancellationToken = default)
        {
            ReadCallCount++;
            if (ThrowOnRead)
            {
                throw new InvalidOperationException("read failed");
            }

            return ValueTask.FromResult(ReadBlock);
        }

        public ValueTask<bool> WriteProcessPayloadAsync(byte[] payload, CancellationToken cancellationToken = default)
        {
            PayloadWriteCallCount++;
            LastPayload = payload;
            return ValueTask.FromResult(PayloadWriteSucceeds);
        }

        public ValueTask<bool> WriteStartAckAsync(CancellationToken cancellationToken = default)
        {
            AckWriteCallCount++;
            return ValueTask.FromResult(AckWriteSucceeds);
        }

        public ValueTask WriteErrorCodeBestEffortAsync(
            WorkStartErrorCode errorCode,
            CancellationToken cancellationToken = default)
        {
            WrittenErrorCodes.Add(errorCode);
            if (ThrowOnErrorWrite)
            {
                throw new InvalidOperationException("error write failed");
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeWorkStartDataQuery : IWorkStartDataQuery
    {
        private readonly WorkStartDataQueryResult _result;

        public FakeWorkStartDataQuery(WorkStartDataQueryResult result)
        {
            _result = result;
        }

        public List<string> QueriedLotIds { get; } = [];

        public Exception? QueryException { get; init; }

        public ValueTask<WorkStartDataQueryResult> QueryAsync(
            string lotId,
            CancellationToken cancellationToken = default)
        {
            QueriedLotIds.Add(lotId);
            if (QueryException is not null)
            {
                throw QueryException;
            }

            return ValueTask.FromResult(_result);
        }
    }
}
