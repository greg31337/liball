using System;

namespace xlsx;

/// <summary>
/// Lists supported pivot data-field summary functions.
/// </summary>
public enum XlsxPivotSummaryFunction
{
    /// <summary>Selects sum.</summary>
    Sum = 0,
    /// <summary>Selects count.</summary>
    Count = 1,
    /// <summary>Selects average.</summary>
    Average = 2,
    /// <summary>Selects maximum.</summary>
    Maximum = 3,
    /// <summary>Selects minimum.</summary>
    Minimum = 4,
    /// <summary>Selects product.</summary>
    Product = 5,
    /// <summary>Selects numeric count.</summary>
    CountNumbers = 6,
    /// <summary>Selects sample standard deviation.</summary>
    StandardDeviation = 7,
    /// <summary>Selects population standard deviation.</summary>
    PopulationStandardDeviation = 8,
    /// <summary>Selects sample variance.</summary>
    Variance = 9,
    /// <summary>Selects population variance.</summary>
    PopulationVariance = 10
}
