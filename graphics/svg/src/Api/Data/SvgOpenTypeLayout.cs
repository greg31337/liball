namespace svg;

/// <summary>Assigns a design-space value to one four-character variation axis.</summary>
public class SvgFontVariationCoordinate
{
    /// <summary>The exact four-character axis tag.</summary>
    public string AxisTag;
    /// <summary>The requested design-space value.</summary>
    public double Value;

    /// <summary>Creates one requested variation coordinate.</summary>
    /// <param name="axisTag">The exact four-character axis tag.</param>
    /// <param name="value">The finite design-space value.</param>
    public SvgFontVariationCoordinate(string axisTag, double value)
    {
        if (axisTag == null || axisTag.Length != 4) throw new System.ArgumentOutOfRangeException("axisTag");
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new System.ArgumentOutOfRangeException("value");
        AxisTag = axisTag;
        Value = value;
    }
}

/// <summary>Contains a clamped and normalized OpenType variation coordinate.</summary>
public class SvgNormalizedVariationCoordinate
{
    /// <summary>The exact four-character axis tag.</summary>
    public string AxisTag;
    /// <summary>The requested design-space value.</summary>
    public double RequestedValue;
    /// <summary>The clamped design-space value.</summary>
    public double Value;
    /// <summary>The normalized and optional <c>avar</c>-mapped value from minus one through one.</summary>
    public double NormalizedValue;

    internal SvgNormalizedVariationCoordinate()
    {
        AxisTag = "";
        RequestedValue = 0;
        Value = 0;
        NormalizedValue = 0;
    }
}

/// <summary>Configures bounded baseline OpenType shaping.</summary>
public class SvgShapingOptions
{
    /// <summary>The exact four-character OpenType script tag.</summary>
    public string ScriptTag;
    /// <summary>The exact four-character OpenType language-system tag.</summary>
    public string LanguageTag;
    /// <summary>The enabled feature tags, or an empty list for baseline defaults.</summary>
    public System.Collections.Generic.List<string> Features;
    /// <summary>The requested design-space variation coordinates.</summary>
    public System.Collections.Generic.List<SvgFontVariationCoordinate> VariationCoordinates;
    /// <summary>The optional synchronous callback used for unsupported shaping.</summary>
    public System.Func<SvgShapingCallbackRequest, SvgShapingCallbackResponse>? ShapingCallback;
    /// <summary>The stable caller or font-database face identifier retained on every output glyph.</summary>
    public int FontIdentifier;
    /// <summary>The output em size used to scale metrics and positioning.</summary>
    public double EmSize;
    /// <summary>Whether vertical advances and vertical layout features are requested.</summary>
    public bool Vertical;
    /// <summary>The maximum glyph count after substitutions.</summary>
    public int MaxGlyphs;
    /// <summary>The maximum selected layout lookup count.</summary>
    public int MaxLookups;
    /// <summary>The maximum substitution and positioning operations.</summary>
    public int MaxOperations;
    /// <summary>The maximum parsed variation-axis count.</summary>
    public int MaxVariationAxes;
    /// <summary>The cancellation token observed during bounded shaping.</summary>
    public System.Threading.CancellationToken CancellationToken;

    /// <summary>Creates baseline horizontal shaping defaults.</summary>
    public SvgShapingOptions()
    {
        ScriptTag = "DFLT";
        LanguageTag = "dflt";
        Features = new System.Collections.Generic.List<string>();
        VariationCoordinates = new System.Collections.Generic.List<SvgFontVariationCoordinate>();
        ShapingCallback = null;
        FontIdentifier = 0;
        EmSize = 16;
        Vertical = false;
        MaxGlyphs = 1000000;
        MaxLookups = 10000;
        MaxOperations = 10000000;
        MaxVariationAxes = 64;
        CancellationToken = System.Threading.CancellationToken.None;
    }
}

