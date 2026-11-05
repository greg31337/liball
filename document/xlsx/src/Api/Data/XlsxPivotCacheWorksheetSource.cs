using System;

namespace xlsx;

/// <summary>
/// Represents a worksheet range, defined name, or table used as a pivot-cache source.
/// </summary>
public class XlsxPivotCacheWorksheetSource
{
    /// <summary>Stores the source range reference.</summary>
    public string Reference;
    /// <summary>Stores the source defined name or table name.</summary>
    public string Name;
    /// <summary>Stores the source worksheet name.</summary>
    public string Sheet;
    /// <summary>Stores the external worksheet relationship identifier.</summary>
    public string RelationshipId;

    /// <summary>Initializes an empty worksheet pivot-cache source.</summary>
    public XlsxPivotCacheWorksheetSource()
    {
        Reference = "";
        Name = "";
        Sheet = "";
        RelationshipId = "";
    }
}
