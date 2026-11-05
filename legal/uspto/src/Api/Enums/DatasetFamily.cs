namespace uspto;

/// <summary>
/// Identifies a bulk-data product family without assuming a particular catalog product is available.
/// </summary>
public enum DatasetFamily
{
    /// <summary>No dataset family has been selected.</summary>
    Unknown = 0,

    /// <summary>Complete Patent File Wrapper data.</summary>
    PatentFileWrapperComplete = 1,

    /// <summary>Incremental Patent File Wrapper data.</summary>
    PatentFileWrapperDelta = 2,

    /// <summary>Published patent-grant full text.</summary>
    PatentGrantFullText = 3,

    /// <summary>Published patent-application full text.</summary>
    PatentApplicationFullText = 4,

    /// <summary>Patent assignment records.</summary>
    PatentAssignment = 5,

    /// <summary>Office Action weekly archives.</summary>
    OfficeAction = 6,

    /// <summary>Patent Trial and Appeal Board records.</summary>
    Ptab = 7,

    /// <summary>Trademark application and status records.</summary>
    TrademarkApplicationAndStatus = 8,

    /// <summary>Trademark assignment records.</summary>
    TrademarkAssignment = 9,

    /// <summary>Trademark Trial and Appeal Board records.</summary>
    Ttab = 10,

    /// <summary>PatentsView patent-grant tables.</summary>
    PatentsViewGrant = 11,

    /// <summary>PatentsView pre-grant publication tables.</summary>
    PatentsViewPreGrant = 12,

    /// <summary>PatentsView annualized tables.</summary>
    PatentsViewAnnualized = 13,

    /// <summary>PatentsView sorted tables.</summary>
    PatentsViewSorted = 14
}
