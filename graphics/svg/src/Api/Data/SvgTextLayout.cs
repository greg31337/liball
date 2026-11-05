namespace svg;

/// <summary>Overrides SVG positioning lists for one addressable-character range.</summary>
public class SvgTextPositioningRange
{
    /// <summary>The first addressable character affected by this range.</summary>
    public int CharacterStart;
    /// <summary>The number of addressable characters in this range.</summary>
    public int CharacterCount;
    /// <summary>The range-local absolute horizontal positions.</summary>
    public System.Collections.Generic.List<double> X;
    /// <summary>The range-local absolute vertical positions.</summary>
    public System.Collections.Generic.List<double> Y;
    /// <summary>The range-local relative horizontal adjustments.</summary>
    public System.Collections.Generic.List<double> Dx;
    /// <summary>The range-local relative vertical adjustments.</summary>
    public System.Collections.Generic.List<double> Dy;
    /// <summary>The range-local rotations in degrees; the last value repeats.</summary>
    public System.Collections.Generic.List<double> Rotate;

    /// <summary>Creates an empty positioning range.</summary>
    public SvgTextPositioningRange()
    {
        CharacterStart = 0;
        CharacterCount = 0;
        X = new System.Collections.Generic.List<double>();
        Y = new System.Collections.Generic.List<double>();
        Dx = new System.Collections.Generic.List<double>();
        Dy = new System.Collections.Generic.List<double>();
        Rotate = new System.Collections.Generic.List<double>();
    }
}

/// <summary>Configures bounded SVG text chunk positioning.</summary>
public class SvgTextLayoutOptions
{
    /// <summary>The initial horizontal current text position.</summary>
    public double InitialX;
    /// <summary>The initial vertical current text position.</summary>
    public double InitialY;
    /// <summary>The inline and block axis arrangement.</summary>
    public SvgWritingMode WritingMode;
    /// <summary>The inline base direction.</summary>
    public SvgTextDirection Direction;
    /// <summary>The anchor applied independently to every text chunk.</summary>
    public SvgTextAnchor TextAnchor;
    /// <summary>The root absolute horizontal position list.</summary>
    public System.Collections.Generic.List<double> X;
    /// <summary>The root absolute vertical position list.</summary>
    public System.Collections.Generic.List<double> Y;
    /// <summary>The root relative horizontal adjustment list.</summary>
    public System.Collections.Generic.List<double> Dx;
    /// <summary>The root relative vertical adjustment list.</summary>
    public System.Collections.Generic.List<double> Dy;
    /// <summary>The root rotation list in degrees; the last value repeats.</summary>
    public System.Collections.Generic.List<double> Rotate;
    /// <summary>Ordered descendant ranges that override root or earlier list values.</summary>
    public System.Collections.Generic.List<SvgTextPositioningRange> PositioningRanges;
    /// <summary>The additional distance between addressable characters.</summary>
    public double LetterSpacing;
    /// <summary>The additional distance following a word-space character.</summary>
    public double WordSpacing;
    /// <summary>The font size used for baseline metrics and keyword shifts.</summary>
    public double FontSize;
    /// <summary>The positive alphabetic ascent.</summary>
    public double Ascent;
    /// <summary>The positive alphabetic descent.</summary>
    public double Descent;
    /// <summary>The positive x-height.</summary>
    public double XHeight;
    /// <summary>The dominant baseline.</summary>
    public SvgDominantBaseline DominantBaseline;
    /// <summary>The baseline aligned to the current text position.</summary>
    public SvgAlignmentBaseline AlignmentBaseline;
    /// <summary>The baseline-shift interpretation.</summary>
    public SvgBaselineShiftKind BaselineShift;
    /// <summary>The user-unit shift used when BaselineShift is Length.</summary>
    public double BaselineShiftValue;
    /// <summary>Whether TextLength contains an explicit target.</summary>
    public bool HasTextLength;
    /// <summary>The nonnegative target text length.</summary>
    public double TextLength;
    /// <summary>How the text-length target is achieved.</summary>
    public SvgTextLengthAdjust LengthAdjust;
    /// <summary>The maximum accepted positioned glyph count.</summary>
    public int MaxGlyphs;
    /// <summary>The maximum accepted positioning-range count.</summary>
    public int MaxPositioningRanges;
    /// <summary>The maximum total root and range positioning-list value count.</summary>
    public int MaxPositionValues;
    /// <summary>The maximum generated text chunk count.</summary>
    public int MaxChunks;
    /// <summary>The cancellation token observed during layout.</summary>
    public System.Threading.CancellationToken CancellationToken;

    /// <summary>Creates horizontal alphabetic positioning defaults.</summary>
    public SvgTextLayoutOptions()
    {
        InitialX = 0;
        InitialY = 0;
        WritingMode = SvgWritingMode.HorizontalTopToBottom;
        Direction = SvgTextDirection.LeftToRight;
        TextAnchor = SvgTextAnchor.Start;
        X = new System.Collections.Generic.List<double>();
        Y = new System.Collections.Generic.List<double>();
        Dx = new System.Collections.Generic.List<double>();
        Dy = new System.Collections.Generic.List<double>();
        Rotate = new System.Collections.Generic.List<double>();
        PositioningRanges = new System.Collections.Generic.List<SvgTextPositioningRange>();
        LetterSpacing = 0;
        WordSpacing = 0;
        FontSize = 16;
        Ascent = 12.8;
        Descent = 3.2;
        XHeight = 8;
        DominantBaseline = SvgDominantBaseline.Auto;
        AlignmentBaseline = SvgAlignmentBaseline.Auto;
        BaselineShift = SvgBaselineShiftKind.Baseline;
        BaselineShiftValue = 0;
        HasTextLength = false;
        TextLength = 0;
        LengthAdjust = SvgTextLengthAdjust.Spacing;
        MaxGlyphs = 1000000;
        MaxPositioningRanges = 100000;
        MaxPositionValues = 1000000;
        MaxChunks = 1000000;
        CancellationToken = System.Threading.CancellationToken.None;
    }
}

