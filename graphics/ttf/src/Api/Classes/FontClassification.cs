namespace ttf;

/// <summary>Describes OpenType style, family, and range classification metadata.</summary>
public class FontClassification : IFontClassification
{
    int weightClass;
    int widthClass;
    int familyClass;
    int familySubclass;
    byte[] panose;
    uint[] unicodeRanges;
    uint[] codePageRanges;
    uint vendorId;
    int selectionFlags;
    bool italic;
    bool oblique;
    bool bold;
    bool monospace;
    bool serif;
    bool script;
    bool symbolic;
    bool hasOpticalSizes;
    double lowerOpticalSize;
    double upperOpticalSize;

    internal FontClassification(
        SfntOs2Table? os2,
        SfntHeadTable? head,
        SfntPostTable? post,
        SfntCmapMapping? cmap)
    {
        panose = new byte[0];
        unicodeRanges = new uint[0];
        codePageRanges = new uint[0];

        if (os2 != null)
        {
            weightClass = os2.GetWeightClass();
            widthClass = os2.GetWidthClass();
            familyClass = os2.GetFamilyClass();
            familySubclass = os2.GetFamilySubclass();
            panose = new byte[10];
            int index = 0;
            while (index < panose.Length)
            {
                panose[index] = (byte)os2.GetPanoseValue(index);
                index += 1;
            }
            unicodeRanges = new uint[4];
            index = 0;
            while (index < unicodeRanges.Length)
            {
                unicodeRanges[index] = os2.GetUnicodeRange(index);
                index += 1;
            }
            if (os2.HasCodePageRanges())
            {
                codePageRanges = new uint[]
                {
                    os2.GetCodePageRange(0),
                    os2.GetCodePageRange(1)
                };
            }
            vendorId = os2.GetVendorId();
            selectionFlags = os2.GetSelectionFlags();
            italic = os2.HasSelectionFlag(0);
            bold = os2.HasSelectionFlag(5);
            oblique = os2.GetVersion() >= 4 && os2.HasSelectionFlag(9);
            serif = familyClass >= 1 && familyClass <= 5 || familyClass == 7;
            script = familyClass == 10;
            symbolic = familyClass == 12;
            hasOpticalSizes = os2.HasOpticalPointSizes();
            if (hasOpticalSizes)
            {
                lowerOpticalSize = os2.GetLowerOpticalPointSizeInPoints();
                upperOpticalSize = os2.GetUpperOpticalPointSizeInPoints();
            }
        }
        else if (head != null)
        {
            italic = head.HasMacStyle(1);
            bold = head.HasMacStyle(0);
        }

        monospace = post != null && post.IsFixedPitch();
        if (cmap != null && cmap.IsSymbolMapping()) { symbolic = true; }
    }

    /// <summary>Reports whether OS/2 classification metadata is available.</summary>
    /// <returns>True when the OS/2 table was parsed.</returns>
    public bool HasOs2Classification() { return panose.Length != 0; }

    /// <summary>Gets the CSS-compatible weight class.</summary>
    /// <returns>A value from 1 through 1000, or zero when unavailable.</returns>
    public int GetWeightClass() { return weightClass; }

    /// <summary>Gets the width class.</summary>
    /// <returns>A value from 1 through 9, or zero when unavailable.</returns>
    public int GetWidthClass() { return widthClass; }

    /// <summary>Gets the IBM family class.</summary>
    /// <returns>The family class, or zero when unavailable.</returns>
    public int GetFamilyClass() { return familyClass; }

    /// <summary>Gets the IBM family subclass.</summary>
    /// <returns>The family subclass, or zero when unavailable.</returns>
    public int GetFamilySubclass() { return familySubclass; }

    /// <summary>Copies the ten PANOSE classification bytes.</summary>
    /// <returns>A new array, or an empty array when unavailable.</returns>
    public byte[] CopyPanose() { return panose.AsSpan().ToArray(); }

    /// <summary>Gets one OS/2 Unicode-range word.</summary>
    /// <param name="index">The zero-based word index.</param>
    /// <returns>The range word.</returns>
    /// <exception cref="System.InvalidOperationException">OS/2 metadata is unavailable.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the range words.</exception>
    public uint GetUnicodeRange(int index)
    {
        if (unicodeRanges.Length == 0) { throw new System.InvalidOperationException("OS/2 Unicode ranges are unavailable."); }
        if (index < 0 || index >= unicodeRanges.Length) { throw new System.ArgumentOutOfRangeException("index"); }
        return unicodeRanges[index];
    }

    /// <summary>Reports whether an OS/2 Unicode-range bit is set.</summary>
    /// <param name="bitIndex">The bit index from 0 through 127.</param>
    /// <returns>True when the bit is set.</returns>
    /// <exception cref="System.InvalidOperationException">OS/2 metadata is unavailable.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>bitIndex</c> is outside 0 through 127.</exception>
    public bool HasUnicodeRange(int bitIndex)
    {
        if (unicodeRanges.Length == 0) { throw new System.InvalidOperationException("OS/2 Unicode ranges are unavailable."); }
        if (bitIndex < 0 || bitIndex > 127) { throw new System.ArgumentOutOfRangeException("bitIndex"); }
        return (unicodeRanges[bitIndex / 32] & 1U << bitIndex % 32) != 0;
    }

