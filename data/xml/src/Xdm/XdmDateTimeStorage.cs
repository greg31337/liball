using System.Numerics;

namespace xml;

internal class XdmDateTimeStorage
{
    internal BigInteger Year;
    internal int Month;
    internal int Day;
    internal int Hour;
    internal int Minute;
    internal BigInteger SecondCoefficient;
    internal int SecondScale;
    internal bool HasTimezone;
    internal int TimezoneMinutes;

    internal XdmDateTimeStorage()
    {
        Year = BigInteger.Zero;
        Month = 1;
        Day = 1;
    }
}
