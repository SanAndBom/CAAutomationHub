using CAAutomationHub.Wpf.Models.Dashboard;
using CAAutomationHub.Wpf.Services;

namespace CAAutomationHub.Wpf.Tests.Services;

public sealed class DashboardRefreshOrchestratorTests
{
    [Fact]
    public void SubmitSnapshot_ThrowsWhenSnapshotIsNull()
    {
        var dispatcher = new RecordingUiDispatcher();
        var orchestrator = new DashboardRefreshOrchestrator(dispatcher, _ => { });

        Assert.Throws<ArgumentNullException>(() => orchestrator.SubmitSnapshot(null!));
    }

    [Fact]
    public void SubmitSnapshot_PostsApplyToDispatcher()
    {
        var dispatcher = new RecordingUiDispatcher();
        var orchestrator = new DashboardRefreshOrchestrator(dispatcher, _ => { });

        orchestrator.SubmitSnapshot(CreateSnapshot(1));

        Assert.Equal(1, dispatcher.PostCount);
        Assert.Equal(1, dispatcher.PendingCount);
    }

    [Fact]
    public void SubmitSnapshot_CoalescesMultipleSubmissionsIntoOneDispatcherPost()
    {
        var dispatcher = new RecordingUiDispatcher();
        var applied = new List<DashboardSnapshot>();
        var first = CreateSnapshot(1);
        var second = CreateSnapshot(2);
        var third = CreateSnapshot(3);
        var orchestrator = new DashboardRefreshOrchestrator(dispatcher, applied.Add);

        orchestrator.SubmitSnapshot(first);
        orchestrator.SubmitSnapshot(second);
        orchestrator.SubmitSnapshot(third);

        Assert.Equal(1, dispatcher.PostCount);

        dispatcher.FlushAll();

        var appliedSnapshot = Assert.Single(applied);
        Assert.Same(third, appliedSnapshot);
    }

    [Fact]
    public void SubmitSnapshot_AppliesReentrantSubmissionInSecondDispatcherPass()
    {
        var dispatcher = new RecordingUiDispatcher();
        var applied = new List<DashboardSnapshot>();
        var first = CreateSnapshot(1);
        var second = CreateSnapshot(2);
        DashboardRefreshOrchestrator orchestrator = null!;
        orchestrator = new DashboardRefreshOrchestrator(
            dispatcher,
            snapshot =>
            {
                applied.Add(snapshot);
                if (applied.Count == 1)
                {
                    orchestrator.SubmitSnapshot(second);
                }
            });

        orchestrator.SubmitSnapshot(first);

        Assert.Equal(1, dispatcher.PostCount);

        dispatcher.FlushOne();

        Assert.Equal([first], applied);
        Assert.Equal(2, dispatcher.PostCount);
        Assert.Equal(1, dispatcher.PendingCount);

        dispatcher.FlushAll();

        Assert.Equal([first, second], applied);
    }

    [Fact]
    public void SubmitSnapshot_IgnoresOlderSnapshotWhenNewerSnapshotIsPending()
    {
        var dispatcher = new RecordingUiDispatcher();
        var applied = new List<DashboardSnapshot>();
        var newer = CreateSnapshot(10);
        var older = CreateSnapshot(9);
        var orchestrator = new DashboardRefreshOrchestrator(dispatcher, applied.Add);

        orchestrator.SubmitSnapshot(newer);
        orchestrator.SubmitSnapshot(older);
        dispatcher.FlushAll();

        var appliedSnapshot = Assert.Single(applied);
        Assert.Same(newer, appliedSnapshot);
    }

    [Fact]
    public void SubmitSnapshot_IgnoresOlderSnapshotAfterNewerSnapshotWasApplied()
    {
        var dispatcher = new RecordingUiDispatcher();
        var applied = new List<DashboardSnapshot>();
        var newer = CreateSnapshot(10);
        var older = CreateSnapshot(9);
        var orchestrator = new DashboardRefreshOrchestrator(dispatcher, applied.Add);

        orchestrator.SubmitSnapshot(newer);
        dispatcher.FlushAll();
        orchestrator.SubmitSnapshot(older);

        Assert.Equal(1, dispatcher.PostCount);
        var appliedSnapshot = Assert.Single(applied);
        Assert.Same(newer, appliedSnapshot);
    }

    [Fact]
    public void DashboardRefreshOrchestrator_DoesNotExposeEventReceivedHandling()
    {
        var publicMembers = typeof(DashboardRefreshOrchestrator)
            .GetMembers()
            .Select(member => member.Name);

        Assert.DoesNotContain(publicMembers, name => name.Contains("EventReceived", StringComparison.Ordinal));
    }

    private static DashboardSnapshot CreateSnapshot(int second)
        => new(
            new RuntimeHealthSnapshot(
                TotalPlcs: 0,
                HealthyCount: 0,
                WarningCount: 0,
                CongestedCount: 0,
                ErrorCount: 0,
                SnapshotTime: new DateTimeOffset(2026, 5, 14, 12, 0, second, TimeSpan.Zero)),
            []);

    private sealed class RecordingUiDispatcher : IUiDispatcher
    {
        private readonly Queue<Action> _actions = new();

        public int PendingCount => _actions.Count;

        public int PostCount { get; private set; }

        public void Post(Action action)
        {
            PostCount++;
            _actions.Enqueue(action);
        }

        public void FlushOne()
        {
            var action = _actions.Dequeue();
            action();
        }

        public void FlushAll()
        {
            while (_actions.Count > 0)
            {
                FlushOne();
            }
        }
    }
}
