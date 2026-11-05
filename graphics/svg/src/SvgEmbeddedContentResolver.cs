namespace svg;

internal class SvgEmbeddedLengthValue
{
    public bool Success;
    public bool Auto;
    public double Value;
    public SvgSourceSpan? Source;

    public SvgEmbeddedLengthValue(bool success, bool auto, double value, SvgSourceSpan? source)
    {
        Success = success;
        Auto = auto;
        Value = value;
        Source = source;
    }
}

internal class SvgEmbeddedUsedSize
{
    public double Width;
    public double Height;
    public double SourceWidth;
    public double SourceHeight;
    public bool HasAspectRatio;
    public double AspectRatio;
    public bool UsedDefault;

    public SvgEmbeddedUsedSize()
    {
        Width = 0;
        Height = 0;
        SourceWidth = 0;
        SourceHeight = 0;
        HasAspectRatio = false;
        AspectRatio = 0;
        UsedDefault = false;
    }
}