/// <summary>Contains one mapped, substituted, and positioned glyph.</summary>
public class SvgShapedGlyph
{
    /// <summary>The stable identity of the font face that owns the glyph identifier.</summary>
    public int FontIdentifier;
    /// <summary>The final glyph identifier.</summary>
    public int GlyphIndex;
    /// <summary>The first source scalar index represented by the glyph.</summary>
    public int ScalarStart;
    /// <summary>The represented source scalar count.</summary>
    public int ScalarCount;
    /// <summary>The first source UTF-16 index represented by the glyph.</summary>
    public int Utf16Start;
    /// <summary>The represented source UTF-16 code-unit length.</summary>
    public int Utf16Length;
    /// <summary>Whether character mapping selected glyph zero.</summary>
    public bool Missing;
    /// <summary>The glyph class read from GDEF.</summary>
    public SvgOpenTypeGlyphClass GlyphClass;
    /// <summary>The scaled horizontal advance.</summary>
    public double XAdvance;
    /// <summary>The scaled vertical advance.</summary>
    public double YAdvance;
    /// <summary>The scaled horizontal placement adjustment.</summary>
    public double XOffset;
    /// <summary>The scaled vertical placement adjustment.</summary>
    public double YOffset;
    /// <summary>The glyph index in this output list to which this glyph attaches, or minus one.</summary>
    public int AttachmentIndex;
    /// <summary>The one-based ligature component used for mark attachment, or zero.</summary>
    public int LigatureComponent;

    /// <summary>Creates an empty glyph suitable for a shaping callback response.</summary>
    public SvgShapedGlyph()
    {
        FontIdentifier = 0;
        GlyphIndex = 0;
        ScalarStart = 0;
        ScalarCount = 0;
        Utf16Start = 0;
        Utf16Length = 0;
        Missing = true;
        GlyphClass = SvgOpenTypeGlyphClass.Unclassified;
        XAdvance = 0;
        YAdvance = 0;
        XOffset = 0;
        YOffset = 0;
        AttachmentIndex = -1;
        LigatureComponent = 0;
    }
}

/// <summary>Contains glyphs, applied features, coordinates, and stable shaping status.</summary>
public class SvgShapingResult
{
    /// <summary>Whether baseline shaping completed.</summary>
    public bool Success;
    /// <summary>The stable shaping outcome.</summary>
    public SvgShapingError Error;
    /// <summary>A deterministic failure explanation, or empty string.</summary>
    public string Message;
    /// <summary>The normalized source text.</summary>
    public string Text;
    /// <summary>The final glyph sequence in logical order.</summary>
    public System.Collections.Generic.List<SvgShapedGlyph> Glyphs;
    /// <summary>The selected feature tags that had layout lookups.</summary>
    public System.Collections.Generic.List<string> AppliedFeatures;
    /// <summary>The selected lookup types not handled by the baseline engine.</summary>
    public System.Collections.Generic.List<int> UnsupportedLookupTypes;
    /// <summary>The retained font table tags that require an external shaper.</summary>
    public System.Collections.Generic.List<string> UnsupportedTableTags;
    /// <summary>The clamped and normalized variation coordinates.</summary>
    public System.Collections.Generic.List<SvgNormalizedVariationCoordinate> VariationCoordinates;
    /// <summary>The stable identity of the font face used by every output glyph.</summary>
    public int FontIdentifier;
    /// <summary>Why an external shaping callback was requested.</summary>
    public SvgShapingCallbackReason CallbackReason;
    /// <summary>Whether the script or retained tables require an external shaping callback.</summary>
    public bool RequiresCallback;
    /// <summary>Whether the configured shaping callback was invoked.</summary>
    public bool CallbackInvoked;
    /// <summary>Whether validated callback glyphs replaced the baseline glyphs.</summary>
    public bool UsedCallback;
    /// <summary>Whether baseline glyphs were retained because callback output was unavailable or invalid.</summary>
    public bool UsedDeterministicFallback;
    /// <summary>The callback response or deterministic fallback explanation.</summary>
    public string CallbackMessage;

    internal SvgShapingResult()
    {
        Success = false;
        Error = SvgShapingError.None;
        Message = "";
        Text = "";
        Glyphs = new System.Collections.Generic.List<SvgShapedGlyph>();
        AppliedFeatures = new System.Collections.Generic.List<string>();
        UnsupportedLookupTypes = new System.Collections.Generic.List<int>();
        UnsupportedTableTags = new System.Collections.Generic.List<string>();
        VariationCoordinates = new System.Collections.Generic.List<SvgNormalizedVariationCoordinate>();
        FontIdentifier = 0;
        CallbackReason = SvgShapingCallbackReason.None;
        RequiresCallback = false;
        CallbackInvoked = false;
        UsedCallback = false;
        UsedDeterministicFallback = false;
        CallbackMessage = "";
    }
}
