using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotFlows.Tests.WorkStart;

public sealed class WorkStartReadBlockOperationResultTests
{
    [Fact]
    public void Success_CopiesData()
    {
        var source = new byte[] { 1, 2, 3 };

        var result = WorkStartReadBlockOperationResult.Success(source);
        source[0] = 9;

        Assert.Equal(WorkStartReadBlockOperationStatus.Success, result.Status);
        Assert.Equal(new byte[] { 1, 2, 3 }, result.Data);
    }

    [Fact]
    public void Failure_HasNoData()
    {
        var operationFailed = WorkStartReadBlockOperationResult.OperationFailed("operation failed");
        var parseFailed = WorkStartReadBlockOperationResult.ParseFailed("parse failed");

        Assert.Equal(WorkStartReadBlockOperationStatus.OperationFailed, operationFailed.Status);
        Assert.Null(operationFailed.Data);
        Assert.Equal("operation failed", operationFailed.Message);

        Assert.Equal(WorkStartReadBlockOperationStatus.ParseFailed, parseFailed.Status);
        Assert.Null(parseFailed.Data);
        Assert.Equal("parse failed", parseFailed.Message);
    }
}
