using CAAutomationHub.PilotApp.Polling;
using CAAutomationHub.Wpf.ViewModels;
using CAAutomationHub.Wpf.ViewModels.Pilot;

namespace CAAutomationHub.Wpf.Tests.ViewModels;

public sealed class PilotPollingViewModelTests
{
    [Fact]
    public async Task StartStopAndPollCommands_InvokePollingServiceAndExposeSnapshot()
    {
        var service = new FakePilotPollingService();
        var viewModel = new PilotPollingViewModel(service);

        viewModel.StartPollingCommand.Execute(null);
        await Task.Yield();
        viewModel.PollOnceCommand.Execute(null);
        await Task.Yield();
        viewModel.StopPollingCommand.Execute(null);
        await Task.Yield();

        Assert.Equal(1, service.StartCallCount);
        Assert.Equal(1, service.PollOnceCallCount);
        Assert.Equal(1, service.StopCallCount);
        Assert.False(viewModel.IsPolling);
        Assert.Equal("WorkStart", viewModel.LastRequestKind);
        Assert.Equal("LOT-START-01", viewModel.LastSelectedLotId);
        Assert.Equal("Stopped", viewModel.LastStatus);
        Assert.Equal("Succeeded", viewModel.LastResultStatus);
        Assert.Equal("None", viewModel.LastErrorCode);
        Assert.Equal("WorkStart processed.", viewModel.LastMessage);
        Assert.Single(viewModel.LogEntries);
        Assert.Equal("fakeplc-local", viewModel.PlcCardTargetId);
        Assert.Equal("localhost:2004", viewModel.PlcCardTargetLabel);
        Assert.Equal("Connected", viewModel.PlcCardConnectionStatus);
        Assert.Equal("Succeeded", viewModel.PlcCardLastReadResultStatus);
        var trendPoint = Assert.Single(viewModel.TrendPoints);
        Assert.Equal(1, trendPoint.SequenceNo);
        Assert.Equal("Succeeded", trendPoint.ResultStatus);
    }

