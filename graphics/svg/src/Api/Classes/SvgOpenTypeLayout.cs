namespace svg;

/// <summary>Maps and applies bounded baseline OpenType substitution and positioning.</summary>
public class SvgOpenTypeLayout : ISvgOpenTypeLayout
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgOpenTypeLayout()
    {
    }

    /// <summary>Shapes UTF-16 text with baseline defaults.</summary>
    /// <param name="font">A successfully parsed OpenType font.</param>
    /// <param name="text">The UTF-16 source text.</param>
    /// <returns>The mapped and positioned glyph sequence.</returns>
    public SvgShapingResult ShapeWithFontAndText(SvgFont? font, string text)
    {
        return new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, text, new SvgShapingOptions());
    }

    /// <summary>Shapes UTF-16 text with explicit features, language, variation, and bounds.</summary>
    /// <param name="font">A successfully parsed OpenType font.</param>
    /// <param name="text">The UTF-16 source text.</param>
    /// <param name="options">The shaping configuration and security bounds.</param>
    /// <returns>The mapped and positioned glyph sequence.</returns>
    public SvgShapingResult ShapeWithSvgFontAndStringAndSvgShapingOptions(SvgFont? font, string text, SvgShapingOptions options)
    {
        SvgShapingResult result = new SvgShapingResult();
        if (options != null) result.FontIdentifier = options.FontIdentifier;
        if (!new SvgOpenTypeLayout().ValidWithSvgFontAndStringAndSvgShapingOptions(font, text, options)) return new SvgOpenTypeLayout().Fail(result, SvgShapingError.InvalidArgument, "OpenType shaping arguments or options are invalid.");
        SvgUnicodeTextOptions unicodeOptions = new SvgUnicodeTextOptions();
        unicodeOptions.MaxScalars = (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaxGlyphs;
        unicodeOptions.MaxClusters = options.MaxGlyphs;
        unicodeOptions.MaxRuns = options.MaxGlyphs;
        unicodeOptions.CancellationToken = options.CancellationToken;
        SvgUnicodeTextResult unicode = new SvgUnicodeTextAnalyzer().AnalyzeWithTextAndOptions(text, unicodeOptions);
        if (!unicode.Success)
        {
            if (unicode.Error == SvgUnicodeTextError.Cancelled) return new SvgOpenTypeLayout().Fail(result, SvgShapingError.Cancelled, unicode.Message);
            if (unicode.Error == SvgUnicodeTextError.LimitExceeded) return new SvgOpenTypeLayout().Fail(result, SvgShapingError.LimitExceeded, unicode.Message);
            return new SvgOpenTypeLayout().Fail(result, SvgShapingError.InvalidArgument, unicode.Message);
        }
        return new SvgOpenTypeLayout().ShapeWithSvgFontAndSvgUnicodeTextAndSvgShapingOptions(font, unicode.UnicodeText, options);
    }

    /// <summary>Shapes a previously normalized Unicode text model.</summary>
    /// <param name="font">A successfully parsed OpenType font.</param>
    /// <param name="text">The normalized Unicode text model.</param>
    /// <param name="options">The shaping configuration and security bounds.</param>
    /// <returns>The mapped and positioned glyph sequence.</returns>
    public SvgShapingResult ShapeWithSvgFontAndSvgUnicodeTextAndSvgShapingOptions(SvgFont? font, SvgUnicodeText text, SvgShapingOptions options)
    {
        SvgShapingResult result = new SvgShapingResult();
        if (options != null) result.FontIdentifier = options.FontIdentifier;
        if (!new SvgOpenTypeLayout().ValidWithSvgFontAndSvgUnicodeTextAndSvgShapingOptions(font, text, options)) return new SvgOpenTypeLayout().Fail(result, SvgShapingError.InvalidArgument, "OpenType shaping model or options are invalid.");
        if (text.Scalars.Count > (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaxGlyphs) return new SvgOpenTypeLayout().Fail(result, SvgShapingError.LimitExceeded, "Initial shaping input exceeds MaxGlyphs.");
        result.Text = text.Text;
        SvgShapingState state = new SvgShapingState(font, text, options, result);
        if (!new SvgOpenTypeLayout().NormalizeVariations(state)) return result;
        if (!new SvgOpenTypeLayout().MapGlyphs(state)) return result;
        if (!new SvgOpenTypeLayout().ApplyLayoutTable(state, "GSUB", true)) return result;
        if (!new SvgOpenTypeLayout().AssignGlyphClasses(state)) return result;
        if (!new SvgOpenTypeLayout().InitializeAdvances(state)) return result;
        if (!new SvgOpenTypeLayout().ApplyLayoutTable(state, "GPOS", false)) return result;
        if (!state.GposKerning && new SvgOpenTypeLayout().FeatureEnabled(options, "kern"))
        {
            if (!new SvgOpenTypeLayout().ApplyKern(state)) return result;
        }
        result.Success = true;
        return SvgShapingCallbacks.Complete(font ?? throw new System.InvalidOperationException("A required SVG value is unavailable."), text, options, result);
    }

    /// <summary>Shapes text with defaults while deriving stable font identity from a registered face.</summary>
    /// <param name="face">The registered font face.</param>
    /// <param name="text">The UTF-16 source text.</param>
    /// <returns>The baseline glyph sequence.</returns>
    public SvgShapingResult ShapeFontFaceWithFaceAndText(SvgFontFace face, string text)
    {
        return new SvgOpenTypeLayout().ShapeFontFaceWithFaceAndTextAndOptions(face, text, new SvgShapingOptions());
    }

    /// <summary>Shapes text while deriving stable font identity from a registered face.</summary>
    /// <param name="face">The registered font face.</param>
    /// <param name="text">The UTF-16 source text.</param>
    /// <param name="options">The shaping configuration and optional callback.</param>
    /// <returns>The baseline or validated callback glyph sequence.</returns>
    public SvgShapingResult ShapeFontFaceWithFaceAndTextAndOptions(SvgFontFace? face, string text, SvgShapingOptions? options)
    {
        if (face == null || options == null) return new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(null, text, (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        SvgShapingOptions copy = new SvgOpenTypeLayout().CopyOptions(options);
        copy.FontIdentifier = face.Identifier;
        return new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(face.Font, text, copy);
    }
SvgShapingOptions CopyOptions(SvgShapingOptions source)
    {
        SvgShapingOptions result = new SvgShapingOptions();
        result.ScriptTag = source.ScriptTag;
        result.LanguageTag = source.LanguageTag;
        result.Features.AddRange(source.Features);
        result.VariationCoordinates.AddRange(source.VariationCoordinates);
        result.ShapingCallback = source.ShapingCallback;
        result.FontIdentifier = source.FontIdentifier;
        result.EmSize = source.EmSize;
        result.Vertical = source.Vertical;
        result.MaxGlyphs = source.MaxGlyphs;
        result.MaxLookups = source.MaxLookups;
        result.MaxOperations = source.MaxOperations;
        result.MaxVariationAxes = source.MaxVariationAxes;
        result.CancellationToken = source.CancellationToken;
        return result;
    }
bool MapGlyphs(SvgShapingState state)
    {
        int index = 0;
        while (index < state.Text.Scalars.Count)
        {
            if (!new SvgOpenTypeLayout().Operation(state)) return false;
            SvgUnicodeScalar scalar = state.Text.Scalars[index];
            if (scalar.IsVariationSelector)
            {
                index = index + 1;
                continue;
            }
            int selector = 0;
            if (index + 1 < state.Text.Scalars.Count && state.Text.Scalars[index + 1].IsVariationSelector) selector = state.Text.Scalars[index + 1].Value;
            SvgOpenTypeGlyphMapping mapping = selector == 0 ? (state.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MapCodePoint(scalar.Value) : (state.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MapVariationSequence(scalar.Value, selector);
            if (!mapping.Success) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "Character mapping failed during shaping.");
            SvgShapedGlyph glyph = new SvgShapedGlyph();
            glyph.GlyphIndex = mapping.GlyphIndex;
            glyph.Missing = mapping.Missing;
            glyph.ScalarStart = index;
            glyph.ScalarCount = selector == 0 ? 1 : 2;
            glyph.Utf16Start = scalar.Utf16Start;
            glyph.Utf16Length = scalar.Utf16Length + (selector == 0 ? 0 : state.Text.Scalars[index + 1].Utf16Length);
            state.Result.Glyphs.Add(glyph);
            index = index + glyph.ScalarCount;
        }
        return true;
    }
bool InitializeAdvances(SvgShapingState state)
    {
        int index = 0;
        while (index < state.Result.Glyphs.Count)
        {
            if (!new SvgOpenTypeLayout().Operation(state)) return false;
            SvgShapedGlyph glyph = state.Result.Glyphs[index];
            SvgOpenTypeGlyphMetrics metrics = (state.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GetGlyphMetrics(glyph.GlyphIndex, state.Options.EmSize);
            if (!metrics.Success) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "Glyph metrics are invalid during shaping.");
            if (state.Options.Vertical)
            {
                glyph.XAdvance = 0;
                glyph.YAdvance = metrics.HasVerticalMetrics ? metrics.ScaledAdvanceHeight : state.Options.EmSize;
            }
            else
            {
                glyph.XAdvance = metrics.ScaledAdvanceWidth;
                glyph.YAdvance = 0;
            }
            if (glyph.GlyphClass == SvgOpenTypeGlyphClass.Mark)
            {
                glyph.XAdvance = 0;
                glyph.YAdvance = 0;
            }
            index = index + 1;
        }
        return true;
    }
bool AssignGlyphClasses(SvgShapingState state)
    {
        SvgOpenTypeTable? table = (state.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FindTable("GDEF");
        if (table == null) return true;
        SvgLayoutReader reader = new SvgLayoutReader(state.Font.Bytes, table);
        if (!reader.Valid(0, 12) || reader.U16(0) != 1) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "The GDEF header is invalid.");
        int classOffset = reader.U16(4);
        if (classOffset == 0) return true;
        int index = 0;
        while (index < state.Result.Glyphs.Count)
        {
            int glyphClass = new SvgOpenTypeLayout().ClassDefinition(reader, classOffset, state.Result.Glyphs[index].GlyphIndex);
            if (glyphClass < 0 || glyphClass > 4) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A GDEF glyph class is invalid.");
            state.Result.Glyphs[index].GlyphClass = (SvgOpenTypeGlyphClass)glyphClass;
            index = index + 1;
        }
        return true;
    }
bool NormalizeVariations(SvgShapingState state)
    {
        SvgOpenTypeTable? table = (state.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FindTable("fvar");
        if (table == null) return state.Options.VariationCoordinates.Count == 0 || new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidArgument, "Variation coordinates require an fvar table.");
        SvgLayoutReader reader = new SvgLayoutReader(state.Font.Bytes, table);
        if (!reader.Valid(0, 16) || reader.U32(0) != 0x00010000) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "The fvar header is invalid.");
        int axesOffset = reader.U16(4);
        int axisCount = reader.U16(8);
        int axisSize = reader.U16(10);
        if (axisCount < 0 || axisCount > state.Options.MaxVariationAxes) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.LimitExceeded, "Variation axes exceed MaxVariationAxes.");
        if (axisSize < 20 || !reader.Valid(axesOffset, axisCount * axisSize)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "The fvar axis array is invalid.");
        int axis = 0;
        while (axis < axisCount)
        {
            int offset = axesOffset + axis * axisSize;
            string tag = reader.Tag(offset);
            double minimum = new SvgOpenTypeLayout().Fixed(reader.U32(offset + 4));
            double defaultValue = new SvgOpenTypeLayout().Fixed(reader.U32(offset + 8));
            double maximum = new SvgOpenTypeLayout().Fixed(reader.U32(offset + 12));
            if (tag.Length != 4 || minimum > defaultValue || defaultValue > maximum) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "An fvar axis is invalid.");
            double requested = new SvgOpenTypeLayout().Coordinate(state.Options, tag, defaultValue);
            double value = requested < minimum ? minimum : requested > maximum ? maximum : requested;
            double normalized = value == defaultValue ? 0 : value < defaultValue ? (value - defaultValue) / (defaultValue - minimum) : (value - defaultValue) / (maximum - defaultValue);
            SvgNormalizedVariationCoordinate coordinate = new SvgNormalizedVariationCoordinate();
            coordinate.AxisTag = tag;
            coordinate.RequestedValue = requested;
            coordinate.Value = value;
            coordinate.NormalizedValue = normalized;
            state.Result.VariationCoordinates.Add(coordinate);
            axis = axis + 1;
        }
        int requestedIndex = 0;
        while (requestedIndex < state.Options.VariationCoordinates.Count)
        {
            bool found = false;
            axis = 0;
            while (axis < state.Result.VariationCoordinates.Count)
            {
                if (state.Result.VariationCoordinates[axis].AxisTag == state.Options.VariationCoordinates[requestedIndex].AxisTag) found = true;
                axis = axis + 1;
            }
            if (!found) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidArgument, "A requested variation axis is absent.");
            requestedIndex = requestedIndex + 1;
        }
        return new SvgOpenTypeLayout().ApplyAvar(state);
    }
bool ApplyAvar(SvgShapingState state)
    {
        SvgOpenTypeTable? table = (state.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FindTable("avar");
        if (table == null || state.Result.VariationCoordinates.Count == 0) return true;
        SvgLayoutReader reader = new SvgLayoutReader(state.Font.Bytes, table);
        if (!reader.Valid(0, 8) || reader.U16(0) != 1 || reader.U16(2) != 0 || reader.U16(6) != state.Result.VariationCoordinates.Count) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "The avar header is invalid.");
        int offset = 8;
        int axis = 0;
        while (axis < state.Result.VariationCoordinates.Count)
        {
            int count = reader.U16(offset);
            if (count < 2 || !reader.Valid(offset + 2, count * 4)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "An avar segment map is invalid.");
            double input = state.Result.VariationCoordinates[axis].NormalizedValue;
            double mapped = input;
            int point = 0;
            double priorFrom = new SvgOpenTypeLayout().F2Dot14(reader.I16(offset + 2));
            double priorTo = new SvgOpenTypeLayout().F2Dot14(reader.I16(offset + 4));
            while (point + 1 < count)
            {
                double nextFrom = new SvgOpenTypeLayout().F2Dot14(reader.I16(offset + 2 + (point + 1) * 4));
                double nextTo = new SvgOpenTypeLayout().F2Dot14(reader.I16(offset + 4 + (point + 1) * 4));
                if (input >= priorFrom && input <= nextFrom)
                {
                    mapped = nextFrom == priorFrom ? priorTo : priorTo + (input - priorFrom) * (nextTo - priorTo) / (nextFrom - priorFrom);
                    break;
                }
                priorFrom = nextFrom;
                priorTo = nextTo;
                point = point + 1;
            }
            state.Result.VariationCoordinates[axis].NormalizedValue = mapped;
            offset = offset + 2 + count * 4;
            axis = axis + 1;
        }
        return true;
    }
bool ApplyLayoutTable(SvgShapingState state, string tag, bool substitution)
    {
        SvgOpenTypeTable? table = (state.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FindTable(tag);
        if (table == null) return true;
        SvgLayoutReader reader = new SvgLayoutReader(state.Font.Bytes, table);
        if (!reader.Valid(0, 10) || reader.U16(0) != 1) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "An OpenType layout header is invalid.");
        int scriptList = reader.U16(4);
        int featureList = reader.U16(6);
        int lookupList = reader.U16(8);
        System.Collections.Generic.List<SvgLayoutLookupSelection>? selected = new SvgOpenTypeLayout().SelectLookups(state, reader, scriptList, featureList);
        if (selected == null) return false;
        if (selected.Count > state.Options.MaxLookups) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.LimitExceeded, "Selected layout lookups exceed MaxLookups.");
        int selection = 0;
        while (selection < selected.Count)
        {
            if (!new SvgOpenTypeLayout().ApplyLookup(state, reader, lookupList, selected[selection], substitution)) return false;
            selection = selection + 1;
        }
        return true;
    }
