namespace ttf;

/// <summary>Describes how a Unicode character mapping was resolved.</summary>
public enum FontCharacterMappingKind
{
    /// <summary>No selected cmap subtable maps the requested sequence.</summary>
    Missing = 0,

    /// <summary>A selected nominal cmap subtable maps the scalar directly.</summary>
    Direct = 1,

    /// <summary>A Windows symbol cmap maps the scalar through its private-use alias.</summary>
    SymbolRemapped = 2,

    /// <summary>A format 14 default sequence uses the nominal cmap glyph.</summary>
    DefaultVariation = 3,

    /// <summary>A format 14 non-default sequence supplies an explicit glyph.</summary>
    NonDefaultVariation = 4
}
