using System.Data;
using CAAutomationHub.PilotFlows.WorkStart;
using Microsoft.Data.SqlClient;

namespace CAAutomationHub.PilotFlows.SqlServer.WorkStart;

public sealed class SqlServerWorkStartDataQuery : IWorkStartDataQuery
{
    private readonly SqlServerWorkStartDataQueryOptions _options;

    public SqlServerWorkStartDataQuery(SqlServerWorkStartDataQueryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _options = options;
    }

    public async ValueTask<WorkStartDataQueryResult> QueryAsync(
        string lotId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lotId))
        {
            return WorkStartDataQueryResult.Failed(lotId, "LOT ID is required.");
        }

        try
        {
            await using var connection = new SqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = _options.SqlText;
            command.CommandTimeout = _options.CommandTimeoutSeconds;
            command.Parameters.Add("@LotId", SqlDbType.NVarChar, 64).Value = lotId;

            await using var reader = await command
                .ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken)
                .ConfigureAwait(false);

            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return WorkStartDataQueryResult.NotFound(lotId);
            }

            var processData = SqlServerWorkStartDataQueryMapper.Map(lotId, reader);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return WorkStartDataQueryResult.MultipleRows(lotId);
            }

            return WorkStartDataQueryResult.Success(processData);
        }
        catch (SqlException)
        {
            return WorkStartDataQueryResult.DbException(lotId, "SQL Server WorkStart query exception.");
        }
        catch (InvalidOperationException)
        {
            return WorkStartDataQueryResult.Failed(lotId, "SQL Server WorkStart query result mapping failed.");
        }
        catch (Exception)
        {
            return WorkStartDataQueryResult.Failed(lotId, "SQL Server WorkStart query failed.");
        }
    }
}
