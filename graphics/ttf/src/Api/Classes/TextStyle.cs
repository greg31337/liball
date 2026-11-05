namespace ttf;

/// <summary>Defines point-based adjustments applied to measured text.</summary>
/// <remarks>
/// Spacing and baseline values are expressed in DPI-independent points.
/// Horizontal text spacing is measured before horizontal scaling.
/// Horizontal scale is a multiplier. Synthetic slant is disabled until
/// <see cref="SetSyntheticSlant"/> is called explicitly.
/// </remarks>
public class TextStyle : ITextStyle
{
    double pointSize;
    double characterSpacing;
    double wordSpacing;
    double horizontalScale;
    double baselineShift;
    bool syntheticSlantRequested;
    double syntheticSlantDegrees;

    /// <summary>Creates a style at a DPI-independent point size.</summary>
    /// <param name="pointSize">The nonnegative finite point size.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>pointSize</c> is negative or non-finite.
    /// </exception>
    public TextStyle(double pointSize)
    {
        ValidateNonnegative(pointSize, "pointSize");
        this.pointSize = pointSize;
        characterSpacing = 0;
        wordSpacing = 0;
        horizontalScale = 1;
        baselineShift = 0;
        syntheticSlantRequested = false;
        syntheticSlantDegrees = 0;
    }

    /// <summary>Gets the DPI-independent point size.</summary>
    /// <returns>The point size.</returns>
    public double GetPointSize()
    {
        return pointSize;
    }

    /// <summary>Sets the DPI-independent point size.</summary>
    /// <param name="value">The nonnegative finite point size.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is negative or non-finite.
    /// </exception>
    public void SetPointSize(double value)
    {
        ValidateNonnegative(value, "value");
        pointSize = value;
    }

    /// <summary>Gets the extra spacing applied after each shaped cluster.</summary>
    /// <returns>The character spacing in points.</returns>
    public double GetCharacterSpacing()
    {
        return characterSpacing;
    }

    /// <summary>Sets the extra spacing applied after each shaped cluster.</summary>
    /// <param name="value">The finite character spacing in points.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is non-finite.
    /// </exception>
    public void SetCharacterSpacing(double value)
    {
        ValidateFinite(value, "value");
        characterSpacing = value;
    }

    /// <summary>Gets the additional spacing applied after Unicode space clusters.</summary>
    /// <returns>The word spacing in points.</returns>
    public double GetWordSpacing()
    {
        return wordSpacing;
    }

    /// <summary>Sets additional spacing applied after Unicode space clusters.</summary>
    /// <param name="value">The finite word spacing in points.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is non-finite.
    /// </exception>
    public void SetWordSpacing(double value)
    {
        ValidateFinite(value, "value");
        wordSpacing = value;
    }

    /// <summary>Gets the horizontal coordinate and advance multiplier.</summary>
    /// <returns>The positive horizontal scale.</returns>
    public double GetHorizontalScale()
    {
        return horizontalScale;
    }

    /// <summary>Sets the horizontal coordinate and advance multiplier.</summary>
    /// <param name="value">The positive finite horizontal scale.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is zero, negative, or non-finite.
    /// </exception>
    public void SetHorizontalScale(double value)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        horizontalScale = value;
    }

    /// <summary>Gets the shift perpendicular to the text baseline.</summary>
    /// <returns>The baseline shift in points.</returns>
    public double GetBaselineShift()
    {
        return baselineShift;
    }

    /// <summary>Sets the shift perpendicular to the text baseline.</summary>
    /// <param name="value">The finite baseline shift in points.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is non-finite.
    /// </exception>
    public void SetBaselineShift(double value)
    {
        ValidateFinite(value, "value");
        baselineShift = value;
    }

    /// <summary>Reports whether synthetic slant was explicitly requested.</summary>
    /// <returns>True when synthetic slant is enabled.</returns>
    public bool IsSyntheticSlantRequested()
    {
        return syntheticSlantRequested;
    }

    /// <summary>Gets the requested synthetic slant angle.</summary>
    /// <returns>The angle in degrees, or zero when slant is disabled.</returns>
    public double GetSyntheticSlant()
    {
        return syntheticSlantRequested ? syntheticSlantDegrees : 0;
    }

    /// <summary>Explicitly requests a synthetic slant.</summary>
    /// <param name="degrees">
    /// The finite angle in degrees, greater than -90 and less than 90.
    /// Positive values move upper outline coordinates to the right.
    /// </param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>degrees</c> is outside the supported range.
    /// </exception>
    public void SetSyntheticSlant(double degrees)
    {
        if (!double.IsFinite(degrees) || degrees <= -90 || degrees >= 90)
        {
            throw new System.ArgumentOutOfRangeException("degrees");
        }

        syntheticSlantDegrees = degrees;
        syntheticSlantRequested = true;
    }

    /// <summary>Disables a previously requested synthetic slant.</summary>
    public void ClearSyntheticSlant()
    {
        syntheticSlantRequested = false;
        syntheticSlantDegrees = 0;
    }

    internal double GetSyntheticSlantFactor()
    {
        if (!syntheticSlantRequested)
        {
            return 0;
        }

        return System.Math.Tan(
            syntheticSlantDegrees * System.Math.PI / 180.0);
    }

    internal TextStyle Copy()
    {
        TextStyle copy = new TextStyle(pointSize);
        copy.characterSpacing = characterSpacing;
        copy.wordSpacing = wordSpacing;
        copy.horizontalScale = horizontalScale;
        copy.baselineShift = baselineShift;
        copy.syntheticSlantRequested = syntheticSlantRequested;
        copy.syntheticSlantDegrees = syntheticSlantDegrees;
        return copy;
    }

    static void ValidateFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateNonnegative(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }
}
