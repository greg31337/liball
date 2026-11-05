namespace avif;

/// <summary>
/// Describes an ISO clean aperture using exact rational dimensions and offsets.
/// </summary>
public class AvifCrop : IAvifCrop
{
    uint widthNumerator;
    uint widthDenominator;
    uint heightNumerator;
    uint heightDenominator;
    int horizontalOffsetNumerator;
    uint horizontalOffsetDenominator;
    int verticalOffsetNumerator;
    uint verticalOffsetDenominator;

    internal AvifCrop(
        uint widthNumerator,
        uint widthDenominator,
        uint heightNumerator,
        uint heightDenominator,
        int horizontalOffsetNumerator,
        uint horizontalOffsetDenominator,
        int verticalOffsetNumerator,
        uint verticalOffsetDenominator)
    {
        this.widthNumerator = widthNumerator;
        this.widthDenominator = widthDenominator;
        this.heightNumerator = heightNumerator;
        this.heightDenominator = heightDenominator;
        this.horizontalOffsetNumerator = horizontalOffsetNumerator;
        this.horizontalOffsetDenominator = horizontalOffsetDenominator;
        this.verticalOffsetNumerator = verticalOffsetNumerator;
        this.verticalOffsetDenominator = verticalOffsetDenominator;
    }

    internal AvifCrop CreateCopy()
    {
        return new AvifCrop(
            widthNumerator,
            widthDenominator,
            heightNumerator,
            heightDenominator,
            horizontalOffsetNumerator,
            horizontalOffsetDenominator,
            verticalOffsetNumerator,
            verticalOffsetDenominator);
    }

    internal bool ValuesEqual(AvifCrop other)
    {
        return other != null
            && widthNumerator == other.widthNumerator
            && widthDenominator == other.widthDenominator
            && heightNumerator == other.heightNumerator
            && heightDenominator == other.heightDenominator
            && horizontalOffsetNumerator == other.horizontalOffsetNumerator
            && horizontalOffsetDenominator == other.horizontalOffsetDenominator
            && verticalOffsetNumerator == other.verticalOffsetNumerator
            && verticalOffsetDenominator == other.verticalOffsetDenominator;
    }

    /// <summary>Returns the clean-aperture width numerator.</summary>
    /// <returns>The unsigned numerator.</returns>
    public uint GetWidthNumerator()
    {
        return widthNumerator;
    }

    /// <summary>Returns the clean-aperture width denominator.</summary>
    /// <returns>The positive denominator.</returns>
    public uint GetWidthDenominator()
    {
        return widthDenominator;
    }

    /// <summary>Returns the clean-aperture height numerator.</summary>
    /// <returns>The unsigned numerator.</returns>
    public uint GetHeightNumerator()
    {
        return heightNumerator;
    }

    /// <summary>Returns the clean-aperture height denominator.</summary>
    /// <returns>The positive denominator.</returns>
    public uint GetHeightDenominator()
    {
        return heightDenominator;
    }

    /// <summary>Returns the horizontal centre-offset numerator.</summary>
    /// <returns>The signed numerator.</returns>
    public int GetHorizontalOffsetNumerator()
    {
        return horizontalOffsetNumerator;
    }

    /// <summary>Returns the horizontal centre-offset denominator.</summary>
    /// <returns>The positive denominator.</returns>
    public uint GetHorizontalOffsetDenominator()
    {
        return horizontalOffsetDenominator;
    }

    /// <summary>Returns the vertical centre-offset numerator.</summary>
    /// <returns>The signed numerator.</returns>
    public int GetVerticalOffsetNumerator()
    {
        return verticalOffsetNumerator;
    }

    /// <summary>Returns the vertical centre-offset denominator.</summary>
    /// <returns>The positive denominator.</returns>
    public uint GetVerticalOffsetDenominator()
    {
        return verticalOffsetDenominator;
    }
}
