using CAAutomationHub.PilotFlows.SqlServer.WorkStart;
using CAAutomationHub.PilotFlows.WorkStart;
using Xunit;

namespace CAAutomationHub.PilotFlows.SqlServer.Tests.WorkStart;

public sealed class SqlServerWorkStartDataQuerySmokeTests
{
    [SqlServerSmokeFact]
    public async Task QueryAsync_WithEnvironmentConfiguration_ReturnsTerminalStatus()
    {
        var connectionString = Environment.GetEnvironmentVariable("CAAH_WORKSTART_DB_CONNECTION_STRING");
        var lotId = Environment.GetEnvironmentVariable("CAAH_WORKSTART_TEST_LOT_ID");
        Assert.False(string.IsNullOrWhiteSpace(connectionString));
        Assert.False(string.IsNullOrWhiteSpace(lotId));

        var query = new SqlServerWorkStartDataQuery(new SqlServerWorkStartDataQueryOptions
        {
            ConnectionString = connectionString,
            SqlText = WorkStartSqlQueryText.Default
        });

        var result = await query.QueryAsync(lotId);

        Assert.Contains(
            result.Status,
            new[]
            {
                WorkStartDataQueryStatus.Succeeded,
                WorkStartDataQueryStatus.NotFound,
                WorkStartDataQueryStatus.MultipleRows
            });
        if (result.Status == WorkStartDataQueryStatus.Succeeded)
        {
            Assert.NotNull(result.ProcessData);
            Assert.Equal(lotId, result.ProcessData.LotId);
        }
    }

    private sealed class SqlServerSmokeFactAttribute : FactAttribute
    {
        public SqlServerSmokeFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CAAH_WORKSTART_DB_CONNECTION_STRING"))
                || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CAAH_WORKSTART_TEST_LOT_ID")))
            {
                Skip = "Set CAAH_WORKSTART_DB_CONNECTION_STRING and CAAH_WORKSTART_TEST_LOT_ID to run SQL Server smoke.";
            }
        }
    }
}
