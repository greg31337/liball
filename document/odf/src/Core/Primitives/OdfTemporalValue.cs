namespace odf;

internal class OdfTemporalValue
{
    internal string LexicalValue = "";
    internal global::System.Numerics.BigInteger Year;
    internal int Month;
    internal int Day;
    internal int Hour;
    internal int Minute;
    internal int Second;
    internal string FractionalSecond = "";
    internal int TimeZoneMinutes;
    internal bool HasDate;
    internal bool HasTime;
    internal bool HasTimeZone;
    internal bool IsValid;
}

internal class OdfDurationValue
{
    internal string LexicalValue = "";
    internal global::System.Numerics.BigInteger Years;
    internal global::System.Numerics.BigInteger Months;
    internal global::System.Numerics.BigInteger Days;
    internal global::System.Numerics.BigInteger Hours;
    internal global::System.Numerics.BigInteger Minutes;
    internal global::System.Numerics.BigInteger Seconds;
    internal string FractionalSecond = "";
    internal bool IsNegative;
    internal bool IsValid;
}
