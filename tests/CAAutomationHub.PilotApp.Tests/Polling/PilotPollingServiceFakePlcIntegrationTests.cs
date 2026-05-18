using System.Net;
using System.Net.Sockets;
using AutomationHub.XgtDriverCore.Client;
using AutomationHub.XgtDriverCore.FakePlc.Configuration;
using AutomationHub.XgtDriverCore.FakePlc.Runtime;
using AutomationHub.XgtDriverCore.Transport;
using CAAutomationHub.PilotApp.Polling;
using CAAutomationHub.PilotApp.WorkStart;
using CAAutomationHub.PilotFlows.WorkComplete;
using CAAutomationHub.PilotFlows.WorkStart;
using CAAutomationHub.PilotFlows.Xgt.WorkComplete;
using CAAutomationHub.PilotFlows.Xgt.WorkStart;

namespace CAAutomationHub.PilotApp.Tests.Polling;

public sealed class PilotPollingServiceFakePlcIntegrationTests
{
    private const int FakePlcStartSignalWordIndex = 83;
    private const int FakePlcCompleteSignalWordIndex = 84;

    [Fact]
    public async Task PollOnceAsync_WithFakePlc_ProcessesStartAndCompleteOnOffCycle()
    {
        var runtime = CreateRuntime(startSignal: true, completeSignal: false);
        await using var fakePlc = InProcessFakePlcServer.Start(runtime);
        await using var session = CreateSession(fakePlc.Port);
        var service = CreatePollingService(session);

        await service.StartAsync();

        var startOn = await service.PollOnceAsync();

        Assert.Equal(PilotPollingStatus.WorkStartProcessed, startOn.Status);
        Assert.Equal(WorkRequestKind.WorkStart, startOn.LastRequestKind);
        Assert.Equal("S0007652610B", startOn.LastSelectedLotId);
        Assert.True(startOn.LastStartAckState);
        Assert.Equal(new byte[] { 0x01, 0x00 }, runtime.ReadContinuous(FakePlcMemoryImage.Db11416, 2));

        runtime.MemoryImage.WriteUInt16AtDAddress(5083, 0x0000);

        var startOff = await service.PollOnceAsync();

        Assert.Equal(PilotPollingStatus.WorkStartAckOffWritten, startOff.Status);
        Assert.False(startOff.LastStartAckState);
        Assert.Equal(new byte[] { 0x00, 0x00 }, runtime.ReadContinuous(FakePlcMemoryImage.Db11416, 2));

        runtime.MemoryImage.WriteUInt16AtDAddress(5084, 0x0001);

        var completeOn = await service.PollOnceAsync();

        Assert.Equal(PilotPollingStatus.WorkCompleteAckOnWritten, completeOn.Status);
        Assert.Equal(WorkRequestKind.WorkComplete, completeOn.LastRequestKind);
        Assert.True(completeOn.LastCompleteAckState);
        Assert.Equal(new byte[] { 0x01, 0x00 }, runtime.ReadContinuous(FakePlcMemoryImage.Db11418, 2));

        runtime.MemoryImage.WriteUInt16AtDAddress(5084, 0x0000);

        var completeOff = await service.PollOnceAsync();

        Assert.Equal(PilotPollingStatus.WorkCompleteAckOffWritten, completeOff.Status);
        Assert.False(completeOff.LastCompleteAckState);
        Assert.Equal(new byte[] { 0x00, 0x00 }, runtime.ReadContinuous(FakePlcMemoryImage.Db11418, 2));
    }

    private static PilotPollingService CreatePollingService(XgtSession session)
    {
        var startAckOnOperations = new WorkStartXgtPlcOperations(
            session,
            WorkStartXgtReadOptions.Default,
            WorkStartXgtWriteOptions.Default);
        var startAckOffOperations = new WorkStartXgtPlcOperations(
            session,
            WorkStartXgtReadOptions.Default,
            new WorkStartXgtWriteOptions(
                WorkStartXgtWriteOptions.DefaultProcessPayloadWriteVariable,
                WorkStartXgtWriteOptions.DefaultStartAckWriteVariable,
                startAckValue: 0,
                WorkStartXgtWriteOptions.DefaultErrorCodeWriteVariable));
        var completeOperations = new WorkCompleteXgtPlcOperations(session);

        var workStartOptions = new WorkStartFlowOptions
        {
            StartSignalWordIndex = FakePlcStartSignalWordIndex
        };
        var workCompleteOptions = new WorkCompleteAckOptions
        {
            CompleteSignalWordIndex = FakePlcCompleteSignalWordIndex
        };

        var startFlowService = new WorkStartFlowService(
            startAckOnOperations,
            new FakeWorkStartDataQuery(),
            workStartOptions);
        var startExecutionService = new WorkStartExecutionService(
            new WorkStartFlowServiceRunner(startFlowService));
        var reader = new PilotPollingRequestStateReader(
            startAckOnOperations,
            completeOperations,
            workStartOptions,
            workCompleteOptions);
        var port = new PilotPollingFlowPort(
            reader,
            startExecutionService,
            new WorkStartAckOffService(
                startAckOffOperations,
                new WorkStartAckOffOptions { StartSignalWordIndex = FakePlcStartSignalWordIndex }),
            new WorkCompleteAckService(completeOperations, workCompleteOptions));

        return new PilotPollingService(
            port,
            new PilotPollingOptions { TargetId = "PLC-FAKE" });
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

    private static FakePlcRuntime CreateRuntime(bool startSignal, bool completeSignal)
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
                CompleteSignal = completeSignal,
                HeartbeatEnabled = true,
                HeartbeatInitialValue = true
            },
            Rules = new FakePlcRuleConfig()
        };

        return new FakePlcRuntime(
            FakePlcScenarioInitializer.CreateMemoryImage(config),
            config.Rules);
    }

    private sealed class FakeWorkStartDataQuery : IWorkStartDataQuery
    {
        public ValueTask<WorkStartDataQueryResult> QueryAsync(
            string lotId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(WorkStartDataQueryResult.Success(new WorkStartProcessData
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
            }));
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
