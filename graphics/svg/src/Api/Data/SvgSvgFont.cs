namespace svg;

/// <summary>Configures bounded legacy SVG font parsing.</summary>
public class SvgSvgFontOptions
{
    /// <summary>The maximum number of glyph and missing-glyph elements.</summary>
    public int MaxGlyphs;
    /// <summary>The maximum number of horizontal and vertical kerning rules.</summary>
    public int MaxKerningRules;
    /// <summary>The maximum command count in one glyph path.</summary>
    public int MaxPathSegments;
    /// <summary>The maximum characters in one numeric or range token.</summary>
    public int MaxTokenCharacters;
    /// <summary>The cancellation token observed during bounded loops.</summary>
    public System.Threading.CancellationToken CancellationToken;

    /// <summary>Creates secure bounded legacy SVG font defaults.</summary>
    public SvgSvgFontOptions()
    {
        MaxGlyphs = 65535;
        MaxKerningRules = 100000;
        MaxPathSegments = 1000000;
        MaxTokenCharacters = 1024;
        CancellationToken = System.Threading.CancellationToken.None;
    }
}

/// <summary>Contains one parsed legacy SVG glyph and its effective metrics.</summary>
public class SvgSvgGlyph
{
    /// <summary>The stable zero-based document order among ordinary glyphs.</summary>
    public int Index;
    /// <summary>The source glyph or missing-glyph element.</summary>
    public SvgNode? SourceNode;
    /// <summary>The exact decoded Unicode sequence, or empty string.</summary>
    public string Unicode;
    /// <summary>Whether a Unicode attribute was present.</summary>
    public bool HasUnicode;
    /// <summary>The ordered comma-separated glyph names.</summary>
    public System.Collections.Generic.List<string> Names;
    /// <summary>The supported inline orientation.</summary>
    public SvgSvgFontOrientation Orientation;
    /// <summary>The declared contextual Arabic form.</summary>
    public SvgSvgArabicForm ArabicForm;
    /// <summary>The ordered BCP 47 language filters.</summary>
    public System.Collections.Generic.List<string> Languages;
    /// <summary>The parsed path-data outline, including an empty outline.</summary>
    public SvgPathData? Outline;
    /// <summary>Whether element child content supplements the path-data outline.</summary>
    public bool HasChildContent;
    /// <summary>The effective nonnegative horizontal advance.</summary>
    public double HorizontalAdvance;
    /// <summary>The effective horizontal origin X coordinate.</summary>
    public double HorizontalOriginX;
    /// <summary>The effective horizontal origin Y coordinate.</summary>
    public double HorizontalOriginY;
    /// <summary>The effective vertical origin X coordinate.</summary>
    public double VerticalOriginX;
    /// <summary>The effective vertical origin Y coordinate.</summary>
    public double VerticalOriginY;
    /// <summary>The effective nonnegative vertical advance.</summary>
    public double VerticalAdvance;
    /// <summary>Whether this is the font's missing-glyph record.</summary>
    public bool IsMissingGlyph;

    internal SvgSvgGlyph()
    {
        Index = -1;
        SourceNode = null;
        Unicode = "";
        HasUnicode = false;
        Names = new System.Collections.Generic.List<string>();
        Orientation = SvgSvgFontOrientation.Both;
        ArabicForm = SvgSvgArabicForm.None;
        Languages = new System.Collections.Generic.List<string>();
        Outline = null;
        HasChildContent = false;
        HorizontalAdvance = 0;
        HorizontalOriginX = 0;
        HorizontalOriginY = 0;
        VerticalOriginX = 0;
        VerticalOriginY = 0;
        VerticalAdvance = 0;
        IsMissingGlyph = false;
    }
}

/// <summary>Contains one parsed horizontal or vertical SVG font kerning rule.</summary>
public class SvgSvgKerning
{
    /// <summary>The stable zero-based order within its orientation.</summary>
    public int Index;
    /// <summary>The source hkern or vkern element.</summary>
    public SvgNode? SourceNode;
    /// <summary>Whether this rule adjusts vertical advances.</summary>
    public bool Vertical;
    /// <summary>The first-side Unicode scalar ranges.</summary>
    public System.Collections.Generic.List<SvgUnicodeRange> FirstUnicodeRanges;
    /// <summary>The first-side glyph names.</summary>
    public System.Collections.Generic.List<string> FirstGlyphNames;
    /// <summary>The second-side Unicode scalar ranges.</summary>
    public System.Collections.Generic.List<SvgUnicodeRange> SecondUnicodeRanges;
    /// <summary>The second-side glyph names.</summary>
    public System.Collections.Generic.List<string> SecondGlyphNames;
    /// <summary>The amount subtracted from inter-glyph spacing in font coordinates.</summary>
    public double Adjustment;

