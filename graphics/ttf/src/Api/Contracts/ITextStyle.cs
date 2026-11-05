namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="TextStyle"/>.</summary>
public interface ITextStyle
{
    /// <summary>Gets the DPI-independent point size.</summary>
    /// <returns>The point size.</returns>
    double GetPointSize();

    /// <summary>Sets the DPI-independent point size.</summary>
    /// <param name="value">The nonnegative finite point size.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is negative or non-finite.
    /// </exception>
    void SetPointSize(double value);

    /// <summary>Gets the extra spacing applied after each shaped cluster.</summary>
    /// <returns>The character spacing in points.</returns>
    double GetCharacterSpacing();

    /// <summary>Sets the extra spacing applied after each shaped cluster.</summary>
    /// <param name="value">The finite character spacing in points.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is non-finite.
    /// </exception>
    void SetCharacterSpacing(double value);

    /// <summary>Gets the additional spacing applied after Unicode space clusters.</summary>
    /// <returns>The word spacing in points.</returns>
    double GetWordSpacing();

    /// <summary>Sets additional spacing applied after Unicode space clusters.</summary>
    /// <param name="value">The finite word spacing in points.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is non-finite.
    /// </exception>
    void SetWordSpacing(double value);

    /// <summary>Gets the horizontal coordinate and advance multiplier.</summary>
    /// <returns>The positive horizontal scale.</returns>
    double GetHorizontalScale();

    /// <summary>Sets the horizontal coordinate and advance multiplier.</summary>
    /// <param name="value">The positive finite horizontal scale.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is zero, negative, or non-finite.
    /// </exception>
    void SetHorizontalScale(double value);

    /// <summary>Gets the shift perpendicular to the text baseline.</summary>
    /// <returns>The baseline shift in points.</returns>
    double GetBaselineShift();

    /// <summary>Sets the shift perpendicular to the text baseline.</summary>
    /// <param name="value">The finite baseline shift in points.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is non-finite.
    /// </exception>
    void SetBaselineShift(double value);

    /// <summary>Reports whether synthetic slant was explicitly requested.</summary>
    /// <returns>True when synthetic slant is enabled.</returns>
    bool IsSyntheticSlantRequested();

    /// <summary>Gets the requested synthetic slant angle.</summary>
    /// <returns>The angle in degrees, or zero when slant is disabled.</returns>
    double GetSyntheticSlant();

    /// <summary>Explicitly requests a synthetic slant.</summary>
    /// <param name="degrees">
    /// The finite angle in degrees, greater than -90 and less than 90.
    /// Positive values move upper outline coordinates to the right.
    /// </param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>degrees</c> is outside the supported range.
    /// </exception>
    void SetSyntheticSlant(double degrees);

    /// <summary>Disables a previously requested synthetic slant.</summary>
    void ClearSyntheticSlant();
}
