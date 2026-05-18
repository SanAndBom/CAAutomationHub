using System.Data;
using System.Globalization;
using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotFlows.SqlServer.WorkStart;

internal static class SqlServerWorkStartDataQueryMapper
{
    public static WorkStartProcessData Map(string lotId, IDataRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new WorkStartProcessData
        {
            LotId = lotId,
            Profile = GetString(record, "PROFILE"),
            Tblr = GetString(record, "TBLR"),
            WinType = GetString(record, "WIN_TYPE"),
            CutSize = GetInt32(record, "CUT_SIZE"),
            Lr = GetString(record, "LR"),
            RollerYn = GetString(record, "RollerYN"),
            RollerHolePos = GetInt32(record, "ROLLER_HOLE_POS"),
            RollerHoleWidth = GetInt32(record, "ROLLER_HOLE_WIDTH"),
            RollerHoleLength = GetInt32(record, "ROLLER_HOLE_LENGTH"),
            RollerType = GetString(record, "ROLLER_TYPE"),
            CutDegree = GetInt32(record, "CUT_DEGREE")
        };
    }

    private static string GetString(IDataRecord record, string name)
    {
        var ordinal = GetOrdinal(record, name);
        if (record.IsDBNull(ordinal))
        {
            return string.Empty;
        }

        return Convert.ToString(record.GetValue(ordinal), CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static int GetInt32(IDataRecord record, string name)
    {
        var ordinal = GetOrdinal(record, name);
        if (record.IsDBNull(ordinal))
        {
            return 0;
        }

        return Convert.ToInt32(record.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private static int GetOrdinal(IDataRecord record, string name)
    {
        try
        {
            return record.GetOrdinal(name);
        }
        catch (IndexOutOfRangeException ex)
        {
            throw new InvalidOperationException($"SQL Server WorkStart query result is missing column '{name}'.", ex);
        }
    }
}
