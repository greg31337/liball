namespace iso20022;

/// <summary>Identifies a normalized ISO 20022 repository data-type family.</summary>
public enum DataTypeKind
{
    /// <summary>The data-type family is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>A finite text value.</summary>
    Text = 1,
    /// <summary>An XML Schema string-derived value.</summary>
    String = 2,
    /// <summary>An identifier-set value.</summary>
    Identifier = 3,
    /// <summary>An XML Schema boolean value.</summary>
    Boolean = 4,
    /// <summary>A boolean indicator with defined true and false meanings.</summary>
    Indicator = 5,
    /// <summary>An XML Schema decimal value.</summary>
    Decimal = 6,
    /// <summary>A monetary amount value.</summary>
    Amount = 7,
    /// <summary>A counted quantity value.</summary>
    Quantity = 8,
    /// <summary>A rate value.</summary>
    Rate = 9,
    /// <summary>An XML Schema binary value.</summary>
    Binary = 10,
    /// <summary>An XML Schema date value.</summary>
    Date = 11,
    /// <summary>An XML Schema date-time value.</summary>
    DateTime = 12,
    /// <summary>An XML Schema Gregorian day value.</summary>
    Day = 13,
    /// <summary>An XML Schema duration value.</summary>
    Duration = 14,
    /// <summary>An XML Schema Gregorian month value.</summary>
    Month = 15,
    /// <summary>An XML Schema Gregorian month-day value.</summary>
    MonthDay = 16,
    /// <summary>An XML Schema time value.</summary>
    Time = 17,
    /// <summary>An XML Schema Gregorian year value.</summary>
    Year = 18,
    /// <summary>An XML Schema Gregorian year-month value.</summary>
    YearMonth = 19,
    /// <summary>A repository code set.</summary>
    CodeSet = 20,
    /// <summary>A named XML Schema built-in type.</summary>
    Schema = 21,
    /// <summary>An external schema content model.</summary>
    ExternalSchema = 22,
    /// <summary>A legacy user-defined external content model.</summary>
    UserDefined = 23
}
