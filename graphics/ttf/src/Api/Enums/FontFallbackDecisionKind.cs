namespace ttf;

/// <summary>Identifies one deterministic font-fallback decision.</summary>
public enum FontFallbackDecisionKind
{
    /// <summary>A set-wide fallback family replaced the requested family.</summary>
    FamilySubstitution = 0,

    /// <summary>A configured generic family replaced the generic request.</summary>
    GenericFamilySubstitution = 1,

    /// <summary>A script-specific fallback family supplied the selected font.</summary>
    ScriptFallback = 2,

    /// <summary>The selected font contains unsupported shaping technology.</summary>
    UnsupportedFontTechnology = 3,

    /// <summary>No configured font supplied one required character or sequence.</summary>
    MissingGlyph = 4,

    /// <summary>A partial-coverage font became the final missing-glyph face.</summary>
    FinalMissingGlyphFace = 5
}
