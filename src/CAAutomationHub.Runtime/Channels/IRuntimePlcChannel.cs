using CAAutomationHub.Contracts.Runtime;

namespace CAAutomationHub.Runtime.Channels;

public interface IRuntimePlcChannel
{
    string PlcId { get; }

    ChannelRuntimeState GetState(DateTimeOffset capturedAt);
}
