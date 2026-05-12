using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CAAutomationHub.Wpf.Adapters;

namespace CAAutomationHub.Wpf.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    private readonly IRuntimeDashboardAdapter _adapter;
    private PlcStatusCardViewModel? _selectedPlc;
    private bool _isDetailPaneOpen;
    private GridLength _detailPaneColumnWidth = new(0);

    public DashboardViewModel() : this(new FakeDashboardRuntimeAdapter()) { }

    public DashboardViewModel(IRuntimeDashboardAdapter adapter)
    {
        _adapter = adapter;
        RefreshCommand = new RelayCommand(_ => LoadSnapshot());
        SelectPlcCommand = new RelayCommand(p => SelectPlc(p as PlcStatusCardViewModel));
        CloseDetailPaneCommand = new RelayCommand(_ => IsDetailPaneOpen = false);
        LoadSnapshot();
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
        }
    }

    public GridLength DetailPaneColumnWidth
    {
        get => _detailPaneColumnWidth;
        private set => SetProperty(ref _detailPaneColumnWidth, value);
    }

    private void LoadSnapshot()
    {
        var snapshot = _adapter.GetSnapshot();
        PlcCards.Clear();
        foreach (var card in snapshot.PlcCards) PlcCards.Add(new PlcStatusCardViewModel(card));
        TotalCount = snapshot.Health.TotalPlcs;
        HealthyCount = snapshot.Health.HealthyCount;
        WarningCount = snapshot.Health.WarningCount;
        CongestedCount = snapshot.Health.CongestedCount;
        ErrorCount = snapshot.Health.ErrorCount;
        OnCountsChanged();
    }

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
