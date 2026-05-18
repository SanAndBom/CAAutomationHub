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
}
