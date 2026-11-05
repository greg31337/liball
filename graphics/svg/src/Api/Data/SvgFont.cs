namespace svg;

/// <summary>Describes one validated top-level SFNT table record.</summary>
public class SvgOpenTypeTable
{
    /// <summary>The exact four-character table tag.</summary>
    public string Tag;
    /// <summary>The checksum stored in the table directory.</summary>
    public uint Checksum;
    /// <summary>The table byte offset from the beginning of the font.</summary>
    public int Offset;
    /// <summary>The unpadded table byte length.</summary>
    public int Length;
    /// <summary>The checksum calculated with zero padding and special <c>head</c> handling.</summary>
    public uint ComputedChecksum;
    /// <summary>Whether the stored and calculated checksums match.</summary>
    public bool ChecksumValid;

    internal SvgOpenTypeTable(string tag, uint checksum, int offset, int length)
    {
        Tag = tag;
        Checksum = checksum;
        Offset = offset;
        Length = length;
        ComputedChecksum = 0;
        ChecksumValid = false;
    }
}

/// <summary>Contains parsed global values from the required <c>head</c> table.</summary>
public class SvgOpenTypeHeadTable
{
    /// <summary>The major table version.</summary>
    public int MajorVersion;
    /// <summary>The minor table version.</summary>
    public int MinorVersion;
    /// <summary>The signed 16.16 font revision.</summary>
    public double FontRevision;
    /// <summary>The checksum adjustment stored in the font.</summary>
    public uint ChecksumAdjustment;
    /// <summary>The header flags.</summary>
    public int Flags;
    /// <summary>The design units per em.</summary>
    public int UnitsPerEm;
    /// <summary>The creation time in seconds since 1904-01-01.</summary>
    public long Created;
    /// <summary>The modification time in seconds since 1904-01-01.</summary>
    public long Modified;
    /// <summary>The minimum font-wide glyph x coordinate.</summary>
    public int XMin;
    /// <summary>The minimum font-wide glyph y coordinate.</summary>
    public int YMin;
    /// <summary>The maximum font-wide glyph x coordinate.</summary>
    public int XMax;
    /// <summary>The maximum font-wide glyph y coordinate.</summary>
    public int YMax;
    /// <summary>The Macintosh style flags.</summary>
    public int MacStyle;
    /// <summary>The smallest recommended pixels per em.</summary>
    public int LowestRecommendedPixelsPerEm;
    /// <summary>The deprecated font direction hint.</summary>
    public int FontDirectionHint;
    /// <summary>The <c>loca</c> offset format, zero for short and one for long.</summary>
    public int IndexToLocationFormat;
    /// <summary>The glyph data format.</summary>
    public int GlyphDataFormat;

    internal SvgOpenTypeHeadTable()
    {
    }
}

/// <summary>Contains parsed values from the required <c>hhea</c> table.</summary>
public class SvgOpenTypeHorizontalHeaderTable
{
    /// <summary>The typographic ascender in design units.</summary>
    public int Ascender;
    /// <summary>The typographic descender in design units.</summary>
    public int Descender;
    /// <summary>The typographic line gap in design units.</summary>
    public int LineGap;
    /// <summary>The maximum horizontal advance width.</summary>
    public int AdvanceWidthMaximum;
    /// <summary>The minimum left side bearing.</summary>
    public int MinimumLeftSideBearing;
    /// <summary>The minimum right side bearing.</summary>
    public int MinimumRightSideBearing;
    /// <summary>The maximum horizontal extent.</summary>
    public int MaximumExtent;
    /// <summary>The caret slope rise.</summary>
    public int CaretSlopeRise;
    /// <summary>The caret slope run.</summary>
    public int CaretSlopeRun;
    /// <summary>The caret offset.</summary>
    public int CaretOffset;
    /// <summary>The metric data format.</summary>
    public int MetricDataFormat;
    /// <summary>The number of long horizontal metrics.</summary>
    public int NumberOfHorizontalMetrics;

    internal SvgOpenTypeHorizontalHeaderTable()
    {
    }
}

