using CAAutomationHub.Wpf.Models.Dashboard;
using CAAutomationHub.Wpf.ViewModels;

namespace CAAutomationHub.Wpf.Tests.ViewModels;

public sealed class PlcEditorDialogViewModelTests
{
    [Fact]
    public void Constructor_WhenAddingConfiguration_PopulatesDefaultFields()
    {
        var configuration = CreateConfiguration() with
        {
            PlcId = "PLC-06",
            PlcName = "PLC 06",
            LineName = "Line-1",
            Description = "신규 PLC",
            IpAddress = "192.168.0.26",
            PollingIntervalMs = 1000
        };

        var viewModel = new PlcEditorDialogViewModel(configuration, isEditMode: false);

        Assert.Equal("PLC 추가", viewModel.DialogTitle);
        Assert.Equal("PLC 추가", viewModel.HeaderTitle);
        Assert.Equal("PLC 06", viewModel.PlcName);
        Assert.Equal("Line-1", viewModel.LineName);
        Assert.Equal("신규 PLC", viewModel.Description);
        Assert.Equal("192.168.0.26", viewModel.IpAddress);
        Assert.Equal(1000, viewModel.PollingIntervalMs);
    }

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
    public void TryCreateConfiguration_WhenValid_PreservesPlcIdAndReturnsEditedFields()
    {
        var viewModel = new PlcEditorDialogViewModel(CreateConfiguration(), isEditMode: true)
        {
            PlcName = "Edited PLC",
            LineName = "Edited Line",
            Description = "Edited description",
            IpAddress = "10.1.2.3",
            PortText = "2600",
            PollingIntervalMsText = "700",
            TimeoutMsText = "1300",
            ReconnectIntervalSecText = "8",
            MaxRetryCountText = "4",
            AutoReconnect = false,
            ConnectOnStartup = true,
            IsEnabled = true
        };

        var isValid = viewModel.TryCreateConfiguration(out var result);

        Assert.True(isValid);
        Assert.NotNull(result);
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
        Assert.False(viewModel.HasValidationErrors);
        Assert.Empty(viewModel.ValidationErrors);
    }

    [Fact]
    public void TryCreateConfiguration_WhenNameIsBlank_FailsAndDoesNotCreateConfiguration()
    {
        var viewModel = new PlcEditorDialogViewModel(CreateConfiguration(), isEditMode: false)
        {
            PlcName = "   "
        };

        var isValid = viewModel.TryCreateConfiguration(out var result);

        Assert.False(isValid);
        Assert.Null(result);
        Assert.True(viewModel.HasValidationErrors);
        Assert.Contains(viewModel.ValidationErrors, error => error.Contains("PLC 이름", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("999.1.1.1")]
    [InlineData("2001:db8::1")]
    public void TryCreateConfiguration_WhenIpAddressIsInvalid_Fails(string ipAddress)
    {
        var viewModel = new PlcEditorDialogViewModel(CreateConfiguration(), isEditMode: false)
        {
            IpAddress = ipAddress
        };

        var isValid = viewModel.TryCreateConfiguration(out var result);

        Assert.False(isValid);
        Assert.Null(result);
        Assert.True(viewModel.HasValidationErrors);
        Assert.Contains(viewModel.ValidationErrors, error => error.Contains("IP 주소", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("65536")]
    public void TryCreateConfiguration_WhenPortIsInvalid_Fails(string port)
    {
        var viewModel = new PlcEditorDialogViewModel(CreateConfiguration(), isEditMode: false)
        {
            PortText = port
        };

        var isValid = viewModel.TryCreateConfiguration(out var result);

        Assert.False(isValid);
        Assert.Null(result);
        Assert.True(viewModel.HasValidationErrors);
        Assert.Contains(viewModel.ValidationErrors, error => error.Contains("Port", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("49")]
    [InlineData("60001")]
    public void TryCreateConfiguration_WhenPollingIntervalIsInvalid_Fails(string pollingInterval)
    {
        var viewModel = new PlcEditorDialogViewModel(CreateConfiguration(), isEditMode: false)
        {
            PollingIntervalMsText = pollingInterval
        };

        var isValid = viewModel.TryCreateConfiguration(out var result);

        Assert.False(isValid);
        Assert.Null(result);
        Assert.True(viewModel.HasValidationErrors);
        Assert.Contains(viewModel.ValidationErrors, error => error.Contains("Polling Interval", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("49")]
    [InlineData("60001")]
    public void TryCreateConfiguration_WhenTimeoutIsInvalid_Fails(string timeout)
    {
        var viewModel = new PlcEditorDialogViewModel(CreateConfiguration(), isEditMode: false)
        {
            TimeoutMsText = timeout
        };

        var isValid = viewModel.TryCreateConfiguration(out var result);

        Assert.False(isValid);
        Assert.Null(result);
        Assert.True(viewModel.HasValidationErrors);
        Assert.Contains(viewModel.ValidationErrors, error => error.Contains("Timeout", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("3601")]
    public void TryCreateConfiguration_WhenReconnectIntervalIsInvalid_Fails(string reconnectInterval)
    {
        var viewModel = new PlcEditorDialogViewModel(CreateConfiguration(), isEditMode: false)
        {
            ReconnectIntervalSecText = reconnectInterval
        };

        var isValid = viewModel.TryCreateConfiguration(out var result);

        Assert.False(isValid);
        Assert.Null(result);
        Assert.True(viewModel.HasValidationErrors);
        Assert.Contains(viewModel.ValidationErrors, error => error.Contains("Reconnect Interval", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("101")]
    public void TryCreateConfiguration_WhenMaxRetryCountIsInvalid_Fails(string maxRetryCount)
    {
        var viewModel = new PlcEditorDialogViewModel(CreateConfiguration(), isEditMode: false)
        {
            MaxRetryCountText = maxRetryCount
        };

        var isValid = viewModel.TryCreateConfiguration(out var result);

        Assert.False(isValid);
        Assert.Null(result);
        Assert.True(viewModel.HasValidationErrors);
        Assert.Contains(viewModel.ValidationErrors, error => error.Contains("Max Retry Count", StringComparison.Ordinal));
    }

    [Fact]
    public void TryCreateConfiguration_WhenMaxRetryCountIsZero_Succeeds()
    {
        var viewModel = new PlcEditorDialogViewModel(CreateConfiguration(), isEditMode: false)
        {
            MaxRetryCountText = "0"
        };

        var isValid = viewModel.TryCreateConfiguration(out var result);

        Assert.True(isValid);
        Assert.NotNull(result);
        Assert.Equal(0, result.MaxRetryCount);
        Assert.False(viewModel.HasValidationErrors);
        Assert.Empty(viewModel.ValidationErrors);
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
