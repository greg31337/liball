namespace iso20022;

/// <summary>Specifies an internal or external code lookup and edition-selection policy.</summary>
public class CodeLookupRequest
{
    /// <summary>The required code-set source kind, which must be internal or external.</summary>
    public CodeSetKind Kind;
    /// <summary>The stable identifier or exact name of the requested code set.</summary>
    public string CodeSetIdentifier = string.Empty;
    /// <summary>The exact abbreviated code value to locate.</summary>
    public string Value = string.Empty;
    /// <summary>The pinned artifact edition, or an empty value when selecting the latest edition.</summary>
    public string Edition = string.Empty;
    /// <summary>Whether the latest available edition is selected instead of a pinned edition.</summary>
    public bool SelectLatestEdition;
    /// <summary>The date on which the code must be active when present.</summary>
    public IsoDateValue? ActiveOn;
    /// <summary>Whether inactive or obsolete code values may match.</summary>
    public bool IncludeInactive;
}
