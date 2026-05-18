using CAAutomationHub.PilotApp.WorkStart;
using CAAutomationHub.Wpf.ViewModels.Pilot;

namespace CAAutomationHub.Wpf.Tests.ViewModels;

public sealed class WorkStartPilotViewModelTests
{
    [Fact]
    public async Task ExecuteOnceAsync_UpdatesDisplayState_WhenExecutionSucceeds()
    {
        var service = new FakeWorkStartExecutionService(
            new WorkStartExecutionResult
            {
                Succeeded = true,
                Status = "Succeeded",
                Step = "completed",
                ErrorCode = 0,
                ErrorCodeName = "None",
                Message = null,
                SelectedLotId = "LOT123456789",
                ErrorWriteExpected = false,
                StartedAt = DateTimeOffset.Parse("2026-05-18T10:00:00+09:00"),
                CompletedAt = DateTimeOffset.Parse("2026-05-18T10:00:02+09:00"),
                Duration = TimeSpan.FromSeconds(2)
            });
        var viewModel = new WorkStartPilotViewModel(service, "PLC-01");

        await viewModel.ExecuteOnceAsync();

        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.LastSucceeded);
        Assert.Equal("Succeeded", viewModel.LastStatus);
        Assert.Equal("completed", viewModel.LastStep);
        Assert.Equal(0, viewModel.LastErrorCode);
        Assert.Equal("None", viewModel.LastErrorCodeName);
        Assert.Null(viewModel.LastMessage);
        Assert.Equal("LOT123456789", viewModel.SelectedLotId);
        Assert.False(viewModel.LastErrorWriteExpected);
        Assert.Equal(DateTimeOffset.Parse("2026-05-18T10:00:00+09:00"), viewModel.LastStartedAt);
        Assert.Equal(DateTimeOffset.Parse("2026-05-18T10:00:02+09:00"), viewModel.LastCompletedAt);
        Assert.Equal(TimeSpan.FromSeconds(2), viewModel.LastDuration);
    }

    [Fact]
    public async Task ExecuteOnceAsync_UpdatesDisplayState_WhenExecutionFails()
    {
        var service = new FakeWorkStartExecutionService(
            new WorkStartExecutionResult
            {
                Succeeded = false,
                Status = "Failed",
                Step = "db-query",
                ErrorCode = 2301,
                ErrorCodeName = "DbNotFound",
                Message = "No row found for LOT ID.",
                SelectedLotId = "LOT123456789",
                ErrorWriteExpected = true,
                StartedAt = DateTimeOffset.Parse("2026-05-18T10:05:00+09:00"),
                CompletedAt = DateTimeOffset.Parse("2026-05-18T10:05:01+09:00"),
                Duration = TimeSpan.FromSeconds(1)
            });
        var viewModel = new WorkStartPilotViewModel(service, "PLC-01");

        await viewModel.ExecuteOnceAsync();

        Assert.False(viewModel.IsBusy);
        Assert.False(viewModel.LastSucceeded);
        Assert.Equal("Failed", viewModel.LastStatus);
        Assert.Equal("db-query", viewModel.LastStep);
        Assert.Equal(2301, viewModel.LastErrorCode);
        Assert.Equal("DbNotFound", viewModel.LastErrorCodeName);
        Assert.Equal("No row found for LOT ID.", viewModel.LastMessage);
        Assert.Equal("LOT123456789", viewModel.SelectedLotId);
        Assert.True(viewModel.LastErrorWriteExpected);
        Assert.Equal(TimeSpan.FromSeconds(1), viewModel.LastDuration);
    }

    [Fact]
    public async Task ExecuteOnceAsync_PassesTargetId_ToExecutionService()
    {
        var service = new FakeWorkStartExecutionService(CreateSuccessResult());
        var viewModel = new WorkStartPilotViewModel(service, "PLC-03");

        await viewModel.ExecuteOnceAsync();

        Assert.NotNull(service.LastRequest);
        Assert.Equal("PLC-03", service.LastRequest.TargetId);
    }

    [Fact]
    public async Task ExecuteOnceAsync_BlocksDuplicateExecution_WhenBusy()
    {
        var service = new BlockingWorkStartExecutionService(CreateSuccessResult());
        var viewModel = new WorkStartPilotViewModel(service, "PLC-01");

        var firstExecution = viewModel.ExecuteOnceAsync().AsTask();
        await service.ExecutionStarted.Task;
        await viewModel.ExecuteOnceAsync();

        Assert.True(viewModel.IsBusy);
        Assert.Equal(1, service.CallCount);
        service.Release();
        await firstExecution;
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.LastSucceeded);
    }

    [Fact]
    public async Task ExecuteOnceCommand_InvokesExecutionService()
    {
        var service = new FakeWorkStartExecutionService(CreateSuccessResult());
        var viewModel = new WorkStartPilotViewModel(service, "PLC-01");

        viewModel.ExecuteOnceCommand.Execute(null);
        await Task.Yield();

        Assert.Equal(1, service.CallCount);
        Assert.NotNull(service.LastRequest);
        Assert.Equal("PLC-01", service.LastRequest.TargetId);
        Assert.True(viewModel.LastSucceeded);
        Assert.Equal("completed", viewModel.LastStep);
    }

    [Fact]
    public async Task ExecuteOnceCommand_RespectsBusyState()
    {
        var service = new BlockingWorkStartExecutionService(CreateSuccessResult());
        var viewModel = new WorkStartPilotViewModel(service, "PLC-01");

        viewModel.ExecuteOnceCommand.Execute(null);
        await service.ExecutionStarted.Task;
        viewModel.ExecuteOnceCommand.Execute(null);

        Assert.True(viewModel.IsBusy);
        Assert.Equal(1, service.CallCount);
        service.Release();
        await Task.Yield();
    }

    [Fact]
    public async Task ExecuteOnceCommand_CanExecuteReflectsBusyState()
    {
        var service = new BlockingWorkStartExecutionService(CreateSuccessResult());
        var viewModel = new WorkStartPilotViewModel(service, "PLC-01");

        Assert.True(viewModel.ExecuteOnceCommand.CanExecute(null));
        viewModel.ExecuteOnceCommand.Execute(null);
        await service.ExecutionStarted.Task;

        Assert.False(viewModel.ExecuteOnceCommand.CanExecute(null));
        service.Release();
        await WaitUntilAsync(() => !viewModel.IsBusy);
        Assert.True(viewModel.ExecuteOnceCommand.CanExecute(null));
    }

    [Fact]
    public void ViewModel_DoesNotReferenceForbiddenRuntimeOrDriverTypes()
    {
        var referencedAssemblyNames = typeof(WorkStartPilotViewModel)
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

        var propertyTypeNames = typeof(WorkStartPilotViewModel)
            .GetProperties()
            .Select(static property => property.PropertyType.FullName ?? property.PropertyType.Name)
            .ToArray();

        Assert.DoesNotContain(propertyTypeNames, static name => name.Contains(
            string.Join("", "Run", "time", "Snapshot"),
            StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyTypeNames, static name => name.Contains(
            string.Join("", "Dashboard", "Snapshot"),
            StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyTypeNames, static name => name.Contains(
            string.Join("", "Channel", "Polling", "Result"),
            StringComparison.OrdinalIgnoreCase));
    }

    private static WorkStartExecutionResult CreateSuccessResult()
        => new()
        {
            Succeeded = true,
            Status = "Succeeded",
            Step = "completed",
            ErrorCode = 0,
            ErrorCodeName = "None",
            Message = null,
            SelectedLotId = "LOT123456789",
            ErrorWriteExpected = false,
            StartedAt = DateTimeOffset.Parse("2026-05-18T10:00:00+09:00"),
            CompletedAt = DateTimeOffset.Parse("2026-05-18T10:00:02+09:00"),
            Duration = TimeSpan.FromSeconds(2)
        };

    private sealed class FakeWorkStartExecutionService : IWorkStartExecutionService
    {
        private readonly WorkStartExecutionResult _result;

        public FakeWorkStartExecutionService(WorkStartExecutionResult result)
        {
            _result = result;
        }

        public WorkStartExecutionRequest? LastRequest { get; private set; }

        public int CallCount { get; private set; }

        public ValueTask<WorkStartExecutionResult> ExecuteOnceAsync(
            WorkStartExecutionRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;
            return ValueTask.FromResult(_result);
        }
    }

    private sealed class BlockingWorkStartExecutionService : IWorkStartExecutionService
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly WorkStartExecutionResult _result;

        public BlockingWorkStartExecutionService(WorkStartExecutionResult result)
        {
            _result = result;
        }

        public TaskCompletionSource ExecutionStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CallCount { get; private set; }

        public async ValueTask<WorkStartExecutionResult> ExecuteOnceAsync(
            WorkStartExecutionRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            ExecutionStarted.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
            return _result;
        }

        public void Release() => _release.SetResult();
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        while (!condition())
        {
            await Task.Delay(10, cts.Token);
        }
    }
}
