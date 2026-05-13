using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Adapters;

internal static class FakeRuntimeSignalFactory
{
    private const int SequenceLatencyBucketCount = 10;
    private static readonly TimeSpan SequenceLatencyBucketDuration = TimeSpan.FromSeconds(30);

    public static PlcRuntimeSignalSnapshot Create(PlcConnectionState state, int index, int tick)
    {
        var (sequenceName, sequenceStatus, elapsed, message) = state switch
        {
            PlcConnectionState.Healthy => (
                "폴링",
                RuntimeSequenceStatus.Completed,
                TimeSpan.Zero,
                "최근 시퀀스 응답 정상"),
            PlcConnectionState.Warning => (
                "DB조회",
                RuntimeSequenceStatus.Delayed,
                TimeSpan.FromSeconds(8 + ((tick + index) % 7)),
                "요청~응답 지연 감지"),
            PlcConnectionState.Congested => (
                "데이터전송",
                RuntimeSequenceStatus.Waiting,
                TimeSpan.FromSeconds(5 + ((tick + index) % 9)),
                "시퀀스 응답 대기 중"),
            PlcConnectionState.Error => (
                index % 2 == 0 ? "완공응답" : "착공응답",
                RuntimeSequenceStatus.Failed,
                TimeSpan.FromSeconds(18 + (tick % 10)),
                "시퀀스 응답 오류"),
            PlcConnectionState.Inactive => (
                "대기",
                RuntimeSequenceStatus.Idle,
                TimeSpan.Zero,
                "PLC 비활성"),
            _ => (
                "대기",
                RuntimeSequenceStatus.Idle,
                TimeSpan.Zero,
                null)
        };

        return new PlcRuntimeSignalSnapshot(
            sequenceName,
            sequenceStatus,
            elapsed,
            message,
            CreateSequenceLatencyBuckets(state, index, tick));
    }

    private static IReadOnlyList<SequenceResponseLatencyBucket> CreateSequenceLatencyBuckets(
        PlcConnectionState state,
        int index,
        int tick)
    {
        var now = DateTimeOffset.UtcNow;

        return Enumerable.Range(0, SequenceLatencyBucketCount)
            .Select(bucketIndex =>
            {
                var ageFromNow = SequenceLatencyBucketCount - 1 - bucketIndex;
                var bucketStart = now.AddTicks(-(SequenceLatencyBucketDuration.Ticks * ageFromNow));
                var wave = (int)Math.Round(Math.Sin((bucketIndex + tick + index) / 2.4) * 12);
                var pulse = (bucketIndex + tick + index) % 6 == 0 ? 24 : 0;
                var (startBase, completionBase) = GetSequenceLatencyBase(state);
                var errorSpike = state == PlcConnectionState.Error && (bucketIndex + tick + index) % 4 == 0 ? 220 : 0;

                return new SequenceResponseLatencyBucket(
                    bucketStart,
                    SequenceLatencyBucketDuration,
                    Math.Max(0, startBase + wave + pulse + errorSpike),
                    Math.Max(0, completionBase + wave + (pulse / 2) + errorSpike));
            })
            .ToArray();
    }

    private static (int StartResponseMs, int CompletionResponseMs) GetSequenceLatencyBase(PlcConnectionState state)
        => state switch
        {
            PlcConnectionState.Healthy => (42, 56),
            PlcConnectionState.Warning => (180, 235),
            PlcConnectionState.Congested => (410, 485),
            PlcConnectionState.Error => (620, 720),
            PlcConnectionState.Inactive => (0, 0),
            _ => (0, 0)
        };
}