/// <summary>Contains parsed limits from the required <c>maxp</c> table.</summary>
public class SvgOpenTypeMaximumProfileTable
{
    /// <summary>The packed table version.</summary>
    public uint Version;
    /// <summary>The total glyph count.</summary>
    public int GlyphCount;
    /// <summary>The maximum points in a simple glyph.</summary>
    public int MaximumPoints;
    /// <summary>The maximum contours in a simple glyph.</summary>
    public int MaximumContours;
    /// <summary>The maximum points in a composite glyph.</summary>
    public int MaximumCompositePoints;
    /// <summary>The maximum contours in a composite glyph.</summary>
    public int MaximumCompositeContours;
    /// <summary>The maximum top-level component count.</summary>
    public int MaximumComponentElements;
    /// <summary>The maximum composite recursion depth.</summary>
    public int MaximumComponentDepth;

    internal SvgOpenTypeMaximumProfileTable()
    {
    }
}

/// <summary>Contains one glyph's resolved horizontal advance and left side bearing.</summary>
public class SvgOpenTypeHorizontalMetric
{
    /// <summary>The zero-based glyph index.</summary>
    public int GlyphIndex;
    /// <summary>The horizontal advance width in design units.</summary>
    public int AdvanceWidth;
    /// <summary>The left side bearing in design units.</summary>
    public int LeftSideBearing;

    internal SvgOpenTypeHorizontalMetric(int glyphIndex, int advanceWidth, int leftSideBearing)
    {
        GlyphIndex = glyphIndex;
        AdvanceWidth = advanceWidth;
        LeftSideBearing = leftSideBearing;
    }
}

/// <summary>Contains parsed values from an optional <c>vhea</c> table.</summary>
public class SvgOpenTypeVerticalHeaderTable
{
    /// <summary>The packed 16.16 vertical-header version.</summary>
    public uint Version;
    /// <summary>The vertical typographic ascender in design units.</summary>
    public int Ascender;
    /// <summary>The vertical typographic descender in design units.</summary>
    public int Descender;
    /// <summary>The vertical typographic line gap in design units.</summary>
    public int LineGap;
    /// <summary>The maximum vertical advance height.</summary>
    public int AdvanceHeightMaximum;
    /// <summary>The minimum top side bearing.</summary>
    public int MinimumTopSideBearing;
    /// <summary>The minimum bottom side bearing.</summary>
    public int MinimumBottomSideBearing;
    /// <summary>The maximum vertical extent.</summary>
    public int MaximumExtent;
    /// <summary>The caret slope rise.</summary>
    public int CaretSlopeRise;
    /// <summary>The caret slope run.</summary>
    public int CaretSlopeRun;
    /// <summary>The caret offset.</summary>
    public int CaretOffset;
    /// <summary>The metric data format.</summary>
    public int MetricDataFormat;
    /// <summary>The number of long vertical metrics.</summary>
    public int NumberOfVerticalMetrics;

    internal SvgOpenTypeVerticalHeaderTable()
    {
    }
}

/// <summary>Contains one glyph's resolved vertical advance and top side bearing.</summary>
public class SvgOpenTypeVerticalMetric
{
    /// <summary>The zero-based glyph index.</summary>
    public int GlyphIndex;
    /// <summary>The vertical advance height in design units.</summary>
    public int AdvanceHeight;
    /// <summary>The top side bearing in design units.</summary>
    public int TopSideBearing;

    internal SvgOpenTypeVerticalMetric(int glyphIndex, int advanceHeight, int topSideBearing)
    {
        GlyphIndex = glyphIndex;
        AdvanceHeight = advanceHeight;
        TopSideBearing = topSideBearing;
    }
}

/// <summary>Contains a deterministic Unicode or variation-sequence glyph mapping.</summary>
public class SvgOpenTypeGlyphMapping
{
    /// <summary>The requested Unicode scalar value.</summary>
    public int CodePoint;
    /// <summary>The requested variation selector, or zero for ordinary mapping.</summary>
    public int VariationSelector;
    /// <summary>The mapped glyph index, including zero for the missing glyph.</summary>
    public int GlyphIndex;
    /// <summary>Whether the requested character or sequence has a nonzero glyph.</summary>
    public bool Found;
    /// <summary>Whether glyph zero was selected deterministically.</summary>
    public bool Missing;
    /// <summary>Whether a format-14 non-default mapping selected the glyph.</summary>
    public bool UsedNonDefaultVariation;
    /// <summary>Whether a format-14 default range delegated to the base Unicode map.</summary>
    public bool UsedDefaultVariation;
    /// <summary>The selected base or variation character-map descriptor.</summary>
    public SvgOpenTypeCmapRecord? CharacterMap;
    /// <summary>Whether mapping completed without invalid input or font data.</summary>
    public bool Success;
    /// <summary>The stable mapping outcome.</summary>
    public SvgOpenTypeMappingError Error;

