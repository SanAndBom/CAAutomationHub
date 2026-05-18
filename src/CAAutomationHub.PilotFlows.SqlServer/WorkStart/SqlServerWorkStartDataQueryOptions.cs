namespace CAAutomationHub.PilotFlows.SqlServer.WorkStart;

public sealed record SqlServerWorkStartDataQueryOptions
{
    public required string ConnectionString { get; init; }

    public string SqlText { get; init; } = WorkStartSqlQueryText.Default;

    public int CommandTimeoutSeconds { get; init; } = 30;

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new ArgumentException("SQL Server connection string is required.", nameof(ConnectionString));
        }

        if (string.IsNullOrWhiteSpace(SqlText))
        {
            throw new ArgumentException("SQL Server WorkStart query text is required.", nameof(SqlText));
        }

        if (!SqlText.Contains("@LotId", StringComparison.Ordinal))
        {
            throw new ArgumentException("SQL Server WorkStart query text must use @LotId parameter.", nameof(SqlText));
        }

        if (CommandTimeoutSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(CommandTimeoutSeconds), "SQL Server command timeout must be greater than zero.");
        }
    }
}
