namespace uspto;

/// <summary>
/// Provides stable product identifiers published by the USPTO bulk-data catalog.
/// </summary>
public static class UsptoDatasetIdentifiers
{
    /// <summary>The patent application full-text XML product.</summary>
    public const string PatentApplicationFullTextXml = "APPXML";

    /// <summary>The patent grant full-text XML product.</summary>
    public const string PatentGrantFullTextXml = "PTGRXML";

    /// <summary>The preprocessed weekly Patent File Wrapper product.</summary>
    public const string PatentFileWrapperWeekly = "PTFWPRE";

    /// <summary>The final petition-decision product.</summary>
    public const string PetitionDecisions = "PETDEC";

    /// <summary>The Office Action weekly archive product.</summary>
    public const string OfficeActionWeekly = "OACT";

    /// <summary>The Trademark Trial and Appeal Board bulk product.</summary>
    public const string TrademarkTrialAndAppealBoard = "TTAB";
}
