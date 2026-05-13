using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.ViewModels;

public sealed class PlcStatusCardViewModel : ViewModelBase
{
    private const double MiniTrendMaxBarHeight = 22;
    private bool _isSelected;

    public PlcStatusCardViewModel(PlcCardSnapshot snapshot) => Snapshot = snapshot;

    public PlcCardSnapshot Snapshot { get; private set; }
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string PlcId => Snapshot.PlcId;
    public string PlcName => Snapshot.PlcName;
    public string LineName => Snapshot.LineName;
    public string IpAddress => Snapshot.IpAddress;
    public int Port => Snapshot.Port;
    public string EndpointText => $"{Snapshot.IpAddress}:{Snapshot.Port}";
    public int PollingIntervalMs => Snapshot.PollingIntervalMs;
    public string PollingIntervalText => $"{Snapshot.PollingIntervalMs}ms";
    public int LastResponseMs => Snapshot.LastResponseMs;
    public string LastResponseText => Snapshot.LastResponseMs > 0 ? $"{Snapshot.LastResponseMs}ms" : "-";
    public int TxPerMinute => Snapshot.TxPerMinute;
    public int RxPerMinute => Snapshot.RxPerMinute;
    public string TxRxText => $"{Snapshot.TxPerMinute} / {Snapshot.RxPerMinute}";
    public int ErrorCount => Snapshot.ErrorCount;
    public PlcRuntimeSignalSnapshot RuntimeSignal => Snapshot.RuntimeSignal;
    public string CurrentSequenceName => RuntimeSignal.CurrentSequenceName;
    public RuntimeSequenceStatus CurrentSequenceStatus => RuntimeSignal.CurrentSequenceStatus;
    public TimeSpan CurrentSequenceElapsed => RuntimeSignal.CurrentSequenceElapsed;
    public string? LastSequenceMessage => RuntimeSignal.LastSequenceMessage;
    public IReadOnlyList<SequenceResponseLatencyBucketViewModel> SequenceResponseLatencyBuckets
        => CreateLatencyBucketViewModels(RuntimeSignal.ResponseLatencyBuckets);
    public string CurrentSequenceStatusText => RuntimeSignal.CurrentSequenceStatus switch
    {
        RuntimeSequenceStatus.Idle => "대기",
        RuntimeSequenceStatus.Running => "진행",
        RuntimeSequenceStatus.Waiting => "대기",
        RuntimeSequenceStatus.Delayed => "지연",
        RuntimeSequenceStatus.Failed => "오류",
        RuntimeSequenceStatus.Completed => "완료",
        _ => RuntimeSignal.CurrentSequenceStatus.ToString()
    };
    public string CurrentSequenceElapsedText => FormatElapsed(RuntimeSignal.CurrentSequenceElapsed);
    public string CurrentSequenceText => CreateCurrentSequenceText();
    public PlcConnectionState ConnectionState => Snapshot.ConnectionState;
    public string ConnectionText => Snapshot.ConnectionState.ToString();
    public string StatusText => Snapshot.ConnectionState switch
    {
        PlcConnectionState.Healthy => "정상",
        PlcConnectionState.Warning => "주의",
        PlcConnectionState.Congested => "정체",
        PlcConnectionState.Error => "오류",
        PlcConnectionState.Inactive => "비활성",
        _ => Snapshot.ConnectionState.ToString()
    };

    public void UpdateSnapshot(PlcCardSnapshot snapshot)
    {
        if (Snapshot == snapshot) return;

        Snapshot = snapshot;
        RaiseSnapshotPropertiesChanged();
    }

