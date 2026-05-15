using System.Reflection;
using CAAutomationHub.Contracts.Runtime;

namespace CAAutomationHub.Runtime.Tests;

public sealed class AutomationHubSupervisorContractTests
{
    [Fact]
    public void Contract_ExposesRuntimeControlPlaneMembers()
    {
        var contract = typeof(IAutomationHubSupervisor);

        Assert.Equal(typeof(Task), contract.GetMethod(nameof(IAutomationHubSupervisor.StartAsync))?.ReturnType);
        Assert.Equal(typeof(Task), contract.GetMethod(nameof(IAutomationHubSupervisor.StopAsync))?.ReturnType);
        Assert.Equal(typeof(Task<RuntimeSnapshot>), contract.GetMethod(nameof(IAutomationHubSupervisor.GetSnapshotAsync))?.ReturnType);
        Assert.Equal(
            typeof(Task<RuntimeDashboardCommandResult>),
            contract.GetMethod(nameof(IAutomationHubSupervisor.ExecuteAsync))?.ReturnType);
        Assert.Null(contract.GetMethod("RefreshSnapshotAsync"));

        AssertRuntimeEventHandler<RuntimeSnapshotChangedEventArgs>(contract.GetEvent(nameof(IAutomationHubSupervisor.SnapshotChanged)));
        AssertRuntimeEventHandler<RuntimeEvent>(contract.GetEvent(nameof(IAutomationHubSupervisor.RuntimeEventRaised)));
    }

    private static void AssertRuntimeEventHandler<TEventArgs>(EventInfo? eventInfo)
    {
        Assert.NotNull(eventInfo);
        Assert.Equal(typeof(EventHandler<TEventArgs>), eventInfo.EventHandlerType);
    }
}
