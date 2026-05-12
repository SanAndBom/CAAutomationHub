using System.Collections.ObjectModel;
using System.Windows.Input;
using CAAutomationHub.Wpf.Models.Dashboard;
using CAAutomationHub.Wpf.Services;

namespace CAAutomationHub.Wpf.ViewModels;

public sealed class RealtimeEventLogViewModel : ViewModelBase, IDisposable
{
    private const int MaxVisibleEvents = 200;

    private readonly IEventStreamService _eventStreamService;
    private readonly List<RuntimeEventLogItem> _screenBuffer = new();
    private bool _isPaused;
    private bool _isAutoScrollEnabled = true;
    private string _selectedSeverity = "전체";
    private string _plcFilter = string.Empty;
    private string _searchText = string.Empty;
    private bool _isDisposed;

    public RealtimeEventLogViewModel() : this(new FakeEventStreamService()) { }
    public RealtimeEventLogViewModel(string initialPlcFilter) : this(new FakeEventStreamService(), initialPlcFilter) { }

    public RealtimeEventLogViewModel(IEventStreamService eventStreamService, string initialPlcFilter = "")
    {
        _eventStreamService = eventStreamService;
        _plcFilter = initialPlcFilter;

        PauseResumeCommand = new RelayCommand(_ => TogglePause());
        ClearViewCommand = new RelayCommand(_ => ClearView());
        ExportPlaceholderCommand = new RelayCommand(_ => { }, _ => false);

        _eventStreamService.EventReceived += OnEventReceived;
        _eventStreamService.Start();
    }

    public ObservableCollection<RuntimeEventLogItem> Events { get; } = new();

    public IReadOnlyList<string> SeverityFilters { get; } =
    [
        "전체",
        nameof(EventSeverity.Critical),
        nameof(EventSeverity.Warning),
        nameof(EventSeverity.Info)
    ];

    public ICommand PauseResumeCommand { get; }
    public ICommand ClearViewCommand { get; }
    public ICommand ExportPlaceholderCommand { get; }

    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            if (!SetProperty(ref _isPaused, value)) return;
            RaisePropertyChanged(nameof(PauseResumeText));
            RaisePropertyChanged(nameof(LiveIndicatorText));
        }
    }

    public bool IsAutoScrollEnabled
    {
        get => _isAutoScrollEnabled;
        set => SetProperty(ref _isAutoScrollEnabled, value);
    }

    public string SelectedSeverity
    {
        get => _selectedSeverity;
        set
        {
            if (!SetProperty(ref _selectedSeverity, value)) return;
            ApplyFilters();
        }
    }

    public string PlcFilter
    {
        get => _plcFilter;
        set
        {
            if (!SetProperty(ref _plcFilter, value ?? string.Empty)) return;
            ApplyFilters();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value ?? string.Empty)) return;
            ApplyFilters();
        }
    }

    public string PauseResumeText => IsPaused ? "Resume" : "Pause";
    public string LiveIndicatorText => IsPaused ? "Paused" : "Live";
    public int DisplayedCount => Events.Count;
    public string DisplayCountText => $"{Events.Count} / {_screenBuffer.Count} 표시";

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        _eventStreamService.EventReceived -= OnEventReceived;
        _eventStreamService.Dispose();
    }

    private void OnEventReceived(object? sender, RuntimeEventLogItem item)
    {
        if (_isDisposed) return;

        // AH-WPF-04 prototype policy: while paused, incoming fake events are not buffered.
        // Later runtime-backed implementations can decide whether to keep a pending queue.
        if (IsPaused) return;

        _screenBuffer.Insert(0, item);
        RuntimeEventLogItem? removedItem = null;
        while (_screenBuffer.Count > MaxVisibleEvents)
        {
            removedItem = _screenBuffer[^1];
            _screenBuffer.RemoveAt(_screenBuffer.Count - 1);
        }

        if (MatchesFilters(item))
        {
            Events.Insert(0, item);
        }

        if (removedItem is not null)
        {
            Events.Remove(removedItem);
        }

        RaiseCountPropertiesChanged();
    }

    private void TogglePause() => IsPaused = !IsPaused;

    private void ClearView()
    {
        _screenBuffer.Clear();
        Events.Clear();
        RaiseCountPropertiesChanged();
    }

    private void ApplyFilters()
    {
        Events.Clear();
        foreach (var item in _screenBuffer.Where(MatchesFilters))
        {
            Events.Add(item);
        }

        RaiseCountPropertiesChanged();
    }

    private bool MatchesFilters(RuntimeEventLogItem item)
    {
        if (SelectedSeverity != "전체" &&
            (!Enum.TryParse<EventSeverity>(SelectedSeverity, out var severity) || item.Severity != severity))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(PlcFilter) &&
            !item.PlcName.Contains(PlcFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        return item.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
               item.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
               item.Status.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
               item.PlcName.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    private void RaiseCountPropertiesChanged()
    {
        RaisePropertyChanged(nameof(DisplayedCount));
        RaisePropertyChanged(nameof(DisplayCountText));
    }
}
