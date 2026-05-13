using CAAutomationHub.Wpf.Mappers;
using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Tests.Mappers;

public sealed class RuntimeEventLogItemMapperTests
{
    private static readonly DateTimeOffset OccurredAt = new(2026, 5, 13, 12, 34, 56, TimeSpan.Zero);

    [Theory]
    [InlineData("Critical", EventSeverity.Critical)]
    [InlineData("Error", EventSeverity.Critical)]
    [InlineData("Fatal", EventSeverity.Critical)]
    [InlineData("ERROR", EventSeverity.Critical)]
    [InlineData("Warning", EventSeverity.Warning)]
    [InlineData("Warn", EventSeverity.Warning)]
    [InlineData(" warning ", EventSeverity.Warning)]
    [InlineData("Info", EventSeverity.Info)]
    [InlineData("Information", EventSeverity.Info)]
    [InlineData("Unknown", EventSeverity.Info)]
    [InlineData("", EventSeverity.Info)]
    [InlineData("   ", EventSeverity.Info)]
    public void Map_MapsSeverityPolicy(string severity, EventSeverity expectedSeverity)
    {
        var item = Map(new RuntimeDashboardEvent(OccurredAt, severity, "message"));

        Assert.Equal(expectedSeverity, item.Severity);
    }

    [Fact]
    public void Map_UsesPlcNameWhenProvided()
    {
        var item = Map(new RuntimeDashboardEvent(
            OccurredAt,
            "Info",
            "message",
            Source: "source-plc",
            PlcName: "named-plc"));

        Assert.Equal("named-plc", item.PlcName);
    }

    [Fact]
    public void Map_UsesSourceWhenPlcNameIsMissing()
    {
        var item = Map(new RuntimeDashboardEvent(
            OccurredAt,
            "Info",
            "message",
            Source: "source-plc"));

        Assert.Equal("source-plc", item.PlcName);
    }

    [Fact]
    public void Map_UsesSourceWhenPlcNameIsWhitespace()
    {
        var item = Map(new RuntimeDashboardEvent(
            OccurredAt,
            "Info",
            "message",
            Source: "source-plc",
            PlcName: "   "));

        Assert.Equal("source-plc", item.PlcName);
    }

    [Fact]
    public void Map_UsesRuntimeAsPlcNameWhenPlcNameAndSourceAreMissing()
    {
        var item = Map(new RuntimeDashboardEvent(OccurredAt, "Info", "message"));

        Assert.Equal("Runtime", item.PlcName);
    }

    [Theory]
    [InlineData("Operations", "Operations")]
    [InlineData(null, "Runtime")]
    [InlineData("", "Runtime")]
    [InlineData("   ", "Runtime")]
    public void Map_AppliesCategoryDefaultPolicy(string? category, string expectedCategory)
    {
        var item = Map(new RuntimeDashboardEvent(
            OccurredAt,
            "Info",
            "message",
            Category: category));

        Assert.Equal(expectedCategory, item.Category);
    }

    [Theory]
    [InlineData("Runtime started", "Runtime started")]
    [InlineData(null, "Runtime event received.")]
    [InlineData("", "Runtime event received.")]
    [InlineData("   ", "Runtime event received.")]
    public void Map_AppliesMessageDefaultPolicy(string? message, string expectedMessage)
    {
        var item = Map(new RuntimeDashboardEvent(
            OccurredAt,
            "Info",
            message!));

        Assert.Equal(expectedMessage, item.Message);
    }

    [Theory]
    [InlineData("Manual", "Manual")]
    [InlineData(null, "Open")]
    [InlineData("", "Open")]
    [InlineData("   ", "Open")]
    public void Map_AppliesCriticalStatusDefaultPolicy(string? status, string expectedStatus)
    {
        var item = Map(new RuntimeDashboardEvent(
            OccurredAt,
            "Error",
            "message",
            Status: status));

        Assert.Equal(expectedStatus, item.Status);
    }

    [Theory]
    [InlineData("Manual", "Manual")]
    [InlineData(null, "Watch")]
    [InlineData("", "Watch")]
    [InlineData("   ", "Watch")]
    public void Map_AppliesWarningStatusDefaultPolicy(string? status, string expectedStatus)
    {
        var item = Map(new RuntimeDashboardEvent(
            OccurredAt,
            "Warning",
            "message",
            Status: status));

        Assert.Equal(expectedStatus, item.Status);
    }

    [Theory]
    [InlineData("Manual", "Manual")]
    [InlineData(null, "Live")]
    [InlineData("", "Live")]
    [InlineData("   ", "Live")]
    public void Map_AppliesInfoStatusDefaultPolicy(string? status, string expectedStatus)
    {
        var item = Map(new RuntimeDashboardEvent(
            OccurredAt,
            "Info",
            "message",
            Status: status));

        Assert.Equal(expectedStatus, item.Status);
    }

    [Fact]
    public void Map_PreservesLegacyConstructorUsageAndOccurredAt()
    {
        var dashboardEvent = new RuntimeDashboardEvent(OccurredAt, "Info", "message");

        var item = Map(dashboardEvent);

        Assert.Equal(OccurredAt, item.OccurredAt);
        Assert.Equal(EventSeverity.Info, item.Severity);
        Assert.Equal("Runtime", item.PlcName);
        Assert.Equal("Runtime", item.Category);
        Assert.Equal("message", item.Message);
        Assert.Equal("Live", item.Status);
    }

    [Fact]
    public void Map_ThrowsArgumentNullExceptionWhenDashboardEventIsNull()
    {
        var mapper = new RuntimeEventLogItemMapper();

        Assert.Throws<ArgumentNullException>(() => mapper.Map(null!));
    }

    private static RuntimeEventLogItem Map(RuntimeDashboardEvent dashboardEvent)
    {
        var mapper = new RuntimeEventLogItemMapper();

        return mapper.Map(dashboardEvent);
    }
}
