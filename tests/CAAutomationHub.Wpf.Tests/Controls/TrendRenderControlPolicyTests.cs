using CAAutomationHub.Wpf.Controls;
using CAAutomationHub.Wpf.Models.Dashboard;
using System.Windows.Media;

namespace CAAutomationHub.Wpf.Tests.Controls;

public sealed class TrendRenderControlPolicyTests
{
    [Fact]
    public void GetSeriesRenderPriority_OrdersStatesByRisk()
    {
        var inactive = TrendRenderControl.GetSeriesRenderPriority(PlcConnectionState.Inactive, isWorst: false);
        var healthy = TrendRenderControl.GetSeriesRenderPriority(PlcConnectionState.Healthy, isWorst: false);
        var warning = TrendRenderControl.GetSeriesRenderPriority(PlcConnectionState.Warning, isWorst: false);
        var congested = TrendRenderControl.GetSeriesRenderPriority(PlcConnectionState.Congested, isWorst: false);
        var error = TrendRenderControl.GetSeriesRenderPriority(PlcConnectionState.Error, isWorst: false);

        Assert.True(inactive < healthy);
        Assert.True(healthy < warning);
        Assert.True(warning < congested);
        Assert.True(congested < error);
    }

    [Fact]
    public void GetSeriesRenderPriority_PutsWorstSeriesAboveStateLines()
    {
        var worstHealthy = TrendRenderControl.GetSeriesRenderPriority(PlcConnectionState.Healthy, isWorst: true);
        var error = TrendRenderControl.GetSeriesRenderPriority(PlcConnectionState.Error, isWorst: false);

        Assert.True(worstHealthy > error);
    }

    [Theory]
    [InlineData(249.9, PlcConnectionState.Healthy)]
    [InlineData(250, PlcConnectionState.Warning)]
    [InlineData(499.9, PlcConnectionState.Warning)]
    [InlineData(500, PlcConnectionState.Congested)]
    [InlineData(749.9, PlcConnectionState.Congested)]
    [InlineData(750, PlcConnectionState.Error)]
    public void GetRttSegmentState_UsesSharedThresholdPolicy(double responseMs, PlcConnectionState expected)
    {
        var actual = TrendRenderControl.GetRttSegmentState(
            responseMs,
            warningThresholdMs: 250,
            congestedThresholdMs: 500,
            errorThresholdMs: 750);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetSegmentRenderStyle_MakesHealthyLineVisibleWithoutChangingStatePolicy()
    {
        var overview = TrendRenderControl.GetSegmentRenderStyle(
            PlcConnectionState.Healthy,
            isWorst: false,
            isOverview: true);
        var selected = TrendRenderControl.GetSegmentRenderStyle(
            PlcConnectionState.Healthy,
            isWorst: false,
            isOverview: false);

        Assert.Equal(Color.FromRgb(96, 194, 255), overview.Color);
        Assert.True(overview.Thickness >= 1.15);
        Assert.True(overview.Opacity >= 0.52);
        Assert.True(selected.Thickness >= overview.Thickness);
        Assert.True(selected.Opacity > overview.Opacity);
    }
}