/// <summary>Contains one final positioned glyph and its source-cluster identity.</summary>
public class SvgPositionedGlyph
{
    /// <summary>An independent copy of the shaped glyph.</summary>
    public SvgShapedGlyph? Glyph;
    /// <summary>The consumed addressable-character index.</summary>
    public int CharacterIndex;
    /// <summary>The containing text chunk index.</summary>
    public int ChunkIndex;
    /// <summary>The final horizontal glyph origin.</summary>
    public double X;
    /// <summary>The final vertical glyph origin.</summary>
    public double Y;
    /// <summary>The final authored rotation in degrees.</summary>
    public double Rotation;
    /// <summary>The adjusted positive inline glyph advance before following spacing.</summary>
    public double InlineAdvance;
    /// <summary>The adjusted spacing following this glyph.</summary>
    public double SpacingAfter;
    /// <summary>The inline glyph scale introduced by spacingAndGlyphs.</summary>
    public double InlineScale;
    /// <summary>Whether this glyph represents a word-space source cluster.</summary>
    public bool WordSpace;

    internal SvgPositionedGlyph()
    {
        Glyph = null;
        CharacterIndex = -1;
        ChunkIndex = -1;
        X = 0;
        Y = 0;
        Rotation = 0;
        InlineAdvance = 0;
        SpacingAfter = 0;
        InlineScale = 1;
        WordSpace = false;
    }
}

/// <summary>Contains one independently anchored SVG text chunk.</summary>
public class SvgTextChunk
{
    /// <summary>The zero-based chunk index.</summary>
    public int Index;
    /// <summary>The first positioned glyph index.</summary>
    public int GlyphStart;
    /// <summary>The number of positioned glyphs.</summary>
    public int GlyphCount;
    /// <summary>The current horizontal position before anchoring.</summary>
    public double X;
    /// <summary>The current vertical position before anchoring.</summary>
    public double Y;
    /// <summary>The natural inline advance before textLength adjustment.</summary>
    public double NaturalAdvance;
    /// <summary>The adjusted inline advance.</summary>
    public double Advance;
    /// <summary>The signed inline anchor displacement.</summary>
    public double AnchorOffset;

    internal SvgTextChunk()
    {
        Index = -1;
        GlyphStart = 0;
        GlyphCount = 0;
        X = 0;
        Y = 0;
        NaturalAdvance = 0;
        Advance = 0;
        AnchorOffset = 0;
    }
}

/// <summary>Contains positioned SVG text chunks, glyphs, and adjustment metadata.</summary>
public class SvgTextLayoutResult
{
    /// <summary>Whether text positioning completed successfully.</summary>
    public bool Success;
    /// <summary>The stable positioning outcome.</summary>
    public SvgTextLayoutError Error;
    /// <summary>A deterministic failure explanation, or empty string.</summary>
    public string Message;
    /// <summary>The positioned glyphs in logical shaping order.</summary>
    public System.Collections.Generic.List<SvgPositionedGlyph> Glyphs;
    /// <summary>The normalized source text represented by the positioned glyphs.</summary>
    public string Text;
    /// <summary>The inline and block axis arrangement used for positioning.</summary>
    public SvgWritingMode WritingMode;
    /// <summary>The inline base direction used for positioning.</summary>
    public SvgTextDirection Direction;
    /// <summary>The font size used for logical and decoration metrics.</summary>
    public double FontSize;
    /// <summary>The positive alphabetic ascent used for layout.</summary>
    public double Ascent;
    /// <summary>The positive alphabetic descent used for layout.</summary>
    public double Descent;
    /// <summary>The positive x-height used for layout.</summary>
    public double XHeight;
    /// <summary>The independently anchored text chunks.</summary>
    public System.Collections.Generic.List<SvgTextChunk> Chunks;
    /// <summary>The addressable source-character count.</summary>
    public int CharacterCount;
    /// <summary>The resolved cross-axis baseline offset.</summary>
    public double BaselineOffset;
    /// <summary>The natural sum of chunk advances.</summary>
    public double NaturalTextLength;
    /// <summary>The adjusted sum of chunk advances.</summary>
    public double ActualTextLength;
    /// <summary>The spacing-only adjustment added to every adjustable gap.</summary>
    public double SpacingAdjustment;
    /// <summary>The glyph scale used by spacingAndGlyphs.</summary>
    public double GlyphScale;

    internal SvgTextLayoutResult()
    {
        Success = false;
        Error = SvgTextLayoutError.None;
        Message = "";
        Glyphs = new System.Collections.Generic.List<SvgPositionedGlyph>();
        Text = "";
        WritingMode = SvgWritingMode.HorizontalTopToBottom;
        Direction = SvgTextDirection.LeftToRight;
        FontSize = 16;
        Ascent = 12.8;
        Descent = 3.2;
        XHeight = 8;
        Chunks = new System.Collections.Generic.List<SvgTextChunk>();
        CharacterCount = 0;
        BaselineOffset = 0;
        NaturalTextLength = 0;
        ActualTextLength = 0;
        SpacingAdjustment = 0;
        GlyphScale = 1;
    }
}
