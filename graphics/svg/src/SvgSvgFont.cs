namespace svg;

internal class SvgSvgNumberValue
{
    internal bool Success;
    internal bool Found;
    internal double Value;
    internal SvgAttribute? Attribute;

    internal SvgSvgNumberValue()
    {
        Success = true;
        Found = false;
        Value = 0;
        Attribute = null;
    }
}

internal class SvgSvgRangeValue
{
    internal bool Success;
    internal int Start;
    internal int End;

    internal SvgSvgRangeValue()
    {
        Success = false;
        Start = 0;
        End = 0;
    }
}

internal class SvgSvgHexValue
{
    internal bool Success;
    internal int Value;

    internal SvgSvgHexValue()
    {
        Success = false;
        Value = 0;
    }
}
