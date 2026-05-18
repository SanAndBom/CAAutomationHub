using System.Net;
using System.Net.Sockets;
using AutomationHub.XgtDriverCore.Client;
using AutomationHub.XgtDriverCore.FakePlc.Configuration;
using AutomationHub.XgtDriverCore.FakePlc.Runtime;
using AutomationHub.XgtDriverCore.Transport;
using CAAutomationHub.PilotFlows.WorkStart;
using CAAutomationHub.PilotFlows.Xgt.WorkStart;

namespace CAAutomationHub.PilotFlows.Xgt.Tests.WorkStart;

public sealed class WorkStartXgtFakePlcIntegrationTests
{
    private const string UnsupportedReadStartVariable = "%DB99990";
    private const int FakePlcStartSignalWordIndex = 83;
    private const int LotId1WordOffset = WorkStartReadBlockLayout.DefaultLotId1WordOffset;
    private const int LotId2WordOffset = WorkStartReadBlockLayout.DefaultLotId2WordOffset;
    private const int LotIdWordLength = WorkStartReadBlockLayout.DefaultLotIdWordLength;

    [Fact]
    public async Task RunAsync_CompletesHappyPath_WithFakePlcAndFakeDb()
    {
        var runtime = CreateRuntime();
        var expectedLotId = "S0007652610B";
        var processData = CreateSampleProcessData(lotId: null);
        var expectedPayload = WorkStartProcessDataPayloadBuilder
            .Build(processData with { LotId = expectedLotId })
            .PayloadBytes;
        var query = new FakeWorkStartDataQuery(WorkStartDataQueryResult.Success(processData));
        await using var fakePlc = InProcessFakePlcServer.Start(runtime);
        await using var session = CreateSession(fakePlc.Port);
        var operations = new WorkStartXgtPlcOperations(
            session,
            WorkStartXgtReadOptions.Default,
            WorkStartXgtWriteOptions.Default);
        var service = new WorkStartFlowService(
            operations,
            query,
            new WorkStartFlowOptions
            {
                StartSignalWordIndex = FakePlcStartSignalWordIndex,
                LotId1WordOffset = LotId1WordOffset,
                LotId2WordOffset = LotId2WordOffset,
                LotIdWordLength = LotIdWordLength
            });

        var result = await service.RunAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(WorkStartStep.Completed, result.Step);
        Assert.Equal(WorkStartErrorCode.None, result.ErrorCode);
        Assert.Equal(expectedLotId, result.SelectedLotId);
        Assert.False(result.ErrorWriteExpected);
        Assert.Equal(new[] { expectedLotId }, query.QueriedLotIds);
        Assert.Equal(expectedPayload, runtime.LastBulkWrite);
        Assert.Equal(expectedPayload, runtime.ReadContinuous(FakePlcMemoryImage.Db11000, expectedPayload.Length));
        Assert.Equal((ushort)1, runtime.LastAckValue);
        Assert.Equal(new byte[] { 0x01, 0x00 }, runtime.ReadContinuous(FakePlcMemoryImage.Db11416, 2));
        Assert.Null(runtime.LastErrorCode);
        Assert.Equal(new byte[] { 0x00, 0x00 }, runtime.ReadContinuous(FakePlcMemoryImage.Db11410, 2));
    }

    [Fact]
    public async Task RunAsync_WritesDbNotFoundErrorCode_WhenFakeDbReturnsNotFound()
    {
        await RunDbFailureTransactionAsync(
            lotId => WorkStartDataQueryResult.NotFound(lotId),
            WorkStartErrorCode.DbNotFound,
            new byte[] { 0xFD, 0x08 });
    }

    [Fact]
    public async Task RunAsync_WritesDbMultipleRowsErrorCode_WhenFakeDbReturnsMultipleRows()
    {
        await RunDbFailureTransactionAsync(
            lotId => WorkStartDataQueryResult.MultipleRows(lotId),
            WorkStartErrorCode.DbMultipleRows,
            new byte[] { 0xFE, 0x08 });
    }

    [Fact]
    public async Task RunAsync_WritesDbFailedErrorCode_WhenFakeDbReturnsFailed()
    {
        await RunDbFailureTransactionAsync(
            lotId => WorkStartDataQueryResult.Failed(lotId, "DB query failed."),
            WorkStartErrorCode.DbFailed,
            new byte[] { 0xFF, 0x08 });
    }

