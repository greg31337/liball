namespace languagecodes;

/// <summary>Identifies the outcome of a language lookup.</summary>
public enum LanguageLookupStatus
{
    /// <summary>No matching entity was found.</summary>
    NotFound = 0,
    /// <summary>Exactly one matching entity was found.</summary>
    Found = 1,
    /// <summary>More than one matching entity was found.</summary>
    Ambiguous = 2,
    /// <summary>The value belongs to a reserved range.</summary>
    Reserved = 3,
    /// <summary>The value identifies a retired code.</summary>
    Retired = 4,
    /// <summary>The lookup input is not a valid value.</summary>
    Invalid = 5
}
