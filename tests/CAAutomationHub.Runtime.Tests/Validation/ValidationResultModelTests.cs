using CAAutomationHub.Contracts.Runtime.Validation;

namespace CAAutomationHub.Runtime.Tests.Validation;

public sealed class ValidationResultModelTests
{
    [Fact]
    public void EmptyResult_IsValidAndDoesNotBlockExecution()
    {
        var result = ValidationResult.Empty;

        Assert.True(result.IsValid);
        Assert.False(result.BlocksExecution);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void ResultWithAnyIssue_IsNotValid()
    {
        var issue = new ValidationIssue(
            ruleId: "Structural.MissingFlowId",
            category: ValidationCategory.Structural,
            severity: ValidationSeverity.Info,
            blocksExecution: false,
            message: "Flow id is missing.");

        var result = new ValidationResult([issue]);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ErrorSeverity_DoesNotAutomaticallyBlockExecution()
    {
        var issue = new ValidationIssue(
            ruleId: "Binding.MissingSignalBinding",
            category: ValidationCategory.Binding,
            severity: ValidationSeverity.Error,
            blocksExecution: false,
            message: "Signal binding is missing.");

        var result = new ValidationResult([issue]);

        Assert.False(result.BlocksExecution);
    }

    [Fact]
    public void AnyBlockingIssue_MakesResultBlockExecution()
    {
        var nonBlockingIssue = new ValidationIssue(
            ruleId: "Binding.UnusedBinding",
            category: ValidationCategory.Binding,
            severity: ValidationSeverity.Warning,
            blocksExecution: false,
            message: "Binding is unused.");

        var blockingIssue = new ValidationIssue(
            ruleId: "Policy.PolicyStatusDraftBlocksExecution",
            category: ValidationCategory.Policy,
            severity: ValidationSeverity.ReviewRequired,
            blocksExecution: true,
            message: "Policy status requires review.");

        var result = new ValidationResult([nonBlockingIssue, blockingIssue]);

        Assert.True(result.BlocksExecution);
    }

    [Fact]
    public void RuleId_RemainsStringCode()
    {
        var issue = new ValidationIssue(
            ruleId: "Extension.CustomRule",
            category: ValidationCategory.Extension,
            severity: ValidationSeverity.Warning,
            blocksExecution: false,
            message: "Extension rule reported a warning.");

        Assert.IsType<string>(issue.RuleId);
        Assert.Equal("Extension.CustomRule", issue.RuleId);
    }

    [Fact]
    public void ReviewRequiredSeverity_CanBeRepresented()
    {
        var issue = new ValidationIssue(
            ruleId: "Policy.MissingRecoveryPolicy",
            category: ValidationCategory.Policy,
            severity: ValidationSeverity.ReviewRequired,
            blocksExecution: true,
            message: "Recovery policy requires review.");

        Assert.Equal(ValidationSeverity.ReviewRequired, issue.Severity);
    }

    [Fact]
    public void OptionalTargetPathEvidenceAndMetadata_CanBeRepresented()
    {
        var metadata = new Dictionary<string, string>
        {
            ["policyStatus"] = "Draft",
            ["source"] = "AH-RUNTIME-52"
        };

        var issue = new ValidationIssue(
            ruleId: "Policy.PolicyStatusDraftBlocksExecution",
            category: ValidationCategory.Policy,
            severity: ValidationSeverity.ReviewRequired,
            blocksExecution: true,
            message: "Policy status requires review.",
            targetPath: "flow.metadata.policyStatus",
            evidence: "policyStatus=Draft",
            metadata: metadata);

        Assert.Equal("flow.metadata.policyStatus", issue.TargetPath);
        Assert.Equal("policyStatus=Draft", issue.Evidence);
        Assert.Equal("Draft", issue.Metadata?["policyStatus"]);
        Assert.Equal("AH-RUNTIME-52", issue.Metadata?["source"]);
    }
}
