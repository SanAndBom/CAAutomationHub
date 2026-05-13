using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CAAutomationHub.Wpf.Adapters;
using CAAutomationHub.Wpf.Models.Dashboard;
using CAAutomationHub.Wpf.Services;

namespace CAAutomationHub.Wpf.ViewModels;

public sealed class DashboardViewModel : ViewModelBase, IDisposable
{
    private readonly IRuntimeDashboardAdapter _adapter;
    private readonly IPlcDashboardConfigurationService? _configurationService;
    private readonly DispatcherTimer _refreshTimer;
    private CommunicationTrendSetSnapshot _communicationTrendSet = CommunicationTrendSetSnapshot.Empty;
    private CommunicationTrendSnapshot _currentCommunicationTrend = CommunicationTrendSnapshot.Empty;
    private PlcStatusCardViewModel? _selectedPlc;
    private bool _isDetailPaneOpen;
    private bool _isDisposed;
    private GridLength _detailPaneColumnWidth = new(0);
    private GridLength _detailPaneGapWidth = new(0);
    private double _trendAverageResponseMs;
    private double _trendMaxResponseMs;
    private int _trendErrorCount;
    private int _trendPointCount;

    public DashboardViewModel() : this(CreateDefaultFakeAdapter()) { }

    public DashboardViewModel(IRuntimeDashboardAdapter adapter)
        : this(adapter, adapter as IPlcDashboardConfigurationService)
    {
    }

    public DashboardViewModel(IRuntimeDashboardAdapter adapter, IPlcDashboardConfigurationService? configurationService)
    {
        _adapter = adapter;
        _configurationService = configurationService;
        RefreshCommand = new RelayCommand(_ => LoadSnapshot());
        SelectPlcCommand = new RelayCommand(p => SelectPlc(p as PlcStatusCardViewModel));
        CloseDetailPaneCommand = new RelayCommand(_ => IsDetailPaneOpen = false);
        AddPlcCommand = new RelayCommand(
            p => AddPlc(p as PlcDashboardConfiguration),
            p => _configurationService is not null && p is PlcDashboardConfiguration);
        EditSelectedPlcCommand = new RelayCommand(
            p => EditSelectedPlc(p as PlcDashboardConfiguration),
            p => _configurationService is not null && p is PlcDashboardConfiguration);
        DeleteSelectedPlcCommand = new RelayCommand(
            p => DeletePlc(p as string ?? SelectedPlc?.PlcId),
            p => _configurationService is not null && (p as string ?? SelectedPlc?.PlcId) is not null);
        _refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _refreshTimer.Tick += OnRefreshTimerTick;
        LoadSnapshot();
        StartAutoRefresh();
    }

    public ObservableCollection<PlcStatusCardViewModel> PlcCards { get; } = new();
    public ICommand RefreshCommand { get; }
    public ICommand SelectPlcCommand { get; }
    public ICommand CloseDetailPaneCommand { get; }
    public ICommand AddPlcCommand { get; }
    public ICommand EditSelectedPlcCommand { get; }
    public ICommand DeleteSelectedPlcCommand { get; }
    public int TotalCount { get; private set; }
    public int HealthyCount { get; private set; }
    public int WarningCount { get; private set; }
    public int CongestedCount { get; private set; }
    public int ErrorCount { get; private set; }
    public int InactiveCount { get; private set; }
    public double TrendAverageResponseMs
    {
        get => _trendAverageResponseMs;
        private set => SetProperty(ref _trendAverageResponseMs, value);
    }

    public double TrendMaxResponseMs
    {
        get => _trendMaxResponseMs;
        private set => SetProperty(ref _trendMaxResponseMs, value);
    }

    public int TrendErrorCount
    {
        get => _trendErrorCount;
        private set => SetProperty(ref _trendErrorCount, value);
    }

    public int TrendPointCount
    {
        get => _trendPointCount;
        private set => SetProperty(ref _trendPointCount, value);
    }

