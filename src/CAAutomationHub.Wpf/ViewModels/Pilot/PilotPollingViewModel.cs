using System.Windows.Input;
using CAAutomationHub.PilotApp.Polling;

namespace CAAutomationHub.Wpf.ViewModels.Pilot;

public sealed class PilotPollingViewModel : ViewModelBase
{
    private readonly IPilotPollingService _pollingService;
    private bool _isCommandRunning;
    private bool _isPolling;
    private string _lastRequestKind = WorkRequestKind.None.ToString();
    private string? _lastSelectedLotId;
    private bool _lastStartRequestActive;
    private bool _lastCompleteRequestActive;
    private bool? _lastStartAckState;
    private bool? _lastCompleteAckState;
    private string? _lastStatus;
    private string? _lastResultStatus;
    private string? _lastErrorCode;
    private string? _lastMessage;
    private DateTimeOffset? _lastUpdatedAt;
    private IReadOnlyList<PilotPollingLogEntry> _logEntries = [];

    public PilotPollingViewModel(IPilotPollingService pollingService)
    {
        _pollingService = pollingService ?? throw new ArgumentNullException(nameof(pollingService));
        _pollingService.SnapshotChanged += OnSnapshotChanged;

        StartPollingCommand = new RelayCommand(_ => _ = RunCommandAsync(_pollingService.StartAsync), _ => !IsCommandRunning);
        StopPollingCommand = new RelayCommand(_ => _ = RunCommandAsync(_pollingService.StopAsync), _ => !IsCommandRunning);
        PollOnceCommand = new RelayCommand(_ => _ = RunPollOnceAsync(), _ => !IsCommandRunning);

        ApplySnapshot(_pollingService.CurrentSnapshot);
    }

    public ICommand StartPollingCommand { get; }

    public ICommand StopPollingCommand { get; }

    public ICommand PollOnceCommand { get; }

    public bool IsCommandRunning
    {
        get => _isCommandRunning;
        private set
        {
            if (SetProperty(ref _isCommandRunning, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public bool IsPolling
    {
        get => _isPolling;
        private set => SetProperty(ref _isPolling, value);
    }

    public string LastRequestKind
    {
        get => _lastRequestKind;
        private set => SetProperty(ref _lastRequestKind, value);
    }

    public string? LastSelectedLotId
    {
        get => _lastSelectedLotId;
        private set => SetProperty(ref _lastSelectedLotId, value);
    }

    public bool LastStartRequestActive
    {
        get => _lastStartRequestActive;
        private set => SetProperty(ref _lastStartRequestActive, value);
    }

    public bool LastCompleteRequestActive
    {
        get => _lastCompleteRequestActive;
        private set => SetProperty(ref _lastCompleteRequestActive, value);
    }

    public bool? LastStartAckState
    {
        get => _lastStartAckState;
        private set => SetProperty(ref _lastStartAckState, value);
    }

    public bool? LastCompleteAckState
    {
        get => _lastCompleteAckState;
        private set => SetProperty(ref _lastCompleteAckState, value);
    }

    public string? LastStatus
    {
        get => _lastStatus;
        private set => SetProperty(ref _lastStatus, value);
    }

    public string? LastResultStatus
    {
        get => _lastResultStatus;
        private set => SetProperty(ref _lastResultStatus, value);
    }

    public string? LastErrorCode
    {
        get => _lastErrorCode;
        private set => SetProperty(ref _lastErrorCode, value);
    }

    public string? LastMessage
    {
        get => _lastMessage;
        private set => SetProperty(ref _lastMessage, value);
    }

    public DateTimeOffset? LastUpdatedAt
    {
        get => _lastUpdatedAt;
        private set => SetProperty(ref _lastUpdatedAt, value);
    }

    public IReadOnlyList<PilotPollingLogEntry> LogEntries
    {
        get => _logEntries;
        private set => SetProperty(ref _logEntries, value);
    }

    private async Task RunCommandAsync(Func<CancellationToken, ValueTask> command)
    {
        if (IsCommandRunning) return;

        IsCommandRunning = true;
        try
        {
            await command(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            IsCommandRunning = false;
        }
    }

    private async Task RunPollOnceAsync()
    {
        if (IsCommandRunning) return;

        IsCommandRunning = true;
        try
        {
            var snapshot = await _pollingService.PollOnceAsync().ConfigureAwait(false);
            ApplySnapshot(snapshot);
        }
        finally
        {
            IsCommandRunning = false;
        }
    }

    private void OnSnapshotChanged(object? sender, PilotPollingSnapshotChangedEventArgs e) =>
        ApplySnapshot(e.Snapshot);

    private void ApplySnapshot(PilotPollingSnapshot snapshot)
    {
        IsPolling = snapshot.IsRunning;
        LastRequestKind = snapshot.LastRequestKind.ToString();
        LastSelectedLotId = snapshot.LastSelectedLotId;
        LastStartRequestActive = snapshot.LastStartRequestActive;
        LastCompleteRequestActive = snapshot.LastCompleteRequestActive;
        LastStartAckState = snapshot.LastStartAckState;
        LastCompleteAckState = snapshot.LastCompleteAckState;
        LastStatus = snapshot.Status.ToString();
        LastResultStatus = snapshot.LastResultStatus;
        LastErrorCode = snapshot.LastErrorCode;
        LastMessage = snapshot.LastMessage;
        LastUpdatedAt = snapshot.LastUpdatedAt;
        LogEntries = snapshot.LogEntries;
    }

    private void RaiseCommandStates()
    {
        if (StartPollingCommand is RelayCommand startPollingCommand)
        {
            startPollingCommand.RaiseCanExecuteChanged();
        }

        if (StopPollingCommand is RelayCommand stopPollingCommand)
        {
            stopPollingCommand.RaiseCanExecuteChanged();
        }

        if (PollOnceCommand is RelayCommand pollOnceCommand)
        {
            pollOnceCommand.RaiseCanExecuteChanged();
        }
    }
}
