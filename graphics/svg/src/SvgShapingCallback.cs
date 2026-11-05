namespace svg;

internal static class SvgShapingCallbacks
{
    internal static SvgShapingResult Complete(SvgFont font, SvgUnicodeText text, SvgShapingOptions options, SvgShapingResult result)
    {
        SetFontIdentity(result.Glyphs, options.FontIdentifier);
        AddUnsupportedTables(font, result.UnsupportedTableTags);
        bool unsupportedScript = !BaselineScript(options.ScriptTag);
        bool unsupportedLayout = result.UnsupportedLookupTypes.Count != 0 || result.UnsupportedTableTags.Count != 0;
        result.CallbackReason = Reason(unsupportedScript, unsupportedLayout);
        result.RequiresCallback = result.CallbackReason != SvgShapingCallbackReason.None;
        if (!result.RequiresCallback) return result;
        if (options.ShapingCallback == null) return Fallback(result, "Unsupported shaping retained deterministic baseline glyphs because no callback was configured.");
        if (options.CancellationToken.IsCancellationRequested) return Cancel(result);
        int fontIdentifier = options.FontIdentifier;
        int maxGlyphs = options.MaxGlyphs;
        System.Threading.CancellationToken cancellationToken = options.CancellationToken;
        SvgShapingCallbackRequest request = Request(font, text, options, result);
        result.CallbackInvoked = true;
        SvgShapingCallbackResponse response = options.ShapingCallback(request);
        if (cancellationToken.IsCancellationRequested) return Cancel(result);
        if (response == null) return Fallback(result, "The shaping callback returned no response; deterministic baseline glyphs were retained.");
        if (!response.Success) return Fallback(result, response.Message == null || response.Message.Length == 0 ? "The shaping callback declined the request; deterministic baseline glyphs were retained." : response.Message);
        if (!ValidResponse(font, text, fontIdentifier, maxGlyphs, response)) return Fallback(result, "The shaping callback response was invalid; deterministic baseline glyphs were retained.");
        result.Glyphs = CopyGlyphs(response.Glyphs);
        result.UsedCallback = true;
        result.CallbackMessage = response.Message;
        return result;
    }

    static SvgShapingCallbackRequest Request(SvgFont font, SvgUnicodeText text, SvgShapingOptions options, SvgShapingResult result)
    {
        SvgShapingCallbackRequest request = new SvgShapingCallbackRequest(font);
        request.FontIdentifier = options.FontIdentifier;
        request.Text = text.Text;
        request.ScriptTag = options.ScriptTag;
        request.LanguageTag = options.LanguageTag;
        request.Features.AddRange(options.Features);
        request.VariationCoordinates = CopyCoordinates(result.VariationCoordinates);
        request.BaselineGlyphs = CopyGlyphs(result.Glyphs);
        request.UnsupportedLookupTypes.AddRange(result.UnsupportedLookupTypes);
        request.UnsupportedTableTags.AddRange(result.UnsupportedTableTags);
        request.Reason = result.CallbackReason;
        request.EmSize = options.EmSize;
        request.Vertical = options.Vertical;
        request.MaxGlyphs = options.MaxGlyphs;
        request.CancellationToken = options.CancellationToken;
        return request;
    }

