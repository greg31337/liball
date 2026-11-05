using System;

namespace xlsx;

/// <summary>
/// Represents one typed value in pivot-cache shared items or records.
/// </summary>
public class XlsxPivotCacheValue
{
    /// <summary>Stores the value kind.</summary>
    public XlsxPivotCacheValueKind Kind;
    /// <summary>Stores a string or error value.</summary>
    public string Text;
    /// <summary>Stores a numeric value.</summary>
    public double Number;
    /// <summary>Stores a Boolean value.</summary>
    public bool Boolean;
    /// <summary>Stores a date-time value.</summary>
    public System.DateTime DateTime;
    /// <summary>Stores a shared-item index.</summary>
    public int SharedItemIndex;
    /// <summary>Indicates whether the shared-item index is present.</summary>
    public bool HasSharedItemIndex;
    /// <summary>Indicates whether the item is unused.</summary>
    public bool Unused;
    /// <summary>Indicates whether the unused flag is present.</summary>
    public bool HasUnused;

    /// <summary>Initializes an empty pivot-cache value.</summary>
    public XlsxPivotCacheValue()
    {
        Kind = XlsxPivotCacheValueKind.Missing;
        Text = "";
        Number = 0;
        Boolean = false;
        DateTime = default(System.DateTime);
        SharedItemIndex = 0;
        HasSharedItemIndex = false;
        Unused = false;
        HasUnused = false;
    }
}
