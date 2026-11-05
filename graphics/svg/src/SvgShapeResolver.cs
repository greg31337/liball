namespace svg;

internal class SvgShapeLengthValue
{
    public bool Success;
    public bool Auto;
    public double Value;
    public SvgSourceSpan? Source;

    public SvgShapeLengthValue(bool success, bool auto, double value, SvgSourceSpan? source)
    {
        Success = success;
        Auto = auto;
        Value = value;
        Source = source;
    }
}
