namespace fix;

/// <summary>Identifies the purpose of an Orchestra rule.</summary>
public enum OrchestraRuleKind
{
    /// <summary>The rule kind is not known.</summary>
    Unknown = 0,
    /// <summary>The rule controls member presence.</summary>
    Presence = 1,
    /// <summary>The rule evaluates a condition.</summary>
    Conditional = 2,
    /// <summary>The rule assigns a value.</summary>
    Assignment = 3
}
