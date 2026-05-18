using CAAutomationHub.PilotApp.Polling;
using CAAutomationHub.PilotComposition.Configuration;

namespace CAAutomationHub.PilotComposition.Polling;

public sealed record PilotPollingComposition(
    IPilotPollingService PollingService,
    PilotLocalConfiguration Configuration,
    string StatusMessage);