    internal SvgOpenTypeGlyphMapping(int codePoint, int variationSelector)
    {
        CodePoint = codePoint;
        VariationSelector = variationSelector;
        GlyphIndex = 0;
        Found = false;
        Missing = true;
        UsedNonDefaultVariation = false;
        UsedDefaultVariation = false;
        CharacterMap = null;
        Success = false;
        Error = SvgOpenTypeMappingError.None;
    }
}

/// <summary>Contains one glyph's design-unit and scaled horizontal and vertical metrics.</summary>
public class SvgOpenTypeGlyphMetrics
{
    /// <summary>The zero-based glyph index.</summary>
    public int GlyphIndex;
    /// <summary>The font design units per em.</summary>
    public int UnitsPerEm;
    /// <summary>The requested output em size.</summary>
    public double EmSize;
    /// <summary>The multiplier from design units to the requested em size.</summary>
    public double Scale;
    /// <summary>The horizontal advance width in design units.</summary>
    public int AdvanceWidth;
    /// <summary>The left side bearing in design units.</summary>
    public int LeftSideBearing;
    /// <summary>The scaled horizontal advance width.</summary>
    public double ScaledAdvanceWidth;
    /// <summary>The scaled left side bearing.</summary>
    public double ScaledLeftSideBearing;
    /// <summary>Whether optional vertical metrics are available.</summary>
    public bool HasVerticalMetrics;
    /// <summary>The vertical advance height in design units, or zero when unavailable.</summary>
    public int AdvanceHeight;
    /// <summary>The top side bearing in design units, or zero when unavailable.</summary>
    public int TopSideBearing;
    /// <summary>The scaled vertical advance height, or zero when unavailable.</summary>
    public double ScaledAdvanceHeight;
    /// <summary>The scaled top side bearing, or zero when unavailable.</summary>
    public double ScaledTopSideBearing;
    /// <summary>Whether metric lookup and scaling completed.</summary>
    public bool Success;
    /// <summary>The stable metric outcome.</summary>
    public SvgOpenTypeMetricError Error;

    internal SvgOpenTypeGlyphMetrics(int glyphIndex, double emSize)
    {
        GlyphIndex = glyphIndex;
        UnitsPerEm = 0;
        EmSize = emSize;
        Scale = 0;
        AdvanceWidth = 0;
        LeftSideBearing = 0;
        ScaledAdvanceWidth = 0;
        ScaledLeftSideBearing = 0;
        HasVerticalMetrics = false;
        AdvanceHeight = 0;
        TopSideBearing = 0;
        ScaledAdvanceHeight = 0;
        ScaledTopSideBearing = 0;
        Success = false;
        Error = SvgOpenTypeMetricError.None;
    }
}

/// <summary>Contains selection and line metrics from the required <c>OS/2</c> table.</summary>
public class SvgOpenTypeOs2Table
{
    /// <summary>The table version from zero through five.</summary>
    public int Version;
    /// <summary>The average character width.</summary>
    public int AverageCharacterWidth;
    /// <summary>The font weight class.</summary>
    public int WeightClass;
    /// <summary>The font width class.</summary>
    public int WidthClass;
    /// <summary>The embedding and licensing flags.</summary>
    public int EmbeddingFlags;
    /// <summary>The family classification.</summary>
    public int FamilyClass;
    /// <summary>The exact four-character vendor identifier.</summary>
    public string VendorIdentifier;
    /// <summary>The font selection flags.</summary>
    public int SelectionFlags;
    /// <summary>The first encoded BMP character index.</summary>
    public int FirstCharacterIndex;
    /// <summary>The last encoded BMP character index.</summary>
    public int LastCharacterIndex;
    /// <summary>The preferred typographic ascender.</summary>
    public int TypographicAscender;
    /// <summary>The preferred typographic descender.</summary>
    public int TypographicDescender;
    /// <summary>The preferred typographic line gap.</summary>
    public int TypographicLineGap;
    /// <summary>The Windows ascent.</summary>
    public int WindowsAscent;
    /// <summary>The Windows descent.</summary>
    public int WindowsDescent;
    /// <summary>The x-height when present, otherwise zero.</summary>
    public int XHeight;
    /// <summary>The capital height when present, otherwise zero.</summary>
    public int CapitalHeight;
    /// <summary>Whether the table carries x-height and capital-height fields.</summary>
    public bool HasHeightMetrics;

