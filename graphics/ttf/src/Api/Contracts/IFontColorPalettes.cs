namespace ttf;

/// <summary>Defines public CPAL palette enumeration and color resolution.</summary>
public interface IFontColorPalettes
{
    /// <summary>Gets the requested language used for localized labels.</summary>
    /// <returns>The language tag, or null for default selection.</returns>
    string? GetRequestedLanguage();

    /// <summary>Gets the supported CPAL table version.</summary>
    /// <returns>The CPAL version, or zero when the table is absent.</returns>
    int GetVersion();

    /// <summary>Gets the number of palettes.</summary>
    /// <returns>The palette count.</returns>
    int GetPaletteCount();

    /// <summary>Gets the number of entries in every palette.</summary>
    /// <returns>The palette entry count.</returns>
    int GetPaletteEntryCount();

    /// <summary>Gets one palette.</summary>
    /// <param name="index">The zero-based palette index.</param>
    /// <returns>The immutable palette.</returns>
    ColorPalette GetPalette(int index);

    /// <summary>Gets palette zero.</summary>
    /// <returns>The default palette.</returns>
    ColorPalette GetDefaultPalette();

    /// <summary>Reports whether palette type metadata is present.</summary>
    /// <returns>True when palette types are present; otherwise false.</returns>
    bool HasPaletteTypes();

    /// <summary>Reports whether palette labels are present.</summary>
    /// <returns>True when the palette label array is present; otherwise false.</returns>
    bool HasPaletteLabels();

    /// <summary>Reports whether palette entry labels are present.</summary>
    /// <returns>True when the entry label array is present; otherwise false.</returns>
    bool HasPaletteEntryLabels();

    /// <summary>Reports whether one palette entry has a label.</summary>
    /// <param name="entryIndex">The zero-based palette entry index.</param>
    /// <returns>True when a label identifier is present; otherwise false.</returns>
    bool HasPaletteEntryLabel(int entryIndex);

    /// <summary>Gets the OpenType name identifier for one palette entry label.</summary>
    /// <param name="entryIndex">The zero-based palette entry index.</param>
    /// <returns>The unsigned 16-bit name identifier.</returns>
    int GetPaletteEntryLabelNameId(int entryIndex);

    /// <summary>Gets one localized palette entry label.</summary>
    /// <param name="entryIndex">The zero-based palette entry index.</param>
    /// <returns>The selected label, or null when no label is present.</returns>
    FontNameValue? GetPaletteEntryLabel(int entryIndex);

    /// <summary>Gets the COLR foreground-color palette entry sentinel.</summary>
    /// <returns>The foreground palette entry value.</returns>
    int GetForegroundPaletteEntryIndex();

    /// <summary>Reports whether a palette entry value denotes the foreground color.</summary>
    /// <param name="paletteEntryIndex">The palette entry value.</param>
    /// <returns>True when the value denotes the foreground color.</returns>
    bool IsForegroundPaletteEntry(int paletteEntryIndex);

    /// <summary>Resolves a palette entry or foreground-color reference.</summary>
    /// <param name="paletteIndex">The zero-based palette index.</param>
    /// <param name="paletteEntryIndex">The entry index or foreground sentinel.</param>
    /// <param name="foregroundColor">The caller-selected foreground color.</param>
    /// <returns>The resolved immutable RGBA color.</returns>
    ColorGlyphColor ResolveColor(
        int paletteIndex,
        int paletteEntryIndex,
        ColorGlyphColor foregroundColor);
}
