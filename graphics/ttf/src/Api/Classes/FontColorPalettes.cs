namespace ttf;

/// <summary>Provides localized public CPAL palette metadata and colors.</summary>
public class FontColorPalettes : IFontColorPalettes
{
    string? requestedLanguage;
    SfntCpalTable? table;
    FontNames names;

    internal FontColorPalettes(
        FontFace face,
        FontNames names,
        string? requestedLanguage)
    {
        if (face == null)
        {
            throw new System.ArgumentNullException("face");
        }
        if (names == null)
        {
            throw new System.ArgumentNullException("names");
        }

        this.names = names;
        this.requestedLanguage = requestedLanguage;
        table = face.GetCpalTable();

        if (table == null && face.HasTable(FontTags.Cpal))
        {
            throw FontFormatException.Create(
                "Color palette inspection requires a supported CPAL table.");
        }
    }

    /// <summary>Gets the requested language used for localized labels.</summary>
    /// <returns>The language tag, or null for default selection.</returns>
    public string? GetRequestedLanguage() { return requestedLanguage; }

    /// <summary>Gets the supported CPAL table version.</summary>
    /// <returns>The CPAL version, or zero when the table is absent.</returns>
    public int GetVersion() { return table == null ? 0 : table.GetVersion(); }

    /// <summary>Gets the number of palettes.</summary>
    /// <returns>The palette count.</returns>
    public int GetPaletteCount()
    {
        return table == null ? 0 : table.GetPaletteCount();
    }

    /// <summary>Gets the number of entries in every palette.</summary>
    /// <returns>The palette entry count.</returns>
    public int GetPaletteEntryCount()
    {
        return table == null ? 0 : table.GetPaletteEntryCount();
    }

    /// <summary>Gets one palette.</summary>
    /// <param name="index">The zero-based palette index.</param>
    /// <returns>The immutable palette.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the palette list.
    /// </exception>
    public ColorPalette GetPalette(int index)
    {
        if (table == null)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return new ColorPalette(table.GetPalette(index), names);
    }

    /// <summary>Gets palette zero.</summary>
    /// <returns>The default palette.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The font does not contain a supported CPAL table.
    /// </exception>
    public ColorPalette GetDefaultPalette()
    {
        if (table == null)
        {
            throw new System.InvalidOperationException(
                "This font does not contain a supported CPAL table.");
        }
        return new ColorPalette(table.GetDefaultPalette(), names);
    }

    /// <summary>Reports whether palette type metadata is present.</summary>
    /// <returns>True when palette types are present; otherwise false.</returns>
    public bool HasPaletteTypes()
    {
        return table != null && table.HasPaletteTypes();
    }

    /// <summary>Reports whether palette labels are present.</summary>
    /// <returns>True when the palette label array is present; otherwise false.</returns>
    public bool HasPaletteLabels()
    {
        return table != null && table.HasPaletteLabels();
    }

    /// <summary>Reports whether palette entry labels are present.</summary>
    /// <returns>True when the entry label array is present; otherwise false.</returns>
    public bool HasPaletteEntryLabels()
    {
        return table != null && table.HasPaletteEntryLabels();
    }

    /// <summary>Reports whether one palette entry has a label.</summary>
    /// <param name="entryIndex">The zero-based palette entry index.</param>
    /// <returns>True when a label identifier is present; otherwise false.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>entryIndex</c> is outside the palette entry range.
    /// </exception>
    public bool HasPaletteEntryLabel(int entryIndex)
    {
        if (table == null)
        {
            throw new System.ArgumentOutOfRangeException("entryIndex");
        }
        return table.HasPaletteEntryLabel(entryIndex);
    }

    /// <summary>Gets the OpenType name identifier for one palette entry label.</summary>
    /// <param name="entryIndex">The zero-based palette entry index.</param>
    /// <returns>The unsigned 16-bit name identifier.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>entryIndex</c> is outside the palette entry range.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// The entry does not provide a label.
    /// </exception>
    public int GetPaletteEntryLabelNameId(int entryIndex)
    {
        if (table == null)
        {
            throw new System.ArgumentOutOfRangeException("entryIndex");
        }
        return table.GetPaletteEntryLabelNameId(entryIndex);
    }

    /// <summary>Gets one localized palette entry label.</summary>
    /// <param name="entryIndex">The zero-based palette entry index.</param>
    /// <returns>The selected label, or null when no label is present.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>entryIndex</c> is outside the palette entry range.
    /// </exception>
    public FontNameValue? GetPaletteEntryLabel(int entryIndex)
    {
        if (table == null)
        {
            throw new System.ArgumentOutOfRangeException("entryIndex");
        }
        if (!table.HasPaletteEntryLabel(entryIndex)) { return null; }
        return names.GetName(table.GetPaletteEntryLabelNameId(entryIndex));
    }

    /// <summary>Gets the COLR foreground-color palette entry sentinel.</summary>
    /// <returns>The foreground palette entry value.</returns>
    public int GetForegroundPaletteEntryIndex()
    {
        return table == null ? 0xFFFF : table.GetForegroundPaletteEntryIndex();
    }

    /// <summary>Reports whether a palette entry value denotes the foreground color.</summary>
    /// <param name="paletteEntryIndex">The palette entry value.</param>
    /// <returns>True when the value denotes the foreground color.</returns>
    public bool IsForegroundPaletteEntry(int paletteEntryIndex)
    {
        return paletteEntryIndex == GetForegroundPaletteEntryIndex();
    }

    /// <summary>Resolves a palette entry or foreground-color reference.</summary>
    /// <param name="paletteIndex">The zero-based palette index.</param>
    /// <param name="paletteEntryIndex">The entry index or foreground sentinel.</param>
    /// <param name="foregroundColor">The caller-selected foreground color.</param>
    /// <returns>The resolved immutable RGBA color.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <c>foregroundColor</c> is null.
    /// </exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// The palette or ordinary entry index is outside its range.
    /// </exception>
    public ColorGlyphColor ResolveColor(
        int paletteIndex,
        int paletteEntryIndex,
        ColorGlyphColor foregroundColor)
    {
        if (foregroundColor == null)
        {
            throw new System.ArgumentNullException("foregroundColor");
        }
        if (table == null)
        {
            throw new System.ArgumentOutOfRangeException("paletteIndex");
        }
        if (table.IsForegroundPaletteEntry(paletteEntryIndex))
        {
            table.GetPalette(paletteIndex);
            return foregroundColor;
        }

        SfntCpalColor resolved = table.ResolveColor(
            paletteIndex,
            paletteEntryIndex,
            foregroundColor.ToCpalColor());
        return ColorPalette.ConvertColor(resolved);
    }
}
