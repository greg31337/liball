namespace CurrencyCodes.DataGenerator;

internal sealed class IsoPartialDate
{
    internal IsoPartialDate(bool isKnown, int year, int month, int day, IsoDatePrecision precision)
    {
        IsKnown = isKnown;
        Year = year;
        Month = month;
        Day = day;
        Precision = precision;
    }

    internal bool IsKnown { get; }

    internal int Year { get; }

    internal int Month { get; }

    internal int Day { get; }

    internal IsoDatePrecision Precision { get; }
}
