using CAAutomationHub.Contracts.Runtime.Validation;

namespace CAAutomationHub.FlowDefinitions;

public interface IFlowDefinitionValidator
{
    ValueTask<ValidationResult> ValidateAsync(
        FlowDefinitionCandidate candidate,
        CancellationToken cancellationToken = default);
}
