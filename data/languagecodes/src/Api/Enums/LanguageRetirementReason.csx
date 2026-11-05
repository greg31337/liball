namespace languagecodes;

/// <summary>Identifies the authoritative reason an ISO 639 Set 3 code was retired.</summary>
public enum LanguageRetirementReason
{
    /// <summary>The identifier changed.</summary>
    Change = 0,
    /// <summary>The identifier duplicated another identifier.</summary>
    Duplicate = 1,
    /// <summary>The identified language was determined not to exist.</summary>
    Nonexistent = 2,
    /// <summary>The identifier was split into multiple identifiers.</summary>
    Split = 3,
    /// <summary>The identifier was merged into another identifier.</summary>
    Merge = 4
}
