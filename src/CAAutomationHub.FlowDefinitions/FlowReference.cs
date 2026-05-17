namespace CAAutomationHub.FlowDefinitions;

public sealed record FlowReference
{
    public FlowReference(string key, string kind)
    {
        Key = string.IsNullOrWhiteSpace(key)
            ? throw new ArgumentException("Key must not be empty.", nameof(key))
            : key;
        Kind = string.IsNullOrWhiteSpace(kind)
            ? throw new ArgumentException("Kind must not be empty.", nameof(kind))
            : kind;
    }

    public string Key { get; }
    public string Kind { get; }
}
