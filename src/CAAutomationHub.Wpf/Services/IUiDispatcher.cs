namespace CAAutomationHub.Wpf.Services;

public interface IUiDispatcher
{
    void Post(Action action);
}
