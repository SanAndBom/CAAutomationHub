using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Windows.Input;
using System.Windows.Media;
using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.ViewModels;

public sealed class PlcEditorDialogViewModel : ViewModelBase
{
    private readonly string _plcId;
    private PlcConnectionTestState _testState = PlcConnectionTestState.NotTested;
    private string _plcName = "롤러홀개공기";
    private string _lineName = "SF절단라인";
    private string _description = "절단기 보조 PLC";
    private string _ipAddress = "192.168.0.133";
    private string _portText = "2004";
    private bool _isEnabled = true;
    private string _pollingIntervalMsText = "200";
    private string _timeoutMsText = "800";
    private string _reconnectIntervalSecText = "5";
    private string _maxRetryCountText = "5";
    private bool _autoReconnect = true;
    private bool _connectOnStartup = true;
    private IReadOnlyList<string> _validationErrors = Array.Empty<string>();

    public PlcEditorDialogViewModel()
        : this(CreateDefaultPrototypeConfiguration(), isEditMode: false)
    {
    }

    public PlcEditorDialogViewModel(PlcDashboardConfiguration configuration, bool isEditMode)
    {
        _plcId = configuration.PlcId;
        _plcName = configuration.PlcName;
        _lineName = configuration.LineName;
        _description = configuration.Description;
        _ipAddress = configuration.IpAddress;
        _portText = configuration.Port.ToString(CultureInfo.InvariantCulture);
        _isEnabled = configuration.IsEnabled;
        _pollingIntervalMsText = configuration.PollingIntervalMs.ToString(CultureInfo.InvariantCulture);
        _timeoutMsText = configuration.TimeoutMs.ToString(CultureInfo.InvariantCulture);
        _reconnectIntervalSecText = configuration.ReconnectIntervalSeconds.ToString(CultureInfo.InvariantCulture);
        _maxRetryCountText = configuration.MaxRetryCount.ToString(CultureInfo.InvariantCulture);
        _autoReconnect = configuration.AutoReconnect;
        _connectOnStartup = configuration.ConnectOnStartup;
        DialogTitle = isEditMode ? "PLC 수정" : "PLC 추가";
        HeaderTitle = DialogTitle;
        HeaderSubtitle = isEditMode ? "선택된 PLC 설정 Prototype" : "PLC 설정 정적 Prototype";
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
    public string DialogTitle { get; }
    public string HeaderTitle { get; }
    public string HeaderSubtitle { get; }
    public IReadOnlyList<string> ValidationErrors => _validationErrors;
    public bool HasValidationErrors => ValidationErrors.Count > 0;

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
        get => TryParseInteger(PortText, out var port) ? port : 0;
    }

