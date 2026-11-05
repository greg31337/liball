namespace ttf;

/// <summary>Defines inspection of the legacy kern table.</summary>
public interface IFontLegacyKerning
{
    /// <summary>Gets the raw table version.</summary>
    /// <returns>The raw version value.</returns>
    uint GetVersion();

    /// <summary>Reports whether the table uses the Apple header form.</summary>
    /// <returns>True for the Apple form.</returns>
    bool IsAppleFormat();

    /// <summary>Gets the number of kerning subtables.</summary>
    /// <returns>The subtable count.</returns>
    int GetSubtableCount();

    /// <summary>Gets one kerning subtable.</summary>
    /// <param name="index">The zero-based subtable index.</param>
    /// <returns>The stable subtable inspection.</returns>
    FontLegacyKerningSubtable GetSubtable(int index);
}
