namespace uspto;

/// <summary>
/// Identifies the normalized document or literature category of a citation.
/// </summary>
public enum CitationDocumentCategory
{
    /// <summary>The citation category is not known.</summary>
    Unknown = 0,

    /// <summary>The citation refers to a United States granted patent.</summary>
    UnitedStatesPatent = 1,

    /// <summary>The citation refers to a United States pre-grant publication.</summary>
    UnitedStatesApplication = 2,

    /// <summary>The citation refers to a foreign patent document.</summary>
    ForeignPatent = 3,

    /// <summary>The citation refers to non-patent literature.</summary>
    NonPatentLiterature = 4,

    /// <summary>The citation is another kind of reference.</summary>
    Other = 5
}
