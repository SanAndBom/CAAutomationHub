namespace CAAutomationHub.Wpf.ViewModels;

public sealed class PlcDetailPaneViewModel : ViewModelBase
{
    private PlcStatusCardViewModel? _selected;
    public PlcStatusCardViewModel? Selected
    {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }
}