System.Collections.Generic.List<SvgLayoutLookupSelection>? SelectLookups(SvgShapingState state, SvgLayoutReader reader, int scriptList, int featureList)
    {
        System.Collections.Generic.List<SvgLayoutLookupSelection> result = new System.Collections.Generic.List<SvgLayoutLookupSelection>();
        int langSys = new SvgOpenTypeLayout().FindLangSys(reader, scriptList, state.Options.ScriptTag, state.Options.LanguageTag);
        if (langSys < 0) return result;
        if (!reader.Valid(langSys, 6))
        {
            new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A language-system table is invalid.");
            return null;
        }
        int required = reader.U16(langSys + 2);
        int count = reader.U16(langSys + 4);
        if (!reader.Valid(langSys + 6, count * 2))
        {
            new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "Language-system feature indices are invalid.");
            return null;
        }
        int featureCount = reader.U16(featureList);
        if (featureCount < 0 || !reader.Valid(featureList + 2, featureCount * 6))
        {
            new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "The feature list is invalid.");
            return null;
        }
        int item = -1;
        while (item < count)
        {
            int featureIndex = item < 0 ? required : reader.U16(langSys + 6 + item * 2);
            if (featureIndex != 0xFFFF)
            {
                if (featureIndex < 0 || featureIndex >= featureCount)
                {
                    new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A language-system feature index is invalid.");
                    return null;
                }
                int record = featureList + 2 + featureIndex * 6;
                string featureTag = reader.Tag(record);
                if (item < 0 || new SvgOpenTypeLayout().FeatureEnabled(state.Options, featureTag))
                {
                    int feature = featureList + reader.U16(record + 4);
                    int lookupCount = reader.U16(feature + 2);
                    if (!reader.Valid(feature, 4) || !reader.Valid(feature + 4, lookupCount * 2))
                    {
                        new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A feature table is invalid.");
                        return null;
                    }
                    new SvgOpenTypeLayout().AddFeature(state.Result.AppliedFeatures, featureTag);
                    int lookup = 0;
                    while (lookup < lookupCount)
                    {
                        new SvgOpenTypeLayout().AddLookup(result, reader.U16(feature + 4 + lookup * 2), featureTag);
                        lookup = lookup + 1;
                    }
                }
            }
            item = item + 1;
        }
        new SvgOpenTypeLayout().SortLookups(result);
        return result;
    }