    internal SvgSvgKerning()
    {
        Index = -1;
        SourceNode = null;
        Vertical = false;
        FirstUnicodeRanges = new System.Collections.Generic.List<SvgUnicodeRange>();
        FirstGlyphNames = new System.Collections.Generic.List<string>();
        SecondUnicodeRanges = new System.Collections.Generic.List<SvgUnicodeRange>();
        SecondGlyphNames = new System.Collections.Generic.List<string>();
        Adjustment = 0;
    }

    /// <summary>Determines whether this rule selects an ordered glyph pair.</summary>
    /// <param name="first">The visually first glyph.</param>
    /// <param name="second">The visually second glyph.</param>
    /// <returns><c>true</c> when both selector sides match.</returns>
    internal bool Matches(SvgSvgGlyph? first, SvgSvgGlyph? second)
    {
        if (first == null || second == null) return false;
        return MatchesSide(first, FirstUnicodeRanges, FirstGlyphNames) && MatchesSide(second, SecondUnicodeRanges, SecondGlyphNames);
    }

    internal static bool MatchesSide(SvgSvgGlyph glyph, System.Collections.Generic.List<SvgUnicodeRange> ranges, System.Collections.Generic.List<string> names)
    {
        int nameIndex = 0;
        while (nameIndex < glyph.Names.Count)
        {
            int selectorIndex = 0;
            while (selectorIndex < names.Count)
            {
                if (glyph.Names[nameIndex] == names[selectorIndex]) return true;
                selectorIndex = selectorIndex + 1;
            }
            nameIndex = nameIndex + 1;
        }
        int scalar = SingleScalar(glyph.Unicode);
        if (!glyph.HasUnicode || scalar < 0) return false;
        int rangeIndex = 0;
        while (rangeIndex < ranges.Count)
        {
            if (ranges[rangeIndex].Contains(scalar)) return true;
            rangeIndex = rangeIndex + 1;
        }
        return false;
    }

    internal static int SingleScalar(string value)
    {
        if (value.Length == 1)
        {
            int scalar = value[0];
            if (scalar < 0xD800 || scalar > 0xDFFF) return scalar;
            return -1;
        }
        if (value.Length == 2 && value[0] >= 0xD800 && value[0] <= 0xDBFF && value[1] >= 0xDC00 && value[1] <= 0xDFFF)
        {
            return 0x10000 + ((value[0] - 0xD800) << 10) + value[1] - 0xDC00;
        }
        return -1;
    }
}

/// <summary>Contains an ordered legacy SVG glyph selection.</summary>
public class SvgSvgGlyphMatch
{
    /// <summary>Whether the lookup arguments were valid.</summary>
    public bool Success;
    /// <summary>Whether an ordinary glyph matched.</summary>
    public bool Found;
    /// <summary>Whether the font's missing-glyph record was selected.</summary>
    public bool UsedMissingGlyph;
    /// <summary>The selected ordinary or missing glyph, or <c>null</c>.</summary>
    public SvgSvgGlyph? Glyph;
    /// <summary>The number of consumed UTF-16 code units.</summary>
    public int ConsumedLength;

    internal SvgSvgGlyphMatch()
    {
        Success = false;
        Found = false;
        UsedMissingGlyph = false;
        Glyph = null;
        ConsumedLength = 0;
    }
}

/// <summary>Contains one legacy SVG font kerning lookup.</summary>
public class SvgSvgKerningResult
{
    /// <summary>Whether the glyph arguments belonged to the font.</summary>
    public bool Success;
    /// <summary>Whether a kerning rule matched.</summary>
    public bool Found;
    /// <summary>The amount to subtract from spacing, or zero.</summary>
    public double Adjustment;
    /// <summary>The selected rule, or <c>null</c>.</summary>
    public SvgSvgKerning? Rule;

    internal SvgSvgKerningResult()
    {
        Success = false;
        Found = false;
        Adjustment = 0;
        Rule = null;
    }
}

