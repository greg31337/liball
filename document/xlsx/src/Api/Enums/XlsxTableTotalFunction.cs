using System;

namespace xlsx;

/// <summary>
/// Lists the built-in total functions supported by worksheet tables.
/// </summary>
public enum XlsxTableTotalFunction
{
    /// <summary>
    /// Selects no total function.
    /// </summary>
    None = 0,
    /// <summary>
    /// Selects the average total function.
    /// </summary>
    Average = 1,
    /// <summary>
    /// Selects the nonblank-value count total function.
    /// </summary>
    Count = 2,
    /// <summary>
    /// Selects the numeric-value count total function.
    /// </summary>
    CountNumbers = 3,
    /// <summary>
    /// Selects the maximum total function.
    /// </summary>
    Maximum = 4,
    /// <summary>
    /// Selects the minimum total function.
    /// </summary>
    Minimum = 5,
    /// <summary>
    /// Selects the sum total function.
    /// </summary>
    Sum = 6,
    /// <summary>
    /// Selects the sample standard-deviation total function.
    /// </summary>
    StandardDeviation = 7,
    /// <summary>
    /// Selects the sample variance total function.
    /// </summary>
    Variance = 8,
    /// <summary>
    /// Selects a custom total formula.
    /// </summary>
    Custom = 9
}