    [Fact]
    public async Task RunAsync_DoesNotWriteErrorCode_WhenStartSignalInactive()
    {
        var runtime = CreateRuntime(startSignal: false);
        var query = new FakeWorkStartDataQuery(
            WorkStartDataQueryResult.Success(CreateSampleProcessData(lotId: null)));
        await using var fakePlc = InProcessFakePlcServer.Start(runtime);
        await using var session = CreateSession(fakePlc.Port);
        var service = CreateService(session, query);

        var result = await service.RunAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(WorkStartStep.StartSignal, result.Step);
        Assert.Equal(WorkStartErrorCode.StartSignalInactive, result.ErrorCode);
        Assert.Null(result.SelectedLotId);
        Assert.False(result.ErrorWriteExpected);
        Assert.Empty(query.QueriedLotIds);
        Assert.Null(runtime.LastErrorCode);
        Assert.Equal(new byte[] { 0x00, 0x00 }, runtime.ReadContinuous(FakePlcMemoryImage.Db11410, 2));
        Assert.Null(runtime.LastBulkWrite);
        Assert.Null(runtime.LastAckValue);
    }

    [Fact]
    public async Task RunAsync_ReturnsReadFailed_WhenFakePlcRejectsWorkStartReadAddress()
    {
        var runtime = CreateRuntime();
        var query = new FakeWorkStartDataQuery(
            WorkStartDataQueryResult.Success(CreateSampleProcessData(lotId: null)));
        await using var fakePlc = InProcessFakePlcServer.Start(runtime);
        await using var session = CreateSession(fakePlc.Port);
        var service = CreateService(
            session,
            query,
            new WorkStartXgtReadOptions(
                UnsupportedReadStartVariable,
                WorkStartXgtReadOptions.DefaultReadWordCount));

        var result = await service.RunAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(WorkStartStep.GroupRead, result.Step);
        Assert.Equal(WorkStartErrorCode.ReadFailed, result.ErrorCode);
        Assert.Equal(1101, (int)result.ErrorCode);
        Assert.Null(result.SelectedLotId);
        Assert.False(result.ErrorWriteExpected);
        Assert.Empty(query.QueriedLotIds);
        Assert.Null(runtime.LastBulkWrite);
        Assert.Null(runtime.LastAckValue);
        Assert.Null(runtime.LastErrorCode);
        Assert.Equal(new byte[70 * 2], runtime.ReadContinuous(FakePlcMemoryImage.Db11000, 70 * 2));
        Assert.Equal(new byte[] { 0x00, 0x00 }, runtime.ReadContinuous(FakePlcMemoryImage.Db11416, 2));
        Assert.Equal(new byte[] { 0x00, 0x00 }, runtime.ReadContinuous(FakePlcMemoryImage.Db11410, 2));
    }

