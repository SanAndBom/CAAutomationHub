using System.Net;
using System.Net.Sockets;
using AutomationHub.XgtDriverCore.Client;
using AutomationHub.XgtDriverCore.FakePlc.Configuration;
using AutomationHub.XgtDriverCore.FakePlc.Runtime;
using AutomationHub.XgtDriverCore.Transport;
using CAAutomationHub.PilotFlows.WorkComplete;
using CAAutomationHub.PilotFlows.Xgt.WorkComplete;

namespace CAAutomationHub.PilotFlows.Xgt.Tests.WorkComplete;

public sealed class WorkCompleteXgtFakePlcIntegrationTests
{
    private const int FakePlcCompleteSignalWordIndex = 84;

    [Fact]
    public async Task AckOnOffAsync_WithFakePlc_WritesOneThenZeroToCompleteAckTarget()
    {
        var runtime = CreateRuntime(completeSignal: true);
        await using var fakePlc = InProcessFakePlcServer.Start(runtime);
        await using var session = CreateSession(fakePlc.Port);
        var operations = new WorkCompleteXgtPlcOperations(session);
        var service = new WorkCompleteAckService(
            operations,
            new WorkCompleteAckOptions { CompleteSignalWordIndex = FakePlcCompleteSignalWordIndex });

        var ackOnResult = await service.AckOnAsync();

        Assert.True(ackOnResult.Succeeded);
        Assert.Equal(WorkCompleteAckStatus.AckOnWritten, ackOnResult.Status);
        Assert.Equal(new byte[] { 0x01, 0x00 }, runtime.ReadContinuous(FakePlcMemoryImage.Db11418, 2));

        runtime.MemoryImage.WriteUInt16AtDAddress(5084, 0x0000);

        var ackOffResult = await service.AckOffAsync();

        Assert.True(ackOffResult.Succeeded);
        Assert.Equal(WorkCompleteAckStatus.AckOffWritten, ackOffResult.Status);
        Assert.Equal(new byte[] { 0x00, 0x00 }, runtime.ReadContinuous(FakePlcMemoryImage.Db11418, 2));
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

    private static FakePlcRuntime CreateRuntime(bool completeSignal)
    {
        var config = new FakePlcMapConfig
        {
            BaseBlocks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [FakePlcMemoryImage.Db10000] = new('0', WorkCompleteXgtReadOptions.DefaultReadWordCount * 2 * 2),
                [FakePlcMemoryImage.Db11000] = new('0', 70 * 2 * 2),
                [FakePlcMemoryImage.Db11410] = "0000",
                [FakePlcMemoryImage.Db11416] = "0000",
                [FakePlcMemoryImage.Db11418] = "0000"
            },
            Scenario = new FakePlcScenarioConfig
            {
                LotId1 = "S0007652610B",
                LotId2 = string.Empty,
                StartSignal = false,
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
