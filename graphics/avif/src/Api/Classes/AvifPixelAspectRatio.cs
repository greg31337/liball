namespace avif;

/// <summary>
/// Describes the horizontal-to-vertical spacing ratio of image pixels.
/// </summary>
public class AvifPixelAspectRatio : IAvifPixelAspectRatio
{
    uint horizontalSpacing;
    uint verticalSpacing;

    internal AvifPixelAspectRatio(uint horizontalSpacing, uint verticalSpacing)
    {
        this.horizontalSpacing = horizontalSpacing;
        this.verticalSpacing = verticalSpacing;
    }

    internal AvifPixelAspectRatio CreateCopy()
    {
        return new AvifPixelAspectRatio(horizontalSpacing, verticalSpacing);
    }

    internal AvifPixelAspectRatio CreateInverse()
    {
        return new AvifPixelAspectRatio(verticalSpacing, horizontalSpacing);
    }

    internal bool ValuesEqual(AvifPixelAspectRatio other)
    {
        return other != null
            && horizontalSpacing == other.horizontalSpacing
            && verticalSpacing == other.verticalSpacing;
    }

    /// <summary>Returns the horizontal pixel spacing.</summary>
    /// <returns>The positive horizontal spacing.</returns>
    public uint GetHorizontalSpacing()
    {
        return horizontalSpacing;
    }

    /// <summary>Returns the vertical pixel spacing.</summary>
    /// <returns>The positive vertical spacing.</returns>
    public uint GetVerticalSpacing()
    {
        return verticalSpacing;
    }

    /// <summary>Returns the horizontal-to-vertical spacing as a floating-point ratio.</summary>
    /// <returns>The pixel aspect ratio.</returns>
    public double GetRatio()
    {
        return (double)horizontalSpacing / verticalSpacing;
    }
}