    [Fact]
    public async Task ReadWorkStartBlockAsync_ReturnsOperationFailed_WhenFakePlcRejectsReadAddress()
    {
        await using var fakePlc = InProcessFakePlcServer.Start(CreateRuntime());
        await using var session = CreateSession(fakePlc.Port);
        var operations = new WorkStartXgtPlcOperations(
            session,
            new WorkStartXgtReadOptions(
                UnsupportedReadStartVariable,
                WorkStartXgtReadOptions.DefaultReadWordCount));

        await operations.EnsureConnectedAsync();

        var result = await operations.ReadWorkStartBlockAsync();

        Assert.Equal(WorkStartReadBlockOperationStatus.OperationFailed, result.Status);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task ReadWorkStartBlockAsync_WithFakePlcMemoryMap_ReadsLotIdsAndStartSignalUsingTestSpecificLayout()
    {
        await using var fakePlc = InProcessFakePlcServer.Start(CreateRuntime());
        await using var session = CreateSession(fakePlc.Port);
        var operations = new WorkStartXgtPlcOperations(session, WorkStartXgtReadOptions.Default);

        await operations.EnsureConnectedAsync();

        var result = await operations.ReadWorkStartBlockAsync();

        Assert.Equal(WorkStartReadBlockOperationStatus.Success, result.Status);
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(WorkStartReadBlockLayout.DefaultReadWordCount * 2, data.Length);

        Assert.True(
            WorkStartReadBlockInterpreter.IsStartSignalActive(data, FakePlcStartSignalWordIndex));

        var lotId1 = WorkStartReadBlockInterpreter.ExtractLotId(data, LotId1WordOffset, LotIdWordLength);
        var lotId2 = WorkStartReadBlockInterpreter.ExtractLotId(data, LotId2WordOffset, LotIdWordLength);
        var selectedLotId = WorkStartReadBlockInterpreter.SelectLotId(lotId1.LotId, lotId2.LotId);

        Assert.True(lotId1.IsInRange);
        Assert.True(lotId2.IsInRange);
        Assert.Equal("S0007652610B", lotId1.LotId);
        Assert.Equal(string.Empty, lotId2.LotId);
        Assert.True(selectedLotId.HasSelection);
        Assert.Equal("S0007652610B", selectedLotId.SelectedLotId);
        Assert.Equal(WorkStartLotIdSelectionSource.LotId1, selectedLotId.Source);
    }

    [Fact]
    public async Task WriteProcessPayloadAsync_WithFakePlc_WritesPayloadToBulkTarget()
    {
        var runtime = CreateRuntime();
        var payload = Enumerable.Range(0, 140)
            .Select(value => (byte)(value + 1))
            .ToArray();
        await using var fakePlc = InProcessFakePlcServer.Start(runtime);
        await using var session = CreateSession(fakePlc.Port);
        var operations = new WorkStartXgtPlcOperations(session);

        await operations.EnsureConnectedAsync();

        var result = await operations.WriteProcessPayloadAsync(payload);

        Assert.True(result);
        Assert.Equal(payload, runtime.LastBulkWrite);
        Assert.Equal(payload, runtime.ReadContinuous(FakePlcMemoryImage.Db11000, payload.Length));
    }

    [Fact]
    public async Task WriteStartAckAsync_WithFakePlc_WritesAckValueToAckTarget()
    {
        var runtime = CreateRuntime();
        await using var fakePlc = InProcessFakePlcServer.Start(runtime);
        await using var session = CreateSession(fakePlc.Port);
        var operations = new WorkStartXgtPlcOperations(session);

        await operations.EnsureConnectedAsync();

        var result = await operations.WriteStartAckAsync();

        Assert.True(result);
        Assert.Equal((ushort)1, runtime.LastAckValue);
        Assert.Equal(new byte[] { 0x01, 0x00 }, runtime.ReadContinuous(FakePlcMemoryImage.Db11416, 2));
    }

    [Fact]
    public async Task AckOffAsync_WithFakePlc_WritesZeroToStartAckTarget_WhenStartRequestIsOff()
    {
        var runtime = CreateRuntime(startSignal: false);
        runtime.WriteContinuous(FakePlcMemoryImage.Db11416, new byte[] { 0x01, 0x00 });
        await using var fakePlc = InProcessFakePlcServer.Start(runtime);
        await using var session = CreateSession(fakePlc.Port);
        var operations = new WorkStartXgtPlcOperations(
            session,
            WorkStartXgtReadOptions.Default,
            new WorkStartXgtWriteOptions(
                WorkStartXgtWriteOptions.DefaultProcessPayloadWriteVariable,
                WorkStartXgtWriteOptions.DefaultStartAckWriteVariable,
                startAckValue: 0,
                WorkStartXgtWriteOptions.DefaultErrorCodeWriteVariable));
        var service = new WorkStartAckOffService(
            operations,
            new WorkStartAckOffOptions { StartSignalWordIndex = FakePlcStartSignalWordIndex });

        var result = await service.AckOffAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(WorkStartAckOffStatus.AckOffWritten, result.Status);
        Assert.Equal((ushort)0, runtime.LastAckValue);
        Assert.Equal(new byte[] { 0x00, 0x00 }, runtime.ReadContinuous(FakePlcMemoryImage.Db11416, 2));
    }

    [Fact]
    public async Task WriteErrorCodeBestEffortAsync_WithFakePlc_WritesErrorCodeToErrorTarget()
    {
        var runtime = CreateRuntime();
        await using var fakePlc = InProcessFakePlcServer.Start(runtime);
        await using var session = CreateSession(fakePlc.Port);
        var operations = new WorkStartXgtPlcOperations(session);

        await operations.EnsureConnectedAsync();

        await operations.WriteErrorCodeBestEffortAsync(WorkStartErrorCode.DbNotFound);

        Assert.Equal((ushort)WorkStartErrorCode.DbNotFound, runtime.LastErrorCode);
        Assert.Equal(new byte[] { 0xFD, 0x08 }, runtime.ReadContinuous(FakePlcMemoryImage.Db11410, 2));
    }

    private static async Task RunDbFailureTransactionAsync(
        Func<string, WorkStartDataQueryResult> createQueryResult,
        WorkStartErrorCode expectedErrorCode,
        byte[] expectedErrorCodeBytes)
    {
        var runtime = CreateRuntime();
        var expectedLotId = "S0007652610B";
        var query = new FakeWorkStartDataQuery(createQueryResult(expectedLotId));
        await using var fakePlc = InProcessFakePlcServer.Start(runtime);
        await using var session = CreateSession(fakePlc.Port);
        var service = CreateService(session, query);

        var result = await service.RunAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(WorkStartStep.DbQuery, result.Step);
        Assert.Equal(expectedErrorCode, result.ErrorCode);
        Assert.Equal(expectedLotId, result.SelectedLotId);
        Assert.True(result.ErrorWriteExpected);
        Assert.Equal(new[] { expectedLotId }, query.QueriedLotIds);
        Assert.Equal((ushort)expectedErrorCode, runtime.LastErrorCode);
        Assert.Equal(expectedErrorCodeBytes, runtime.ReadContinuous(FakePlcMemoryImage.Db11410, 2));
        Assert.Null(runtime.LastBulkWrite);
        Assert.Null(runtime.LastAckValue);
        Assert.Equal(new byte[70 * 2], runtime.ReadContinuous(FakePlcMemoryImage.Db11000, 70 * 2));
        Assert.Equal(new byte[] { 0x00, 0x00 }, runtime.ReadContinuous(FakePlcMemoryImage.Db11416, 2));
    }

    private static WorkStartFlowService CreateService(
        XgtSession session,
        IWorkStartDataQuery query,
        WorkStartXgtReadOptions? readOptions = null)
    {
        var operations = new WorkStartXgtPlcOperations(
            session,
            readOptions ?? WorkStartXgtReadOptions.Default,
            WorkStartXgtWriteOptions.Default);

        return new WorkStartFlowService(
            operations,
            query,
            new WorkStartFlowOptions
            {
                StartSignalWordIndex = FakePlcStartSignalWordIndex,
                LotId1WordOffset = LotId1WordOffset,
                LotId2WordOffset = LotId2WordOffset,
                LotIdWordLength = LotIdWordLength
            });
    }

    private static XgtSession CreateSession(int port)
    {
        var transport = new TcpTransport(new XgtTransportOptions
        {
            Host = "127.0.0.1",
            Port = port,
            ConnectTimeout = TimeSpan.FromSeconds(1),
            SendTimeout = TimeSpan.FromSeconds(1),
            ReceiveTimeout = TimeSpan.FromSeconds(1)
        });

        return new XgtSession(transport);
    }

    private static FakePlcRuntime CreateRuntime(bool startSignal = true)
    {
        var config = new FakePlcMapConfig
        {
            BaseBlocks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [FakePlcMemoryImage.Db10000] = new('0', WorkStartXgtReadOptions.DefaultReadWordCount * 2 * 2),
                [FakePlcMemoryImage.Db11000] = new('0', 70 * 2 * 2),
                [FakePlcMemoryImage.Db11410] = "0000",
                [FakePlcMemoryImage.Db11416] = "0000",
                [FakePlcMemoryImage.Db11418] = "0000"
            },
            Scenario = new FakePlcScenarioConfig
            {
                LotId1 = "S0007652610B",
                LotId2 = string.Empty,
                StartSignal = startSignal,
                CompleteSignal = false,
                HeartbeatEnabled = true,
                HeartbeatInitialValue = true
            },
            Rules = new FakePlcRuleConfig()
        };

        return new FakePlcRuntime(
            FakePlcScenarioInitializer.CreateMemoryImage(config),
            config.Rules);
    }

