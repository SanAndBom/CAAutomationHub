using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotFlows.Tests.WorkStart;

public sealed class WorkStartFlowResultPolicyTests
{
    [Fact]
    public void ErrorPolicy_ReturnsTrue_ForErrorWriteCodes()
    {
        var writeCodes = new[]
        {
            WorkStartErrorCode.LotIdEmpty,
            WorkStartErrorCode.DbException,
            WorkStartErrorCode.DbNotFound,
            WorkStartErrorCode.DbMultipleRows,
            WorkStartErrorCode.DbFailed,
            WorkStartErrorCode.PayloadBuildFailed,
            WorkStartErrorCode.BulkWriteFailed,
            WorkStartErrorCode.AckWriteFailed
        };

        foreach (var code in writeCodes)
        {
            Assert.True(WorkStartErrorWritePolicy.ShouldWriteErrorCode(code));
        }
    }

    [Fact]
    public void ErrorPolicy_ReturnsFalse_ForNonWriteCodes()
    {
        var nonWriteCodes = new[]
        {
            WorkStartErrorCode.None,
            WorkStartErrorCode.ReadFailed,
            WorkStartErrorCode.ReadParseFailed,
            WorkStartErrorCode.StartSignalInactive,
            WorkStartErrorCode.UnexpectedException
        };

        foreach (var code in nonWriteCodes)
        {
            Assert.False(WorkStartErrorWritePolicy.ShouldWriteErrorCode(code));
        }
    }

    [Fact]
    public void ErrorCode_ExposesOriginalNumericCode()
    {
        Assert.Equal(0, (int)WorkStartErrorCode.None);
        Assert.Equal(1101, (int)WorkStartErrorCode.ReadFailed);
        Assert.Equal(1102, (int)WorkStartErrorCode.ReadParseFailed);
        Assert.Equal(1200, (int)WorkStartErrorCode.StartSignalInactive);
        Assert.Equal(2201, (int)WorkStartErrorCode.LotIdEmpty);
        Assert.Equal(2300, (int)WorkStartErrorCode.DbException);
        Assert.Equal(2301, (int)WorkStartErrorCode.DbNotFound);
        Assert.Equal(2302, (int)WorkStartErrorCode.DbMultipleRows);
        Assert.Equal(2303, (int)WorkStartErrorCode.DbFailed);
        Assert.Equal(2400, (int)WorkStartErrorCode.PayloadBuildFailed);
        Assert.Equal(2501, (int)WorkStartErrorCode.BulkWriteFailed);
        Assert.Equal(2601, (int)WorkStartErrorCode.AckWriteFailed);
        Assert.Equal(2999, (int)WorkStartErrorCode.UnexpectedException);
    }

    [Fact]
    public void Step_ExposesOriginalStepCode()
    {
        Assert.Equal("completed", WorkStartStep.Completed.ToCode());
        Assert.Equal("group-read", WorkStartStep.GroupRead.ToCode());
        Assert.Equal("group-read-parse", WorkStartStep.GroupReadParse.ToCode());
        Assert.Equal("start-signal", WorkStartStep.StartSignal.ToCode());
        Assert.Equal("lotid", WorkStartStep.LotId.ToCode());
        Assert.Equal("db-query", WorkStartStep.DbQuery.ToCode());
        Assert.Equal("payload-build", WorkStartStep.PayloadBuild.ToCode());
        Assert.Equal("bulk-write", WorkStartStep.BulkWrite.ToCode());
        Assert.Equal("ack-write", WorkStartStep.AckWrite.ToCode());
        Assert.Equal("exception", WorkStartStep.Exception.ToCode());
    }

    [Fact]
    public void Result_Success_HasCompletedStepAndNoError()
    {
        var result = WorkStartFlowResult.Success("LOT-1");

        Assert.True(result.Succeeded);
        Assert.Equal(WorkStartStep.Completed, result.Step);
        Assert.Equal(WorkStartErrorCode.None, result.ErrorCode);
        Assert.Null(result.Message);
        Assert.Equal("LOT-1", result.SelectedLotId);
        Assert.False(result.ErrorWriteExpected);
        Assert.Equal(WorkStartFlowStatus.Succeeded, result.Status);
    }

    [Fact]
    public void Result_Failure_PreservesStepErrorMessageAndLotId()
    {
        var result = WorkStartFlowResult.Failure(
            WorkStartStep.DbQuery,
            WorkStartErrorCode.DbNotFound,
            "DB row was not found.",
            "LOT-9");

        Assert.False(result.Succeeded);
        Assert.Equal(WorkStartStep.DbQuery, result.Step);
        Assert.Equal(WorkStartErrorCode.DbNotFound, result.ErrorCode);
        Assert.Equal("DB row was not found.", result.Message);
        Assert.Equal("LOT-9", result.SelectedLotId);
        Assert.Equal(WorkStartFlowStatus.Failed, result.Status);
    }

    [Fact]
    public void Result_Failure_DerivesErrorWriteExpectedFromPolicy()
    {
        var writeResult = WorkStartFlowResult.Failure(
            WorkStartStep.PayloadBuild,
            WorkStartErrorCode.PayloadBuildFailed,
            "Payload build failed.",
            "LOT-1");

        var nonWriteResult = WorkStartFlowResult.Failure(
            WorkStartStep.GroupRead,
            WorkStartErrorCode.ReadFailed,
            "PLC read failed.",
            selectedLotId: null);

        Assert.True(writeResult.ErrorWriteExpected);
        Assert.False(nonWriteResult.ErrorWriteExpected);
    }

    [Fact]
    public void Result_DoesNotRequireDiagnosticHexOrRuntimeState()
    {
        var propertyNames = typeof(WorkStartFlowResult)
            .GetProperties()
            .Select(static property => property.Name)
            .ToArray();

        Assert.DoesNotContain("RequestHex", propertyNames);
        Assert.DoesNotContain("ResponseHex", propertyNames);
        Assert.DoesNotContain("ElapsedMs", propertyNames);
        Assert.DoesNotContain("ReconnectAttemptCount", propertyNames);
        Assert.DoesNotContain(string.Join("", "Runtime", "Snapshot"), propertyNames);
        Assert.DoesNotContain(string.Join("", "Channel", "Polling", "Result"), propertyNames);
    }
}
