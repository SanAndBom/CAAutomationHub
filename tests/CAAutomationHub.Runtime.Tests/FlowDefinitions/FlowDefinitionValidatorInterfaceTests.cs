using System.Reflection;
using CAAutomationHub.Contracts.Runtime.Validation;
using CAAutomationHub.FlowDefinitions;

namespace CAAutomationHub.Runtime.Tests.FlowDefinitions;

public sealed class FlowDefinitionValidatorInterfaceTests
{
    [Fact]
    public void ValidatorInterface_ExposesAsyncCandidateValidationContract()
    {
        var method = typeof(IFlowDefinitionValidator).GetMethod(
            "ValidateAsync",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(method);
        Assert.Equal(typeof(ValueTask<ValidationResult>), method.ReturnType);

        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(FlowDefinitionCandidate), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public async Task FakeValidator_CanReturnSuccessResult()
    {
        IFlowDefinitionValidator validator = new FakeValidator(_ => ValidationResult.Success);

        var result = await validator.ValidateAsync(CreateCandidate());

        Assert.True(result.IsValid);
        Assert.False(result.BlocksExecution);
    }

    [Fact]
    public async Task FakeValidator_CanReturnResultWithIssue()
    {
        var issue = new ValidationIssue(
            ruleId: "Structural.MissingInitialState",
            category: ValidationCategory.Structural,
            severity: ValidationSeverity.Error,
            blocksExecution: true,
            message: "Initial state is missing.");

        IFlowDefinitionValidator validator = new FakeValidator(_ => new ValidationResult([issue]));

        var result = await validator.ValidateAsync(CreateCandidate());

        Assert.False(result.IsValid);
        Assert.True(result.BlocksExecution);
        Assert.Same(issue, Assert.Single(result.Issues));
    }

    private static FlowDefinitionCandidate CreateCandidate()
    {
        return new FlowDefinitionCandidate(
            flowId: "work-start",
            flowKind: "Pilot",
            initialState: "read-ready-signal",
            steps:
            [
                new FlowStepCandidate(
                    stepId: "read-ready-signal",
                    actionKind: "ReadSignal",
                    onSuccess: null,
                    onFailure: null)
            ]);
    }

    private sealed class FakeValidator(Func<FlowDefinitionCandidate, ValidationResult> validate)
        : IFlowDefinitionValidator
    {
        public ValueTask<ValidationResult> ValidateAsync(
            FlowDefinitionCandidate candidate,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(validate(candidate));
        }
    }
}
