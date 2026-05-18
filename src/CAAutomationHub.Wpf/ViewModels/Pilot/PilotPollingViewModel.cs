using System.Windows.Input;
using CAAutomationHub.PilotApp.Polling;

namespace CAAutomationHub.Wpf.ViewModels.Pilot;

public sealed class PilotPollingViewModel : ViewModelBase
{
    private readonly IPilotPollingService _pollingService;
    private readonly SynchronizationContext? _synchronizationContext;
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
    private string _plcCardTargetId = "-";
    private string _plcCardTargetLabel = "-";
    private string _plcCardDisplayName = "-";
    private string _plcCardLineName = "-";
    private string _plcCardHostPort = "-";
    private string _plcCardConnectionStatus = PilotPlcConnectionStatus.Unknown.ToString();
    private string _plcCardLastReadResultStatus = "-";
    private string _plcDetailDisplayName = "-";
    private string _plcDetailHostPort = "-";
    private string _plcDetailPollingStatus = "-";
    private string _plcDetailLastResponse = "-";
    private string _plcDetailLastMessage = "-";
    private string _scenarioObservation = "-";
    private IReadOnlyList<PilotPollingTrendPoint> _trendPoints = [];
    private IReadOnlyList<PilotPollingLogEntry> _logEntries = [];

    public PilotPollingViewModel(IPilotPollingService pollingService)
    {
        _pollingService = pollingService ?? throw new ArgumentNullException(nameof(pollingService));
        _synchronizationContext = SynchronizationContext.Current;
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

    public string PlcCardTargetId
    {
        get => _plcCardTargetId;
        private set => SetProperty(ref _plcCardTargetId, value);
    }

    public string PlcCardTargetLabel
    {
        get => _plcCardTargetLabel;
        private set => SetProperty(ref _plcCardTargetLabel, value);
    }

    public string PlcCardDisplayName
    {
        get => _plcCardDisplayName;
        private set => SetProperty(ref _plcCardDisplayName, value);
    }

    public string PlcCardLineName
    {
        get => _plcCardLineName;
        private set => SetProperty(ref _plcCardLineName, value);
    }

    public string PlcCardHostPort
    {
        get => _plcCardHostPort;
        private set => SetProperty(ref _plcCardHostPort, value);
    }

    public string PlcCardConnectionStatus
    {
        get => _plcCardConnectionStatus;
        private set => SetProperty(ref _plcCardConnectionStatus, value);
    }

    public string PlcCardLastReadResultStatus
    {
        get => _plcCardLastReadResultStatus;
        private set => SetProperty(ref _plcCardLastReadResultStatus, value);
    }

    public string PlcDetailDisplayName
    {
        get => _plcDetailDisplayName;
        private set => SetProperty(ref _plcDetailDisplayName, value);
    }

    public string PlcDetailHostPort
    {
        get => _plcDetailHostPort;
        private set => SetProperty(ref _plcDetailHostPort, value);
    }

    public string PlcDetailPollingStatus
    {
        get => _plcDetailPollingStatus;
        private set => SetProperty(ref _plcDetailPollingStatus, value);
    }

    public string PlcDetailLastResponse
    {
        get => _plcDetailLastResponse;
        private set => SetProperty(ref _plcDetailLastResponse, value);
    }

    public string PlcDetailLastMessage
    {
        get => _plcDetailLastMessage;
        private set => SetProperty(ref _plcDetailLastMessage, value);
    }

    public string ScenarioObservation
    {
        get => _scenarioObservation;
        private set => SetProperty(ref _scenarioObservation, value);
    }

    public IReadOnlyList<PilotPollingTrendPoint> TrendPoints
    {
        get => _trendPoints;
        private set => SetProperty(ref _trendPoints, value);
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
            await command(CancellationToken.None);
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
            var snapshot = await _pollingService.PollOnceAsync();
            ApplySnapshot(snapshot);
        }
        finally
        {
            IsCommandRunning = false;
        }
    }

    private void OnSnapshotChanged(object? sender, PilotPollingSnapshotChangedEventArgs e) =>
        RunOnCapturedContext(() => ApplySnapshot(e.Snapshot));

    private void RunOnCapturedContext(Action action)
    {
        if (_synchronizationContext is null || SynchronizationContext.Current == _synchronizationContext)
        {
            action();
            return;
        }

        _synchronizationContext.Post(static state =>
        {
            var callback = (Action)state!;
            callback();
        }, action);
    }

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
        PlcCardTargetId = snapshot.PlcCardStatus.TargetId;
        PlcCardTargetLabel = snapshot.PlcCardStatus.TargetLabel;
        PlcCardDisplayName = snapshot.PlcCardStatus.DisplayName;
        PlcCardLineName = snapshot.PlcCardStatus.LineName;
        PlcCardHostPort = snapshot.PlcCardStatus.HostPort;
        PlcCardConnectionStatus = snapshot.PlcCardStatus.ConnectionStatus.ToString();
        PlcCardLastReadResultStatus = snapshot.PlcCardStatus.LastReadResultStatus ?? "-";
        PlcDetailDisplayName = snapshot.PlcCardStatus.DisplayName;
        PlcDetailHostPort = snapshot.PlcCardStatus.HostPort;
        PlcDetailPollingStatus = snapshot.PlcCardStatus.PollingStatus;
        PlcDetailLastResponse = snapshot.PlcCardStatus.LastResultStatus
            ?? snapshot.PlcCardStatus.LastReadResultStatus
            ?? "-";
        PlcDetailLastMessage = snapshot.LastMessage ?? "-";
        ScenarioObservation = CreateScenarioObservation(snapshot);
        TrendPoints = snapshot.TrendPoints;
        LogEntries = snapshot.LogEntries;
    }

    private static string CreateScenarioObservation(PilotPollingSnapshot snapshot)
    {
        return snapshot.LastRequestKind switch
        {
            WorkRequestKind.WorkStart =>
                $"{snapshot.LastRequestKind} {snapshot.Status} Start ACK: {FormatAck(snapshot.LastStartAckState)}",
            WorkRequestKind.WorkComplete =>
                $"{snapshot.LastRequestKind} {snapshot.Status} Complete ACK: {FormatAck(snapshot.LastCompleteAckState)}",
            _ => $"{snapshot.LastRequestKind} {snapshot.Status}"
        };
    }

    private static string FormatAck(bool? value) =>
        value.HasValue ? value.Value.ToString() : "-";

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
