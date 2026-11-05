namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontClassification"/>.</summary>
public interface IFontClassification
{
    /// <summary>Reports whether OS/2 classification metadata is available.</summary>
    /// <returns>True when the OS/2 table was parsed.</returns>
    bool HasOs2Classification();

    /// <summary>Gets the CSS-compatible weight class.</summary>
    /// <returns>A value from 1 through 1000, or zero when unavailable.</returns>
    int GetWeightClass();

    /// <summary>Gets the width class.</summary>
    /// <returns>A value from 1 through 9, or zero when unavailable.</returns>
    int GetWidthClass();

    /// <summary>Gets the IBM family class.</summary>
    /// <returns>The family class, or zero when unavailable.</returns>
    int GetFamilyClass();

    /// <summary>Gets the IBM family subclass.</summary>
    /// <returns>The family subclass, or zero when unavailable.</returns>
    int GetFamilySubclass();

    /// <summary>Copies the ten PANOSE classification bytes.</summary>
    /// <returns>A new array, or an empty array when unavailable.</returns>
    byte[] CopyPanose();

    /// <summary>Gets one OS/2 Unicode-range word.</summary>
    /// <param name="index">The zero-based word index.</param>
    /// <returns>The range word.</returns>
    /// <exception cref="System.InvalidOperationException">OS/2 metadata is unavailable.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the range words.</exception>
    uint GetUnicodeRange(int index);

    /// <summary>Reports whether an OS/2 Unicode-range bit is set.</summary>
    /// <param name="bitIndex">The bit index from 0 through 127.</param>
    /// <returns>True when the bit is set.</returns>
    /// <exception cref="System.InvalidOperationException">OS/2 metadata is unavailable.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>bitIndex</c> is outside 0 through 127.</exception>
    bool HasUnicodeRange(int bitIndex);

    /// <summary>Reports whether OS/2 code-page ranges are available.</summary>
    /// <returns>True when code-page range words exist.</returns>
    bool HasCodePageRanges();

    /// <summary>Gets one OS/2 code-page range word.</summary>
    /// <param name="index">The zero-based word index.</param>
    /// <returns>The range word.</returns>
    /// <exception cref="System.InvalidOperationException">Code-page ranges are unavailable.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the range words.</exception>
    uint GetCodePageRange(int index);

    /// <summary>Reports whether an OS/2 code-page range bit is set.</summary>
    /// <param name="bitIndex">The bit index from 0 through 63.</param>
    /// <returns>True when the bit is set.</returns>
    /// <exception cref="System.InvalidOperationException">Code-page ranges are unavailable.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>bitIndex</c> is outside 0 through 63.</exception>
    bool HasCodePageRange(int bitIndex);

    /// <summary>Gets the four-byte OS/2 vendor identifier.</summary>
    /// <returns>The big-endian vendor identifier, or zero when unavailable.</returns>
    uint GetVendorId();

    /// <summary>Gets the raw OS/2 selection flags.</summary>
    /// <returns>The selection flags, or zero when unavailable.</returns>
    int GetSelectionFlags();

    /// <summary>Reports whether an OS/2 selection bit is set.</summary>
    /// <param name="bitIndex">The bit index from 0 through 15.</param>
    /// <returns>True when the bit is set.</returns>
    /// <exception cref="System.InvalidOperationException">OS/2 metadata is unavailable.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>bitIndex</c> is outside 0 through 15.</exception>
    bool HasSelectionFlag(int bitIndex);

    /// <summary>Reports whether the font is italic.</summary>
    /// <returns>True for italic style metadata.</returns>
    bool IsItalic();

    /// <summary>Reports whether the font is oblique.</summary>
    /// <returns>True for oblique style metadata.</returns>
    bool IsOblique();

    /// <summary>Reports whether the font is bold.</summary>
    /// <returns>True for bold style metadata.</returns>
    bool IsBold();

    /// <summary>Reports whether the font declares fixed-pitch glyphs.</summary>
    /// <returns>True when the post table declares a nonzero fixed-pitch value.</returns>
    bool IsMonospace();

    /// <summary>Reports whether the IBM family class identifies a serif family.</summary>
    /// <returns>True for a serif family class.</returns>
    bool IsSerif();

    /// <summary>Reports whether the IBM family class identifies a script family.</summary>
    /// <returns>True for a script family class.</returns>
    bool IsScript();

    /// <summary>Reports whether metadata identifies a symbolic font.</summary>
    /// <returns>True for a symbolic family or cmap.</returns>
    bool IsSymbolic();

    /// <summary>Reports whether optical point-size bounds are available.</summary>
    /// <returns>True when version 5 OS/2 fields exist.</returns>
    bool HasOpticalPointSizes();

    /// <summary>Gets the lower optical point-size bound.</summary>
    /// <returns>The lower bound in points.</returns>
    /// <exception cref="System.InvalidOperationException">Optical bounds are unavailable.</exception>
    double GetLowerOpticalPointSize();

    /// <summary>Gets the upper optical point-size bound.</summary>
    /// <returns>The upper bound in points, possibly positive infinity.</returns>
    /// <exception cref="System.InvalidOperationException">Optical bounds are unavailable.</exception>
    double GetUpperOpticalPointSize();
}
