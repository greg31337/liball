namespace uri;

/// <summary>
/// Identifies the equivalence level used to compare URNs.
/// </summary>
public enum UrnEquivalenceLevel
{
    /// <summary>Compare complete URN text including optional components exactly.</summary>
    Exact = 0,

    /// <summary>Compare assigned names using generic RFC 8141 equivalence.</summary>
    AssignedName = 1,

    /// <summary>Compare namespace-specific strings using an application-supplied handler.</summary>
    NamespaceSpecific = 2
}
