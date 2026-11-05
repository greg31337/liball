namespace ttf;

/// <summary>Represents one script, direction, and language shaping run.</summary>
public class TextShapingRun : ITextShapingRun
{
    ShapedRun shapedRun;
    FontInstance fontInstance;
    string script;
    int scriptTag;
    int languageSystemTag;
    int bidiLevel;
    int utf16Start;
    int utf16Length;
    TextShapingPlanKind planKind;
    int planStageCount;

    internal TextShapingRun(
        ShapedRun shapedRun,
        FontInstance fontInstance,
        string script,
        int scriptTag,
        int languageSystemTag,
        int bidiLevel,
        int utf16Start,
        int utf16Length,
        TextShapingPlanKind planKind,
        int planStageCount)
    {
        this.shapedRun = shapedRun;
        this.fontInstance = fontInstance;
        this.script = script;
        this.scriptTag = scriptTag;
        this.languageSystemTag = languageSystemTag;
        this.bidiLevel = bidiLevel;
        this.utf16Start = utf16Start;
        this.utf16Length = utf16Length;
        this.planKind = planKind;
        this.planStageCount = planStageCount;
    }

    /// <summary>Gets the immutable positioned glyph run.</summary>
    /// <returns>The shaped glyph data.</returns>
    public ShapedRun GetShapedRun() { return shapedRun; }

    /// <summary>Gets the font instance used for this run.</summary>
    /// <returns>The resolved instance.</returns>
    public FontInstance GetFontInstance() { return fontInstance; }

    /// <summary>Gets the source face used for this run.</summary>
    /// <returns>The resolved face.</returns>
    public FontFace GetFontFace() { return fontInstance.GetFace(); }

    /// <summary>Gets the canonical ISO 15924 script code.</summary>
    /// <returns>The four-letter script code.</returns>
    public string GetScript() { return script; }

    /// <summary>Gets the selected OpenType script tag.</summary>
    /// <returns>The big-endian tag.</returns>
    public int GetScriptTag() { return scriptTag; }

    /// <summary>Gets the selected OpenType language-system tag.</summary>
    /// <returns>The big-endian tag, or zero.</returns>
    public int GetLanguageSystemTag() { return languageSystemTag; }

    /// <summary>Gets the resolved Unicode bidi level.</summary>
    /// <returns>The nonnegative embedding level.</returns>
    public int GetBidiLevel() { return bidiLevel; }

    /// <summary>Gets the source UTF-16 start.</summary>
    /// <returns>The zero-based offset.</returns>
    public int GetUtf16Start() { return utf16Start; }

    /// <summary>Gets the source UTF-16 length.</summary>
    /// <returns>The code-unit length.</returns>
    public int GetUtf16Length() { return utf16Length; }

    /// <summary>Gets total horizontal design-unit advance.</summary>
    /// <returns>The horizontal advance.</returns>
    public double GetTotalXAdvance()
    {
        return shapedRun.GetTotalXAdvance();
    }

    /// <summary>Gets total vertical design-unit advance.</summary>
    /// <returns>The vertical advance.</returns>
    public double GetTotalYAdvance()
    {
        return shapedRun.GetTotalYAdvance();
    }

    /// <summary>Measures total advance and exact ink bounds.</summary>
    /// <param name="pointSize">The nonnegative finite point size.</param>
    /// <returns>The scaled measurement.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>pointSize</c> is invalid.</exception>
    public TextMeasurement Measure(double pointSize)
    {
        return shapedRun.Measure(fontInstance, pointSize);
    }

    /// <summary>Measures with point-based style adjustments.</summary>
    /// <param name="style">The shaping measurement style.</param>
    /// <returns>The adjusted measurement.</returns>
    /// <exception cref="System.ArgumentNullException"><c>style</c> is null.</exception>
    public TextMeasurement MeasureWithStyle(TextStyle style)
    {
        return shapedRun.MeasureWithStyle(fontInstance, style);
    }

    /// <summary>Gets the script-specific shaping model used for this run.</summary>
    /// <returns>The resolved shaping-plan kind.</returns>
    public TextShapingPlanKind GetPlanKind() { return planKind; }

    /// <summary>Gets the number of ordered stages in the resolved shaping plan.</summary>
    /// <returns>The stage count.</returns>
    public int GetPlanStageCount() { return planStageCount; }
}
