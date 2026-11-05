namespace iso20022;

/// <summary>Identifies which Gregorian components are present in a partial date.</summary>
public enum PartialDateKind
{
    /// <summary>No partial-date form has been identified.</summary>
    Unknown = 0,
    /// <summary>A year value.</summary>
    Year = 1,
    /// <summary>A month value.</summary>
    Month = 2,
    /// <summary>A day value.</summary>
    Day = 3,
    /// <summary>A year and month value.</summary>
    YearMonth = 4,
    /// <summary>A month and day value.</summary>
    MonthDay = 5
}
