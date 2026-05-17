namespace CAAutomationHub.FlowDefinitions;

public sealed record FlowStepCandidate
{
    public FlowStepCandidate(
        string stepId,
        string actionKind,
        string? onSuccess,
        string? onFailure,
        IReadOnlyList<string>? requiredBindingRefs = null,
        IReadOnlyList<string>? requiredPolicyRefs = null)
    {
        StepId = string.IsNullOrWhiteSpace(stepId)
            ? throw new ArgumentException("Step id must not be empty.", nameof(stepId))
            : stepId;
        ActionKind = string.IsNullOrWhiteSpace(actionKind)
            ? throw new ArgumentException("Action kind must not be empty.", nameof(actionKind))
            : actionKind;
        OnSuccess = onSuccess;
        OnFailure = onFailure;
        RequiredBindingRefs = requiredBindingRefs?.ToArray();
        RequiredPolicyRefs = requiredPolicyRefs?.ToArray();
    }

    public string StepId { get; }
    public string ActionKind { get; }
    public string? OnSuccess { get; }
    public string? OnFailure { get; }
    public IReadOnlyList<string>? RequiredBindingRefs { get; }
    public IReadOnlyList<string>? RequiredPolicyRefs { get; }
}
