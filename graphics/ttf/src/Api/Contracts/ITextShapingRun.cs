namespace ttf;

/// <summary>Defines one script, direction, and language shaping run.</summary>
public interface ITextShapingRun
{
    /// <summary>Gets the immutable positioned glyph run.</summary>
    /// <returns>The shaped glyph data.</returns>
    ShapedRun GetShapedRun();

    /// <summary>Gets the font instance used for this run.</summary>
    /// <returns>The resolved instance.</returns>
    FontInstance GetFontInstance();

    /// <summary>Gets the source face used for this run.</summary>
    /// <returns>The resolved face.</returns>
    FontFace GetFontFace();

    /// <summary>Gets the canonical ISO 15924 script code.</summary>
    /// <returns>The four-letter script code.</returns>
    string GetScript();

    /// <summary>Gets the selected OpenType script tag.</summary>
    /// <returns>The big-endian tag.</returns>
    int GetScriptTag();

    /// <summary>Gets the selected OpenType language-system tag.</summary>
    /// <returns>The big-endian tag, or zero.</returns>
    int GetLanguageSystemTag();

    /// <summary>Gets the resolved Unicode bidi level.</summary>
    /// <returns>The nonnegative embedding level.</returns>
    int GetBidiLevel();

    /// <summary>Gets the source UTF-16 start.</summary>
    /// <returns>The zero-based offset.</returns>
    int GetUtf16Start();

    /// <summary>Gets the source UTF-16 length.</summary>
    /// <returns>The code-unit length.</returns>
    int GetUtf16Length();

    /// <summary>Gets the script-specific shaping model used for this run.</summary>
    /// <returns>The resolved shaping-plan kind.</returns>
    TextShapingPlanKind GetPlanKind();

    /// <summary>Gets the number of ordered stages in the resolved shaping plan.</summary>
    /// <returns>The stage count.</returns>
    int GetPlanStageCount();

    /// <summary>Gets total horizontal design-unit advance.</summary>
    /// <returns>The horizontal advance.</returns>
    double GetTotalXAdvance();

    /// <summary>Gets total vertical design-unit advance.</summary>
    /// <returns>The vertical advance.</returns>
    double GetTotalYAdvance();

    /// <summary>Measures total advance and exact ink bounds.</summary>
    /// <param name="pointSize">The nonnegative finite point size.</param>
    /// <returns>The scaled measurement.</returns>
    TextMeasurement Measure(double pointSize);

    /// <summary>Measures with point-based style adjustments.</summary>
    /// <param name="style">The shaping measurement style.</param>
    /// <returns>The adjusted measurement.</returns>
    TextMeasurement MeasureWithStyle(TextStyle style);
}