    static bool ValidResponse(SvgFont font, SvgUnicodeText text, int fontIdentifier, int maxGlyphs, SvgShapingCallbackResponse response)
    {
        if (response.FontIdentifier != fontIdentifier || response.Glyphs == null || response.Message == null || response.Glyphs.Count > maxGlyphs) return false;
        int previousScalar = -1;
        int index = 0;
        while (index < response.Glyphs.Count)
        {
            SvgShapedGlyph glyph = response.Glyphs[index];
            if (glyph == null || glyph.FontIdentifier != fontIdentifier || glyph.GlyphIndex < 0 || glyph.GlyphIndex >= (font.MaximumProfile ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GlyphCount) return false;
            if (glyph.ScalarStart < previousScalar || glyph.ScalarStart < 0 || glyph.ScalarCount < 1 || (long)glyph.ScalarStart + glyph.ScalarCount > text.Scalars.Count) return false;
            SvgUnicodeScalar first = text.Scalars[glyph.ScalarStart];
            SvgUnicodeScalar last = text.Scalars[glyph.ScalarStart + glyph.ScalarCount - 1];
            int expectedLength = last.Utf16Start + last.Utf16Length - first.Utf16Start;
            if (glyph.Utf16Start != first.Utf16Start || glyph.Utf16Length != expectedLength) return false;
            if (!Finite(glyph.XAdvance) || !Finite(glyph.YAdvance) || !Finite(glyph.XOffset) || !Finite(glyph.YOffset)) return false;
            if (glyph.AttachmentIndex < -1 || glyph.AttachmentIndex >= response.Glyphs.Count || glyph.AttachmentIndex == index || glyph.LigatureComponent < 0) return false;
            int glyphClass = (int)glyph.GlyphClass;
            if (glyphClass < 0 || glyphClass > 4) return false;
            previousScalar = glyph.ScalarStart;
            index = index + 1;
        }
        return true;
    }

    static bool Finite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    static SvgShapingCallbackReason Reason(bool unsupportedScript, bool unsupportedLayout)
    {
        if (unsupportedScript && unsupportedLayout) return SvgShapingCallbackReason.UnsupportedScriptAndLayout;
        if (unsupportedScript) return SvgShapingCallbackReason.UnsupportedScript;
        if (unsupportedLayout) return SvgShapingCallbackReason.UnsupportedLayout;
        return SvgShapingCallbackReason.None;
    }

    static bool BaselineScript(string tag)
    {
        return tag == "DFLT" || tag == "latn" || tag == "grek" || tag == "cyrl" || tag == "armn" || tag == "geor" || tag == "hebr" || tag == "zyyy" || tag == "zinh" || tag == "hani" || tag == "kana" || tag == "hira" || tag == "hang" || tag == "bopo";
    }

    static void AddUnsupportedTables(SvgFont font, System.Collections.Generic.List<string> result)
    {
        AddTable(font, result, "morx");
        AddTable(font, result, "mort");
        AddTable(font, result, "kerx");
        AddTable(font, result, "ankr");
        AddTable(font, result, "trak");
        AddTable(font, result, "Silf");
        AddTable(font, result, "Glat");
        AddTable(font, result, "Gloc");
        AddTable(font, result, "Feat");
        AddTable(font, result, "Sill");
    }

    static void AddTable(SvgFont font, System.Collections.Generic.List<string> result, string tag)
    {
        if (font.FindTable(tag) != null) result.Add(tag);
    }

    static void SetFontIdentity(System.Collections.Generic.List<SvgShapedGlyph> glyphs, int identifier)
    {
        int index = 0;
        while (index < glyphs.Count)
        {
            glyphs[index].FontIdentifier = identifier;
            index = index + 1;
        }
    }

    static System.Collections.Generic.List<SvgShapedGlyph> CopyGlyphs(System.Collections.Generic.List<SvgShapedGlyph> source)
    {
        System.Collections.Generic.List<SvgShapedGlyph> result = new System.Collections.Generic.List<SvgShapedGlyph>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(CopyGlyph(source[index]));
            index = index + 1;
        }
        return result;
    }

    static SvgShapedGlyph CopyGlyph(SvgShapedGlyph source)
    {
        SvgShapedGlyph result = new SvgShapedGlyph();
        result.FontIdentifier = source.FontIdentifier;
        result.GlyphIndex = source.GlyphIndex;
        result.ScalarStart = source.ScalarStart;
        result.ScalarCount = source.ScalarCount;
        result.Utf16Start = source.Utf16Start;
        result.Utf16Length = source.Utf16Length;
        result.Missing = source.Missing;
        result.GlyphClass = source.GlyphClass;
        result.XAdvance = source.XAdvance;
        result.YAdvance = source.YAdvance;
        result.XOffset = source.XOffset;
        result.YOffset = source.YOffset;
        result.AttachmentIndex = source.AttachmentIndex;
        result.LigatureComponent = source.LigatureComponent;
        return result;
    }

    static System.Collections.Generic.List<SvgNormalizedVariationCoordinate> CopyCoordinates(System.Collections.Generic.List<SvgNormalizedVariationCoordinate> source)
    {
        System.Collections.Generic.List<SvgNormalizedVariationCoordinate> result = new System.Collections.Generic.List<SvgNormalizedVariationCoordinate>();
        int index = 0;
        while (index < source.Count)
        {
            SvgNormalizedVariationCoordinate coordinate = new SvgNormalizedVariationCoordinate();
            coordinate.AxisTag = source[index].AxisTag;
            coordinate.RequestedValue = source[index].RequestedValue;
            coordinate.Value = source[index].Value;
            coordinate.NormalizedValue = source[index].NormalizedValue;
            result.Add(coordinate);
            index = index + 1;
        }
        return result;
    }

    static SvgShapingResult Fallback(SvgShapingResult result, string message)
    {
        result.UsedDeterministicFallback = true;
        result.CallbackMessage = message;
        return result;
    }

    static SvgShapingResult Cancel(SvgShapingResult result)
    {
        result.Success = false;
        result.Error = SvgShapingError.Cancelled;
        result.Message = "OpenType shaping was cancelled during the shaping callback.";
        return result;
    }
}
