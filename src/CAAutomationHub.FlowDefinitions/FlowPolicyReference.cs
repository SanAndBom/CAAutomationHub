namespace CAAutomationHub.FlowDefinitions;

public sealed record FlowPolicyReference
{
    public FlowPolicyReference(string key, string kind, string? status = null)
    {
        Key = string.IsNullOrWhiteSpace(key)
            ? throw new ArgumentException("Key must not be empty.", nameof(key))
            : key;
        Kind = string.IsNullOrWhiteSpace(kind)
            ? throw new ArgumentException("Kind must not be empty.", nameof(kind))
            : kind;
        Status = status;
    }

    public string Key { get; }
    public string Kind { get; }
    public string? Status { get; }
}
