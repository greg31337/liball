using System.Numerics;

namespace xml;

internal class XdmCalendarDate
{
    internal BigInteger Year;
    internal int Month;
    internal int Day;

    internal XdmCalendarDate(BigInteger year, int month, int day)
    {
        Year = year;
        Month = month;
        Day = day;
    }
}
