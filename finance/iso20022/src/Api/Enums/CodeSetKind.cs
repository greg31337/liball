namespace iso20022;

/// <summary>Identifies the source and derivation role of a code set.</summary>
public enum CodeSetKind
{
    /// <summary>The code-set kind is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>A code set defined within the selected ISO 20022 repository.</summary>
    Internal = 1,
    /// <summary>A code set maintained independently of the repository edition.</summary>
    External = 2,
    /// <summary>A code set derived from another repository code set.</summary>
    Derived = 3
}
