using System.Collections.ObjectModel;

namespace CAAutomationHub.Contracts.Runtime.Validation;

public sealed record ValidationIssue
{
    public ValidationIssue(
        string ruleId,
        ValidationCategory category,
        ValidationSeverity severity,
        bool blocksExecution,
        string message,
        string? targetPath = null,
        string? evidence = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        RuleId = string.IsNullOrWhiteSpace(ruleId)
            ? throw new ArgumentException("Rule id must not be empty.", nameof(ruleId))
            : ruleId;
        Category = category;
        Severity = severity;
        BlocksExecution = blocksExecution;
        Message = string.IsNullOrWhiteSpace(message)
            ? throw new ArgumentException("Message must not be empty.", nameof(message))
            : message;
        TargetPath = targetPath;
        Evidence = evidence;
        Metadata = metadata is null
            ? null
            : new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(metadata));
    }

    public string RuleId { get; }
    public ValidationCategory Category { get; }
    public ValidationSeverity Severity { get; }
    public bool BlocksExecution { get; }
    public string Message { get; }
    public string? TargetPath { get; }
    public string? Evidence { get; }
    public IReadOnlyDictionary<string, string>? Metadata { get; }
}
