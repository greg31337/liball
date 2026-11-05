using System;

namespace xlsx;

/// <summary>
/// Lists supported typed label and value filters for pivot tables.
/// </summary>
public enum XlsxPivotFilterKind
{
    /// <summary>Selects label equality.</summary>
    LabelEquals = 0,
    /// <summary>Selects label inequality.</summary>
    LabelDoesNotEqual = 1,
    /// <summary>Selects label begins-with matching.</summary>
    LabelBeginsWith = 2,
    /// <summary>Selects label ends-with matching.</summary>
    LabelEndsWith = 3,
    /// <summary>Selects label contains matching.</summary>
    LabelContains = 4,
    /// <summary>Selects label greater-than comparison.</summary>
    LabelGreaterThan = 5,
    /// <summary>Selects label less-than comparison.</summary>
    LabelLessThan = 6,
    /// <summary>Selects value equality.</summary>
    ValueEquals = 7,
    /// <summary>Selects value inequality.</summary>
    ValueDoesNotEqual = 8,
    /// <summary>Selects value greater-than comparison.</summary>
    ValueGreaterThan = 9,
    /// <summary>Selects value less-than comparison.</summary>
    ValueLessThan = 10,
    /// <summary>Selects value greater-than-or-equal comparison.</summary>
    ValueGreaterThanOrEqual = 11,
    /// <summary>Selects value less-than-or-equal comparison.</summary>
    ValueLessThanOrEqual = 12
}