/// <summary>Contains parsed SVG 1.1 font metrics, glyphs, outlines, and kerning.</summary>
public class SvgSvgFont
{
    /// <summary>The source font element.</summary>
    public SvgNode? SourceNode;
    /// <summary>The source font-face element.</summary>
    public SvgNode? FontFaceNode;
    /// <summary>The declared font family, or empty string.</summary>
    public string Family;
    /// <summary>The positive font coordinate units per em.</summary>
    public double UnitsPerEm;
    /// <summary>The font ascent in font coordinates.</summary>
    public double Ascent;
    /// <summary>The font descent in font coordinates.</summary>
    public double Descent;
    /// <summary>The default horizontal origin X coordinate.</summary>
    public double HorizontalOriginX;
    /// <summary>The default horizontal origin Y coordinate.</summary>
    public double HorizontalOriginY;
    /// <summary>The required default nonnegative horizontal advance.</summary>
    public double HorizontalAdvance;
    /// <summary>The default vertical origin X coordinate.</summary>
    public double VerticalOriginX;
    /// <summary>The default vertical origin Y coordinate.</summary>
    public double VerticalOriginY;
    /// <summary>The default nonnegative vertical advance.</summary>
    public double VerticalAdvance;
    /// <summary>The ordinary glyphs in document order.</summary>
    public System.Collections.Generic.List<SvgSvgGlyph> Glyphs;
    /// <summary>The optional missing-glyph record.</summary>
    public SvgSvgGlyph? MissingGlyph;
    /// <summary>The horizontal kerning rules in document order.</summary>
    public System.Collections.Generic.List<SvgSvgKerning> HorizontalKerning;
    /// <summary>The vertical kerning rules in document order.</summary>
    public System.Collections.Generic.List<SvgSvgKerning> VerticalKerning;

    internal SvgSvgFont()
    {
        SourceNode = null;
        FontFaceNode = null;
        Family = "";
        UnitsPerEm = 1000;
        Ascent = 1000;
        Descent = 1000;
        HorizontalOriginX = 0;
        HorizontalOriginY = 0;
        HorizontalAdvance = 0;
        VerticalOriginX = 0;
        VerticalOriginY = 1000;
        VerticalAdvance = 1000;
        Glyphs = new System.Collections.Generic.List<SvgSvgGlyph>();
        MissingGlyph = null;
        HorizontalKerning = new System.Collections.Generic.List<SvgSvgKerning>();
        VerticalKerning = new System.Collections.Generic.List<SvgSvgKerning>();
    }

    /// <summary>Selects the first eligible document-order glyph for a text prefix.</summary>
    /// <param name="text">The UTF-16 source text.</param>
    /// <param name="offset">The UTF-16 lookup offset.</param>
    /// <param name="orientation">The requested inline orientation.</param>
    /// <param name="language">The requested BCP 47 language, or empty string.</param>
    /// <param name="arabicForm">The requested contextual Arabic form.</param>
    /// <returns>The ordinary or missing-glyph selection and consumed length.</returns>
    internal SvgSvgGlyphMatch MatchGlyph(string? text, int offset, SvgSvgFontOrientation orientation, string? language, SvgSvgArabicForm arabicForm)
    {
        SvgSvgGlyphMatch result = new SvgSvgGlyphMatch();
        if (text == null || language == null || offset < 0 || offset >= text.Length || orientation < SvgSvgFontOrientation.Both || orientation > SvgSvgFontOrientation.Vertical || arabicForm < SvgSvgArabicForm.None || arabicForm > SvgSvgArabicForm.Isolated)
        {
            return result;
        }
        int index = 0;
        while (index < Glyphs.Count)
        {
            SvgSvgGlyph glyph = Glyphs[index];
            if (glyph.HasUnicode && StartsWith(text, offset, glyph.Unicode) && Eligible(glyph, orientation, language, arabicForm))
            {
                result.Success = true;
                result.Found = true;
                result.Glyph = glyph;
                result.ConsumedLength = glyph.Unicode.Length;
                return result;
            }
            index = index + 1;
        }
        result.Success = true;
        result.Glyph = MissingGlyph;
        result.UsedMissingGlyph = MissingGlyph != null;
        result.ConsumedLength = ScalarLength(text, offset);
        return result;
    }

    /// <summary>Finds the first document-order glyph with an exact glyph name.</summary>
    /// <param name="name">The case-sensitive glyph name.</param>
    /// <returns>The first matching glyph, or <c>null</c>.</returns>
    internal SvgSvgGlyph? FindGlyphByName(string? name)
    {
        if (name == null || name.Length == 0) return null;
        int glyphIndex = 0;
        while (glyphIndex < Glyphs.Count)
        {
            int nameIndex = 0;
            while (nameIndex < Glyphs[glyphIndex].Names.Count)
            {
                if (Glyphs[glyphIndex].Names[nameIndex] == name) return Glyphs[glyphIndex];
                nameIndex = nameIndex + 1;
            }
            glyphIndex = glyphIndex + 1;
        }
        return null;
    }

