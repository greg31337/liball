namespace ttf;

/// <summary>Defines one axis coordinate in a format 4 <c>STAT</c> value.</summary>
public interface IFontStatAxisValueRecord
{
    /// <summary>Gets the design-axis index.</summary>
    /// <returns>The zero-based axis index.</returns>
    int GetAxisIndex();

    /// <summary>Gets the design-axis tag.</summary>
    /// <returns>The big-endian axis tag.</returns>
    int GetAxisTag();

    /// <summary>Gets the design-axis value.</summary>
    /// <returns>The value.</returns>
    double GetValue();
}
