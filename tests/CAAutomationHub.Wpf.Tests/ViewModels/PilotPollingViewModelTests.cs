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
            PilotPollingSnapshot.Initial;

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
            PilotPollingSnapshot.Initial;

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
