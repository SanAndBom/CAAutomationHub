using CAAutomationHub.PilotApp.WorkStart;
using CAAutomationHub.PilotFlows.WorkStart;

namespace CAAutomationHub.PilotComposition.WorkStart;

public sealed class WorkStartDemoComposition : IWorkStartExecutionServiceFactory
{
    private static readonly DateTimeOffset DefaultStartedAt = new(2026, 5, 18, 0, 0, 0, TimeSpan.Zero);
    private readonly WorkStartDemoOptions _options;

    public WorkStartDemoComposition()
        : this(new WorkStartDemoOptions())
    {
    }

    public WorkStartDemoComposition(WorkStartDemoOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public IWorkStartExecutionService Create()
    {
        var runner = new DemoWorkStartFlowRunner(_options);
        var clock = new DemoWorkStartExecutionClock(_options.StartedAt ?? DefaultStartedAt);

        return new WorkStartExecutionService(runner, clock);
    }

    private sealed class DemoWorkStartFlowRunner : IWorkStartFlowRunner
    {
        private readonly WorkStartDemoOptions _options;

        public DemoWorkStartFlowRunner(WorkStartDemoOptions options)
        {
            _options = options;
        }

        public ValueTask<WorkStartFlowResult> RunAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = _options.ShouldSucceed
                ? WorkStartFlowResult.Success(_options.SimulatedLotId)
                : WorkStartFlowResult.Failure(
                    WorkStartStep.Exception,
                    WorkStartErrorCode.UnexpectedException,
                    _options.FailureMessage ?? "Demo WorkStart execution failed.",
                    selectedLotId: null);

            return ValueTask.FromResult(result);
        }
    }

    private sealed class DemoWorkStartExecutionClock : IWorkStartExecutionClock
    {
        private readonly DateTimeOffset _startedAt;
        private int _callCount;

        public DemoWorkStartExecutionClock(DateTimeOffset startedAt)
        {
            _startedAt = startedAt;
        }

        public DateTimeOffset GetUtcNow()
        {
            var call = Interlocked.Increment(ref _callCount);
            return _startedAt.AddSeconds(call - 1);
        }
    }
}
