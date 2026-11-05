using System;

namespace currencycodes;

/// <summary>
/// Stores a territory and currency usage association together with its known interval and provenance.
/// </summary>
public class CurrencyUsage
{
    /// <summary>
    /// Stores the stable territory key used by catalog operations.
    /// </summary>
    public string TerritoryKey = string.Empty;

    /// <summary>
    /// Stores the canonical alphabetic currency code.
    /// </summary>
    public string CurrencyCode = string.Empty;

    /// <summary>
    /// Stores the usage start date with its original source precision.
    /// </summary>
    public PartialDate SourceStartDate = new PartialDate();

    /// <summary>
    /// Stores the usage end date with its original source precision.
    /// </summary>
    public PartialDate SourceEndDate = new PartialDate();

    /// <summary>
    /// Indicates whether an inclusive normalized start date is known.
    /// </summary>
    public bool HasInclusiveStartDate = false;

    /// <summary>
    /// Stores the inclusive normalized start date when <see cref="HasInclusiveStartDate"/> is <c>true</c>.
    /// </summary>
    public DateOnly InclusiveStartDate = default(DateOnly);

    /// <summary>
    /// Indicates whether an inclusive normalized end date is known.
    /// </summary>
    public bool HasInclusiveEndDate = false;

    /// <summary>
    /// Stores the inclusive normalized end date when <see cref="HasInclusiveEndDate"/> is <c>true</c>.
    /// </summary>
    public DateOnly InclusiveEndDate = default(DateOnly);

    /// <summary>
    /// Indicates whether the currency usage is legal tender.
    /// </summary>
    public bool IsTender = false;

    /// <summary>
    /// Indicates whether the usage association is current in the pinned data snapshot.
    /// </summary>
    public bool IsCurrent = false;

    /// <summary>
    /// Indicates whether ISO List One supplies the usage association.
    /// </summary>
    public bool FromIsoListOne = false;

    /// <summary>
    /// Indicates whether ISO List Three supplies the usage association.
    /// </summary>
    public bool FromIsoListThree = false;

    /// <summary>
    /// Indicates whether CLDR supplies the usage association or interval metadata.
    /// </summary>
    public bool FromCldr = false;
}