    internal SvgOpenTypeOs2Table()
    {
        VendorIdentifier = "";
    }
}

/// <summary>Contains one bounded record from the required <c>name</c> table.</summary>
public class SvgOpenTypeNameRecord
{
    /// <summary>The platform identifier.</summary>
    public int PlatformIdentifier;
    /// <summary>The platform encoding identifier.</summary>
    public int EncodingIdentifier;
    /// <summary>The numeric language identifier.</summary>
    public int LanguageIdentifier;
    /// <summary>The logical name identifier.</summary>
    public int NameIdentifier;
    /// <summary>The string byte length.</summary>
    public int Length;
    /// <summary>The byte offset within the string storage.</summary>
    public int Offset;
    /// <summary>The decoded string, or an empty string for an unsupported platform encoding.</summary>
    public string Value;
    /// <summary>The decoded format-one language tag, or an empty string.</summary>
    public string LanguageTag;
    /// <summary>Whether the record string was decoded.</summary>
    public bool Decoded;

    internal SvgOpenTypeNameRecord()
    {
        Value = "";
        LanguageTag = "";
        Decoded = false;
    }
}

/// <summary>Describes one validated encoding subtable in the required <c>cmap</c> table.</summary>
public class SvgOpenTypeCmapRecord
{
    /// <summary>The platform identifier.</summary>
    public int PlatformIdentifier;
    /// <summary>The platform encoding identifier.</summary>
    public int EncodingIdentifier;
    /// <summary>The subtable byte offset relative to the <c>cmap</c> table.</summary>
    public int Offset;
    /// <summary>The mapping subtable format.</summary>
    public int Format;
    /// <summary>The validated mapping subtable byte length.</summary>
    public int Length;
    /// <summary>The mapping language value when the format defines one.</summary>
    public uint Language;
    /// <summary>Whether the mapping format defines a language field.</summary>
    public bool HasLanguage;

    internal SvgOpenTypeCmapRecord()
    {
    }
}

/// <summary>Contains the fixed metadata header from the required <c>post</c> table.</summary>
public class SvgOpenTypePostTable
{
    /// <summary>The packed table version.</summary>
    public uint Version;
    /// <summary>The signed italic angle in degrees.</summary>
    public double ItalicAngle;
    /// <summary>The underline position in design units.</summary>
    public int UnderlinePosition;
    /// <summary>The underline thickness in design units.</summary>
    public int UnderlineThickness;
    /// <summary>Whether the font declares fixed-pitch advances.</summary>
    public bool FixedPitch;

    internal SvgOpenTypePostTable()
    {
    }
}

/// <summary>Contains one bounded parsed SFNT/OpenType font and its core metadata.</summary>
public class SvgFont
{
    /// <summary>The font bytes retained according to the read ownership option.</summary>
    public byte[]? Bytes;
    /// <summary>Whether <c>Bytes</c> is an independent copy owned by this font.</summary>
    public bool OwnsBytes;
    /// <summary>The SFNT version stored in the table directory.</summary>
    public uint SfntVersion;
    /// <summary>The identified outline table family.</summary>
    public SvgOpenTypeOutlineKind OutlineKind;
    /// <summary>The validated top-level table records in tag order.</summary>
    public System.Collections.Generic.List<SvgOpenTypeTable> Tables;
    /// <summary>The parsed required font header.</summary>
    public SvgOpenTypeHeadTable? Head;
    /// <summary>The parsed required horizontal header.</summary>
    public SvgOpenTypeHorizontalHeaderTable? HorizontalHeader;
    /// <summary>The parsed required maximum profile.</summary>
    public SvgOpenTypeMaximumProfileTable? MaximumProfile;
    /// <summary>The dense horizontal metrics indexed by glyph ID.</summary>
    public System.Collections.Generic.List<SvgOpenTypeHorizontalMetric> HorizontalMetrics;
    /// <summary>The parsed optional vertical header, or <c>null</c>.</summary>
    public SvgOpenTypeVerticalHeaderTable? VerticalHeader;
    /// <summary>The dense optional vertical metrics indexed by glyph ID.</summary>
    public System.Collections.Generic.List<SvgOpenTypeVerticalMetric> VerticalMetrics;
    /// <summary>The parsed required OS/2 metadata and metrics.</summary>
    public SvgOpenTypeOs2Table? Os2;
    /// <summary>The parsed bounded naming records.</summary>
    public System.Collections.Generic.List<SvgOpenTypeNameRecord> Names;
    /// <summary>The validated character-map encoding records.</summary>
    public System.Collections.Generic.List<SvgOpenTypeCmapRecord> CharacterMaps;
    /// <summary>The parsed required PostScript metadata header.</summary>
    public SvgOpenTypePostTable? Post;
    /// <summary>The selected typographic or legacy family name.</summary>
    public string FamilyName;
    /// <summary>The selected typographic or legacy subfamily name.</summary>
    public string SubfamilyName;
    /// <summary>The selected full font name.</summary>
    public string FullName;
    /// <summary>The selected PostScript name.</summary>
    public string PostScriptName;
    /// <summary>The selected version name.</summary>
    public string VersionName;
    /// <summary>The ordered parsing diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether all required parsing and validation completed.</summary>
    public bool Success;

