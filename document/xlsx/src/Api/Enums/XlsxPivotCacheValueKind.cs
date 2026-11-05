using System;

namespace xlsx;

/// <summary>
/// Lists the value kinds stored in pivot-cache shared items and records.
/// </summary>
public enum XlsxPivotCacheValueKind
{
    /// <summary>Selects a missing value.</summary>
    Missing = 0,
    /// <summary>Selects a string value.</summary>
    String = 1,
    /// <summary>Selects a numeric value.</summary>
    Number = 2,
    /// <summary>Selects a Boolean value.</summary>
    Boolean = 3,
    /// <summary>Selects a date-time value.</summary>
    DateTime = 4,
    /// <summary>Selects an Excel error value.</summary>
    Error = 5,
    /// <summary>Selects an index into a field's shared items.</summary>
    SharedItemIndex = 6
}
