using System.Collections.ObjectModel;

namespace CAAutomationHub.FlowDefinitions;

public sealed record FlowDefinitionCandidate
{
    public FlowDefinitionCandidate(
        string flowId,
        string flowKind,
        string initialState,
        IReadOnlyList<FlowStepCandidate> steps,
        IReadOnlyDictionary<string, FlowReference>? bindings = null,
        IReadOnlyDictionary<string, FlowPolicyReference>? policies = null)
    {
        FlowId = string.IsNullOrWhiteSpace(flowId)
            ? throw new ArgumentException("Flow id must not be empty.", nameof(flowId))
            : flowId;
        FlowKind = string.IsNullOrWhiteSpace(flowKind)
            ? throw new ArgumentException("Flow kind must not be empty.", nameof(flowKind))
            : flowKind;
        InitialState = string.IsNullOrWhiteSpace(initialState)
            ? throw new ArgumentException("Initial state must not be empty.", nameof(initialState))
            : initialState;
        Steps = steps?.ToArray()
            ?? throw new ArgumentNullException(nameof(steps));
        Bindings = bindings is null
            ? null
            : new ReadOnlyDictionary<string, FlowReference>(
                new Dictionary<string, FlowReference>(bindings));
        Policies = policies is null
            ? null
            : new ReadOnlyDictionary<string, FlowPolicyReference>(
                new Dictionary<string, FlowPolicyReference>(policies));
    }

    public string FlowId { get; }
    public string FlowKind { get; }
    public string InitialState { get; }
    public IReadOnlyList<FlowStepCandidate> Steps { get; }
    public IReadOnlyDictionary<string, FlowReference>? Bindings { get; }
    public IReadOnlyDictionary<string, FlowPolicyReference>? Policies { get; }
}
