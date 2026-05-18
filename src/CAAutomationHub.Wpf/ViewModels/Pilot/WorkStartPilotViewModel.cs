using System.Windows.Input;
using CAAutomationHub.PilotApp.WorkStart;

namespace CAAutomationHub.Wpf.ViewModels.Pilot;

public sealed class WorkStartPilotViewModel : ViewModelBase
{
    private readonly IWorkStartExecutionService _executionService;
    private readonly string _targetId;
    private bool _isBusy;
    private bool? _lastSucceeded;
    private string? _lastStatus;
    private string? _lastStep;
    private int? _lastErrorCode;
    private string? _lastErrorCodeName;
    private string? _lastMessage;
    private string? _selectedLotId;
    private bool _lastErrorWriteExpected;
    private DateTimeOffset? _lastStartedAt;
    private DateTimeOffset? _lastCompletedAt;
    private TimeSpan? _lastDuration;

    public WorkStartPilotViewModel(IWorkStartExecutionService executionService, string targetId)
    {
        _executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));
        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("Target id is required.", nameof(targetId));
        }

        _targetId = targetId;
        ExecuteOnceCommand = new RelayCommand(
            _ => _ = ExecuteOnceAsync().AsTask(),
            _ => !IsBusy);
    }

    public string TargetId => _targetId;

    public ICommand ExecuteOnceCommand { get; }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value)
                && ExecuteOnceCommand is RelayCommand executeOnceCommand)
            {
                executeOnceCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool? LastSucceeded
    {
        get => _lastSucceeded;
        private set => SetProperty(ref _lastSucceeded, value);
    }

    public string? LastStatus
    {
        get => _lastStatus;
        private set => SetProperty(ref _lastStatus, value);
    }

    public string? LastStep
    {
        get => _lastStep;
        private set => SetProperty(ref _lastStep, value);
    }

    public int? LastErrorCode
    {
        get => _lastErrorCode;
        private set => SetProperty(ref _lastErrorCode, value);
    }

    public string? LastErrorCodeName
    {
        get => _lastErrorCodeName;
        private set => SetProperty(ref _lastErrorCodeName, value);
    }

    public string? LastMessage
    {
        get => _lastMessage;
        private set => SetProperty(ref _lastMessage, value);
    }

    public string? SelectedLotId
    {
        get => _selectedLotId;
        private set => SetProperty(ref _selectedLotId, value);
    }

    public bool LastErrorWriteExpected
    {
        get => _lastErrorWriteExpected;
        private set => SetProperty(ref _lastErrorWriteExpected, value);
    }

    public DateTimeOffset? LastStartedAt
    {
        get => _lastStartedAt;
        private set => SetProperty(ref _lastStartedAt, value);
    }

    public DateTimeOffset? LastCompletedAt
    {
        get => _lastCompletedAt;
        private set => SetProperty(ref _lastCompletedAt, value);
    }

    public TimeSpan? LastDuration
    {
        get => _lastDuration;
        private set => SetProperty(ref _lastDuration, value);
    }

    public async ValueTask ExecuteOnceAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            var result = await _executionService
                .ExecuteOnceAsync(new WorkStartExecutionRequest(TargetId: _targetId), cancellationToken)
                .ConfigureAwait(false);

            ApplyResult(result);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            ApplyException(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyResult(WorkStartExecutionResult result)
    {
        LastSucceeded = result.Succeeded;
        LastStatus = result.Status;
        LastStep = result.Step;
        LastErrorCode = result.ErrorCode;
        LastErrorCodeName = result.ErrorCodeName;
        LastMessage = result.Message;
        SelectedLotId = result.SelectedLotId;
        LastErrorWriteExpected = result.ErrorWriteExpected;
        LastStartedAt = result.StartedAt;
        LastCompletedAt = result.CompletedAt;
        LastDuration = result.Duration;
    }

    private void ApplyException(Exception exception)
    {
        LastSucceeded = false;
        LastStatus = "Failed";
        LastStep = "exception";
        LastErrorCode = null;
        LastErrorCodeName = null;
        LastMessage = exception.Message;
        SelectedLotId = null;
        LastErrorWriteExpected = false;
        LastStartedAt = null;
        LastCompletedAt = null;
        LastDuration = null;
    }
}
