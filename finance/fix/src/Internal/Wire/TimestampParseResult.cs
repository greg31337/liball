namespace fix;

internal class TimestampParseResult
{
    internal bool Success;
    internal FixFieldType FieldType;
    internal int Year;
    internal int Month;
    internal int Day;
    internal int Week;
    internal int Hour;
    internal int Minute;
    internal int Second;
    internal bool HasDay;
    internal bool HasWeek;
    internal bool HasSecond;
    internal bool HasFraction;
    internal int FractionalDigits;
    internal string Fraction = "";
    internal bool HasOffset;
    internal bool IsUtcZulu;
    internal int OffsetSign;
    internal int OffsetHour;
    internal int OffsetMinute;
    internal bool HasOffsetMinute;
    internal int Consumed;
    internal int Required;
    internal int ErrorIndex = -1;
    internal string ErrorCode = "";
}
