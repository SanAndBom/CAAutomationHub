using AutomationHub.XgtDriverCore.Client;
using AutomationHub.XgtDriverCore.Transport;
using CAAutomationHub.PilotFlows.Xgt.WorkStart;
using CAAutomationHub.PilotSmoke;

var configuration = PilotSmokeConfigurationLoader.Load(
    args,
    Environment.GetEnvironmentVariable);

Console.WriteLine("CAAutomationHub PilotSmoke WorkStart read-only harness");
Console.WriteLine($"Target: {configuration.MaskedHost}:{configuration.Connection.Port}");

if (!configuration.ShouldExecuteRead)
{
    Console.WriteLine($"SKIPPED: {configuration.SkipReason}");
    return 0;
}

await using var session = new XgtSession(new TcpTransport(new XgtTransportOptions
{
    Host = configuration.Connection.Host,
    Port = configuration.Connection.Port,
    ConnectTimeout = configuration.Connection.ConnectTimeout,
    SendTimeout = configuration.Connection.SendTimeout,
    ReceiveTimeout = configuration.Connection.ReceiveTimeout
}));

var operations = new WorkStartXgtPlcOperations(
    session,
    new WorkStartXgtReadOptions(
        configuration.Read.ReadStartVariable,
        configuration.Read.ReadWordCount));

var result = await WorkStartReadOnlySmoke.RunAsync(operations, configuration.Layout);

Console.WriteLine($"Connection success: {result.ConnectionSucceeded}");
Console.WriteLine($"Read success: {result.ReadSucceeded}");
Console.WriteLine($"Start signal active: {result.StartRequestActive}");
Console.WriteLine($"LOT ID 1: {result.LotId1 ?? string.Empty}");
Console.WriteLine($"LOT ID 2: {result.LotId2 ?? string.Empty}");
Console.WriteLine($"Selected LOT ID: {result.SelectedLotId ?? string.Empty}");
Console.WriteLine($"Raw length: {result.RawLength}");

if (!string.IsNullOrWhiteSpace(result.Message))
{
    Console.WriteLine($"Message: {result.Message}");
}

return result.ConnectionSucceeded && result.ReadSucceeded ? 0 : 1;
