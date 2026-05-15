namespace CAAutomationHub.Runtime.Polling;

/// <summary>
/// Vendor-neutral Runtime classification for a polling event failure.
/// </summary>
public enum ChannelPollingFailureKind
{
    Timeout,
    Connection,
    Protocol,
    UnexpectedResponse,
    Cancelled,
    Unknown
}
