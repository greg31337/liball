namespace ttf;

/// <summary>Defines one public CPAL color palette.</summary>
public interface IColorPalette
{
    /// <summary>Gets the zero-based palette index.</summary>
    /// <returns>The palette index.</returns>
    int GetIndex();

    /// <summary>Gets the number of entries in the palette.</summary>
    /// <returns>The palette entry count.</returns>
    int GetEntryCount();

    /// <summary>Gets one palette color.</summary>
    /// <param name="entryIndex">The zero-based palette entry index.</param>
    /// <returns>The immutable RGBA color.</returns>
    ColorGlyphColor GetColor(int entryIndex);

    /// <summary>Reports whether the CPAL table supplies palette type metadata.</summary>
    /// <returns>True when type metadata is present; otherwise false.</returns>
    bool HasType();

    /// <summary>Gets the palette's preferred background type.</summary>
    /// <returns>The preferred background type.</returns>
    ColorPaletteType GetPaletteType();

    /// <summary>Reports whether the palette is suitable for a light background.</summary>
    /// <returns>True when the palette is suitable; otherwise false.</returns>
    bool IsUsableWithLightBackground();

    /// <summary>Reports whether the palette is suitable for a dark background.</summary>
    /// <returns>True when the palette is suitable; otherwise false.</returns>
    bool IsUsableWithDarkBackground();

    /// <summary>Reports whether the palette has a label name identifier.</summary>
    /// <returns>True when a label identifier is present; otherwise false.</returns>
    bool HasLabel();

    /// <summary>Gets the OpenType name identifier for the palette label.</summary>
    /// <returns>The unsigned 16-bit name identifier.</returns>
    int GetLabelNameId();

    /// <summary>Gets the localized palette label.</summary>
    /// <returns>The selected label, or null when no label is present.</returns>
    FontNameValue? GetLabel();
}
