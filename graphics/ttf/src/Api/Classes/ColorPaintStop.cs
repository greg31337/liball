namespace ttf;

/// <summary>Represents one stop in a normalized COLR gradient.</summary>
public class ColorPaintStop : IColorPaintStop
{
    double offset;
    ColorPaint color;

    internal ColorPaintStop(double offset, ColorPaint color)
    {
        this.offset = offset;
        this.color = color;
    }

    /// <summary>Gets the normalized stop offset.</summary>
    /// <returns>The stop offset.</returns>
    public double GetOffset() { return offset; }

    /// <summary>Gets the resolved color paint for this stop.</summary>
    /// <returns>The palette or foreground color paint.</returns>
    public ColorPaint GetColor() { return color; }
}