    [Fact]
    public void PollOnceCommand_ReenablesOnCapturedContextAfterPollingFailure()
    {
        using var context = new PumpingSynchronizationContext();
        var previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var service = new FailingPilotPollingService();
            var viewModel = new PilotPollingViewModel(service);
            var ownerThreadId = Environment.CurrentManagedThreadId;
            var canExecuteChangedEvents = new List<(int ThreadId, bool CanExecute)>();
            viewModel.PollOnceCommand.CanExecuteChanged += (_, _) =>
                canExecuteChangedEvents.Add((
                    Environment.CurrentManagedThreadId,
                    viewModel.PollOnceCommand.CanExecute(null)));

            viewModel.PollOnceCommand.Execute(null);
            Assert.False(viewModel.PollOnceCommand.CanExecute(null));

            context.PumpUntil(() => service.PollOnceCallCount == 1 && !viewModel.IsCommandRunning);

            Assert.Equal(PilotPollingStatus.Failed.ToString(), viewModel.LastStatus);
            Assert.Equal("DbException", viewModel.LastErrorCode);
            Assert.False(viewModel.IsCommandRunning);
            Assert.True(viewModel.PollOnceCommand.CanExecute(null));
            Assert.Equal((ownerThreadId, true), canExecuteChangedEvents.Last());

            viewModel.PollOnceCommand.Execute(null);
            context.PumpUntil(() => service.PollOnceCallCount == 2 && !viewModel.IsCommandRunning);

            Assert.Equal(2, service.PollOnceCallCount);
            Assert.True(viewModel.PollOnceCommand.CanExecute(null));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    [Fact]
    public void WorkStartScenario_ShowsAckOffObservationAfterRequestOff()
    {
        var service = new WorkStartAckOffPilotPollingService();
        var viewModel = new PilotPollingViewModel(service);

        viewModel.PollOnceCommand.Execute(null);

        Assert.Equal("WorkStartProcessed", viewModel.LastStatus);
        Assert.Equal("WorkStart WorkStartProcessed Start ACK: True", viewModel.ScenarioObservation);

        viewModel.PollOnceCommand.Execute(null);

        Assert.Equal("WorkStartAckOffWritten", viewModel.LastStatus);
        Assert.False(viewModel.LastStartAckState);
        Assert.Equal("WorkStart WorkStartAckOffWritten Start ACK: False", viewModel.ScenarioObservation);
        Assert.Equal(2, viewModel.TrendPoints.Count);
    }

    [Fact]
    public void ViewModel_DoesNotReferenceForbiddenRuntimeOrDriverTypes()
    {
        var referencedAssemblyNames = typeof(PilotPollingViewModel)
            .Assembly
            .GetReferencedAssemblies()
            .Select(static assembly => assembly.Name)
            .Where(static name => name is not null)
            .Cast<string>()
            .ToArray();

        var forbiddenAssemblyNames = new[]
        {
            "AutomationHub." + "X" + "gtDriverCore",
            "AutomationHub." + "X" + "gtDriverCore." + "Fake" + "Plc",
            "X" + "gtChannelRunner",
            string.Join('.', "Microsoft", "Data", "SqlClient")
        };

        foreach (var forbiddenAssemblyName in forbiddenAssemblyNames)
        {
            Assert.DoesNotContain(forbiddenAssemblyName, referencedAssemblyNames);
        }
    }

    [Fact]
    public void MainWindowViewModel_CreateDefaultPilotLocal_ProvidesPilotPollingViewModel()
    {
        var viewModel = MainWindowViewModel.CreateDefaultPilotLocal();

        Assert.NotNull(viewModel.PilotPolling);
        Assert.False(string.IsNullOrWhiteSpace(viewModel.PilotStatusMessage));
    }

    private sealed class FakePilotPollingService : IPilotPollingService
    {
        public event EventHandler<PilotPollingSnapshotChangedEventArgs>? SnapshotChanged;

        public int StartCallCount { get; private set; }

        public int StopCallCount { get; private set; }

        public int PollOnceCallCount { get; private set; }

        public PilotPollingSnapshot CurrentSnapshot { get; private set; } =
            PilotPollingSnapshot.Initial with
            {
                PlcCardStatus = PilotPlcCardStatus.CreateInitial("fakeplc-local", "localhost:2004")
            };

        public ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            StartCallCount++;
            CurrentSnapshot = CurrentSnapshot with { IsRunning = true, Status = PilotPollingStatus.Running };
            SnapshotChanged?.Invoke(this, new PilotPollingSnapshotChangedEventArgs(CurrentSnapshot));
            return ValueTask.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            StopCallCount++;
            CurrentSnapshot = CurrentSnapshot with { IsRunning = false, Status = PilotPollingStatus.Stopped };
            SnapshotChanged?.Invoke(this, new PilotPollingSnapshotChangedEventArgs(CurrentSnapshot));
            return ValueTask.CompletedTask;
        }

        public ValueTask<PilotPollingSnapshot> PollOnceAsync(CancellationToken cancellationToken = default)
        {
            PollOnceCallCount++;
            CurrentSnapshot = CurrentSnapshot with
            {
                LastRequestKind = WorkRequestKind.WorkStart,
                LastSelectedLotId = "LOT-START-01",
                Status = PilotPollingStatus.WorkStartProcessed,
                LastResultStatus = "Succeeded",
                LastErrorCode = "None",
                LastMessage = "WorkStart processed.",
                LastUpdatedAt = DateTimeOffset.Parse("2026-05-18T10:00:00+09:00"),
                PlcCardStatus = CurrentSnapshot.PlcCardStatus with
                {
                    ConnectionStatus = PilotPlcConnectionStatus.Connected,
                    PollingStatus = PilotPollingStatus.WorkStartProcessed.ToString(),
                    LastReadResultStatus = "Succeeded",
                    LastRequestKind = WorkRequestKind.WorkStart,
                    SelectedLotId = "LOT-START-01",
                    StartRequestActive = true,
                    CompleteRequestActive = false,
                    StartAckState = true,
                    CompleteAckState = null,
                    LastResultStatus = "Succeeded",
                    LastErrorCode = "None",
                    LastUpdatedAt = DateTimeOffset.Parse("2026-05-18T10:00:00+09:00")
                },
                TrendPoints =
                [
                    new PilotPollingTrendPoint(
                        1,
                        DateTimeOffset.Parse("2026-05-18T10:00:00+09:00"),
                        true,
                        WorkRequestKind.WorkStart,
                        DurationMs: 10,
                        "LOT-START-01",
                        "Succeeded",
                        "None")
                ],
                LogEntries =
                [
                    new PilotPollingLogEntry(
                        DateTimeOffset.Parse("2026-05-18T10:00:00+09:00"),
                        WorkRequestKind.WorkStart,
                        PilotPollingStatus.WorkStartProcessed.ToString(),
                        "WorkStart processed.")
                ]
            };
            SnapshotChanged?.Invoke(this, new PilotPollingSnapshotChangedEventArgs(CurrentSnapshot));
            return ValueTask.FromResult(CurrentSnapshot);
        }
    }