int FindLangSys(SvgLayoutReader reader, int scriptList, string scriptTag, string languageTag)
    {
        int count = reader.U16(scriptList);
        if (count < 0 || !reader.Valid(scriptList + 2, count * 6)) return -1;
        int script = -1;
        int fallback = -1;
        int index = 0;
        while (index < count)
        {
            int record = scriptList + 2 + index * 6;
            string tag = reader.Tag(record);
            if (tag == scriptTag) script = scriptList + reader.U16(record + 4);
            if (tag == "DFLT") fallback = scriptList + reader.U16(record + 4);
            index = index + 1;
        }
        if (script < 0) script = fallback;
        if (script < 0 || !reader.Valid(script, 4)) return -1;
        int defaultOffset = reader.U16(script);
        int languages = reader.U16(script + 2);
        if (!reader.Valid(script + 4, languages * 6)) return -1;
        index = 0;
        while (index < languages)
        {
            int record = script + 4 + index * 6;
            if (reader.Tag(record) == languageTag) return script + reader.U16(record + 4);
            index = index + 1;
        }
        return defaultOffset == 0 ? -1 : script + defaultOffset;
    }
bool ApplyLookup(SvgShapingState state, SvgLayoutReader reader, int lookupList, SvgLayoutLookupSelection selection, bool substitution)
    {
        int count = reader.U16(lookupList);
        if (selection.Index < 0 || selection.Index >= count || !reader.Valid(lookupList + 2, count * 2)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A layout lookup index is invalid.");
        int lookup = lookupList + reader.U16(lookupList + 2 + selection.Index * 2);
        int type = reader.U16(lookup);
        int subtableCount = reader.U16(lookup + 4);
        if (!reader.Valid(lookup, 6) || !reader.Valid(lookup + 6, subtableCount * 2)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A layout lookup table is invalid.");
        bool supported = substitution ? type >= 1 && type <= 4 || type == 7 : type >= 1 && type <= 4 || type == 6 || type == 9;
        if (!supported)
        {
            new SvgOpenTypeLayout().AddUnsupported(state.Result.UnsupportedLookupTypes, substitution ? type : 100 + type);
            return true;
        }
        int subtable = 0;
        while (subtable < subtableCount)
        {
            int offset = lookup + reader.U16(lookup + 6 + subtable * 2);
            if (substitution)
            {
                if (!new SvgOpenTypeLayout().ApplyGsub(state, reader, type, offset)) return false;
            }
            else
            {
                if (!new SvgOpenTypeLayout().ApplyGpos(state, reader, type, offset)) return false;
            }
            subtable = subtable + 1;
        }
        return true;
    }
bool ApplyGsub(SvgShapingState state, SvgLayoutReader reader, int type, int offset)
    {
        if (type == 7)
        {
            if (!reader.Valid(offset, 8) || reader.U16(offset) != 1) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A GSUB extension subtable is invalid.");
            int extensionType = reader.U16(offset + 2);
            uint extensionOffset = reader.U32(offset + 4);
            if (extensionOffset > int.MaxValue || extensionType == 7) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A GSUB extension offset is invalid.");
            if (extensionType < 1 || extensionType > 4)
            {
                new SvgOpenTypeLayout().AddUnsupported(state.Result.UnsupportedLookupTypes, extensionType);
                return true;
            }
            return new SvgOpenTypeLayout().ApplyGsub(state, reader, extensionType, offset + (int)extensionOffset);
        }
        int format = reader.U16(offset);
        int coverage = offset + reader.U16(offset + 2);
        int glyph = 0;
        while (glyph < state.Result.Glyphs.Count)
        {
            int coverageIndex = new SvgOpenTypeLayout().Coverage(reader, coverage, state.Result.Glyphs[glyph].GlyphIndex);
            if (coverageIndex >= 0)
            {
                if (!new SvgOpenTypeLayout().Operation(state)) return false;
                if (type == 1)
                {
                    if (format == 1) state.Result.Glyphs[glyph].GlyphIndex = (state.Result.Glyphs[glyph].GlyphIndex + reader.I16(offset + 4)) & 0xFFFF;
                    else if (format == 2)
                    {
                        int count = reader.U16(offset + 4);
                        if (coverageIndex >= count || !reader.Valid(offset + 6, count * 2)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A single-substitution array is invalid.");
                        state.Result.Glyphs[glyph].GlyphIndex = reader.U16(offset + 6 + coverageIndex * 2);
                    }
                    else return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A single-substitution format is invalid.");
                }
                else if (type == 2)
                {
                    if (format != 1) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A multiple-substitution format is invalid.");
                    int count = reader.U16(offset + 4);
                    if (coverageIndex >= count || !reader.Valid(offset + 6, count * 2)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A multiple-substitution sequence array is invalid.");
                    int sequence = offset + reader.U16(offset + 6 + coverageIndex * 2);
                    int replacements = reader.U16(sequence);
                    if (replacements < 1 || !reader.Valid(sequence + 2, replacements * 2)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A multiple-substitution sequence is invalid.");
                    if ((long)state.Result.Glyphs.Count - 1 + replacements > state.Options.MaxGlyphs) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.LimitExceeded, "Multiple substitution exceeds MaxGlyphs.");
                    SvgShapedGlyph source = state.Result.Glyphs[glyph];
                    state.Result.Glyphs.RemoveAt(glyph);
                    int replacement = 0;
                    while (replacement < replacements)
                    {
                        SvgShapedGlyph copy = new SvgOpenTypeLayout().CopyGlyph(source);
                        copy.GlyphIndex = reader.U16(sequence + 2 + replacement * 2);
                        state.Result.Glyphs.Insert(glyph + replacement, copy);
                        replacement = replacement + 1;
                    }
                    glyph = glyph + replacements - 1;
                }
                else if (type == 3)
                {
                    if (format != 1) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "An alternate-substitution format is invalid.");
                    int count = reader.U16(offset + 4);
                    if (coverageIndex >= count || !reader.Valid(offset + 6, count * 2)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "An alternate-substitution set array is invalid.");
                    int set = offset + reader.U16(offset + 6 + coverageIndex * 2);
                    int alternates = reader.U16(set);
                    if (alternates < 1 || !reader.Valid(set + 2, alternates * 2)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "An alternate-substitution set is invalid.");
                    state.Result.Glyphs[glyph].GlyphIndex = reader.U16(set + 2);
                }
                else if (type == 4)
                {
                    if (!new SvgOpenTypeLayout().ApplyLigature(state, reader, offset, format, coverageIndex, glyph)) return false;
                }
            }
            glyph = glyph + 1;
        }
        return true;
    }
bool ApplyLigature(SvgShapingState state, SvgLayoutReader reader, int offset, int format, int coverageIndex, int glyphIndex)
    {
        if (format != 1) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A ligature-substitution format is invalid.");
        int setCount = reader.U16(offset + 4);
        if (coverageIndex >= setCount || !reader.Valid(offset + 6, setCount * 2)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A ligature-set array is invalid.");
        int set = offset + reader.U16(offset + 6 + coverageIndex * 2);
        int ligatureCount = reader.U16(set);
        if (!reader.Valid(set + 2, ligatureCount * 2)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A ligature set is invalid.");
        int ligature = 0;
        while (ligature < ligatureCount)
        {
            int record = set + reader.U16(set + 2 + ligature * 2);
            int componentCount = reader.U16(record + 2);
            if (componentCount < 2 || !reader.Valid(record + 4, (componentCount - 1) * 2)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A ligature record is invalid.");
            bool match = glyphIndex + componentCount <= state.Result.Glyphs.Count;
            int component = 1;
            while (match && component < componentCount)
            {
                if (state.Result.Glyphs[glyphIndex + component].GlyphIndex != reader.U16(record + 4 + (component - 1) * 2)) match = false;
                component = component + 1;
            }
            if (match)
            {
                SvgShapedGlyph first = state.Result.Glyphs[glyphIndex];
                SvgShapedGlyph last = state.Result.Glyphs[glyphIndex + componentCount - 1];
                first.GlyphIndex = reader.U16(record);
                first.ScalarCount = last.ScalarStart + last.ScalarCount - first.ScalarStart;
                first.Utf16Length = last.Utf16Start + last.Utf16Length - first.Utf16Start;
                first.LigatureComponent = componentCount;
                int remove = 1;
                while (remove < componentCount)
                {
                    state.Result.Glyphs.RemoveAt(glyphIndex + 1);
                    remove = remove + 1;
                }
                return true;
            }
            ligature = ligature + 1;
        }
        return true;
    }
bool ApplyGpos(SvgShapingState state, SvgLayoutReader reader, int type, int offset)
    {
        if (type == 9)
        {
            if (!reader.Valid(offset, 8) || reader.U16(offset) != 1) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A GPOS extension subtable is invalid.");
            int extensionType = reader.U16(offset + 2);
            uint extensionOffset = reader.U32(offset + 4);
            if (extensionOffset > int.MaxValue || extensionType == 9) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A GPOS extension offset is invalid.");
            if (extensionType < 1 || extensionType > 4 && extensionType != 6)
            {
                new SvgOpenTypeLayout().AddUnsupported(state.Result.UnsupportedLookupTypes, 100 + extensionType);
                return true;
            }
            return new SvgOpenTypeLayout().ApplyGpos(state, reader, extensionType, offset + (int)extensionOffset);
        }
        if (type == 1) return new SvgOpenTypeLayout().ApplySinglePosition(state, reader, offset);
        if (type == 2) return new SvgOpenTypeLayout().ApplyPairPosition(state, reader, offset);
        if (type == 3) return new SvgOpenTypeLayout().ApplyCursivePosition(state, reader, offset);
        if (type == 4) return new SvgOpenTypeLayout().ApplyMarkBasePosition(state, reader, offset, false);
        if (type == 6) return new SvgOpenTypeLayout().ApplyMarkBasePosition(state, reader, offset, true);
        return true;
    }
bool ApplySinglePosition(SvgShapingState state, SvgLayoutReader reader, int offset)
    {
        int format = reader.U16(offset);
        int coverage = offset + reader.U16(offset + 2);
        int valueFormat = reader.U16(offset + 4);
        if (format != 1 && format != 2) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A single-positioning format is invalid.");
        SvgLayoutValue? sample = new SvgOpenTypeLayout().ReadValue(reader, offset + (format == 1 ? 6 : 8), valueFormat);
        if (sample == null) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A single-positioning value is invalid.");
        int count = format == 2 ? reader.U16(offset + 6) : 1;
        if (format == 2 && !reader.Valid(offset + 8, count * sample.Size)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A single-positioning value array is invalid.");
        int glyph = 0;
        while (glyph < state.Result.Glyphs.Count)
        {
            int coverageIndex = new SvgOpenTypeLayout().Coverage(reader, coverage, state.Result.Glyphs[glyph].GlyphIndex);
            if (coverageIndex >= 0)
            {
                if (coverageIndex >= count) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A single-positioning coverage index is invalid.");
                if (!new SvgOpenTypeLayout().Operation(state)) return false;
                SvgLayoutValue? value = format == 1 ? sample : new SvgOpenTypeLayout().ReadValue(reader, offset + 8 + coverageIndex * sample.Size, valueFormat);
                new SvgOpenTypeLayout().ApplyValue(state, state.Result.Glyphs[glyph], value);
            }
            glyph = glyph + 1;
        }
        return true;
    }
bool ApplyPairPosition(SvgShapingState state, SvgLayoutReader reader, int offset)
    {
        int format = reader.U16(offset);
        int coverage = offset + reader.U16(offset + 2);
        int valueFormat1 = reader.U16(offset + 4);
        int valueFormat2 = reader.U16(offset + 6);
        SvgLayoutValue? firstSample = new SvgOpenTypeLayout().ReadValue(reader, offset + (format == 1 ? 10 : 16), valueFormat1);
        SvgLayoutValue? secondSample = new SvgOpenTypeLayout().ReadValue(reader, offset + (format == 1 ? 10 : 16) + (firstSample == null ? 0 : firstSample.Size), valueFormat2);
        if (firstSample == null || secondSample == null) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A pair-positioning value format is invalid.");
        int glyph = 0;
        while (glyph + 1 < state.Result.Glyphs.Count)
        {
            int coverageIndex = new SvgOpenTypeLayout().Coverage(reader, coverage, state.Result.Glyphs[glyph].GlyphIndex);
            if (coverageIndex >= 0)
            {
                SvgLayoutValue? first = null;
                SvgLayoutValue? second = null;
                if (format == 1)
                {
                    int pairSetCount = reader.U16(offset + 8);
                    if (coverageIndex >= pairSetCount || !reader.Valid(offset + 10, pairSetCount * 2)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A pair-set array is invalid.");
                    int pairSet = offset + reader.U16(offset + 10 + coverageIndex * 2);
                    int pairCount = reader.U16(pairSet);
                    int recordSize = 2 + firstSample.Size + secondSample.Size;
                    if (!reader.Valid(pairSet + 2, pairCount * recordSize)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A pair-value array is invalid.");
                    int pair = 0;
                    while (pair < pairCount)
                    {
                        int record = pairSet + 2 + pair * recordSize;
                        if (reader.U16(record) == state.Result.Glyphs[glyph + 1].GlyphIndex)
                        {
                            first = new SvgOpenTypeLayout().ReadValue(reader, record + 2, valueFormat1);
                            second = new SvgOpenTypeLayout().ReadValue(reader, record + 2 + firstSample.Size, valueFormat2);
                            break;
                        }
                        pair = pair + 1;
                    }
                }
                else if (format == 2)
                {
                    int class1 = new SvgOpenTypeLayout().ClassDefinition(reader, offset + reader.U16(offset + 8), state.Result.Glyphs[glyph].GlyphIndex);
                    int class2 = new SvgOpenTypeLayout().ClassDefinition(reader, offset + reader.U16(offset + 10), state.Result.Glyphs[glyph + 1].GlyphIndex);
                    int class1Count = reader.U16(offset + 12);
                    int class2Count = reader.U16(offset + 14);
                    int recordSize = firstSample.Size + secondSample.Size;
                    if (class1 < 0 || class2 < 0 || class1 >= class1Count || class2 >= class2Count || !reader.Valid(offset + 16, class1Count * class2Count * recordSize)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A class pair-positioning matrix is invalid.");
                    int record = offset + 16 + (class1 * class2Count + class2) * recordSize;
                    first = new SvgOpenTypeLayout().ReadValue(reader, record, valueFormat1);
                    second = new SvgOpenTypeLayout().ReadValue(reader, record + firstSample.Size, valueFormat2);
                }
                else return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A pair-positioning format is invalid.");
                if (first != null && second != null)
                {
                    if (!new SvgOpenTypeLayout().Operation(state)) return false;
                    new SvgOpenTypeLayout().ApplyValue(state, state.Result.Glyphs[glyph], first);
                    new SvgOpenTypeLayout().ApplyValue(state, state.Result.Glyphs[glyph + 1], second);
                    state.GposKerning = true;
                }
            }
            glyph = glyph + 1;
        }
        return true;
    }
bool ApplyCursivePosition(SvgShapingState state, SvgLayoutReader reader, int offset)
    {
        if (reader.U16(offset) != 1) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A cursive-positioning format is invalid.");
        int coverage = offset + reader.U16(offset + 2);
        int count = reader.U16(offset + 4);
        if (!reader.Valid(offset + 6, count * 4)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A cursive entry-exit array is invalid.");
        int glyph = 1;
        while (glyph < state.Result.Glyphs.Count)
        {
            int previousCoverage = new SvgOpenTypeLayout().Coverage(reader, coverage, state.Result.Glyphs[glyph - 1].GlyphIndex);
            int currentCoverage = new SvgOpenTypeLayout().Coverage(reader, coverage, state.Result.Glyphs[glyph].GlyphIndex);
            if (previousCoverage >= 0 && currentCoverage >= 0 && previousCoverage < count && currentCoverage < count)
            {
                int exitOffset = reader.U16(offset + 6 + previousCoverage * 4 + 2);
                int entryOffset = reader.U16(offset + 6 + currentCoverage * 4);
                if (exitOffset != 0 && entryOffset != 0)
                {
                    int[]? exitAnchor = new SvgOpenTypeLayout().Anchor(reader, offset + exitOffset);
                    int[]? entryAnchor = new SvgOpenTypeLayout().Anchor(reader, offset + entryOffset);
                    if (exitAnchor == null || entryAnchor == null) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A cursive anchor is invalid.");
                    if (!new SvgOpenTypeLayout().Operation(state)) return false;
                    SvgShapedGlyph previous = state.Result.Glyphs[glyph - 1];
                    SvgShapedGlyph current = state.Result.Glyphs[glyph];
                    current.XOffset = previous.XOffset + previous.XAdvance + (exitAnchor[0] - entryAnchor[0]) * state.Scale;
                    current.YOffset = previous.YOffset + (exitAnchor[1] - entryAnchor[1]) * state.Scale;
                    current.AttachmentIndex = glyph - 1;
                }
            }
            glyph = glyph + 1;
        }
        return true;
    }
bool ApplyMarkBasePosition(SvgShapingState state, SvgLayoutReader reader, int offset, bool markToMark)
    {
        if (reader.U16(offset) != 1 || !reader.Valid(offset, 12)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A mark-positioning format is invalid.");
        int markCoverage = offset + reader.U16(offset + 2);
        int baseCoverage = offset + reader.U16(offset + 4);
        int classCount = reader.U16(offset + 6);
        int markArray = offset + reader.U16(offset + 8);
        int baseArray = offset + reader.U16(offset + 10);
        int markCount = reader.U16(markArray);
        if (classCount < 1 || !reader.Valid(markArray + 2, markCount * 4)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A mark array is invalid.");
        int glyph = 1;
        while (glyph < state.Result.Glyphs.Count)
        {
            int markIndex = new SvgOpenTypeLayout().Coverage(reader, markCoverage, state.Result.Glyphs[glyph].GlyphIndex);
            if (markIndex >= 0)
            {
                if (markIndex >= markCount) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A mark coverage index is invalid.");
                int baseGlyph = glyph - 1;
                while (baseGlyph >= 0 && new SvgOpenTypeLayout().Coverage(reader, baseCoverage, state.Result.Glyphs[baseGlyph].GlyphIndex) < 0) baseGlyph = baseGlyph - 1;
                if (baseGlyph >= 0)
                {
                    int baseIndex = new SvgOpenTypeLayout().Coverage(reader, baseCoverage, state.Result.Glyphs[baseGlyph].GlyphIndex);
                    int baseCount = reader.U16(baseArray);
                    if (baseIndex >= baseCount || !reader.Valid(baseArray + 2, baseCount * classCount * 2)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A base anchor array is invalid.");
                    int markClass = reader.U16(markArray + 2 + markIndex * 4);
                    int markAnchorOffset = reader.U16(markArray + 4 + markIndex * 4);
                    if (markClass >= classCount) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A mark class is invalid.");
                    int baseAnchorOffset = reader.U16(baseArray + 2 + (baseIndex * classCount + markClass) * 2);
                    if (markAnchorOffset != 0 && baseAnchorOffset != 0)
                    {
                        int[]? markAnchor = new SvgOpenTypeLayout().Anchor(reader, markArray + markAnchorOffset);
                        int[]? baseAnchor = new SvgOpenTypeLayout().Anchor(reader, baseArray + baseAnchorOffset);
                        if (markAnchor == null || baseAnchor == null) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A mark attachment anchor is invalid.");
                        if (!new SvgOpenTypeLayout().Operation(state)) return false;
                        SvgShapedGlyph mark = state.Result.Glyphs[glyph];
                        SvgShapedGlyph baseValue = state.Result.Glyphs[baseGlyph];
                        mark.XOffset = baseValue.XOffset + (baseAnchor[0] - markAnchor[0]) * state.Scale;
                        mark.YOffset = baseValue.YOffset + (baseAnchor[1] - markAnchor[1]) * state.Scale;
                        mark.XAdvance = 0;
                        mark.YAdvance = 0;
                        mark.AttachmentIndex = baseGlyph;
                    }
                }
            }
            glyph = glyph + 1;
        }
        return true;
    }
bool ApplyKern(SvgShapingState state)
    {
        SvgOpenTypeTable? table = (state.Font ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FindTable("kern");
        if (table == null) return true;
        SvgLayoutReader reader = new SvgLayoutReader(state.Font.Bytes, table);
        int tables;
        int offset;
        if (reader.U16(0) == 0)
        {
            tables = reader.U16(2);
            offset = 4;
        }
        else return true;
        int subtable = 0;
        while (subtable < tables)
        {
            if (!reader.Valid(offset, 6)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A kern subtable header is invalid.");
            int length = reader.U16(offset + 2);
            int coverage = reader.U16(offset + 4);
            int format = coverage >> 8;
            bool horizontal = (coverage & 1) != 0;
            if (length < 6 || !reader.Valid(offset, length)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A kern subtable length is invalid.");
            if (format == 0 && horizontal == !state.Options.Vertical)
            {
                int pairs = reader.U16(offset + 6);
                if (!reader.Valid(offset + 14, pairs * 6)) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.InvalidTable, "A kern format-zero pair array is invalid.");
                int glyph = 0;
                while (glyph + 1 < state.Result.Glyphs.Count)
                {
                    int pair = new SvgOpenTypeLayout().FindKernPair(reader, offset + 14, pairs, state.Result.Glyphs[glyph].GlyphIndex, state.Result.Glyphs[glyph + 1].GlyphIndex);
                    if (pair >= 0)
                    {
                        if (!new SvgOpenTypeLayout().Operation(state)) return false;
                        double adjustment = reader.I16(pair + 4) * state.Scale;
                        if (state.Options.Vertical) state.Result.Glyphs[glyph].YAdvance = state.Result.Glyphs[glyph].YAdvance + adjustment;
                        else state.Result.Glyphs[glyph].XAdvance = state.Result.Glyphs[glyph].XAdvance + adjustment;
                    }
                    glyph = glyph + 1;
                }
            }
            offset = offset + length;
            subtable = subtable + 1;
        }
        return true;
    }
int FindKernPair(SvgLayoutReader reader, int offset, int count, int left, int right)
    {
        uint key = (uint)left << 16 | (uint)right;
        int low = 0;
        int high = count - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int record = offset + middle * 6;
            uint candidate = (uint)reader.U16(record) << 16 | (uint)reader.U16(record + 2);
            if (candidate == key) return record;
            if (candidate < key) low = middle + 1;
            else high = middle - 1;
        }
        return -1;
    }
SvgLayoutValue? ReadValue(SvgLayoutReader reader, int offset, int format)
    {
        if ((format & 0xFF00) != 0) return null;
        SvgLayoutValue result = new SvgLayoutValue();
        int cursor = offset;
        int bit = 1;
        while (bit <= 0x80)
        {
            if ((format & bit) != 0)
            {
                if (!reader.Valid(cursor, 2)) return null;
                int value = reader.I16(cursor);
                if (bit == 1) result.XPlacement = value;
                else if (bit == 2) result.YPlacement = value;
                else if (bit == 4) result.XAdvance = value;
                else if (bit == 8) result.YAdvance = value;
                cursor = cursor + 2;
            }
            bit = bit << 1;
        }
        result.Size = cursor - offset;
        return result;
    }
void ApplyValue(SvgShapingState state, SvgShapedGlyph glyph, SvgLayoutValue? value)
    {
        glyph.XOffset = glyph.XOffset + (value ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).XPlacement * state.Scale;
        glyph.YOffset = glyph.YOffset + value.YPlacement * state.Scale;
        glyph.XAdvance = glyph.XAdvance + value.XAdvance * state.Scale;
        glyph.YAdvance = glyph.YAdvance + value.YAdvance * state.Scale;
    }
int[]? Anchor(SvgLayoutReader reader, int offset)
    {
        int format = reader.U16(offset);
        if (format < 1 || format > 3 || !reader.Valid(offset, format == 1 ? 6 : 8)) return null;
        return new int[] { reader.I16(offset + 2), reader.I16(offset + 4) };
    }
int Coverage(SvgLayoutReader reader, int offset, int glyph)
    {
        int format = reader.U16(offset);
        int count = reader.U16(offset + 2);
        if (format == 1)
        {
            if (count < 0 || !reader.Valid(offset + 4, count * 2)) return -1;
            int low = 0;
            int high = count - 1;
            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                int value = reader.U16(offset + 4 + middle * 2);
                if (value == glyph) return middle;
                if (value < glyph) low = middle + 1;
                else high = middle - 1;
            }
            return -1;
        }
        if (format == 2)
        {
            if (count < 0 || !reader.Valid(offset + 4, count * 6)) return -1;
            int range = 0;
            while (range < count)
            {
                int record = offset + 4 + range * 6;
                int first = reader.U16(record);
                int last = reader.U16(record + 2);
                if (glyph >= first && glyph <= last) return reader.U16(record + 4) + glyph - first;
                range = range + 1;
            }
        }
        return -1;
    }
int ClassDefinition(SvgLayoutReader reader, int offset, int glyph)
    {
        int format = reader.U16(offset);
        if (format == 1)
        {
            int start = reader.U16(offset + 2);
            int count = reader.U16(offset + 4);
            if (!reader.Valid(offset + 6, count * 2)) return -1;
            if (glyph < start || glyph >= start + count) return 0;
            return reader.U16(offset + 6 + (glyph - start) * 2);
        }
        if (format == 2)
        {
            int count = reader.U16(offset + 2);
            if (!reader.Valid(offset + 4, count * 6)) return -1;
            int index = 0;
            while (index < count)
            {
                int record = offset + 4 + index * 6;
                if (glyph >= reader.U16(record) && glyph <= reader.U16(record + 2)) return reader.U16(record + 4);
                index = index + 1;
            }
            return 0;
        }
        return -1;
    }
SvgShapedGlyph CopyGlyph(SvgShapedGlyph source)
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
double Coordinate(SvgShapingOptions options, string tag, double defaultValue)
    {
        double result = defaultValue;
        int index = 0;
        while (index < options.VariationCoordinates.Count)
        {
            if (options.VariationCoordinates[index].AxisTag == tag) result = options.VariationCoordinates[index].Value;
            index = index + 1;
        }
        return result;
    }
double Fixed(uint value)
    {
        long signed = value >= 2147483648u ? (long)value - 4294967296L : value;
        return signed / 65536.0;
    }
double F2Dot14(int value)
    {
        return value / 16384.0;
    }
bool FeatureEnabled(SvgShapingOptions options, string feature)
    {
        if (options.Features.Count == 0)
        {
            if (options.Vertical) return feature == "ccmp" || feature == "locl" || feature == "rlig" || feature == "liga" || feature == "clig" || feature == "vert" || feature == "vrt2" || feature == "kern" || feature == "mark" || feature == "mkmk" || feature == "curs";
            return feature == "ccmp" || feature == "locl" || feature == "rlig" || feature == "liga" || feature == "clig" || feature == "kern" || feature == "mark" || feature == "mkmk" || feature == "curs";
        }
        int index = 0;
        while (index < options.Features.Count)
        {
            if (options.Features[index] == feature) return true;
            index = index + 1;
        }
        return false;
    }
void AddLookup(System.Collections.Generic.List<SvgLayoutLookupSelection> lookups, int index, string feature)
    {
        int current = 0;
        while (current < lookups.Count)
        {
            if (lookups[current].Index == index) return;
            current = current + 1;
        }
        lookups.Add(new SvgLayoutLookupSelection(index, feature));
    }
void SortLookups(System.Collections.Generic.List<SvgLayoutLookupSelection> lookups)
    {
        int index = 1;
        while (index < lookups.Count)
        {
            SvgLayoutLookupSelection value = lookups[index];
            int insertion = index - 1;
            while (insertion >= 0 && lookups[insertion].Index > value.Index)
            {
                lookups[insertion + 1] = lookups[insertion];
                insertion = insertion - 1;
            }
            lookups[insertion + 1] = value;
            index = index + 1;
        }
    }
void AddFeature(System.Collections.Generic.List<string> features, string feature)
    {
        int index = 0;
        while (index < features.Count)
        {
            if (features[index] == feature) return;
            index = index + 1;
        }
        features.Add(feature);
    }
void AddUnsupported(System.Collections.Generic.List<int> values, int value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value) return;
            index = index + 1;
        }
        values.Add(value);
    }
bool Operation(SvgShapingState state)
    {
        if (state.Options.CancellationToken.IsCancellationRequested) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.Cancelled, "OpenType shaping was cancelled.");
        if (state.Operations >= state.Options.MaxOperations) return new SvgOpenTypeLayout().FailState(state, SvgShapingError.LimitExceeded, "OpenType shaping exceeds MaxOperations.");
        state.Operations = state.Operations + 1;
        return true;
    }
bool ValidWithSvgFontAndStringAndSvgShapingOptions(SvgFont? font, string text, SvgShapingOptions? options)
    {
        return text != null && new SvgOpenTypeLayout().ValidWithFontAndOptions(font, options);
    }
bool ValidWithSvgFontAndSvgUnicodeTextAndSvgShapingOptions(SvgFont? font, SvgUnicodeText text, SvgShapingOptions? options)
    {
        return text != null && text.Scalars != null && new SvgOpenTypeLayout().ValidWithFontAndOptions(font, options);
    }
bool ValidWithFontAndOptions(SvgFont? font, SvgShapingOptions? options)
    {
        if (font == null || !font.Success || font.Bytes == null || font.Head == null || font.MaximumProfile == null || font.HorizontalMetrics == null || options == null) return false;
        if (options.ScriptTag == null || options.ScriptTag.Length != 4 || options.LanguageTag == null || options.LanguageTag.Length != 4 || options.Features == null || options.VariationCoordinates == null) return false;
        if (double.IsNaN(options.EmSize) || double.IsInfinity(options.EmSize) || options.EmSize < 0 || options.MaxGlyphs < 1 || options.MaxLookups < 1 || options.MaxOperations < 1 || options.MaxVariationAxes < 1) return false;
        int index = 0;
        while (index < options.Features.Count)
        {
            if (options.Features[index] == null || options.Features[index].Length != 4) return false;
            index = index + 1;
        }
        index = 0;
        while (index < options.VariationCoordinates.Count)
        {
            SvgFontVariationCoordinate coordinate = options.VariationCoordinates[index];
            if (coordinate == null || coordinate.AxisTag == null || coordinate.AxisTag.Length != 4 || double.IsNaN(coordinate.Value) || double.IsInfinity(coordinate.Value)) return false;
            index = index + 1;
        }
        return true;
    }
bool FailState(SvgShapingState state, SvgShapingError error, string message)
    {
        new SvgOpenTypeLayout().Fail(state.Result, error, message);
        return false;
    }
SvgShapingResult Fail(SvgShapingResult result, SvgShapingError error, string message)
    {
        if (result.Error == SvgShapingError.None) result.Error = error;
        result.Message = message;
        return result;
    }
}
