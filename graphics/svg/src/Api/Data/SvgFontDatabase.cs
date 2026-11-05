namespace svg;

/// <summary>Defines one inclusive Unicode scalar range.</summary>
public class SvgUnicodeRange
{
    /// <summary>The first included Unicode scalar.</summary>
    public int Start;
    /// <summary>The last included Unicode scalar.</summary>
    public int End;

    /// <summary>Creates one inclusive Unicode scalar range.</summary>
    /// <param name="start">The first included scalar.</param>
    /// <param name="end">The last included scalar.</param>
    public SvgUnicodeRange(int start, int end)
    {
        if (!ValidScalar(start) || !ValidScalar(end) || end < start) throw new System.ArgumentOutOfRangeException("start");
        Start = start;
        End = end;
    }

    /// <summary>Determines whether the range includes a Unicode scalar.</summary>
    /// <param name="codePoint">The Unicode scalar to test.</param>
    /// <returns><c>true</c> when the scalar is included.</returns>
    internal bool Contains(int codePoint)
    {
        return ValidScalar(codePoint) && codePoint >= Start && codePoint <= End;
    }

    internal static bool ValidScalar(int codePoint)
    {
        return codePoint >= 0 && codePoint <= 0x10FFFF && (codePoint < 0xD800 || codePoint > 0xDFFF);
    }
}

/// <summary>Describes one caller byte, URL, or local font source.</summary>
public class SvgFontSource
{
    /// <summary>The source kind.</summary>
    public SvgFontSourceKind Kind;
    /// <summary>The resolved URL, local name, or empty string.</summary>
    public string Value;
    /// <summary>The declared format or media type, or empty string.</summary>
    public string Format;
    /// <summary>The source span that declared the source, or <c>null</c>.</summary>
    public SvgSourceSpan? Source;

    /// <summary>Creates one font source descriptor.</summary>
    /// <param name="kind">The source kind.</param>
    /// <param name="value">The resolved URL, local name, or empty string.</param>
    /// <param name="format">The declared format or media type, or empty string.</param>
    /// <param name="source">The source span, or <c>null</c>.</param>
    public SvgFontSource(SvgFontSourceKind kind, string value, string format, SvgSourceSpan? source)
    {
        if (value == null) throw new System.ArgumentNullException("value");
        if (format == null) throw new System.ArgumentNullException("format");
        Kind = kind;
        Value = value;
        Format = format;
        Source = source;
    }
}

/// <summary>Supplies font-face descriptors for caller or resolved font bytes.</summary>
public class SvgFontFaceDescriptor
{
    /// <summary>The declared family, or empty string to use OpenType metadata.</summary>
    public string Family;
    /// <summary>The declared style, or <see cref="SvgFontStyle.Auto"/> to use OpenType metadata.</summary>
    public SvgFontStyle Style;
    /// <summary>The minimum supported CSS weight, or zero to use OpenType metadata.</summary>
    public int MinimumWeight;
    /// <summary>The maximum supported CSS weight, or zero to use OpenType metadata.</summary>
    public int MaximumWeight;
    /// <summary>The minimum supported CSS width percentage, or zero to use OpenType metadata.</summary>
    public double MinimumStretch;
    /// <summary>The maximum supported CSS width percentage, or zero to use OpenType metadata.</summary>
    public double MaximumStretch;
    /// <summary>The declared Unicode ranges; an empty list covers all Unicode scalars.</summary>
    public System.Collections.Generic.List<SvgUnicodeRange> UnicodeRanges;
    /// <summary>The ordered declared sources.</summary>
    public System.Collections.Generic.List<SvgFontSource> Sources;
    /// <summary>The source span of the font-face declaration, or <c>null</c>.</summary>
    public SvgSourceSpan? Source;

    /// <summary>Creates descriptors that derive family, style, weight, and width from the font.</summary>
    public SvgFontFaceDescriptor()
    {
        Family = "";
        Style = SvgFontStyle.Auto;
        MinimumWeight = 0;
        MaximumWeight = 0;
        MinimumStretch = 0;
        MaximumStretch = 0;
        UnicodeRanges = new System.Collections.Generic.List<SvgUnicodeRange>();
        Sources = new System.Collections.Generic.List<SvgFontSource>();
        Source = null;
    }
}

/// <summary>Contains one parsed and selectable font face.</summary>
public class SvgFontFace
{
    /// <summary>The stable database-local face identifier.</summary>
    public int Identifier;
    /// <summary>The deterministic zero-based registration order.</summary>
    public int Order;
    /// <summary>The face family name.</summary>
    public string Family;
    /// <summary>The face style.</summary>
    public SvgFontStyle Style;
    /// <summary>The minimum supported CSS weight.</summary>
    public int MinimumWeight;
    /// <summary>The maximum supported CSS weight.</summary>
    public int MaximumWeight;
    /// <summary>The minimum supported CSS width percentage.</summary>
    public double MinimumStretch;
    /// <summary>The maximum supported CSS width percentage.</summary>
    public double MaximumStretch;
    /// <summary>The effective Unicode ranges; an empty list covers all Unicode scalars.</summary>
    public System.Collections.Generic.List<SvgUnicodeRange> UnicodeRanges;
    /// <summary>The ordered caller or font-face sources.</summary>
    public System.Collections.Generic.List<SvgFontSource> Sources;
    /// <summary>The parsed OpenType font.</summary>
    public SvgFont? Font;
    /// <summary>How the face entered the database.</summary>
    public SvgFontFaceOrigin Origin;
    /// <summary>The font-face declaration source, or <c>null</c>.</summary>
    public SvgSourceSpan? Source;
    /// <summary>Whether matching may select the face.</summary>
    public bool Enabled;
    System.Collections.Generic.Dictionary<int, SvgOpenTypeGlyphMapping> mappings;

