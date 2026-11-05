namespace xml;

internal class XdmDurationParserState
{
    internal string Source;
    internal int Index;
    internal bool Negative;
    internal bool InTime;
    internal bool HasComponent;
    internal bool HasTimeComponent;
    internal bool HasYear;
    internal bool HasDateMonth;
    internal bool HasDay;
    internal bool HasHour;
    internal bool HasTimeMinute;
    internal bool HasSecond;
    internal int LastDateOrder;
    internal int LastTimeOrder;
    internal int DigitCount;
    internal string? Years;
    internal string? DateMonths;
    internal string? Days;
    internal string? Hours;
    internal string? TimeMinutes;
    internal string? Seconds;

    internal XdmDurationParserState(string source)
    {
        Source = source;
        Index = 0;
    }
}
