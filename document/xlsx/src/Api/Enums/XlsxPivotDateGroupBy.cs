using System;

namespace xlsx;

/// <summary>
/// Lists supported pivot-cache date grouping units.
/// </summary>
public enum XlsxPivotDateGroupBy
{
    /// <summary>Selects seconds.</summary>
    Seconds = 0,
    /// <summary>Selects minutes.</summary>
    Minutes = 1,
    /// <summary>Selects hours.</summary>
    Hours = 2,
    /// <summary>Selects days.</summary>
    Days = 3,
    /// <summary>Selects months.</summary>
    Months = 4,
    /// <summary>Selects quarters.</summary>
    Quarters = 5,
    /// <summary>Selects years.</summary>
    Years = 6
}