    internal SvgFontFace()
    {
        Identifier = 0;
        Order = 0;
        Family = "";
        Style = SvgFontStyle.Normal;
        MinimumWeight = 400;
        MaximumWeight = 400;
        MinimumStretch = 100;
        MaximumStretch = 100;
        UnicodeRanges = new System.Collections.Generic.List<SvgUnicodeRange>();
        Sources = new System.Collections.Generic.List<SvgFontSource>();
        Font = null;
        Origin = SvgFontFaceOrigin.CallerBytes;
        Source = null;
        Enabled = true;
        mappings = new System.Collections.Generic.Dictionary<int, SvgOpenTypeGlyphMapping>();
    }

    /// <summary>Determines whether the declared Unicode ranges include a scalar.</summary>
    /// <param name="codePoint">The Unicode scalar to test.</param>
    /// <returns><c>true</c> when no range excludes the scalar.</returns>
    internal bool Includes(int codePoint)
    {
        if (!ValidScalar(codePoint)) return false;
        if (UnicodeRanges.Count == 0) return true;
        int index = 0;
        while (index < UnicodeRanges.Count)
        {
            if (UnicodeRanges[index] != null && UnicodeRanges[index].Contains(codePoint)) return true;
            index = index + 1;
        }
        return false;
    }

    /// <summary>Determines whether the effective character map contains a scalar.</summary>
    /// <param name="codePoint">The Unicode scalar to test.</param>
    /// <returns><c>true</c> when both the descriptor and font include the scalar.</returns>
    internal bool Covers(int codePoint)
    {
        if (!Enabled || Font == null || !Includes(codePoint)) return false;
        SvgOpenTypeGlyphMapping mapping;
        if (mappings.ContainsKey(codePoint)) mapping = mappings[codePoint];
        else
        {
            mapping = Font.MapCodePoint(codePoint);
            mappings[codePoint] = mapping;
        }
        return mapping.Success && mapping.Found;
    }

    internal void ClearCache()
    {
        mappings.Clear();
    }

    internal static bool ValidScalar(int codePoint)
    {
        return codePoint >= 0 && codePoint <= 0x10FFFF && (codePoint < 0xD800 || codePoint > 0xDFFF);
    }
}

/// <summary>Contains the result of registering one font face.</summary>
public class SvgFontFaceResult
{
    /// <summary>Whether the face was registered.</summary>
    public bool Success;
    /// <summary>The stable database outcome.</summary>
    public SvgFontDatabaseError Error;
    /// <summary>The registered face, or <c>null</c>.</summary>
    public SvgFontFace? Face;
    /// <summary>The underlying OpenType read result, or <c>null</c>.</summary>
    public SvgOpenTypeReadResult? ReadResult;
    /// <summary>A deterministic failure explanation, or empty string.</summary>
    public string Message;

    internal SvgFontFaceResult()
    {
        Success = false;
        Error = SvgFontDatabaseError.None;
        Face = null;
        ReadResult = null;
        Message = "";
    }
}

/// <summary>Defines one CSS-style font face match request.</summary>
public class SvgFontMatchRequest
{
    /// <summary>The ordered family names, including optional CSS generic keywords.</summary>
    public System.Collections.Generic.List<string> Families;
    /// <summary>The requested style.</summary>
    public SvgFontStyle Style;
    /// <summary>The requested CSS weight from 1 through 1000.</summary>
    public int Weight;
    /// <summary>The requested positive CSS width percentage.</summary>
    public double Stretch;
    /// <summary>The Unicode scalar whose glyph is required.</summary>
    public int CodePoint;
    /// <summary>Whether <see cref="CodePoint"/> participates in matching.</summary>
    public bool HasCodePoint;
    /// <summary>Whether registered families not named by the request may provide final fallback.</summary>
    public bool AllowAnyFamilyFallback;

    /// <summary>Creates a normal 400-weight, 100-percent-width request.</summary>
    public SvgFontMatchRequest()
    {
        Families = new System.Collections.Generic.List<string>();
        Style = SvgFontStyle.Normal;
        Weight = 400;
        Stretch = 100;
        CodePoint = 0;
        HasCodePoint = false;
        AllowAnyFamilyFallback = true;
    }
}

/// <summary>Contains one deterministic font face match.</summary>
public class SvgFontMatchResult
{
    /// <summary>Whether a matching face was found.</summary>
    public bool Success;
    /// <summary>The stable matching outcome.</summary>
    public SvgFontDatabaseError Error;
    /// <summary>The selected face, or <c>null</c>.</summary>
    public SvgFontFace? Face;
    /// <summary>The selected glyph mapping, or <c>null</c> when no scalar was requested.</summary>
    public SvgOpenTypeGlyphMapping? GlyphMapping;
    /// <summary>The zero-based requested family index, or minus one for database fallback.</summary>
    public int FamilyIndex;
    /// <summary>The resolved concrete family name, or empty string.</summary>
    public string ResolvedFamily;
    /// <summary>Whether a configured CSS generic family mapping was used.</summary>
    public bool UsedGenericFamily;

    internal SvgFontMatchResult()
    {
        Success = false;
        Error = SvgFontDatabaseError.NoMatch;
        Face = null;
        GlyphMapping = null;
        FamilyIndex = -1;
        ResolvedFamily = "";
        UsedGenericFamily = false;
    }
}

/// <summary>Contains one contiguous UTF-16 fallback run.</summary>
public class SvgFontFallbackRun
{
    /// <summary>The UTF-16 start index.</summary>
    public int Start;
    /// <summary>The UTF-16 code-unit length.</summary>
    public int Length;
    /// <summary>The selected face, or <c>null</c> for missing scalars.</summary>
    public SvgFontFace? Face;
    /// <summary>Whether the run contains no selectable glyph.</summary>
    public bool Missing;

