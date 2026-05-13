using CAAutomationHub.Wpf.Models.Dashboard;
using CAAutomationHub.Wpf.ViewModels;

namespace CAAutomationHub.Wpf.Tests.ViewModels;

public sealed class PlcEditorDialogViewModelTests
{
    [Fact]
    public void Constructor_WhenEditingConfiguration_PopulatesEditableFields()
    {
        var configuration = CreateConfiguration() with
        {
            PlcName = "Edit PLC",
            LineName = "Line-X",
            Description = "Existing fake PLC",
            IpAddress = "10.0.0.9",
            Port = 2200,
            PollingIntervalMs = 650,
            TimeoutMs = 1200,
            ReconnectIntervalSeconds = 7,
            MaxRetryCount = 9,
            AutoReconnect = false,
            ConnectOnStartup = false,
            IsEnabled = false
        };

        var viewModel = new PlcEditorDialogViewModel(configuration, isEditMode: true);

        Assert.Equal("PLC 수정", viewModel.DialogTitle);
        Assert.Equal("PLC 수정", viewModel.HeaderTitle);
        Assert.Equal("Edit PLC", viewModel.PlcName);
        Assert.Equal("Line-X", viewModel.LineName);
        Assert.Equal("Existing fake PLC", viewModel.Description);
        Assert.Equal("10.0.0.9", viewModel.IpAddress);
        Assert.Equal(2200, viewModel.Port);
        Assert.Equal(650, viewModel.PollingIntervalMs);
        Assert.Equal(1200, viewModel.TimeoutMs);
        Assert.Equal(7, viewModel.ReconnectIntervalSec);
        Assert.Equal(9, viewModel.MaxRetryCount);
        Assert.False(viewModel.AutoReconnect);
        Assert.False(viewModel.ConnectOnStartup);
        Assert.False(viewModel.IsEnabled);
    }

    [Fact]
    public void ToConfiguration_PreservesPlcIdAndReturnsEditedFields()
    {
        var viewModel = new PlcEditorDialogViewModel(CreateConfiguration(), isEditMode: true)
        {
            PlcName = "Edited PLC",
            LineName = "Edited Line",
            Description = "Edited description",
            IpAddress = "10.1.2.3",
            Port = 2600,
            PollingIntervalMs = 700,
            TimeoutMs = 1300,
            ReconnectIntervalSec = 8,
            MaxRetryCount = 4,
            AutoReconnect = false,
            ConnectOnStartup = true,
            IsEnabled = true
        };

        var result = viewModel.ToConfiguration();

        Assert.Equal("PLC-42", result.PlcId);
        Assert.Equal("Edited PLC", result.PlcName);
        Assert.Equal("Edited Line", result.LineName);
        Assert.Equal("Edited description", result.Description);
        Assert.Equal("10.1.2.3", result.IpAddress);
        Assert.Equal(2600, result.Port);
        Assert.Equal(700, result.PollingIntervalMs);
        Assert.Equal(1300, result.TimeoutMs);
        Assert.Equal(8, result.ReconnectIntervalSeconds);
        Assert.Equal(4, result.MaxRetryCount);
        Assert.False(result.AutoReconnect);
        Assert.True(result.ConnectOnStartup);
        Assert.True(result.IsEnabled);
    }

    private static PlcDashboardConfiguration CreateConfiguration()
        => new(
            "PLC-42",
            "PLC 42",
            "Line-1",
            "Description",
            "192.168.0.42",
            2004,
            500,
            800,
            5,
            5,
            AutoReconnect: true,
            ConnectOnStartup: true,
            IsEnabled: true);
}
