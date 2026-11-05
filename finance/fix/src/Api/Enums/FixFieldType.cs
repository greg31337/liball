namespace fix;

/// <summary>Identifies a FIX field's lexical datatype.</summary>
public enum FixFieldType
{
    /// <summary>The datatype is not known.</summary>
    Unknown = 0,
    /// <summary>A signed whole number.</summary>
    Int = 1,
    /// <summary>A message sequence number.</summary>
    SeqNum = 2,
    /// <summary>A repeating-group entry count.</summary>
    NumInGroup = 3,
    /// <summary>A byte length.</summary>
    Length = 4,
    /// <summary>A FIX tag number.</summary>
    TagNum = 5,
    /// <summary>A decimal number.</summary>
    Float = 6,
    /// <summary>A quantity.</summary>
    Qty = 7,
    /// <summary>A price.</summary>
    Price = 8,
    /// <summary>A price offset.</summary>
    PriceOffset = 9,
    /// <summary>A monetary amount.</summary>
    Amt = 10,
    /// <summary>A percentage.</summary>
    Percentage = 11,
    /// <summary>A single character.</summary>
    Char = 12,
    /// <summary>A FIX Boolean value.</summary>
    Boolean = 13,
    /// <summary>A character string.</summary>
    String = 14,
    /// <summary>A space-separated set of character values.</summary>
    MultipleCharValue = 15,
    /// <summary>A space-separated set of string values.</summary>
    MultipleStringValue = 16,
    /// <summary>An ISO currency code.</summary>
    Currency = 17,
    /// <summary>An ISO country code.</summary>
    Country = 18,
    /// <summary>An exchange or market identifier.</summary>
    Exchange = 19,
    /// <summary>A year and month with an optional day or week.</summary>
    MonthYear = 20,
    /// <summary>A day within a month.</summary>
    DayOfMonth = 21,
    /// <summary>A local market date.</summary>
    LocalMktDate = 22,
    /// <summary>A UTC calendar date.</summary>
    UtcDateOnly = 23,
    /// <summary>A UTC time of day.</summary>
    UtcTimeOnly = 24,
    /// <summary>A UTC date and time.</summary>
    UtcTimestamp = 25,
    /// <summary>A time of day with a timezone offset.</summary>
    TzTimeOnly = 26,
    /// <summary>A date and time with a timezone offset.</summary>
    TzTimestamp = 27,
    /// <summary>An arbitrary byte sequence.</summary>
    Data = 28,
    /// <summary>An XML byte sequence.</summary>
    XmlData = 29
}