    public string PortText
    {
        get => _portText;
        set
        {
            if (!SetProperty(ref _portText, value)) return;
            RaisePropertyChanged(nameof(Port));
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public int PollingIntervalMs
    {
        get => TryParseInteger(PollingIntervalMsText, out var pollingIntervalMs) ? pollingIntervalMs : 0;
    }

    public string PollingIntervalMsText
    {
        get => _pollingIntervalMsText;
        set
        {
            if (!SetProperty(ref _pollingIntervalMsText, value)) return;
            RaisePropertyChanged(nameof(PollingIntervalMs));
            RaisePropertyChanged(nameof(PollingIntervalSummary));
        }
    }

    public int TimeoutMs
    {
        get => TryParseInteger(TimeoutMsText, out var timeoutMs) ? timeoutMs : 0;
    }

    public string TimeoutMsText
    {
        get => _timeoutMsText;
        set
        {
            if (!SetProperty(ref _timeoutMsText, value)) return;
            RaisePropertyChanged(nameof(TimeoutMs));
        }
    }

    public int ReconnectIntervalSec
    {
        get => TryParseInteger(ReconnectIntervalSecText, out var reconnectIntervalSec) ? reconnectIntervalSec : 0;
    }

    public string ReconnectIntervalSecText
    {
        get => _reconnectIntervalSecText;
        set
        {
            if (!SetProperty(ref _reconnectIntervalSecText, value)) return;
            RaisePropertyChanged(nameof(ReconnectIntervalSec));
        }
    }

    public int MaxRetryCount
    {
        get => TryParseInteger(MaxRetryCountText, out var maxRetryCount) ? maxRetryCount : 0;
    }

    public string MaxRetryCountText
    {
        get => _maxRetryCountText;
        set
        {
            if (!SetProperty(ref _maxRetryCountText, value)) return;
            RaisePropertyChanged(nameof(MaxRetryCount));
        }
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

    public string PollingIntervalSummary => $"{PollingIntervalMsText} ms";
    public string AutoReconnectSummary => AutoReconnect ? "ON" : "OFF";

    public bool TryCreateConfiguration(out PlcDashboardConfiguration? configuration)
    {
        var errors = Validate(
            out var plcName,
            out var lineName,
            out var description,
            out var ipAddress,
            out var port,
            out var pollingIntervalMs,
            out var timeoutMs,
            out var reconnectIntervalSec,
            out var maxRetryCount);

        SetValidationErrors(errors);

        if (errors.Count > 0)
        {
            configuration = null;
            return false;
        }

        configuration = new PlcDashboardConfiguration(
            _plcId,
            plcName,
            lineName,
            description,
            ipAddress,
            port,
            pollingIntervalMs,
            timeoutMs,
            reconnectIntervalSec,
            maxRetryCount,
            AutoReconnect,
            ConnectOnStartup,
            IsEnabled);
        return true;
    }

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

    private IReadOnlyList<string> Validate(
        out string plcName,
        out string lineName,
        out string description,
        out string ipAddress,
        out int port,
        out int pollingIntervalMs,
        out int timeoutMs,
        out int reconnectIntervalSec,
        out int maxRetryCount)
    {
        var errors = new List<string>();
        plcName = PlcName.Trim();
        lineName = LineName.Trim();
        description = Description.Trim();
        ipAddress = IpAddress.Trim();

        if (string.IsNullOrWhiteSpace(plcName))
        {
            errors.Add("PLC 이름은 필수입니다.");
        }

        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            errors.Add("IP 주소는 필수입니다.");
        }
        else if (!IPAddress.TryParse(ipAddress, out var parsedAddress)
                 || parsedAddress.AddressFamily != AddressFamily.InterNetwork)
        {
            errors.Add("IP 주소는 IPv4 형식이어야 합니다. 예: 192.168.0.133");
        }

        ValidateInteger(errors, "Port", PortText, 1, 65535, out port);
        ValidateInteger(errors, "Polling Interval", PollingIntervalMsText, 50, 60000, out pollingIntervalMs, "ms");
        ValidateInteger(errors, "Timeout", TimeoutMsText, 50, 60000, out timeoutMs, "ms");
        ValidateInteger(errors, "Reconnect Interval", ReconnectIntervalSecText, 1, 3600, out reconnectIntervalSec, "sec");
        ValidateInteger(errors, "Max Retry Count", MaxRetryCountText, 0, 100, out maxRetryCount);

        return errors;
    }

    private void SetValidationErrors(IReadOnlyList<string> errors)
    {
        _validationErrors = errors;
        RaisePropertyChanged(nameof(ValidationErrors));
        RaisePropertyChanged(nameof(HasValidationErrors));
    }

    private static void ValidateInteger(
        ICollection<string> errors,
        string fieldName,
        string text,
        int minimum,
        int maximum,
        out int value,
        string? unit = null)
    {
        value = 0;
        var trimmed = text.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            errors.Add($"{fieldName}은 필수입니다.");
            return;
        }

        if (!TryParseInteger(trimmed, out value))
        {
            errors.Add($"{fieldName}은 정수로 입력해야 합니다.");
            return;
        }

        if (value < minimum || value > maximum)
        {
            var suffix = string.IsNullOrWhiteSpace(unit) ? string.Empty : $" {unit}";
            errors.Add($"{fieldName}은 {minimum} ~ {maximum}{suffix} 범위여야 합니다.");
        }
    }

    private static bool TryParseInteger(string text, out int value)
        => int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value);

    private static PlcDashboardConfiguration CreateDefaultPrototypeConfiguration()
        => new(
            "PLC-PROTOTYPE",
            "롤러홀개공기",
            "SF절단라인",
            "절단기 보조 PLC",
            "192.168.0.133",
            2004,
            200,
            800,
            5,
            5,
            AutoReconnect: true,
            ConnectOnStartup: true,
            IsEnabled: true);
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
