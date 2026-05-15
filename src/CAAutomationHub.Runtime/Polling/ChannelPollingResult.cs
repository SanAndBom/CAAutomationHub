namespace CAAutomationHub.Runtime.Polling;

/// <summary>
/// Vendor-neutral Runtime result for one PLC channel polling event.
/// </summary>
public sealed record ChannelPollingResult
{
    private ChannelPollingResult(
        string plcId,
        DateTimeOffset occurredAt,
        bool isSuccess,
        int? responseTimeMs,
        ChannelPollingFailureKind? failureKind,
        string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(plcId))
        {
            throw new ArgumentException("PLC id is required.", nameof(plcId));
        }

        if (responseTimeMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(responseTimeMs), responseTimeMs, "Response time must not be negative.");
        }

        PlcId = plcId;
        OccurredAt = occurredAt;
        IsSuccess = isSuccess;
        ResponseTimeMs = responseTimeMs;
        FailureKind = failureKind;
        ErrorMessage = errorMessage;
    }

    public string PlcId { get; }

    public DateTimeOffset OccurredAt { get; }

    public bool IsSuccess { get; }

    public int? ResponseTimeMs { get; }

    public ChannelPollingFailureKind? FailureKind { get; }

    public string? ErrorMessage { get; }

    public static ChannelPollingResult Success(
        string plcId,
        DateTimeOffset occurredAt,
        int? responseTimeMs = null)
        => new(
            plcId,
            occurredAt,
            isSuccess: true,
            responseTimeMs,
            failureKind: null,
            errorMessage: null);

    public static ChannelPollingResult Failure(
        string plcId,
        DateTimeOffset occurredAt,
        ChannelPollingFailureKind failureKind,
        string? errorMessage,
        int? responseTimeMs = null)
        => new(
            plcId,
            occurredAt,
            isSuccess: false,
            responseTimeMs,
            failureKind,
            errorMessage);
}