    /// <summary>Finds the path-data outline of the first glyph with an exact name.</summary>
    /// <param name="name">The case-sensitive glyph name.</param>
    /// <returns>The parsed outline, or <c>null</c>.</returns>
    internal SvgPathData? FindOutline(string name)
    {
        SvgSvgGlyph? glyph = FindGlyphByName(name);
        return glyph == null ? null : glyph.Outline;
    }

    /// <summary>Finds the first document-order horizontal or vertical kerning adjustment.</summary>
    /// <param name="first">The visually first glyph.</param>
    /// <param name="second">The visually second glyph.</param>
    /// <param name="vertical">Whether vertical kerning is requested.</param>
    /// <returns>The matching adjustment or a successful zero adjustment.</returns>
    internal SvgSvgKerningResult GetKerning(SvgSvgGlyph first, SvgSvgGlyph second, bool vertical)
    {
        SvgSvgKerningResult result = new SvgSvgKerningResult();
        if (!Contains(first) || !Contains(second)) return result;
        result.Success = true;
        System.Collections.Generic.List<SvgSvgKerning> rules = vertical ? VerticalKerning : HorizontalKerning;
        int index = 0;
        while (index < rules.Count)
        {
            if (rules[index].Matches(first, second))
            {
                result.Found = true;
                result.Adjustment = rules[index].Adjustment;
                result.Rule = rules[index];
                return result;
            }
            index = index + 1;
        }
        return result;
    }

    internal bool Contains(SvgSvgGlyph glyph)
    {
        if (glyph == null) return false;
        if (object.ReferenceEquals(glyph, MissingGlyph)) return true;
        int index = 0;
        while (index < Glyphs.Count)
        {
            if (object.ReferenceEquals(glyph, Glyphs[index])) return true;
            index = index + 1;
        }
        return false;
    }

    internal static bool StartsWith(string text, int offset, string value)
    {
        if (value.Length == 0 || offset + value.Length > text.Length) return false;
        int index = 0;
        while (index < value.Length)
        {
            if (text[offset + index] != value[index]) return false;
            index = index + 1;
        }
        return true;
    }

    internal static bool Eligible(SvgSvgGlyph glyph, SvgSvgFontOrientation orientation, string language, SvgSvgArabicForm arabicForm)
    {
        if (orientation != SvgSvgFontOrientation.Both && glyph.Orientation != SvgSvgFontOrientation.Both && glyph.Orientation != orientation) return false;
        if (arabicForm == SvgSvgArabicForm.None)
        {
            if (glyph.ArabicForm != SvgSvgArabicForm.None) return false;
        }
        else if (glyph.ArabicForm != SvgSvgArabicForm.None && glyph.ArabicForm != arabicForm) return false;
        if (glyph.Languages.Count == 0) return true;
        if (language.Length == 0) return false;
        int index = 0;
        while (index < glyph.Languages.Count)
        {
            string entry = glyph.Languages[index];
            if (string.Equals(language, entry, System.StringComparison.OrdinalIgnoreCase)) return true;
            if (entry.Length > language.Length && entry.StartsWith(language, System.StringComparison.OrdinalIgnoreCase) && entry[language.Length] == '-') return true;
            index = index + 1;
        }
        return false;
    }

    internal static int ScalarLength(string text, int offset)
    {
        if (text[offset] >= 0xD800 && text[offset] <= 0xDBFF && offset + 1 < text.Length && text[offset + 1] >= 0xDC00 && text[offset + 1] <= 0xDFFF) return 2;
        return 1;
    }
}

/// <summary>Contains a completed or failed legacy SVG font read.</summary>
public class SvgSvgFontReadResult
{
    /// <summary>The complete or partially populated font model.</summary>
    public SvgSvgFont Font;
    /// <summary>Whether all supported font data parsed successfully.</summary>
    public bool Success;
    /// <summary>The stable parsing outcome.</summary>
    public SvgSvgFontError Error;
    /// <summary>The ordered parsing diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    internal SvgSvgFontReadResult()
    {
        Font = new SvgSvgFont();
        Success = false;
        Error = SvgSvgFontError.None;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
    }
}