    private sealed class FailingPilotPollingService : IPilotPollingService
    {
        public event EventHandler<PilotPollingSnapshotChangedEventArgs>? SnapshotChanged;

        public int PollOnceCallCount { get; private set; }

        public PilotPollingSnapshot CurrentSnapshot { get; private set; } =
            PilotPollingSnapshot.Initial with
            {
                PlcCardStatus = PilotPlcCardStatus.CreateInitial("fakeplc-local", "localhost:2004")
            };

        public ValueTask StartAsync(CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask StopAsync(CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public async ValueTask<PilotPollingSnapshot> PollOnceAsync(CancellationToken cancellationToken = default)
        {
            PollOnceCallCount++;
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            CurrentSnapshot = CurrentSnapshot with
            {
                LastRequestKind = WorkRequestKind.WorkStart,
                LastSelectedLotId = "LOT-FAIL-01",
                Status = PilotPollingStatus.Failed,
                LastResultStatus = "Failed",
                LastErrorCode = "DbException",
                LastMessage = "SQL Server WorkStart query exception.",
                LastUpdatedAt = DateTimeOffset.Parse("2026-05-18T10:05:00+09:00"),
                PlcCardStatus = CurrentSnapshot.PlcCardStatus with
                {
                    ConnectionStatus = PilotPlcConnectionStatus.Connected,
                    PollingStatus = PilotPollingStatus.Failed.ToString(),
                    LastReadResultStatus = "Succeeded",
                    LastRequestKind = WorkRequestKind.WorkStart,
                    SelectedLotId = "LOT-FAIL-01",
                    LastResultStatus = "Failed",
                    LastErrorCode = "DbException",
                    LastUpdatedAt = DateTimeOffset.Parse("2026-05-18T10:05:00+09:00")
                },
                LogEntries =
                [
                    new PilotPollingLogEntry(
                        DateTimeOffset.Parse("2026-05-18T10:05:00+09:00"),
                        WorkRequestKind.WorkStart,
                        PilotPollingStatus.Failed.ToString(),
                        "SQL Server WorkStart query exception.")
                ]
            };
            SnapshotChanged?.Invoke(this, new PilotPollingSnapshotChangedEventArgs(CurrentSnapshot));
            return CurrentSnapshot;
        }
    }

    private sealed class WorkStartAckOffPilotPollingService : IPilotPollingService
    {
        public event EventHandler<PilotPollingSnapshotChangedEventArgs>? SnapshotChanged;

        private int _sequence;

        public PilotPollingSnapshot CurrentSnapshot { get; private set; } =
            PilotPollingSnapshot.Initial with
            {
                PlcCardStatus = PilotPlcCardStatus.CreateInitial("fakeplc-local", "localhost:2004")
            };

        public ValueTask StartAsync(CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask StopAsync(CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask<PilotPollingSnapshot> PollOnceAsync(CancellationToken cancellationToken = default)
        {
            _sequence++;
            CurrentSnapshot = _sequence == 1
                ? CreateStartAckOnSnapshot()
                : CreateStartAckOffSnapshot();
            SnapshotChanged?.Invoke(this, new PilotPollingSnapshotChangedEventArgs(CurrentSnapshot));
            return ValueTask.FromResult(CurrentSnapshot);
        }

        private PilotPollingSnapshot CreateStartAckOnSnapshot()
        {
            var timestamp = DateTimeOffset.Parse("2026-05-18T10:10:00+09:00");
            return CurrentSnapshot with
            {
                LastRequestKind = WorkRequestKind.WorkStart,
                LastSelectedLotId = "LOT-START-01",
                LastStartRequestActive = true,
                LastStartAckState = true,
                Status = PilotPollingStatus.WorkStartProcessed,
                LastResultStatus = "Succeeded",
                LastErrorCode = "None",
                LastUpdatedAt = timestamp,
                PlcCardStatus = CurrentSnapshot.PlcCardStatus with
                {
                    ConnectionStatus = PilotPlcConnectionStatus.Connected,
                    PollingStatus = PilotPollingStatus.WorkStartProcessed.ToString(),
                    LastReadResultStatus = "Succeeded",
                    LastRequestKind = WorkRequestKind.WorkStart,
                    SelectedLotId = "LOT-START-01",
                    StartRequestActive = true,
                    StartAckState = true,
                    LastResultStatus = "Succeeded",
                    LastErrorCode = "None",
                    LastUpdatedAt = timestamp
                },
                TrendPoints =
                [
                    new PilotPollingTrendPoint(
                        1,
                        timestamp,
                        true,
                        WorkRequestKind.WorkStart,
                        DurationMs: 15,
                        "LOT-START-01",
                        "Succeeded",
                        "None")
                ],
                LogEntries = []
            };
        }

        private PilotPollingSnapshot CreateStartAckOffSnapshot()
        {
            var timestamp = DateTimeOffset.Parse("2026-05-18T10:10:03+09:00");
            return CurrentSnapshot with
            {
                LastRequestKind = WorkRequestKind.WorkStart,
                LastSelectedLotId = "LOT-START-01",
                LastStartRequestActive = false,
                LastStartAckState = false,
                Status = PilotPollingStatus.WorkStartAckOffWritten,
                LastResultStatus = "AckOffWritten",
                LastErrorCode = null,
                LastUpdatedAt = timestamp,
                PlcCardStatus = CurrentSnapshot.PlcCardStatus with
                {
                    ConnectionStatus = PilotPlcConnectionStatus.Connected,
                    PollingStatus = PilotPollingStatus.WorkStartAckOffWritten.ToString(),
                    LastReadResultStatus = "Succeeded",
                    LastRequestKind = WorkRequestKind.WorkStart,
                    SelectedLotId = "LOT-START-01",
                    StartRequestActive = false,
                    StartAckState = false,
                    LastResultStatus = "AckOffWritten",
                    LastErrorCode = null,
                    LastUpdatedAt = timestamp
                },
                TrendPoints =
                [
                    CurrentSnapshot.TrendPoints[0],
                    new PilotPollingTrendPoint(
                        2,
                        timestamp,
                        true,
                        WorkRequestKind.WorkStart,
                        DurationMs: 8,
                        "LOT-START-01",
                        "AckOffWritten",
                        null)
                ],
                LogEntries = []
            };
        }
    }

    private sealed class PumpingSynchronizationContext : SynchronizationContext, IDisposable
    {
        private readonly Queue<(SendOrPostCallback Callback, object? State)> _callbacks = new();

        public override void Post(SendOrPostCallback d, object? state)
        {
            lock (_callbacks)
            {
                _callbacks.Enqueue((d, state));
            }
        }

        public void PumpUntil(Func<bool> condition)
        {
            var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
            while (!condition())
            {
                if (!PumpOne())
                {
                    Thread.Sleep(1);
                }

                if (DateTimeOffset.UtcNow > deadline)
                {
                    throw new TimeoutException("Timed out waiting for synchronization context callbacks.");
                }
            }

            while (PumpOne())
            {
            }
        }

        public void Dispose()
        {
            while (PumpOne())
            {
            }
        }

        private bool PumpOne()
        {
            (SendOrPostCallback Callback, object? State) item;
            lock (_callbacks)
            {
                if (_callbacks.Count == 0)
                {
                    return false;
                }

                item = _callbacks.Dequeue();
            }

            item.Callback(item.State);
            return true;
        }
    }
}