    public CommunicationTrendSnapshot CurrentCommunicationTrend
    {
        get => _currentCommunicationTrend;
        private set
        {
            if (!SetProperty(ref _currentCommunicationTrend, value)) return;
            UpdateTrendSummary();
        }
    }

    public PlcStatusCardViewModel? SelectedPlc
    {
        get => _selectedPlc;
        private set
        {
            if (!SetProperty(ref _selectedPlc, value)) return;
            SyncSelectedCardStates();
            UpdateCurrentCommunicationTrend();
            RaiseConfigurationCommandStatesChanged();
        }
    }

    public bool IsDetailPaneOpen
    {
        get => _isDetailPaneOpen;
        set
        {
            if (!SetProperty(ref _isDetailPaneOpen, value)) return;
            DetailPaneColumnWidth = value ? new GridLength(360) : new GridLength(0);
            DetailPaneGapWidth = value ? new GridLength(16) : new GridLength(0);
        }
    }

    public GridLength DetailPaneColumnWidth
    {
        get => _detailPaneColumnWidth;
        private set => SetProperty(ref _detailPaneColumnWidth, value);
    }

    public GridLength DetailPaneGapWidth
    {
        get => _detailPaneGapWidth;
        private set => SetProperty(ref _detailPaneGapWidth, value);
    }

    private void LoadSnapshot()
    {
        if (_isDisposed) return;

        var snapshot = _adapter.GetSnapshot();
        ApplySnapshot(snapshot);
    }

    private void ApplySnapshot(DashboardSnapshot snapshot)
    {
        _communicationTrendSet = snapshot.CommunicationTrend;
        var snapshotPlcIds = snapshot.PlcCards.Select(card => card.PlcId).ToHashSet();

        for (var index = PlcCards.Count - 1; index >= 0; index--)
        {
            if (snapshotPlcIds.Contains(PlcCards[index].Snapshot.PlcId)) continue;

            PlcCards.RemoveAt(index);
        }

        if (SelectedPlc is not null && !snapshotPlcIds.Contains(SelectedPlc.Snapshot.PlcId))
        {
            SelectedPlc = null;
            IsDetailPaneOpen = false;
        }

        foreach (var card in snapshot.PlcCards)
        {
            var existing = PlcCards.FirstOrDefault(plc => plc.Snapshot.PlcId == card.PlcId);
            if (existing is null)
            {
                PlcCards.Add(new PlcStatusCardViewModel(card));
                continue;
            }

            existing.UpdateSnapshot(card);
        }

        if (SelectedPlc is not null)
        {
            var selected = PlcCards.FirstOrDefault(plc => plc.Snapshot.PlcId == SelectedPlc.Snapshot.PlcId);
            if (selected is not null && !ReferenceEquals(selected, SelectedPlc)) SelectedPlc = selected;
            if (selected is null)
            {
                SelectedPlc = null;
                IsDetailPaneOpen = false;
            }
        }

        SyncSelectedCardStates();
        UpdateCurrentCommunicationTrend();

        TotalCount = snapshot.Health.TotalPlcs;
        HealthyCount = snapshot.Health.HealthyCount;
        WarningCount = snapshot.Health.WarningCount;
        CongestedCount = snapshot.Health.CongestedCount;
        ErrorCount = snapshot.Health.ErrorCount;
        InactiveCount = snapshot.Health.InactiveCount;
        OnCountsChanged();
    }

    public void StartAutoRefresh()
    {
        if (_isDisposed || _refreshTimer.IsEnabled) return;
        _refreshTimer.Start();
    }

    public void StopAutoRefresh()
    {
        if (_refreshTimer.IsEnabled) _refreshTimer.Stop();
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        StopAutoRefresh();
        _refreshTimer.Tick -= OnRefreshTimerTick;
    }

    public PlcDashboardConfiguration? GetSelectedPlcConfiguration()
        => SelectedPlc is null ? null : _configurationService?.GetPlcConfiguration(SelectedPlc.Snapshot.PlcId);

