namespace unicode;

/// <summary>
/// Reports whether a PDF font can render individual Unicode scalar values.
/// </summary>
public interface IUnicodePdfFontCoverage
{
    /// <summary>
    /// Reports whether the font covers the specified Unicode scalar.
    /// </summary>
    /// <param name="codePoint">The Unicode scalar to inspect.</param>
    /// <returns><c>true</c> when the font covers the scalar; otherwise, <c>false</c>.</returns>
    bool CoversScalar(int codePoint);
}
