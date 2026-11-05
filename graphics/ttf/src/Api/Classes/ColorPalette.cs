namespace ttf;

/// <summary>Represents one immutable CPAL color palette.</summary>
public class ColorPalette : IColorPalette
{
    SfntCpalPalette palette;
    FontNames names;

    internal ColorPalette(SfntCpalPalette palette, FontNames names)
    {
        if (palette == null)
        {
            throw new System.ArgumentNullException("palette");
        }
        if (names == null)
        {
            throw new System.ArgumentNullException("names");
        }

        this.palette = palette;
        this.names = names;
    }

    /// <summary>Gets the zero-based palette index.</summary>
    /// <returns>The palette index.</returns>
    public int GetIndex() { return palette.GetIndex(); }

    /// <summary>Gets the number of entries in the palette.</summary>
    /// <returns>The palette entry count.</returns>
    public int GetEntryCount() { return palette.GetEntryCount(); }

    /// <summary>Gets one palette color.</summary>
    /// <param name="entryIndex">The zero-based palette entry index.</param>
    /// <returns>The immutable RGBA color.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>entryIndex</c> is outside the palette.
    /// </exception>
    public ColorGlyphColor GetColor(int entryIndex)
    {
        return ConvertColor(palette.GetColor(entryIndex));
    }

    /// <summary>Reports whether the CPAL table supplies palette type metadata.</summary>
    /// <returns>True when type metadata is present; otherwise false.</returns>
    public bool HasType() { return palette.HasType(); }

    /// <summary>Gets the palette's preferred background type.</summary>
    /// <returns>The preferred background type.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The CPAL table does not provide palette types.
    /// </exception>
    public ColorPaletteType GetPaletteType()
    {
        return (ColorPaletteType)(int)palette.GetTypeFlags();
    }

    /// <summary>Reports whether the palette is suitable for a light background.</summary>
    /// <returns>True when the palette is suitable; otherwise false.</returns>
    public bool IsUsableWithLightBackground()
    {
        return palette.IsUsableWithLightBackground();
    }

    /// <summary>Reports whether the palette is suitable for a dark background.</summary>
    /// <returns>True when the palette is suitable; otherwise false.</returns>
    public bool IsUsableWithDarkBackground()
    {
        return palette.IsUsableWithDarkBackground();
    }

    /// <summary>Reports whether the palette has a label name identifier.</summary>
    /// <returns>True when a label identifier is present; otherwise false.</returns>
    public bool HasLabel() { return palette.HasLabel(); }

    /// <summary>Gets the OpenType name identifier for the palette label.</summary>
    /// <returns>The unsigned 16-bit name identifier.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The palette does not provide a label.
    /// </exception>
    public int GetLabelNameId() { return palette.GetLabelNameId(); }

    /// <summary>Gets the localized palette label.</summary>
    /// <returns>The selected label, or null when no label is present.</returns>
    public FontNameValue? GetLabel()
    {
        if (!palette.HasLabel()) { return null; }
        return names.GetName(palette.GetLabelNameId());
    }

    internal static ColorGlyphColor ConvertColor(SfntCpalColor color)
    {
        return new ColorGlyphColor(
            color.GetRed(),
            color.GetGreen(),
            color.GetBlue(),
            color.GetAlpha());
    }
}
