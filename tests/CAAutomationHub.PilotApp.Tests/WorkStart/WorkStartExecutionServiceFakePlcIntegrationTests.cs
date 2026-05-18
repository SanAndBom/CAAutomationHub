using System.Net;
using System.Net.Sockets;
using AutomationHub.XgtDriverCore.Client;
using AutomationHub.XgtDriverCore.FakePlc.Configuration;
using AutomationHub.XgtDriverCore.FakePlc.Runtime;
using AutomationHub.XgtDriverCore.Transport;
using CAAutomationHub.PilotApp.WorkStart;
using CAAutomationHub.PilotFlows.WorkStart;
using CAAutomationHub.PilotFlows.Xgt.WorkStart;

namespace CAAutomationHub.PilotApp.Tests.WorkStart;

public sealed class WorkStartExecutionServiceFakePlcIntegrationTests
{
    private const int FakePlcStartSignalWordIndex = 83;
    private const int LotId1WordOffset = WorkStartReadBlockLayout.DefaultLotId1WordOffset;
    private const int LotId2WordOffset = WorkStartReadBlockLayout.DefaultLotId2WordOffset;
    private const int LotIdWordLength = WorkStartReadBlockLayout.DefaultLotIdWordLength;

    [Fact]
    public async Task ExecuteOnceAsync_ReturnsSuccess_WhenFakePlcWorkStartTransactionSucceeds()
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
        var service = CreateExecutionService(session, query);

        var result = await service.ExecuteOnceAsync(new WorkStartExecutionRequest(TargetId: "PLC-01"));

        Assert.True(result.Succeeded);
        Assert.Equal("Succeeded", result.Status);
        Assert.Equal("completed", result.Step);
        Assert.Equal(0, result.ErrorCode);
        Assert.Equal("None", result.ErrorCodeName);
        Assert.Null(result.Message);
        Assert.Equal(expectedLotId, result.SelectedLotId);
        Assert.False(result.ErrorWriteExpected);
        Assert.True(result.Duration >= TimeSpan.Zero);
        Assert.Equal(new[] { expectedLotId }, query.QueriedLotIds);
        Assert.Equal(expectedPayload, runtime.LastBulkWrite);
        Assert.Equal(expectedPayload, runtime.ReadContinuous(FakePlcMemoryImage.Db11000, expectedPayload.Length));
        Assert.Equal((ushort)1, runtime.LastAckValue);
        Assert.Equal(new byte[] { 0x01, 0x00 }, runtime.ReadContinuous(FakePlcMemoryImage.Db11416, 2));
        Assert.Null(runtime.LastErrorCode);
        Assert.Equal(new byte[] { 0x00, 0x00 }, runtime.ReadContinuous(FakePlcMemoryImage.Db11410, 2));
    }

    [Fact]
    public async Task ExecuteOnceAsync_ReturnsFailureDisplayResult_WhenFakeDbNotFound()
    {
        var runtime = CreateRuntime();
        var expectedLotId = "S0007652610B";
        var query = new FakeWorkStartDataQuery(WorkStartDataQueryResult.NotFound(expectedLotId));
        await using var fakePlc = InProcessFakePlcServer.Start(runtime);
        await using var session = CreateSession(fakePlc.Port);
        var service = CreateExecutionService(session, query);

        var result = await service.ExecuteOnceAsync(new WorkStartExecutionRequest(TargetId: "PLC-01"));

        Assert.False(result.Succeeded);
        Assert.Equal("Failed", result.Status);
        Assert.Equal("db-query", result.Step);
        Assert.Equal(2301, result.ErrorCode);
        Assert.Equal("DbNotFound", result.ErrorCodeName);
        Assert.Equal(expectedLotId, result.SelectedLotId);
        Assert.True(result.ErrorWriteExpected);
        Assert.True(result.Duration >= TimeSpan.Zero);
        Assert.Equal(new[] { expectedLotId }, query.QueriedLotIds);
        Assert.Equal((ushort)WorkStartErrorCode.DbNotFound, runtime.LastErrorCode);
        Assert.Equal(new byte[] { 0xFD, 0x08 }, runtime.ReadContinuous(FakePlcMemoryImage.Db11410, 2));
        Assert.Null(runtime.LastBulkWrite);
        Assert.Null(runtime.LastAckValue);
    }

    private static WorkStartExecutionService CreateExecutionService(
        XgtSession session,
        IWorkStartDataQuery query)
    {
        var operations = new WorkStartXgtPlcOperations(
            session,
            WorkStartXgtReadOptions.Default,
            WorkStartXgtWriteOptions.Default);
        var flowService = new WorkStartFlowService(
            operations,
            query,
            new WorkStartFlowOptions
            {
                StartSignalWordIndex = FakePlcStartSignalWordIndex,
                LotId1WordOffset = LotId1WordOffset,
                LotId2WordOffset = LotId2WordOffset,
                LotIdWordLength = LotIdWordLength
            });

        return new WorkStartExecutionService(new WorkStartFlowServiceRunner(flowService));
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

    private static FakePlcRuntime CreateRuntime()
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
                StartSignal = true,
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
                    logPrefix: "[pilotapp-test-fake-plc]",
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
