using System;

namespace xlsx;

/// <summary>
/// Represents a calculated item stored in a pivot cache.
/// </summary>
public class XlsxPivotCacheCalculatedItem
{
    /// <summary>Stores the owning cache field index.</summary>
    public int FieldIndex;
    /// <summary>Indicates whether the field index is present.</summary>
    public bool HasFieldIndex;
    /// <summary>Stores the calculated item name.</summary>
    public string Name;
    /// <summary>Stores the calculated item formula.</summary>
    public string Formula;

    /// <summary>Initializes an empty calculated cache item.</summary>
    public XlsxPivotCacheCalculatedItem()
    {
        FieldIndex = 0;
        HasFieldIndex = false;
        Name = "";
        Formula = "";
    }
}