    private static WorkStartProcessData CreateSampleProcessData(string? lotId) =>
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

    private sealed class FakeWorkStartDataQuery : IWorkStartDataQuery
    {
        private readonly WorkStartDataQueryResult _result;

        public FakeWorkStartDataQuery(WorkStartDataQueryResult result)
        {
            _result = result;
        }

        public List<string> QueriedLotIds { get; } = [];

        public ValueTask<WorkStartDataQueryResult> QueryAsync(
            string lotId,
            CancellationToken cancellationToken = default)
        {
            QueriedLotIds.Add(lotId);
            return ValueTask.FromResult(_result);
        }
    }

    private sealed class InProcessFakePlcServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly FakePlcRuntime _runtime;
        private readonly CancellationTokenSource _shutdown = new();
        private readonly List<Task> _clientTasks = [];
        private readonly Task _acceptLoop;

        private InProcessFakePlcServer(FakePlcRuntime runtime)
        {
            _runtime = runtime;
            _listener = new TcpListener(IPAddress.Loopback, port: 0);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _acceptLoop = Task.Run(AcceptLoopAsync);
        }

        public int Port { get; }

        public static InProcessFakePlcServer Start(FakePlcRuntime runtime) => new(runtime);

        public async ValueTask DisposeAsync()
        {
            _shutdown.Cancel();
            _listener.Stop();

            try
            {
                await _acceptLoop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }

            if (_clientTasks.Count > 0)
            {
                await Task.WhenAll(_clientTasks).ConfigureAwait(false);
            }

            _shutdown.Dispose();
        }

        private async Task AcceptLoopAsync()
        {
            while (!_shutdown.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(_shutdown.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }

                _clientTasks.Add(Task.Run(() => HandleClientAsync(client)));
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                await FakePlcProtocolHandler.HandleClientAsync(
                    client,
                    _runtime,
                    logPrefix: "[test-fake-plc]",
                    _shutdown.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                client.Dispose();
            }
        }
    }
}
