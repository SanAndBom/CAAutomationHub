namespace CAAutomationHub.PilotSmoke;

public sealed record PilotSmokeConnectionOptions(
    string Host,
    int Port,
    TimeSpan ConnectTimeout,
    TimeSpan SendTimeout,
    TimeSpan ReceiveTimeout);

public sealed record PilotSmokeReadOptions(
    string ReadStartVariable,
    int ReadWordCount);

public sealed record PilotSmokeReadLayout(
    int StartSignalWordIndex,
    int LotId1WordOffset,
    int LotId2WordOffset,
    int LotIdWordLength);

public sealed record PilotSmokeConfiguration(
    bool ShouldExecuteRead,
    string? SkipReason,
    PilotSmokeConnectionOptions Connection,
    PilotSmokeReadOptions Read,
    PilotSmokeReadLayout Layout)
{
    public string MaskedHost => MaskHost(Connection.Host);

    private static string MaskHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return "(not configured)";
        }

        var parts = host.Split('.');
        if (parts.Length == 4 && parts.All(part => int.TryParse(part, out _)))
        {
            return string.Join('.', parts.Take(3)) + ".x";
        }

        return host.Length == 1 ? "*" : $"{host[0]}***";
    }
}