    /// <summary>Reports whether OS/2 code-page ranges are available.</summary>
    /// <returns>True when code-page range words exist.</returns>
    public bool HasCodePageRanges() { return codePageRanges.Length != 0; }

    /// <summary>Gets one OS/2 code-page range word.</summary>
    /// <param name="index">The zero-based word index.</param>
    /// <returns>The range word.</returns>
    /// <exception cref="System.InvalidOperationException">Code-page ranges are unavailable.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the range words.</exception>
    public uint GetCodePageRange(int index)
    {
        if (codePageRanges.Length == 0) { throw new System.InvalidOperationException("OS/2 code-page ranges are unavailable."); }
        if (index < 0 || index >= codePageRanges.Length) { throw new System.ArgumentOutOfRangeException("index"); }
        return codePageRanges[index];
    }

    /// <summary>Reports whether an OS/2 code-page range bit is set.</summary>
    /// <param name="bitIndex">The bit index from 0 through 63.</param>
    /// <returns>True when the bit is set.</returns>
    /// <exception cref="System.InvalidOperationException">Code-page ranges are unavailable.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>bitIndex</c> is outside 0 through 63.</exception>
    public bool HasCodePageRange(int bitIndex)
    {
        if (codePageRanges.Length == 0) { throw new System.InvalidOperationException("OS/2 code-page ranges are unavailable."); }
        if (bitIndex < 0 || bitIndex > 63) { throw new System.ArgumentOutOfRangeException("bitIndex"); }
        return (codePageRanges[bitIndex / 32] & 1U << bitIndex % 32) != 0;
    }

    /// <summary>Gets the four-byte OS/2 vendor identifier.</summary>
    /// <returns>The big-endian vendor identifier, or zero when unavailable.</returns>
    public uint GetVendorId() { return vendorId; }

    /// <summary>Gets the raw OS/2 selection flags.</summary>
    /// <returns>The selection flags, or zero when unavailable.</returns>
    public int GetSelectionFlags() { return selectionFlags; }

    /// <summary>Reports whether an OS/2 selection bit is set.</summary>
    /// <param name="bitIndex">The bit index from 0 through 15.</param>
    /// <returns>True when the bit is set.</returns>
    /// <exception cref="System.InvalidOperationException">OS/2 metadata is unavailable.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>bitIndex</c> is outside 0 through 15.</exception>
    public bool HasSelectionFlag(int bitIndex)
    {
        if (panose.Length == 0) { throw new System.InvalidOperationException("OS/2 selection flags are unavailable."); }
        if (bitIndex < 0 || bitIndex > 15) { throw new System.ArgumentOutOfRangeException("bitIndex"); }
        return (selectionFlags & 1 << bitIndex) != 0;
    }

    /// <summary>Reports whether the font is italic.</summary>
    /// <returns>True for italic style metadata.</returns>
    public bool IsItalic() { return italic; }

    /// <summary>Reports whether the font is oblique.</summary>
    /// <returns>True for oblique style metadata.</returns>
    public bool IsOblique() { return oblique; }

    /// <summary>Reports whether the font is bold.</summary>
    /// <returns>True for bold style metadata.</returns>
    public bool IsBold() { return bold; }

    /// <summary>Reports whether the font declares fixed-pitch glyphs.</summary>
    /// <returns>True when the post table declares a nonzero fixed-pitch value.</returns>
    public bool IsMonospace() { return monospace; }

    /// <summary>Reports whether the IBM family class identifies a serif family.</summary>
    /// <returns>True for a serif family class.</returns>
    public bool IsSerif() { return serif; }

    /// <summary>Reports whether the IBM family class identifies a script family.</summary>
    /// <returns>True for a script family class.</returns>
    public bool IsScript() { return script; }

    /// <summary>Reports whether metadata identifies a symbolic font.</summary>
    /// <returns>True for a symbolic family or cmap.</returns>
    public bool IsSymbolic() { return symbolic; }

    /// <summary>Reports whether optical point-size bounds are available.</summary>
    /// <returns>True when version 5 OS/2 fields exist.</returns>
    public bool HasOpticalPointSizes() { return hasOpticalSizes; }

    /// <summary>Gets the lower optical point-size bound.</summary>
    /// <returns>The lower bound in points.</returns>
    /// <exception cref="System.InvalidOperationException">Optical bounds are unavailable.</exception>
    public double GetLowerOpticalPointSize()
    {
        if (!hasOpticalSizes) { throw new System.InvalidOperationException("Optical point sizes are unavailable."); }
        return lowerOpticalSize;
    }

    /// <summary>Gets the upper optical point-size bound.</summary>
    /// <returns>The upper bound in points, possibly positive infinity.</returns>
    /// <exception cref="System.InvalidOperationException">Optical bounds are unavailable.</exception>
    public double GetUpperOpticalPointSize()
    {
        if (!hasOpticalSizes) { throw new System.InvalidOperationException("Optical point sizes are unavailable."); }
        return upperOpticalSize;
    }
}
