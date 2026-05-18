using AutomationHub.XgtDriverCore.Protocol;
using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotFlows.Xgt.WorkStart;

internal static class WorkStartXgtReadResultMapper
{
    public static WorkStartReadBlockOperationResult Map(XgtReadResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.Status.IsNak)
        {
            return WorkStartReadBlockOperationResult.OperationFailed("XGT read returned NAK.");
        }

        if (response.VariableBlocks.Count == 0)
        {
            return WorkStartReadBlockOperationResult.ParseFailed(
                "XGT ACK read response did not include a data block.");
        }

        var data = response.VariableBlocks[0].Data;
        if (data is null || data.Length == 0)
        {
            return WorkStartReadBlockOperationResult.ParseFailed(
                "XGT ACK read response did not include readable data block bytes.");
        }

        return WorkStartReadBlockOperationResult.Success(data);
    }
}