    internal SvgFont(byte[]? bytes, bool ownsBytes)
    {
        Bytes = bytes;
        OwnsBytes = ownsBytes;
        SfntVersion = 0;
        OutlineKind = SvgOpenTypeOutlineKind.Unknown;
        Tables = new System.Collections.Generic.List<SvgOpenTypeTable>();
        Head = null;
        HorizontalHeader = null;
        MaximumProfile = null;
        HorizontalMetrics = new System.Collections.Generic.List<SvgOpenTypeHorizontalMetric>();
        VerticalHeader = null;
        VerticalMetrics = new System.Collections.Generic.List<SvgOpenTypeVerticalMetric>();
        Os2 = null;
        Names = new System.Collections.Generic.List<SvgOpenTypeNameRecord>();
        CharacterMaps = new System.Collections.Generic.List<SvgOpenTypeCmapRecord>();
        Post = null;
        FamilyName = "";
        SubfamilyName = "";
        FullName = "";
        PostScriptName = "";
        VersionName = "";
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Success = false;
    }

    /// <summary>Finds a top-level table by its exact four-character tag.</summary>
    /// <param name="tag">The exact case-sensitive tag.</param>
    /// <returns>The table record, or <c>null</c>.</returns>
    internal SvgOpenTypeTable? FindTable(string? tag)
    {
        if (tag == null) return null;
        int index = 0;
        while (index < Tables.Count)
        {
            if (Tables[index].Tag == tag) return Tables[index];
            index = index + 1;
        }
        return null;
    }

