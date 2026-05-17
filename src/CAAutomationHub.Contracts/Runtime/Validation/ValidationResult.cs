namespace CAAutomationHub.Contracts.Runtime.Validation;

public sealed record ValidationResult
{
    public ValidationResult(IReadOnlyList<ValidationIssue>? issues)
    {
        Issues = issues is null
            ? Array.Empty<ValidationIssue>()
            : issues.ToArray();
    }

    public IReadOnlyList<ValidationIssue> Issues { get; }
    public bool IsValid => Issues.Count == 0;
    public bool BlocksExecution => Issues.Any(issue => issue.BlocksExecution);

    public static ValidationResult Empty { get; } = new(Array.Empty<ValidationIssue>());
    public static ValidationResult Success => Empty;
}