    internal SvgFontFallbackRun(int start, int length, SvgFontFace? face, bool missing)
    {
        Start = start;
        Length = length;
        Face = face;
        Missing = missing;
    }
}

/// <summary>Contains deterministic font fallback runs for one string.</summary>
public class SvgFontFallbackResult
{
    /// <summary>Whether the request and input were valid.</summary>
    public bool Success;
    /// <summary>The stable fallback outcome.</summary>
    public SvgFontDatabaseError Error;
    /// <summary>The ordered contiguous UTF-16 runs.</summary>
    public System.Collections.Generic.List<SvgFontFallbackRun> Runs;
    /// <summary>The count of Unicode scalars without a glyph.</summary>
    public int MissingCodePoints;

    internal SvgFontFallbackResult()
    {
        Success = false;
        Error = SvgFontDatabaseError.None;
        Runs = new System.Collections.Generic.List<SvgFontFallbackRun>();
        MissingCodePoints = 0;
    }
}

/// <summary>Contains an independent database registration snapshot.</summary>
public class SvgFontDatabaseSnapshot
{
    /// <summary>The database revision represented by the snapshot.</summary>
    public int Revision;
    /// <summary>The total registered font byte count.</summary>
    public long TotalBytes;
    /// <summary>The registered faces in deterministic order.</summary>
    public System.Collections.Generic.List<SvgFontFace> Faces;
    /// <summary>Independent generic-family mapping lists keyed by generic family.</summary>
    public System.Collections.Generic.Dictionary<SvgGenericFontFamily, System.Collections.Generic.List<string>> GenericFamilies;

    internal SvgFontDatabaseSnapshot()
    {
        Revision = 0;
        TotalBytes = 0;
        Faces = new System.Collections.Generic.List<SvgFontFace>();
        GenericFamilies = new System.Collections.Generic.Dictionary<SvgGenericFontFamily, System.Collections.Generic.List<string>>();
    }
}

/// <summary>Stores only explicitly supplied or caller-resolved fonts and performs deterministic CSS-style matching.</summary>
public class SvgFontDatabase
{
    /// <summary>The database font count and byte limits.</summary>
    public SvgFontLimits Limits;
    /// <summary>The revision incremented by every successful mutation.</summary>
    public int Revision;

    System.Collections.Generic.List<SvgFontFace> faces;
    System.Collections.Generic.Dictionary<SvgGenericFontFamily, System.Collections.Generic.List<string>> genericFamilies;
    long totalBytes;
    int nextIdentifier;

    /// <summary>Creates an empty database with secure font limits.</summary>
    public SvgFontDatabase()
    {
        Limits = new SvgFontLimits();
        Revision = 0;
        faces = new System.Collections.Generic.List<SvgFontFace>();
        genericFamilies = new System.Collections.Generic.Dictionary<SvgGenericFontFamily, System.Collections.Generic.List<string>>();
        totalBytes = 0;
        nextIdentifier = 1;
    }

    /// <summary>Creates an empty database with explicit font limits.</summary>
    /// <param name="limits">The database font count and byte limits.</param>
    /// <returns>A new empty font database.</returns>
    internal static SvgFontDatabase Create(SvgFontLimits limits)
    {
        if (limits == null) throw new System.ArgumentNullException("limits");
        SvgFontDatabase result = new SvgFontDatabase();
        result.Limits = limits;
        return result;
    }

    /// <summary>Parses and registers caller-provided font bytes using derived descriptors.</summary>
    /// <param name="bytes">The SFNT, WOFF, or WOFF2 bytes.</param>
    /// <returns>The registration result.</returns>
    internal SvgFontFaceResult AddFont(byte[] bytes)
    {
        return AddFontWithOptions(bytes, new SvgFontFaceDescriptor(), null);
    }

    /// <summary>Parses and registers caller-provided font bytes.</summary>
    /// <param name="bytes">The SFNT, WOFF, or WOFF2 bytes.</param>
    /// <param name="descriptor">The face descriptors.</param>
    /// <returns>The registration result.</returns>
    internal SvgFontFaceResult AddFontWithDescriptor(byte[] bytes, SvgFontFaceDescriptor descriptor)
    {
        return AddFontWithOptions(bytes, descriptor, null);
    }

    /// <summary>Parses and registers caller-provided font bytes with explicit read options.</summary>
    /// <param name="bytes">The SFNT, WOFF, or WOFF2 bytes.</param>
    /// <param name="descriptor">The face descriptors.</param>
    /// <param name="options">The OpenType read options, or <c>null</c> for secure defaults.</param>
    /// <returns>The registration result.</returns>
    internal SvgFontFaceResult AddFontWithOptions(byte[] bytes, SvgFontFaceDescriptor descriptor, SvgOpenTypeReadOptions? options)
    {
        return AddBytes(bytes, descriptor, options, SvgFontFaceOrigin.CallerBytes, null);
    }

    /// <summary>Registers bytes returned by an explicit caller-mediated font resource acquisition.</summary>
    /// <param name="resource">The completed resource acquisition.</param>
    /// <param name="descriptor">The font-face descriptors.</param>
    /// <returns>The registration result.</returns>
    internal SvgFontFaceResult AddResolvedFont(SvgResourceAcquisitionResult resource, SvgFontFaceDescriptor descriptor)
    {
        return AddResolvedFontWithOptions(resource, descriptor, null);
    }