    private void RaiseSnapshotPropertiesChanged()
    {
        RaisePropertyChanged(nameof(Snapshot));
        RaisePropertyChanged(nameof(PlcId));
        RaisePropertyChanged(nameof(PlcName));
        RaisePropertyChanged(nameof(LineName));
        RaisePropertyChanged(nameof(IpAddress));
        RaisePropertyChanged(nameof(Port));
        RaisePropertyChanged(nameof(EndpointText));
        RaisePropertyChanged(nameof(PollingIntervalMs));
        RaisePropertyChanged(nameof(PollingIntervalText));
        RaisePropertyChanged(nameof(LastResponseMs));
        RaisePropertyChanged(nameof(LastResponseText));
        RaisePropertyChanged(nameof(TxPerMinute));
        RaisePropertyChanged(nameof(RxPerMinute));
        RaisePropertyChanged(nameof(TxRxText));
        RaisePropertyChanged(nameof(ErrorCount));
        RaisePropertyChanged(nameof(RuntimeSignal));
        RaisePropertyChanged(nameof(CurrentSequenceName));
        RaisePropertyChanged(nameof(CurrentSequenceStatus));
        RaisePropertyChanged(nameof(CurrentSequenceElapsed));
        RaisePropertyChanged(nameof(LastSequenceMessage));
        RaisePropertyChanged(nameof(SequenceResponseLatencyBuckets));
        RaisePropertyChanged(nameof(CurrentSequenceStatusText));
        RaisePropertyChanged(nameof(CurrentSequenceElapsedText));
        RaisePropertyChanged(nameof(CurrentSequenceText));
        RaisePropertyChanged(nameof(ConnectionState));
        RaisePropertyChanged(nameof(ConnectionText));
        RaisePropertyChanged(nameof(StatusText));
    }

    private string CreateCurrentSequenceText()
    {
        var name = string.IsNullOrWhiteSpace(RuntimeSignal.CurrentSequenceName)
            ? PlcRuntimeSignalSnapshot.Empty.CurrentSequenceName
            : RuntimeSignal.CurrentSequenceName;

        return RuntimeSignal.CurrentSequenceStatus switch
        {
            RuntimeSequenceStatus.Idle => $"현재: {name}",
            RuntimeSequenceStatus.Completed when RuntimeSignal.CurrentSequenceElapsed <= TimeSpan.Zero => $"현재: {name}",
            RuntimeSequenceStatus.Completed => $"현재: {name} · 완료 {CurrentSequenceElapsedText}",
            RuntimeSequenceStatus.Delayed => $"현재: {name} · 지연 {CurrentSequenceElapsedText}",
            RuntimeSequenceStatus.Failed => $"현재: {name} · 오류",
            RuntimeSequenceStatus.Waiting => $"현재: {name} · 대기 {CurrentSequenceElapsedText}",
            RuntimeSequenceStatus.Running => $"현재: {name} · {CurrentSequenceElapsedText}",
            _ => $"현재: {name}"
        };
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero) return "0s";
        if (elapsed.TotalMinutes >= 1) return $"{(int)elapsed.TotalMinutes}m {elapsed.Seconds}s";
        return $"{Math.Max(1, (int)Math.Round(elapsed.TotalSeconds))}s";
    }

    private static IReadOnlyList<SequenceResponseLatencyBucketViewModel> CreateLatencyBucketViewModels(
        IReadOnlyList<SequenceResponseLatencyBucket> buckets)
    {
        if (buckets.Count == 0) return Array.Empty<SequenceResponseLatencyBucketViewModel>();

        var maxResponse = buckets
            .SelectMany(bucket => new[] { bucket.StartResponseMs, bucket.CompletionResponseMs })
            .DefaultIfEmpty(0)
            .Max();
        var yMax = Math.Max(1, maxResponse);

        return buckets
            .Select(bucket => new SequenceResponseLatencyBucketViewModel(
                bucket,
                ScaleBarHeight(bucket.StartResponseMs, yMax),
                ScaleBarHeight(bucket.CompletionResponseMs, yMax)))
            .ToArray();
    }

    private static double ScaleBarHeight(int responseMs, int yMax)
        => responseMs <= 0 ? 1 : Math.Max(2, Math.Round((responseMs / (double)yMax) * MiniTrendMaxBarHeight));
}

public sealed record SequenceResponseLatencyBucketViewModel(
    SequenceResponseLatencyBucket Bucket,
    double StartBarHeight,
    double CompletionBarHeight)
{
    public int StartResponseMs => Bucket.StartResponseMs;
    public int CompletionResponseMs => Bucket.CompletionResponseMs;
}
