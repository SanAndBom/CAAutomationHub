namespace CAAutomationHub.Contracts.Runtime;

public sealed record RuntimeDashboardCommand
{
    public RuntimeDashboardCommand(
        string commandId,
        RuntimeDashboardCommandKind kind,
        string? plcId,
        DateTimeOffset requestedAt,
        IReadOnlyDictionary<string, string>? parameters)
    {
        CommandId = commandId;
        Kind = kind;
        PlcId = plcId;
        RequestedAt = requestedAt;
        Parameters = parameters ?? new Dictionary<string, string>();
    }

    public string CommandId { get; init; }
    public RuntimeDashboardCommandKind Kind { get; init; }
    public string? PlcId { get; init; }
    public DateTimeOffset RequestedAt { get; init; }
    public IReadOnlyDictionary<string, string> Parameters { get; init; }
}
