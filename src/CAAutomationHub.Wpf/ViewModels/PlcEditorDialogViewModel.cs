using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;

namespace CAAutomationHub.Wpf.ViewModels;

public sealed class PlcEditorDialogViewModel : ViewModelBase
{
    private PlcConnectionTestState _testState = PlcConnectionTestState.NotTested;
    private string _plcName = "롤러홀개공기";
    private string _lineName = "SF절단라인";
    private string _description = "절단기 보조 PLC";
    private string _ipAddress = "192.168.0.133";
    private int _port = 2004;
    private bool _isEnabled = true;
    private int _pollingIntervalMs = 200;
    private int _timeoutMs = 800;
    private int _reconnectIntervalSec = 5;
    private int _maxRetryCount = 5;
    private bool _autoReconnect = true;
    private bool _connectOnStartup = true;

    public PlcEditorDialogViewModel()
    {
        TestConnectionCommand = new RelayCommand(_ => CycleTestState());
        TestStateOptions = new ObservableCollection<PlcConnectionTestStateOption>
        {
            new(PlcConnectionTestState.NotTested, "테스트 전", "아직 연결 테스트를 실행하지 않은 상태입니다."),
            new(PlcConnectionTestState.Success, "연결 성공", "Prototype 표시용 성공 상태입니다."),
            new(PlcConnectionTestState.Failure, "연결 실패", "Prototype 표시용 실패 상태입니다.")
        };
        RefreshStateOptions();
    }

    public ObservableCollection<PlcConnectionTestStateOption> TestStateOptions { get; }
    public ICommand TestConnectionCommand { get; }

    public string PlcName
    {
        get => _plcName;
        set => SetProperty(ref _plcName, value);
    }

    public string LineName
    {
        get => _lineName;
        set => SetProperty(ref _lineName, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string IpAddress
    {
        get => _ipAddress;
        set => SetProperty(ref _ipAddress, value);
    }

    public int Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public int PollingIntervalMs
    {
        get => _pollingIntervalMs;
        set
        {
            if (!SetProperty(ref _pollingIntervalMs, value)) return;
            RaisePropertyChanged(nameof(PollingIntervalSummary));
        }
    }

    public int TimeoutMs
    {
        get => _timeoutMs;
        set => SetProperty(ref _timeoutMs, value);
    }

    public int ReconnectIntervalSec
    {
        get => _reconnectIntervalSec;
        set => SetProperty(ref _reconnectIntervalSec, value);
    }

    public int MaxRetryCount
    {
        get => _maxRetryCount;
        set => SetProperty(ref _maxRetryCount, value);
    }

    public bool AutoReconnect
    {
        get => _autoReconnect;
        set
        {
            if (!SetProperty(ref _autoReconnect, value)) return;
            RaisePropertyChanged(nameof(AutoReconnectSummary));
        }
    }

    public bool ConnectOnStartup
    {
        get => _connectOnStartup;
        set => SetProperty(ref _connectOnStartup, value);
    }

    public string PollingIntervalSummary => $"{PollingIntervalMs} ms";
    public string AutoReconnectSummary => AutoReconnect ? "ON" : "OFF";

    public string TestStateDisplayName => TestState switch
    {
        PlcConnectionTestState.Success => "연결 성공",
        PlcConnectionTestState.Failure => "연결 실패",
        _ => "테스트 전"
    };

    public Brush TestStateBrush => TestState switch
    {
        PlcConnectionTestState.Success => Brushes.LightGreen,
        PlcConnectionTestState.Failure => Brushes.Tomato,
        _ => Brushes.Khaki
    };

    private PlcConnectionTestState TestState
    {
        get => _testState;
        set
        {
            if (!SetProperty(ref _testState, value)) return;
            RaisePropertyChanged(nameof(TestStateDisplayName));
            RaisePropertyChanged(nameof(TestStateBrush));
            RefreshStateOptions();
        }
    }

    private void CycleTestState()
    {
        TestState = TestState switch
        {
            PlcConnectionTestState.NotTested => PlcConnectionTestState.Success,
            PlcConnectionTestState.Success => PlcConnectionTestState.Failure,
            _ => PlcConnectionTestState.NotTested
        };
    }

    private void RefreshStateOptions()
    {
        foreach (var option in TestStateOptions)
        {
            option.IsCurrent = option.State == TestState;
        }
    }
}

public enum PlcConnectionTestState
{
    NotTested,
    Success,
    Failure
}

public sealed class PlcConnectionTestStateOption : ViewModelBase
{
    private static readonly Brush CurrentBackgroundBrush = new SolidColorBrush(Color.FromRgb(32, 42, 62));
    private static readonly Brush IdleBackgroundBrush = new SolidColorBrush(Color.FromRgb(24, 31, 46));
    private static readonly Brush IdleBorderBrush = new SolidColorBrush(Color.FromRgb(62, 76, 104));
    private bool _isCurrent;

    public PlcConnectionTestStateOption(PlcConnectionTestState state, string displayName, string description)
    {
        State = state;
        DisplayName = displayName;
        Description = description;
    }

    public PlcConnectionTestState State { get; }
    public string DisplayName { get; }
    public string Description { get; }

    public bool IsCurrent
    {
        get => _isCurrent;
        set
        {
            if (!SetProperty(ref _isCurrent, value)) return;
            RaisePropertyChanged(nameof(BackgroundBrush));
            RaisePropertyChanged(nameof(BorderBrush));
            RaisePropertyChanged(nameof(ForegroundBrush));
        }
    }

    public Brush BackgroundBrush => IsCurrent ? CurrentBackgroundBrush : IdleBackgroundBrush;

    public Brush BorderBrush => IsCurrent
        ? State switch
        {
            PlcConnectionTestState.Success => Brushes.LightGreen,
            PlcConnectionTestState.Failure => Brushes.Tomato,
            _ => Brushes.Khaki
        }
        : IdleBorderBrush;

    public Brush ForegroundBrush => IsCurrent ? BorderBrush : Brushes.White;
}
