namespace CAAutomationHub.PilotFlows.Xgt.Polling;

public sealed record XgtPilotConnectionOptions
{
    public required string Host { get; init; }

    public required int Port { get; init; }

    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(1);

    public TimeSpan SendTimeout { get; init; } = TimeSpan.FromSeconds(1);

    public TimeSpan ReceiveTimeout { get; init; } = TimeSpan.FromSeconds(1);
}
