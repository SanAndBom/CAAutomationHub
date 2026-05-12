using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CAAutomationHub.Wpf.Adapters;
using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.ViewModels;

public sealed class DashboardViewModel : ViewModelBase, IDisposable
{
    private readonly IRuntimeDashboardAdapter _adapter;
    private readonly DispatcherTimer _refreshTimer;
    private PlcStatusCardViewModel? _selectedPlc;
    private bool _isDetailPaneOpen;
    private bool _isDisposed;
    private GridLength _detailPaneColumnWidth = new(0);
    private GridLength _detailPaneGapWidth = new(0);

    public DashboardViewModel() : this(new FakeDashboardRuntimeAdapter()) { }

    public DashboardViewModel(IRuntimeDashboardAdapter adapter)
    {
        _adapter = adapter;
        RefreshCommand = new RelayCommand(_ => LoadSnapshot());
        SelectPlcCommand = new RelayCommand(p => SelectPlc(p as PlcStatusCardViewModel));
        CloseDetailPaneCommand = new RelayCommand(_ => IsDetailPaneOpen = false);
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
    public int TotalCount { get; private set; }
    public int HealthyCount { get; private set; }
    public int WarningCount { get; private set; }
    public int CongestedCount { get; private set; }
    public int ErrorCount { get; private set; }

    public PlcStatusCardViewModel? SelectedPlc
    {
        get => _selectedPlc;
        private set => SetProperty(ref _selectedPlc, value);
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
        }

        TotalCount = snapshot.Health.TotalPlcs;
        HealthyCount = snapshot.Health.HealthyCount;
        WarningCount = snapshot.Health.WarningCount;
        CongestedCount = snapshot.Health.CongestedCount;
        ErrorCount = snapshot.Health.ErrorCount;
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

    private void OnRefreshTimerTick(object? sender, EventArgs e) => LoadSnapshot();

    private void SelectPlc(PlcStatusCardViewModel? plc)
    {
        if (plc is null) return;
        SelectedPlc = plc;
        IsDetailPaneOpen = true;
    }

    private void OnCountsChanged()
    {
        RaisePropertyChanged(nameof(TotalCount));
        RaisePropertyChanged(nameof(HealthyCount));
        RaisePropertyChanged(nameof(WarningCount));
        RaisePropertyChanged(nameof(CongestedCount));
        RaisePropertyChanged(nameof(ErrorCount));
    }
}
