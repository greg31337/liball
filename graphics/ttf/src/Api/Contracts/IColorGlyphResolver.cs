namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="ColorGlyphResolver"/>.</summary>
public interface IColorGlyphResolver
{
    /// <summary>Resolves a glyph using palette zero and automatic selection.</summary>
    /// <param name="instance">The resolved font instance.</param>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <returns>The available representations and deterministic selection.</returns>
    ColorGlyph ResolveGlyph(FontInstance instance, int glyphId);

    /// <summary>Resolves a glyph using explicit palette and representation choices.</summary>
    /// <param name="instance">The resolved font instance.</param>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <param name="options">Palette, foreground, and representation choices.</param>
    /// <returns>The available representations and deterministic selection.</returns>
    ColorGlyph ResolveGlyphWithOptions(FontInstance instance, int glyphId, ColorGlyphOptions options);
}