    /// <summary>Registers bytes returned by an explicit caller-mediated font resource acquisition.</summary>
    /// <param name="resource">The completed resource acquisition.</param>
    /// <param name="descriptor">The font-face descriptors.</param>
    /// <param name="options">The OpenType read options, or <c>null</c> for secure defaults.</param>
    /// <returns>The registration result.</returns>
    internal SvgFontFaceResult AddResolvedFontWithOptions(SvgResourceAcquisitionResult? resource, SvgFontFaceDescriptor? descriptor, SvgOpenTypeReadOptions? options)
    {
        if (resource == null || !resource.Success || resource.Bytes == null)
        {
            return Failure(SvgFontDatabaseError.ResourceFailed, "The resolved font resource is unavailable.");
        }
        SvgFontSource source = new SvgFontSource(SvgFontSourceKind.Url, resource.FinalUri == null ? "" : resource.FinalUri, resource.EffectiveMediaType == null ? "" : resource.EffectiveMediaType, descriptor == null ? null : descriptor.Source);
        return AddBytes(resource.Bytes, descriptor, options, SvgFontFaceOrigin.ResolvedFontFace, source);
    }

    /// <summary>Removes one registered face by its stable identifier.</summary>
    /// <param name="identifier">The database-local face identifier.</param>
    /// <returns><c>true</c> when a face was removed.</returns>
    internal bool RemoveFont(int identifier)
    {
        int index = 0;
        while (index < faces.Count)
        {
            SvgFontFace face = faces[index];
            if (face.Identifier == identifier)
            {
                if (face.Font != null && face.Font.Bytes != null) totalBytes = totalBytes - face.Font.Bytes.Length;
                faces.RemoveAt(index);
                Revision = Revision + 1;
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    /// <summary>Replaces the ordered concrete-family mapping for one CSS generic family.</summary>
    /// <param name="genericFamily">The CSS generic family.</param>
    /// <param name="families">The ordered nonempty concrete family names.</param>
    internal void SetGenericFamily(SvgGenericFontFamily genericFamily, System.Collections.Generic.List<string> families)
    {
        if (genericFamily == SvgGenericFontFamily.None) throw new System.ArgumentOutOfRangeException("genericFamily");
        if (families == null) throw new System.ArgumentNullException("families");
        System.Collections.Generic.List<string> copy = new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < families.Count)
        {
            string family = families[index];
            if (family == null || family.Trim().Length == 0) throw new System.ArgumentOutOfRangeException("families");
            copy.Add(family.Trim());
            index = index + 1;
        }
        genericFamilies[genericFamily] = copy;
        Revision = Revision + 1;
    }

    /// <summary>Removes a configured CSS generic-family mapping.</summary>
    /// <param name="genericFamily">The CSS generic family.</param>
    /// <returns><c>true</c> when a mapping was removed.</returns>
    internal bool ClearGenericFamily(SvgGenericFontFamily genericFamily)
    {
        bool removed = genericFamilies.Remove(genericFamily);
        if (removed) Revision = Revision + 1;
        return removed;
    }

    /// <summary>Matches one face using family, width, style, weight, Unicode range, and character-map rules.</summary>
    /// <param name="request">The match request.</param>
    /// <returns>The selected face and optional glyph mapping.</returns>
    internal SvgFontMatchResult Match(SvgFontMatchRequest request)
    {
        SvgFontMatchResult invalid = new SvgFontMatchResult();
        if (!ValidRequest(request))
        {
            invalid.Error = SvgFontDatabaseError.InvalidArgument;
            return invalid;
        }
        int familyIndex = 0;
        while (familyIndex < request.Families.Count)
        {
            string requestedFamily = request.Families[familyIndex];
            SvgGenericFontFamily genericFamily = GenericFromName(requestedFamily);
            if (genericFamily == SvgGenericFontFamily.None)
            {
                SvgFontMatchResult direct = MatchFamily(requestedFamily.Trim(), request, familyIndex, false);
                if (direct.Success) return direct;
            }
            else
            {
                if (genericFamilies.ContainsKey(genericFamily))
                {
                    System.Collections.Generic.List<string> mappings = genericFamilies[genericFamily];
                    int mappingIndex = 0;
                    while (mappingIndex < mappings.Count)
                    {
                        SvgFontMatchResult mapped = MatchFamily(mappings[mappingIndex], request, familyIndex, true);
                        if (mapped.Success) return mapped;
                        mappingIndex = mappingIndex + 1;
                    }
                }
            }
            familyIndex = familyIndex + 1;
        }
        if (request.AllowAnyFamilyFallback && request.HasCodePoint)
        {
            System.Collections.Generic.List<string> visited = new System.Collections.Generic.List<string>();
            int faceIndex = 0;
            while (faceIndex < faces.Count)
            {
                string family = faces[faceIndex].Family;
                if (!ContainsFamily(visited, family) && !RequestedConcreteFamily(request, family))
                {
                    visited.Add(family);
                    SvgFontMatchResult fallback = MatchFamily(family, request, -1, false);
                    if (fallback.Success) return fallback;
                }
                faceIndex = faceIndex + 1;
            }
        }
        return invalid;
    }

    /// <summary>Constructs contiguous UTF-16 runs using deterministic per-scalar fallback.</summary>
    /// <param name="text">The source text.</param>
    /// <param name="request">The base face match request.</param>
    /// <returns>The ordered fallback runs and missing-scalar count.</returns>
    internal SvgFontFallbackResult CreateFallbackRuns(string text, SvgFontMatchRequest request)
    {
        SvgFontFallbackResult result = new SvgFontFallbackResult();
        if (text == null || !ValidRequest(request))
        {
            result.Error = SvgFontDatabaseError.InvalidArgument;
            return result;
        }
        int offset = 0;
        while (offset < text.Length)
        {
            int scalarLength = 1;
            int codePoint = text[offset];
            if (codePoint >= 0xD800 && codePoint <= 0xDBFF && offset + 1 < text.Length)
            {
                int low = text[offset + 1];
                if (low >= 0xDC00 && low <= 0xDFFF)
                {
                    codePoint = 0x10000 + ((codePoint - 0xD800) << 10) + low - 0xDC00;
                    scalarLength = 2;
                }
                else codePoint = 0xFFFD;
            }
            else if (codePoint >= 0xD800 && codePoint <= 0xDFFF) codePoint = 0xFFFD;
            SvgFontMatchRequest scalarRequest = CopyRequest(request);
            scalarRequest.CodePoint = codePoint;
            scalarRequest.HasCodePoint = true;
            SvgFontMatchResult match = Match(scalarRequest);
            SvgFontFace? face = match.Success ? match.Face : null;
            bool missing = !match.Success;
            if (missing) result.MissingCodePoints = result.MissingCodePoints + 1;
            if (result.Runs.Count > 0)
            {
                SvgFontFallbackRun previous = result.Runs[result.Runs.Count - 1];
                if (previous.Face == face && previous.Missing == missing && previous.Start + previous.Length == offset)
                {
                    previous.Length = previous.Length + scalarLength;
                    offset = offset + scalarLength;
                    continue;
                }
            }
            result.Runs.Add(new SvgFontFallbackRun(offset, scalarLength, face, missing));
            offset = offset + scalarLength;
        }
        result.Success = true;
        return result;
    }

    /// <summary>Returns an independent registration and generic-mapping snapshot.</summary>
    /// <returns>The current database snapshot.</returns>
    internal SvgFontDatabaseSnapshot Snapshot()
    {
        SvgFontDatabaseSnapshot snapshot = new SvgFontDatabaseSnapshot();
        snapshot.Revision = Revision;
        snapshot.TotalBytes = totalBytes;
        snapshot.Faces.AddRange(faces);
        foreach (System.Collections.Generic.KeyValuePair<SvgGenericFontFamily, System.Collections.Generic.List<string>> pair in genericFamilies)
        {
            snapshot.GenericFamilies[pair.Key] = new System.Collections.Generic.List<string>(pair.Value);
        }
        return snapshot;
    }

    /// <summary>Clears transient matching state without removing registered fonts.</summary>
    internal void ClearCaches()
    {
        int index = 0;
        while (index < faces.Count)
        {
            faces[index].ClearCache();
            index = index + 1;
        }
    }

    internal SvgFontFaceResult AddBytes(byte[] bytes, SvgFontFaceDescriptor? descriptor, SvgOpenTypeReadOptions? options, SvgFontFaceOrigin origin, SvgFontSource? resolvedSource)
    {
        if (bytes == null || descriptor == null) return Failure(SvgFontDatabaseError.InvalidArgument, "Font bytes and descriptors are required.");
        if (!ValidLimits() || !ValidDescriptor(descriptor)) return Failure(SvgFontDatabaseError.InvalidArgument, "Font limits or descriptors are invalid.");
        if (faces.Count >= Limits.MaxFonts || bytes.LongLength > Limits.MaxBytesPerFont || totalBytes + bytes.LongLength > Limits.MaxTotalBytes)
        {
            return Failure(SvgFontDatabaseError.LimitExceeded, "The font database count or byte budget was exceeded.");
        }
        SvgOpenTypeReadOptions effectiveOptions = CopyOptions(options);
        effectiveOptions.CopyInputData = true;
        effectiveOptions.Limits.MaxFonts = Smaller(effectiveOptions.Limits.MaxFonts, Limits.MaxFonts);
        effectiveOptions.Limits.MaxBytesPerFont = Smaller(effectiveOptions.Limits.MaxBytesPerFont, Limits.MaxBytesPerFont);
        effectiveOptions.Limits.MaxTotalBytes = Smaller(effectiveOptions.Limits.MaxTotalBytes, Limits.MaxTotalBytes);
        effectiveOptions.Limits.MaxTablesPerFont = Smaller(effectiveOptions.Limits.MaxTablesPerFont, Limits.MaxTablesPerFont);
        effectiveOptions.Limits.MaxGlyphsPerFont = Smaller(effectiveOptions.Limits.MaxGlyphsPerFont, Limits.MaxGlyphsPerFont);
        effectiveOptions.Limits.MaxPointsPerGlyph = Smaller(effectiveOptions.Limits.MaxPointsPerGlyph, Limits.MaxPointsPerGlyph);
        effectiveOptions.Limits.MaxShapingGlyphs = Smaller(effectiveOptions.Limits.MaxShapingGlyphs, Limits.MaxShapingGlyphs);
        effectiveOptions.Limits.MaxVariationAxes = Smaller(effectiveOptions.Limits.MaxVariationAxes, Limits.MaxVariationAxes);
        if (descriptor.Source != null) effectiveOptions.Source = descriptor.Source;
        SvgOpenTypeReadResult read = new SvgOpenTypeReader().ReadWithBytesAndOptions(bytes, effectiveOptions);
        if (!read.Success)
        {
            SvgFontFaceResult failed = Failure(SvgFontDatabaseError.FontReadFailed, "The supplied font could not be parsed.");
            failed.ReadResult = read;
            return failed;
        }
        if (((read.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).LongLength > Limits.MaxBytesPerFont || totalBytes + (read.Font.Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).LongLength > Limits.MaxTotalBytes)
        {
            SvgFontFaceResult failed = Failure(SvgFontDatabaseError.LimitExceeded, "The decoded font exceeds the database byte budget.");
            failed.ReadResult = read;
            return failed;
        }
        SvgFontFace face = BuildFace(read.Font, descriptor, origin);
        if (face.Family.Length == 0)
        {
            SvgFontFaceResult failed = Failure(SvgFontDatabaseError.InvalidArgument, "The font face has no family name.");
            failed.ReadResult = read;
            return failed;
        }
        if (resolvedSource != null) face.Sources.Add(resolvedSource);
        else if (face.Sources.Count == 0) face.Sources.Add(new SvgFontSource(SvgFontSourceKind.Bytes, "", "", descriptor.Source));
        face.Identifier = nextIdentifier;
        face.Order = faces.Count;
        nextIdentifier = nextIdentifier + 1;
        faces.Add(face);
        totalBytes = totalBytes + ((face.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length;
        Revision = Revision + 1;
        SvgFontFaceResult result = new SvgFontFaceResult();
        result.Success = true;
        result.Error = SvgFontDatabaseError.None;
        result.Face = face;
        result.ReadResult = read;
        return result;
    }

    internal SvgFontFace BuildFace(SvgFont font, SvgFontFaceDescriptor descriptor, SvgFontFaceOrigin origin)
    {
        SvgFontFace face = new SvgFontFace();
        face.Font = font;
        face.Origin = origin;
        face.Family = descriptor.Family.Trim().Length == 0 ? font.FamilyName.Trim() : descriptor.Family.Trim();
        face.Style = descriptor.Style == SvgFontStyle.Auto ? MetadataStyle(font) : descriptor.Style;
        int metadataWeight = font.Os2 == null ? 400 : font.Os2.WeightClass;
        face.MinimumWeight = descriptor.MinimumWeight == 0 ? metadataWeight : descriptor.MinimumWeight;
        face.MaximumWeight = descriptor.MaximumWeight == 0 ? face.MinimumWeight : descriptor.MaximumWeight;
        double metadataStretch = font.Os2 == null ? 100 : WidthPercentage(font.Os2.WidthClass);
        face.MinimumStretch = descriptor.MinimumStretch == 0 ? metadataStretch : descriptor.MinimumStretch;
        face.MaximumStretch = descriptor.MaximumStretch == 0 ? face.MinimumStretch : descriptor.MaximumStretch;
        face.Source = descriptor.Source;
        int index = 0;
        while (index < descriptor.UnicodeRanges.Count)
        {
            SvgUnicodeRange range = descriptor.UnicodeRanges[index];
            face.UnicodeRanges.Add(new SvgUnicodeRange(range.Start, range.End));
            index = index + 1;
        }
        index = 0;
        while (index < descriptor.Sources.Count)
        {
            SvgFontSource source = descriptor.Sources[index];
            face.Sources.Add(new SvgFontSource(source.Kind, source.Value, source.Format, source.Source));
            index = index + 1;
        }
        return face;
    }

    internal SvgFontMatchResult MatchFamily(string family, SvgFontMatchRequest request, int familyIndex, bool usedGeneric)
    {
        System.Collections.Generic.List<SvgFontFace> candidates = new System.Collections.Generic.List<SvgFontFace>();
        int index = 0;
        while (index < faces.Count)
        {
            SvgFontFace face = faces[index];
            if (face.Enabled && string.Equals(face.Family, family, System.StringComparison.OrdinalIgnoreCase)) candidates.Add(face);
            index = index + 1;
        }
        if (candidates.Count == 0) return new SvgFontMatchResult();
        FilterStretch(candidates, request.Stretch);
        FilterStyle(candidates, request.Style);
        FilterWeight(candidates, request.Weight);
        SvgFontFace? selected = null;
        if (!request.HasCodePoint) selected = candidates[0];
        else
        {
            index = candidates.Count - 1;
            while (index >= 0)
            {
                SvgFontFace candidate = candidates[index];
                if (SameDescriptors(candidate, candidates[0]) && candidate.Covers(request.CodePoint))
                {
                    selected = candidate;
                    break;
                }
                index = index - 1;
            }
        }
        if (selected == null) return new SvgFontMatchResult();
        SvgFontMatchResult result = new SvgFontMatchResult();
        result.Success = true;
        result.Error = SvgFontDatabaseError.None;
        result.Face = selected;
        result.FamilyIndex = familyIndex;
        result.ResolvedFamily = selected.Family;
        result.UsedGenericFamily = usedGeneric;
        if (request.HasCodePoint) result.GlyphMapping = (selected.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MapCodePoint(request.CodePoint);
        return result;
    }

    internal static void FilterStretch(System.Collections.Generic.List<SvgFontFace> candidates, double target)
    {
        SvgFontMatchRank? best = null;
        int index = 0;
        while (index < candidates.Count)
        {
            SvgFontMatchRank rank = StretchRank(candidates[index], target);
            if (best == null || CompareRank(rank, best) < 0) best = rank;
            index = index + 1;
        }
        index = candidates.Count - 1;
        while (index >= 0)
        {
            if (CompareRank(StretchRank(candidates[index], target), best) != 0) candidates.RemoveAt(index);
            index = index - 1;
        }
    }

    internal static SvgFontMatchRank StretchRank(SvgFontFace face, double target)
    {
        if (target >= face.MinimumStretch && target <= face.MaximumStretch) return new SvgFontMatchRank(0, 0);
        if (target <= 100)
        {
            if (face.MaximumStretch < target) return new SvgFontMatchRank(1, target - face.MaximumStretch);
            return new SvgFontMatchRank(2, face.MinimumStretch - target);
        }
        if (face.MinimumStretch > target) return new SvgFontMatchRank(1, face.MinimumStretch - target);
        return new SvgFontMatchRank(2, target - face.MaximumStretch);
    }

    internal static void FilterStyle(System.Collections.Generic.List<SvgFontFace> candidates, SvgFontStyle style)
    {
        int best = 100;
        int index = 0;
        while (index < candidates.Count)
        {
            int rank = StyleRank(candidates[index].Style, style);
            if (rank < best) best = rank;
            index = index + 1;
        }
        index = candidates.Count - 1;
        while (index >= 0)
        {
            if (StyleRank(candidates[index].Style, style) != best) candidates.RemoveAt(index);
            index = index - 1;
        }
    }

    internal static int StyleRank(SvgFontStyle face, SvgFontStyle requested)
    {
        if (requested == SvgFontStyle.Auto) requested = SvgFontStyle.Normal;
        if (face == requested) return 0;
        if (requested == SvgFontStyle.Normal) return face == SvgFontStyle.Oblique ? 1 : 2;
        if (requested == SvgFontStyle.Italic) return face == SvgFontStyle.Oblique ? 1 : 2;
        return face == SvgFontStyle.Italic ? 1 : 2;
    }

    internal static void FilterWeight(System.Collections.Generic.List<SvgFontFace> candidates, int target)
    {
        SvgFontMatchRank? best = null;
        int index = 0;
        while (index < candidates.Count)
        {
            SvgFontMatchRank rank = WeightRank(candidates[index], target);
            if (best == null || CompareRank(rank, best) < 0) best = rank;
            index = index + 1;
        }
        index = candidates.Count - 1;
        while (index >= 0)
        {
            if (CompareRank(WeightRank(candidates[index], target), best) != 0) candidates.RemoveAt(index);
            index = index - 1;
        }
    }

    internal static SvgFontMatchRank WeightRank(SvgFontFace face, int target)
    {
        if (target >= face.MinimumWeight && target <= face.MaximumWeight) return new SvgFontMatchRank(0, 0);
        if (target >= 400 && target <= 500)
        {
            if (face.MinimumWeight > target && face.MinimumWeight <= 500) return new SvgFontMatchRank(1, face.MinimumWeight - target);
            if (face.MaximumWeight < target) return new SvgFontMatchRank(2, target - face.MaximumWeight);
            return new SvgFontMatchRank(3, face.MinimumWeight - 500);
        }
        if (target < 400)
        {
            if (face.MaximumWeight < target) return new SvgFontMatchRank(1, target - face.MaximumWeight);
            return new SvgFontMatchRank(2, face.MinimumWeight - target);
        }
        if (face.MinimumWeight > target) return new SvgFontMatchRank(1, face.MinimumWeight - target);
        return new SvgFontMatchRank(2, target - face.MaximumWeight);
    }

    internal static int CompareRank(SvgFontMatchRank first, SvgFontMatchRank? second)
    {
        if (first.Category < (second ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Category) return -1;
        if (first.Category > second.Category) return 1;
        if (first.Distance < second.Distance) return -1;
        if (first.Distance > second.Distance) return 1;
        return 0;
    }

    internal static bool SameDescriptors(SvgFontFace first, SvgFontFace second)
    {
        return first.Style == second.Style && first.MinimumWeight == second.MinimumWeight && first.MaximumWeight == second.MaximumWeight && first.MinimumStretch == second.MinimumStretch && first.MaximumStretch == second.MaximumStretch;
    }

    internal static SvgFontStyle MetadataStyle(SvgFont font)
    {
        if (font.Os2 != null)
        {
            if ((font.Os2.SelectionFlags & 0x0200) != 0) return SvgFontStyle.Oblique;
            if ((font.Os2.SelectionFlags & 0x0001) != 0) return SvgFontStyle.Italic;
        }
        if (font.Post != null && font.Post.ItalicAngle != 0) return SvgFontStyle.Italic;
        return SvgFontStyle.Normal;
    }

    internal static double WidthPercentage(int widthClass)
    {
        if (widthClass == 1) return 50;
        if (widthClass == 2) return 62.5;
        if (widthClass == 3) return 75;
        if (widthClass == 4) return 87.5;
        if (widthClass == 6) return 112.5;
        if (widthClass == 7) return 125;
        if (widthClass == 8) return 150;
        if (widthClass == 9) return 200;
        return 100;
    }

    internal static SvgOpenTypeReadOptions CopyOptions(SvgOpenTypeReadOptions? options)
    {
        SvgOpenTypeReadOptions copy = new SvgOpenTypeReadOptions();
        if (options == null) return copy;
        copy.CopyInputData = options.CopyInputData;
        copy.ValidateChecksums = options.ValidateChecksums;
        copy.Source = options.Source;
        copy.CancellationToken = options.CancellationToken;
        if (options.Limits != null)
        {
            copy.Limits.MaxFonts = options.Limits.MaxFonts;
            copy.Limits.MaxBytesPerFont = options.Limits.MaxBytesPerFont;
            copy.Limits.MaxTotalBytes = options.Limits.MaxTotalBytes;
            copy.Limits.MaxTablesPerFont = options.Limits.MaxTablesPerFont;
            copy.Limits.MaxGlyphsPerFont = options.Limits.MaxGlyphsPerFont;
            copy.Limits.MaxPointsPerGlyph = options.Limits.MaxPointsPerGlyph;
            copy.Limits.MaxShapingGlyphs = options.Limits.MaxShapingGlyphs;
            copy.Limits.MaxVariationAxes = options.Limits.MaxVariationAxes;
        }
        return copy;
    }

    internal static SvgFontMatchRequest CopyRequest(SvgFontMatchRequest request)
    {
        SvgFontMatchRequest copy = new SvgFontMatchRequest();
        copy.Families.AddRange(request.Families);
        copy.Style = request.Style;
        copy.Weight = request.Weight;
        copy.Stretch = request.Stretch;
        copy.CodePoint = request.CodePoint;
        copy.HasCodePoint = request.HasCodePoint;
        copy.AllowAnyFamilyFallback = request.AllowAnyFamilyFallback;
        return copy;
    }

    internal bool ValidLimits()
    {
        return Limits != null && Limits.MaxFonts > 0 && Limits.MaxBytesPerFont > 0 && Limits.MaxTotalBytes > 0 && Limits.MaxTablesPerFont > 0 && Limits.MaxGlyphsPerFont > 0 && Limits.MaxPointsPerGlyph > 0 && Limits.MaxShapingGlyphs > 0 && Limits.MaxVariationAxes > 0;
    }

    internal static bool ValidDescriptor(SvgFontFaceDescriptor descriptor)
    {
        if (descriptor.Family == null || descriptor.UnicodeRanges == null || descriptor.Sources == null) return false;
        if (descriptor.Style < SvgFontStyle.Auto || descriptor.Style > SvgFontStyle.Oblique) return false;
        if (descriptor.MinimumWeight == 0 && descriptor.MaximumWeight != 0) return false;
        if (descriptor.MinimumWeight != 0 && (descriptor.MinimumWeight < 1 || descriptor.MinimumWeight > 1000)) return false;
        if (descriptor.MaximumWeight != 0 && (descriptor.MaximumWeight > 1000 || descriptor.MaximumWeight < descriptor.MinimumWeight)) return false;
        if (descriptor.MinimumStretch == 0 && descriptor.MaximumStretch != 0) return false;
        if (descriptor.MinimumStretch != 0 && (!double.IsFinite(descriptor.MinimumStretch) || descriptor.MinimumStretch <= 0)) return false;
        if (descriptor.MaximumStretch != 0 && (!double.IsFinite(descriptor.MaximumStretch) || descriptor.MaximumStretch < descriptor.MinimumStretch)) return false;
        int index = 0;
        while (index < descriptor.UnicodeRanges.Count)
        {
            if (descriptor.UnicodeRanges[index] == null) return false;
            index = index + 1;
        }
        index = 0;
        while (index < descriptor.Sources.Count)
        {
            if (descriptor.Sources[index] == null) return false;
            index = index + 1;
        }
        return true;
    }

    internal static bool ValidRequest(SvgFontMatchRequest? request)
    {
        if (request == null || request.Families == null || request.Weight < 1 || request.Weight > 1000 || !double.IsFinite(request.Stretch) || request.Stretch <= 0) return false;
        if (request.Style < SvgFontStyle.Auto || request.Style > SvgFontStyle.Oblique) return false;
        if (request.HasCodePoint && (request.CodePoint < 0 || request.CodePoint > 0x10FFFF || (request.CodePoint >= 0xD800 && request.CodePoint <= 0xDFFF))) return false;
        int index = 0;
        while (index < request.Families.Count)
        {
            if (request.Families[index] == null || request.Families[index].Trim().Length == 0) return false;
            index = index + 1;
        }
        return true;
    }

    internal static SvgGenericFontFamily GenericFromName(string name)
    {
        string value = name.Trim();
        if (string.Equals(value, "serif", System.StringComparison.OrdinalIgnoreCase)) return SvgGenericFontFamily.Serif;
        if (string.Equals(value, "sans-serif", System.StringComparison.OrdinalIgnoreCase)) return SvgGenericFontFamily.SansSerif;
        if (string.Equals(value, "monospace", System.StringComparison.OrdinalIgnoreCase)) return SvgGenericFontFamily.Monospace;
        if (string.Equals(value, "cursive", System.StringComparison.OrdinalIgnoreCase)) return SvgGenericFontFamily.Cursive;
        if (string.Equals(value, "fantasy", System.StringComparison.OrdinalIgnoreCase)) return SvgGenericFontFamily.Fantasy;
        if (string.Equals(value, "system-ui", System.StringComparison.OrdinalIgnoreCase)) return SvgGenericFontFamily.SystemUi;
        if (string.Equals(value, "emoji", System.StringComparison.OrdinalIgnoreCase)) return SvgGenericFontFamily.Emoji;
        if (string.Equals(value, "math", System.StringComparison.OrdinalIgnoreCase)) return SvgGenericFontFamily.Math;
        if (string.Equals(value, "fangsong", System.StringComparison.OrdinalIgnoreCase)) return SvgGenericFontFamily.FangSong;
        if (string.Equals(value, "ui-serif", System.StringComparison.OrdinalIgnoreCase)) return SvgGenericFontFamily.UiSerif;
        if (string.Equals(value, "ui-sans-serif", System.StringComparison.OrdinalIgnoreCase)) return SvgGenericFontFamily.UiSansSerif;
        if (string.Equals(value, "ui-monospace", System.StringComparison.OrdinalIgnoreCase)) return SvgGenericFontFamily.UiMonospace;
        if (string.Equals(value, "ui-rounded", System.StringComparison.OrdinalIgnoreCase)) return SvgGenericFontFamily.UiRounded;
        return SvgGenericFontFamily.None;
    }

    internal static bool RequestedConcreteFamily(SvgFontMatchRequest request, string family)
    {
        int index = 0;
        while (index < request.Families.Count)
        {
            string requested = request.Families[index];
            if (GenericFromName(requested) == SvgGenericFontFamily.None && string.Equals(requested.Trim(), family, System.StringComparison.OrdinalIgnoreCase)) return true;
            index = index + 1;
        }
        return false;
    }

    internal static bool ContainsFamily(System.Collections.Generic.List<string> families, string family)
    {
        int index = 0;
        while (index < families.Count)
        {
            if (string.Equals(families[index], family, System.StringComparison.OrdinalIgnoreCase)) return true;
            index = index + 1;
        }
        return false;
    }

    internal static int Smaller(int first, int second)
    {
        return first < second ? first : second;
    }

    internal static long Smaller(long first, long second)
    {
        return first < second ? first : second;
    }

    internal static SvgFontFaceResult Failure(SvgFontDatabaseError error, string message)
    {
        SvgFontFaceResult result = new SvgFontFaceResult();
        result.Error = error;
        result.Message = message;
        return result;
    }
}
