namespace svg;

/// <summary>Contains an independent baseline snapshot supplied to an external text shaper.</summary>
public class SvgShapingCallbackRequest
{
    /// <summary>The exact parsed font whose glyph identifiers must be returned.</summary>
    public SvgFont Font;
    /// <summary>The stable caller or font-database face identifier.</summary>
    public int FontIdentifier;
    /// <summary>The normalized UTF-16 source text.</summary>
    public string Text;
    /// <summary>The requested four-character OpenType script tag.</summary>
    public string ScriptTag;
    /// <summary>The requested four-character OpenType language-system tag.</summary>
    public string LanguageTag;
    /// <summary>The enabled feature tags, or an empty list for baseline defaults.</summary>
    public System.Collections.Generic.List<string> Features;
    /// <summary>The clamped normalized variation coordinates.</summary>
    public System.Collections.Generic.List<SvgNormalizedVariationCoordinate> VariationCoordinates;
    /// <summary>The independent mapped baseline glyph snapshot.</summary>
    public System.Collections.Generic.List<SvgShapedGlyph> BaselineGlyphs;
    /// <summary>The selected baseline lookup types that were unsupported.</summary>
    public System.Collections.Generic.List<int> UnsupportedLookupTypes;
    /// <summary>The retained shaping table tags that were unsupported.</summary>
    public System.Collections.Generic.List<string> UnsupportedTableTags;
    /// <summary>Why the external shaper was requested.</summary>
    public SvgShapingCallbackReason Reason;
    /// <summary>The output em size used by the baseline glyph snapshot.</summary>
    public double EmSize;
    /// <summary>Whether vertical shaping and advances are requested.</summary>
    public bool Vertical;
    /// <summary>The maximum permitted callback glyph count.</summary>
    public int MaxGlyphs;
    /// <summary>The cancellation token for synchronous callback work.</summary>
    public System.Threading.CancellationToken CancellationToken;

    internal SvgShapingCallbackRequest(SvgFont font)
    {
        Font = font;
        FontIdentifier = 0;
        Text = "";
        ScriptTag = "DFLT";
        LanguageTag = "dflt";
        Features = new System.Collections.Generic.List<string>();
        VariationCoordinates = new System.Collections.Generic.List<SvgNormalizedVariationCoordinate>();
        BaselineGlyphs = new System.Collections.Generic.List<SvgShapedGlyph>();
        UnsupportedLookupTypes = new System.Collections.Generic.List<int>();
        UnsupportedTableTags = new System.Collections.Generic.List<string>();
        Reason = SvgShapingCallbackReason.None;
        EmSize = 16;
        Vertical = false;
        MaxGlyphs = 1000000;
        CancellationToken = System.Threading.CancellationToken.None;
    }
}

/// <summary>Contains caller-shaped glyphs expressed in the requested font identity.</summary>
public class SvgShapingCallbackResponse
{
    /// <summary>Whether the caller completed shaping and supplied a glyph sequence.</summary>
    public bool Success;
    /// <summary>The unchanged face identifier from the callback request.</summary>
    public int FontIdentifier;
    /// <summary>The caller-shaped glyphs with logical source clusters and final metrics.</summary>
    public System.Collections.Generic.List<SvgShapedGlyph> Glyphs;
    /// <summary>An optional deterministic caller explanation.</summary>
    public string Message;

    /// <summary>Creates a declined response with an empty glyph sequence.</summary>
    public SvgShapingCallbackResponse()
    {
        Success = false;
        FontIdentifier = 0;
        Glyphs = new System.Collections.Generic.List<SvgShapedGlyph>();
        Message = "";
    }
}
