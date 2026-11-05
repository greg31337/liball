namespace languagecodes;

/// <summary>Identifies the assignment and lifecycle status of a language code.</summary>
public enum LanguageCodeStatus
{
    /// <summary>The status is not known.</summary>
    Unknown = 0,
    /// <summary>The code is actively assigned.</summary>
    Active = 1,
    /// <summary>The code has been retired.</summary>
    Retired = 2,
    /// <summary>The code is deprecated by its registry.</summary>
    Deprecated = 3,
    /// <summary>The code is reserved and not assigned to an entity.</summary>
    Reserved = 4,
    /// <summary>The code is reserved for private use.</summary>
    PrivateUse = 5,
    /// <summary>The code is well formed but unassigned.</summary>
    Unassigned = 6
}
