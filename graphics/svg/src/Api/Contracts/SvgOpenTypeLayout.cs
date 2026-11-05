namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgOpenTypeLayout"/>.</summary>
public interface ISvgOpenTypeLayout
{
    /// <summary>Shapes UTF-16 text with baseline defaults.</summary>
    /// <param name="font">A successfully parsed OpenType font.</param>
    /// <param name="text">The UTF-16 source text.</param>
    /// <returns>The mapped and positioned glyph sequence.</returns>
    SvgShapingResult ShapeWithFontAndText(SvgFont? font, string text);
    /// <summary>Shapes UTF-16 text with explicit features, language, variation, and bounds.</summary>
    /// <param name="font">A successfully parsed OpenType font.</param>
    /// <param name="text">The UTF-16 source text.</param>
    /// <param name="options">The shaping configuration and security bounds.</param>
    /// <returns>The mapped and positioned glyph sequence.</returns>
    SvgShapingResult ShapeWithSvgFontAndStringAndSvgShapingOptions(SvgFont? font, string text, SvgShapingOptions options);
    /// <summary>Shapes a previously normalized Unicode text model.</summary>
    /// <param name="font">A successfully parsed OpenType font.</param>
    /// <param name="text">The normalized Unicode text model.</param>
    /// <param name="options">The shaping configuration and security bounds.</param>
    /// <returns>The mapped and positioned glyph sequence.</returns>
    SvgShapingResult ShapeWithSvgFontAndSvgUnicodeTextAndSvgShapingOptions(SvgFont? font, SvgUnicodeText text, SvgShapingOptions options);
    /// <summary>Shapes text with defaults while deriving stable font identity from a registered face.</summary>
    /// <param name="face">The registered font face.</param>
    /// <param name="text">The UTF-16 source text.</param>
    /// <returns>The baseline glyph sequence.</returns>
    SvgShapingResult ShapeFontFaceWithFaceAndText(SvgFontFace face, string text);
    /// <summary>Shapes text while deriving stable font identity from a registered face.</summary>
    /// <param name="face">The registered font face.</param>
    /// <param name="text">The UTF-16 source text.</param>
    /// <param name="options">The shaping configuration and optional callback.</param>
    /// <returns>The baseline or validated callback glyph sequence.</returns>
    SvgShapingResult ShapeFontFaceWithFaceAndTextAndOptions(SvgFontFace? face, string text, SvgShapingOptions? options);
}