    /// <summary>Selects the best decoded record for one logical name identifier.</summary>
    /// <param name="nameIdentifier">The OpenType name identifier.</param>
    /// <returns>The selected string, or an empty string.</returns>
    internal string FindName(int nameIdentifier)
    {
        int bestScore = -1;
        string best = "";
        int index = 0;
        while (index < Names.Count)
        {
            SvgOpenTypeNameRecord record = Names[index];
            if (record.Decoded && record.NameIdentifier == nameIdentifier)
            {
                int score = 0;
                if (record.PlatformIdentifier == 3) score = score + 20;
                else if (record.PlatformIdentifier == 0) score = score + 10;
                if (record.LanguageIdentifier == 1033) score = score + 5;
                if (record.LanguageTag == "en" || record.LanguageTag.StartsWith("en-", System.StringComparison.OrdinalIgnoreCase)) score = score + 5;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = record.Value;
                }
            }
            index = index + 1;
        }
        return best;
    }

    /// <summary>Maps one Unicode scalar through the font's selected Unicode character map.</summary>
    /// <param name="codePoint">The Unicode scalar value to map.</param>
    /// <returns>The mapped glyph or a deterministic missing-glyph result.</returns>
    internal SvgOpenTypeGlyphMapping MapCodePoint(int codePoint)
    {
        return new SvgOpenTypeMapping().MapCodePoint(this, codePoint);
    }

    /// <summary>Maps a Unicode variation sequence through format 14 and the base Unicode character map.</summary>
    /// <param name="codePoint">The base Unicode scalar value.</param>
    /// <param name="variationSelector">A Unicode variation-selector scalar value.</param>
    /// <returns>The mapped glyph or a deterministic missing-glyph result.</returns>
    internal SvgOpenTypeGlyphMapping MapVariationSequence(int codePoint, int variationSelector)
    {
        return new SvgOpenTypeMapping().MapVariationSequence(this, codePoint, variationSelector);
    }

    /// <summary>Gets one glyph's metrics scaled from design units to an output em size.</summary>
    /// <param name="glyphIndex">The zero-based glyph index.</param>
    /// <param name="emSize">The finite nonnegative output em size.</param>
    /// <returns>The design-unit and scaled metrics.</returns>
    internal SvgOpenTypeGlyphMetrics GetGlyphMetrics(int glyphIndex, double emSize)
    {
        return new SvgOpenTypeMapping().GetGlyphMetrics(this, glyphIndex, emSize);
    }

    /// <summary>Decodes one unhinted TrueType glyph outline using bounded defaults.</summary>
    /// <param name="glyphIndex">The zero-based glyph index.</param>
    /// <returns>The expanded outline and quadratic SVG path.</returns>
    internal SvgTrueTypeGlyphResult DecodeTrueTypeGlyph(int glyphIndex)
    {
        return new SvgTrueTypeGlyphDecoder().DecodeWithFontAndGlyphIndex(this, glyphIndex);
    }

    /// <summary>Decodes one unhinted TrueType glyph outline using explicit security limits.</summary>
    /// <param name="glyphIndex">The zero-based glyph index.</param>
    /// <param name="options">The bounded glyph expansion options.</param>
    /// <returns>The expanded outline and quadratic SVG path.</returns>
    internal SvgTrueTypeGlyphResult DecodeTrueTypeGlyphWithOptions(int glyphIndex, SvgTrueTypeGlyphOptions options)
    {
        return new SvgTrueTypeGlyphDecoder().DecodeWithFontAndGlyphIndexAndOptions(this, glyphIndex, options);
    }

    /// <summary>Decodes one unhinted CFF or CFF2 glyph outline using bounded defaults.</summary>
    /// <param name="glyphIndex">The zero-based glyph index.</param>
    /// <returns>The decoded outline, stable failure, or caller-fallback classification.</returns>
    internal SvgCffGlyphResult DecodeCffGlyph(int glyphIndex)
    {
        return new SvgCffOutlineDecoder().DecodeWithFontAndGlyphIndex(this, glyphIndex);
    }

    /// <summary>Decodes one unhinted CFF or CFF2 glyph outline using explicit security limits.</summary>
    /// <param name="glyphIndex">The zero-based glyph index.</param>
    /// <param name="options">The bounded operand and subroutine options.</param>
    /// <returns>The decoded outline, stable failure, or caller-fallback classification.</returns>
    internal SvgCffGlyphResult DecodeCffGlyphWithOptions(int glyphIndex, SvgCffOutlineOptions options)
    {
        return new SvgCffOutlineDecoder().DecodeWithFontAndGlyphIndexAndOptions(this, glyphIndex, options);
    }
}

/// <summary>Configures bounded SFNT/OpenType parsing and byte ownership.</summary>
public class SvgOpenTypeReadOptions
{
    /// <summary>The font-specific security limits.</summary>
    public SvgFontLimits Limits;
    /// <summary>Whether raw SFNT input is copied; reconstructed web-font bytes are always owned.</summary>
    public bool CopyInputData;
    /// <summary>Whether table and whole-font checksums are required to match.</summary>
    public bool ValidateChecksums;
    /// <summary>The optional diagnostic source span for the font resource.</summary>
    public SvgSourceSpan? Source;
    /// <summary>The cancellation token observed during bounded loops.</summary>
    public System.Threading.CancellationToken CancellationToken;

    /// <summary>Creates secure bounded parsing defaults without copying caller bytes.</summary>
    public SvgOpenTypeReadOptions()
    {
        Limits = new SvgFontLimits();
        CopyInputData = false;
        ValidateChecksums = true;
        Source = null;
        CancellationToken = System.Threading.CancellationToken.None;
    }
}

/// <summary>Contains a completed or failed bounded SFNT/OpenType read.</summary>
public class SvgOpenTypeReadResult
{
    /// <summary>The complete or partially populated font model.</summary>
    public SvgFont? Font;
    /// <summary>Whether all required font data was validated and parsed.</summary>
    public bool Success;
    /// <summary>The stable parsing outcome.</summary>
    public SvgOpenTypeError Error;
    /// <summary>The ordered parsing diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    internal SvgOpenTypeReadResult(SvgFont? font)
    {
        Font = font;
        Success = false;
        Error = SvgOpenTypeError.None;
        Diagnostics = font == null ? new System.Collections.Generic.List<SvgDiagnostic>() : font.Diagnostics;
    }
}
