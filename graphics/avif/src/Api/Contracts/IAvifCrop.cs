namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifCrop"/>.</summary>
public interface IAvifCrop
{
    /// <summary>Returns the clean-aperture width numerator.</summary>
    /// <returns>The unsigned numerator.</returns>
    uint GetWidthNumerator();

    /// <summary>Returns the clean-aperture width denominator.</summary>
    /// <returns>The positive denominator.</returns>
    uint GetWidthDenominator();

    /// <summary>Returns the clean-aperture height numerator.</summary>
    /// <returns>The unsigned numerator.</returns>
    uint GetHeightNumerator();

    /// <summary>Returns the clean-aperture height denominator.</summary>
    /// <returns>The positive denominator.</returns>
    uint GetHeightDenominator();

    /// <summary>Returns the horizontal centre-offset numerator.</summary>
    /// <returns>The signed numerator.</returns>
    int GetHorizontalOffsetNumerator();

    /// <summary>Returns the horizontal centre-offset denominator.</summary>
    /// <returns>The positive denominator.</returns>
    uint GetHorizontalOffsetDenominator();

    /// <summary>Returns the vertical centre-offset numerator.</summary>
    /// <returns>The signed numerator.</returns>
    int GetVerticalOffsetNumerator();

    /// <summary>Returns the vertical centre-offset denominator.</summary>
    /// <returns>The positive denominator.</returns>
    uint GetVerticalOffsetDenominator();
}