    public PlcDashboardConfiguration? CreateDefaultPlcConfiguration()
        => _configurationService?.CreateDefaultPlcConfiguration();

    private void AddPlc(PlcDashboardConfiguration? configuration)
    {
        if (_configurationService is null || configuration is null) return;

        var added = _configurationService.AddPlc(configuration);
        LoadSnapshot();

        var addedCard = PlcCards.FirstOrDefault(card => card.PlcId == added.PlcId);
        if (addedCard is null) return;

        SelectedPlc = addedCard;
        IsDetailPaneOpen = true;
    }

    private void EditSelectedPlc(PlcDashboardConfiguration? configuration)
    {
        if (_configurationService is null || configuration is null) return;

        _configurationService.UpdatePlc(configuration);
        LoadSnapshot();
    }

    private void DeletePlc(string? plcId)
    {
        if (_configurationService is null || string.IsNullOrWhiteSpace(plcId)) return;

        var wasSelected = SelectedPlc?.Snapshot.PlcId == plcId;
        _configurationService.DeletePlc(plcId);
        if (wasSelected)
        {
            SelectedPlc = null;
            IsDetailPaneOpen = false;
        }

        LoadSnapshot();
    }

    private void OnRefreshTimerTick(object? sender, EventArgs e) => LoadSnapshot();

    private void SelectPlc(PlcStatusCardViewModel? plc)
    {
        if (plc is null) return;
        if (SelectedPlc?.Snapshot.PlcId == plc.Snapshot.PlcId)
        {
            SelectedPlc = null;
            IsDetailPaneOpen = false;
            return;
        }

        SelectedPlc = plc;
        IsDetailPaneOpen = true;
    }

    private void SyncSelectedCardStates()
    {
        var selectedPlcId = SelectedPlc?.Snapshot.PlcId;

        foreach (var card in PlcCards)
        {
            card.IsSelected = selectedPlcId is not null && card.Snapshot.PlcId == selectedPlcId;
        }
    }

    private void UpdateCurrentCommunicationTrend()
    {
        var selectedPlcId = SelectedPlc?.Snapshot.PlcId;
        var selectedTrend = selectedPlcId is null
            ? null
            : _communicationTrendSet.PlcTrends.FirstOrDefault(trend => trend.TargetId == selectedPlcId);

        CurrentCommunicationTrend = selectedTrend ?? _communicationTrendSet.Overview;
    }

    private void UpdateTrendSummary()
    {
        var points = CurrentCommunicationTrend.Points;

        if (points.Count == 0)
        {
            TrendAverageResponseMs = 0;
            TrendMaxResponseMs = 0;
            TrendErrorCount = 0;
            TrendPointCount = 0;
            return;
        }

        TrendAverageResponseMs = points.Average(point => point.ResponseMs);
        TrendMaxResponseMs = points.Max(point => point.ResponseMs);
        TrendErrorCount = points.Count(point => point.HasError || point.MarkerKind == TrendMarkerKind.Error);
        TrendPointCount = points.Count;
    }

    private void OnCountsChanged()
    {
        RaisePropertyChanged(nameof(TotalCount));
        RaisePropertyChanged(nameof(HealthyCount));
        RaisePropertyChanged(nameof(WarningCount));
        RaisePropertyChanged(nameof(CongestedCount));
        RaisePropertyChanged(nameof(ErrorCount));
        RaisePropertyChanged(nameof(InactiveCount));
    }

    private void RaiseConfigurationCommandStatesChanged()
    {
        if (AddPlcCommand is RelayCommand addCommand) addCommand.RaiseCanExecuteChanged();
        if (EditSelectedPlcCommand is RelayCommand editCommand) editCommand.RaiseCanExecuteChanged();
        if (DeleteSelectedPlcCommand is RelayCommand deleteCommand) deleteCommand.RaiseCanExecuteChanged();
    }

    private static FakeDashboardRuntimeAdapter CreateDefaultFakeAdapter() => new();
}
