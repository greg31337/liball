namespace svg;

/// <summary>Decodes bounded CFF and CFF2 cubic glyph outlines.</summary>
public class SvgCffOutlineDecoder : ISvgCffOutlineDecoder
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgCffOutlineDecoder()
    {
    }

    /// <summary>Decodes one CFF or CFF2 glyph using bounded defaults.</summary>
    /// <param name="font">A successfully parsed OpenType font.</param>
    /// <param name="glyphIndex">The zero-based glyph index.</param>
    /// <returns>The decoded outline, stable failure, or caller-fallback classification.</returns>
    public SvgCffGlyphResult DecodeWithFontAndGlyphIndex(SvgFont font, int glyphIndex)
    {
        return new SvgCffOutlineDecoder().DecodeWithFontAndGlyphIndexAndOptions(font, glyphIndex, new SvgCffOutlineOptions());
    }

    /// <summary>Decodes one CFF or CFF2 glyph using explicit operand and subroutine limits.</summary>
    /// <param name="font">A successfully parsed OpenType font.</param>
    /// <param name="glyphIndex">The zero-based glyph index.</param>
    /// <param name="options">The bounded outline options.</param>
    /// <returns>The decoded outline, stable failure, or caller-fallback classification.</returns>
    public SvgCffGlyphResult DecodeWithFontAndGlyphIndexAndOptions(SvgFont font, int glyphIndex, SvgCffOutlineOptions options)
    {
        if (!new SvgCffOutlineDecoder().Valid(font, glyphIndex, options)) return new SvgCffOutlineDecoder().Result(false, SvgCffError.InvalidArgument, null, false, SvgCffFallbackReason.None, 0);
        if (font.OutlineKind != SvgOpenTypeOutlineKind.Cff && font.OutlineKind != SvgOpenTypeOutlineKind.Cff2) return new SvgCffOutlineDecoder().Result(false, SvgCffError.UnsupportedOutline, null, false, SvgCffFallbackReason.None, 0);
        string tag = font.OutlineKind == SvgOpenTypeOutlineKind.Cff ? "CFF " : "CFF2";
        SvgOpenTypeTable? table = font.FindTable(tag);
        if (table == null || !new SvgCffOutlineDecoder().TableRange(font.Bytes, table)) return new SvgCffOutlineDecoder().Result(false, SvgCffError.InvalidHeader, null, false, SvgCffFallbackReason.None, 0);
        SvgCffDecodeState state = new SvgCffDecodeState(font, options, table);
        if (!new SvgCffOutlineDecoder().Parse(state)) return new SvgCffOutlineDecoder().StateResult(state, null);
        if ((state.Parsed ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).CharStrings == null || (state.Parsed.CharStrings ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count != (font.MaximumProfile ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GlyphCount) return new SvgCffOutlineDecoder().Result(false, SvgCffError.InvalidIndex, null, false, SvgCffFallbackReason.None, 0);
        state.ActiveGlobalSubrs = new bool[state.Parsed.GlobalSubrs == null ? 0 : state.Parsed.GlobalSubrs.Count];
        state.ActiveLocalSubrs = new bool[state.Parsed.LocalSubrs == null ? 0 : state.Parsed.LocalSubrs.Count];
        int start = state.Parsed.CharStrings.Starts[glyphIndex];
        int length = state.Parsed.CharStrings.Lengths[glyphIndex];
        if (!new SvgCffOutlineDecoder().Execute(state, start, length, false, false, -1, 0)) return new SvgCffOutlineDecoder().StateResult(state, null);
        if (!state.Parsed.Cff2 && !state.Ended) return new SvgCffOutlineDecoder().Result(false, SvgCffError.InvalidCharString, null, false, SvgCffFallbackReason.None, 0);
        if (state.Stack.Count != 0) return new SvgCffOutlineDecoder().Result(false, SvgCffError.InvalidCharString, null, false, SvgCffFallbackReason.None, 0);
        new SvgCffOutlineDecoder().CloseContour(state);
        SvgCffGlyphOutline outline = new SvgCffGlyphOutline(glyphIndex, state.Parsed.Cff2);
        outline.Path = state.Builder.Build();
        outline.Empty = outline.Path.Commands.Count == 0;
        outline.ContourCount = state.ContourCount;
        outline.SubroutineCalls = state.SubroutineCalls;
        outline.HasHints = state.HasHints;
        return new SvgCffOutlineDecoder().Result(true, SvgCffError.None, outline, false, SvgCffFallbackReason.None, 0);
    }
bool Valid(SvgFont? font, int glyphIndex, SvgCffOutlineOptions? options)
    {
        if (font == null || !font.Success || font.Bytes == null || font.MaximumProfile == null) return false;
        if (glyphIndex < 0 || glyphIndex >= font.MaximumProfile.GlyphCount) return false;
        if (options == null || options.Limits == null || options.VariationCoordinates == null || options.Limits.MaxGlyphsPerFont < 1 || options.Limits.MaxPointsPerGlyph < 1) return false;
        if (font.MaximumProfile.GlyphCount > options.Limits.MaxGlyphsPerFont || options.MaximumOperands < 1 || options.MaximumSubroutineDepth < 0 || options.MaximumSubroutineDepth > 10 || options.MaximumSubroutineCalls < 1 || options.MaximumSubroutines < 0) return false;
        if (options.VariationCoordinates.Count > options.Limits.MaxVariationAxes) return false;
        int coordinate = 0;
        while (coordinate < options.VariationCoordinates.Count)
        {
            if (!SvgFiniteMath.IsFinite(options.VariationCoordinates[coordinate]) || options.VariationCoordinates[coordinate] < -1 || options.VariationCoordinates[coordinate] > 1) return false;
            coordinate = coordinate + 1;
        }
        return true;
    }
bool Parse(SvgCffDecodeState state)
    {
        if (state.Font.OutlineKind == SvgOpenTypeOutlineKind.Cff) return new SvgCffOutlineDecoder().ParseCff1(state);
        return new SvgCffOutlineDecoder().ParseCff2(state);
    }
bool ParseCff1(SvgCffDecodeState state)
    {
        if (state.Table.Length < 4) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidHeader);
        byte[]? bytes = state.Font.Bytes;
        int headerSize = (bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[state.Start + 2];
        int headerOffsetSize = bytes[state.Start + 3];
        if (bytes[state.Start] != 1 || headerSize < 4 || headerSize > state.Table.Length || headerOffsetSize < 1 || headerOffsetSize > 4) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidHeader);
        SvgCffIndexData? names = new SvgCffOutlineDecoder().ReadIndex(state, state.Start + headerSize, false, state.Options.Limits.MaxGlyphsPerFont);
        if (names == null || names.Count != 1) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidIndex);
        SvgCffIndexData? topIndex = new SvgCffOutlineDecoder().ReadIndex(state, names.End, false, state.Options.Limits.MaxGlyphsPerFont);
        if (topIndex == null || topIndex.Count != 1) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidIndex);
        SvgCffDictionaryData? top = new SvgCffOutlineDecoder().ReadDictionary(state, topIndex.Starts[0], topIndex.Lengths[0], 0, false);
        if (top == null) return false;
        SvgCffIndexData? strings = new SvgCffOutlineDecoder().ReadIndex(state, topIndex.End, false, new SvgCffOutlineDecoder().IndexLimit(state));
        if (strings == null) return false;
        SvgCffIndexData? global = new SvgCffOutlineDecoder().ReadIndex(state, strings.End, false, state.Options.MaximumSubroutines);
        if (global == null) return false;
        if (top.Cid) return new SvgCffOutlineDecoder().Fallback(state, SvgCffFallbackReason.CidFont, 0, SvgCffError.UnsupportedStructure);
        if (top.CharStringType != 2 || top.CharStringsOffset < 0) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidDictionary);
        SvgCffIndexData? charStrings = new SvgCffOutlineDecoder().ReadIndex(state, new SvgCffOutlineDecoder().Relative(state, top.CharStringsOffset), false, state.Options.Limits.MaxGlyphsPerFont);
        if (charStrings == null) return false;
        SvgCffIndexData? local = null;
        if (top.HasPrivate)
        {
            if (!new SvgCffOutlineDecoder().RelativeRange(state, top.PrivateOffset, top.PrivateSize)) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidDictionary);
            SvgCffDictionaryData? privateDictionary = new SvgCffOutlineDecoder().ReadDictionary(state, new SvgCffOutlineDecoder().Relative(state, top.PrivateOffset), top.PrivateSize, 2, false);
            if (privateDictionary == null) return false;
            if (privateDictionary.LocalSubrOffset >= 0)
            {
                long relative = (long)top.PrivateOffset + privateDictionary.LocalSubrOffset;
                if (relative > System.Int32.MaxValue) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidIndex);
                local = new SvgCffOutlineDecoder().ReadIndex(state, new SvgCffOutlineDecoder().Relative(state, (int)relative), false, state.Options.MaximumSubroutines);
                if (local == null) return false;
            }
        }
        state.Parsed = new SvgCffParsedFont(false);
        state.Parsed.CharStrings = charStrings;
        state.Parsed.GlobalSubrs = global;
        state.Parsed.LocalSubrs = local;
        return true;
    }
bool ParseCff2(SvgCffDecodeState state)
    {
        if (state.Table.Length < 5) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidHeader);
        byte[]? bytes = state.Font.Bytes;
        int headerSize = (bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[state.Start + 2];
        int topSize = new SvgCffOutlineDecoder().ReadUInt16(bytes, state.Start + 3);
        if (bytes[state.Start] != 2 || bytes[state.Start + 1] != 0 || headerSize < 5 || (long)headerSize + topSize > state.Table.Length) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidHeader);
        SvgCffDictionaryData? top = new SvgCffOutlineDecoder().ReadDictionary(state, state.Start + headerSize, topSize, 0, true);
        if (top == null) return false;
        SvgCffIndexData? global = new SvgCffOutlineDecoder().ReadIndex(state, state.Start + headerSize + topSize, true, state.Options.MaximumSubroutines);
        if (global == null) return false;
        if (top.CharStringsOffset < 0 || top.FontDictOffset < 0) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidDictionary);
        SvgCffIndexData? charStrings = new SvgCffOutlineDecoder().ReadIndex(state, new SvgCffOutlineDecoder().Relative(state, top.CharStringsOffset), true, state.Options.Limits.MaxGlyphsPerFont);
        if (charStrings == null) return false;
        SvgCffIndexData? fontDictionaries = new SvgCffOutlineDecoder().ReadIndex(state, new SvgCffOutlineDecoder().Relative(state, top.FontDictOffset), true, state.Options.Limits.MaxGlyphsPerFont);
        if (fontDictionaries == null || fontDictionaries.Count < 1) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidIndex);
        if (fontDictionaries.Count != 1 || top.FontDictSelectOffset >= 0) return new SvgCffOutlineDecoder().Fallback(state, SvgCffFallbackReason.UnsupportedStructure, 0, SvgCffError.UnsupportedStructure);
        SvgCffDictionaryData? fontDictionary = new SvgCffOutlineDecoder().ReadDictionary(state, fontDictionaries.Starts[0], fontDictionaries.Lengths[0], 1, true);
        if (fontDictionary == null || !fontDictionary.HasPrivate) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidDictionary);
        SvgCffIndexData? local = null;
        if (fontDictionary.PrivateSize != 0 || fontDictionary.PrivateOffset != 0)
        {
            if (!new SvgCffOutlineDecoder().RelativeRange(state, fontDictionary.PrivateOffset, fontDictionary.PrivateSize)) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidDictionary);
            SvgCffDictionaryData? privateDictionary = new SvgCffOutlineDecoder().ReadDictionary(state, new SvgCffOutlineDecoder().Relative(state, fontDictionary.PrivateOffset), fontDictionary.PrivateSize, 2, true);
            if (privateDictionary == null) return false;
            if (privateDictionary.LocalSubrOffset >= 0)
            {
                long relative = (long)fontDictionary.PrivateOffset + privateDictionary.LocalSubrOffset;
                if (relative > System.Int32.MaxValue) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidIndex);
                local = new SvgCffOutlineDecoder().ReadIndex(state, new SvgCffOutlineDecoder().Relative(state, (int)relative), true, state.Options.MaximumSubroutines);
                if (local == null) return false;
            }
        }
        state.Parsed = new SvgCffParsedFont(true);
        state.Parsed.CharStrings = charStrings;
        state.Parsed.GlobalSubrs = global;
        state.Parsed.LocalSubrs = local;
        state.Parsed.VariationStoreOffset = top.VariationStoreOffset;
        return true;
    }
SvgCffIndexData? ReadIndex(SvgCffDecodeState state, int start, bool cff2, int maximumCount)
    {
        int countWidth = cff2 ? 4 : 2;
        if (start < state.Start || (long)start + countWidth > state.End) return new SvgCffOutlineDecoder().IndexFail(state);
        uint countValue = cff2 ? new SvgCffOutlineDecoder().ReadUInt32(state.Font.Bytes, start) : (uint)new SvgCffOutlineDecoder().ReadUInt16(state.Font.Bytes, start);
        if (countValue > System.Int32.MaxValue || countValue > maximumCount) return new SvgCffOutlineDecoder().IndexFail(state);
        int count = (int)countValue;
        SvgCffIndexData result = new SvgCffIndexData(count);
        if (count == 0)
        {
            result.End = start + countWidth;
            return result;
        }
        int offsetSizePosition = start + countWidth;
        if (offsetSizePosition >= state.End) return new SvgCffOutlineDecoder().IndexFail(state);
        int offsetSize = (state.Font.Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[offsetSizePosition];
        if (offsetSize < 1 || offsetSize > 4) return new SvgCffOutlineDecoder().IndexFail(state);
        long offsetsLength = (long)(count + 1) * offsetSize;
        long dataStartValue = (long)offsetSizePosition + 1 + offsetsLength;
        if (dataStartValue > state.End) return new SvgCffOutlineDecoder().IndexFail(state);
        int dataStart = (int)dataStartValue;
        uint preceding = 0;
        int index = 0;
        while (index <= count)
        {
            uint offset = new SvgCffOutlineDecoder().ReadOffset(state.Font.Bytes, offsetSizePosition + 1 + index * offsetSize, offsetSize);
            if (index == 0 && offset != 1 || offset < 1 || index > 0 && offset < preceding) return new SvgCffOutlineDecoder().IndexFail(state);
            if (index < count)
            {
                long objectStart = (long)dataStart + offset - 1;
                if (objectStart > state.End) return new SvgCffOutlineDecoder().IndexFail(state);
                result.Starts[index] = (int)objectStart;
            }
            else
            {
                long end = (long)dataStart + offset - 1;
                if (end > state.End) return new SvgCffOutlineDecoder().IndexFail(state);
                result.End = (int)end;
            }
            if (index > 0) result.Lengths[index - 1] = (int)(offset - preceding);
            preceding = offset;
            index = index + 1;
        }
        return result;
    }
SvgCffDictionaryData? ReadDictionary(SvgCffDecodeState state, int start, int length, int kind, bool cff2)
    {
        if (start < state.Start || length < 0 || (long)start + length > state.End) return new SvgCffOutlineDecoder().DictionaryFail(state);
        SvgCffDictionaryData result = new SvgCffDictionaryData();
        System.Collections.Generic.List<double> operands = new System.Collections.Generic.List<double>();
        int position = start;
        int end = start + length;
        while (position < end)
        {
            int value = (state.Font.Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[position];
            if (new SvgCffOutlineDecoder().IsDictNumber(value))
            {
                SvgCffNumber number = new SvgCffOutlineDecoder().ReadNumber(state.Font.Bytes, position, end, true);
                if (!number.Valid || operands.Count >= new SvgCffOutlineDecoder().OperandLimit(state)) return new SvgCffOutlineDecoder().DictionaryFail(state);
                operands.Add(number.Value);
                position = number.Next;
                continue;
            }
            position = position + 1;
            int operation = value;
            if (value == 12)
            {
                if (position >= end) return new SvgCffOutlineDecoder().DictionaryFail(state);
                operation = 3072 + state.Font.Bytes[position];
                position = position + 1;
            }
            if (!new SvgCffOutlineDecoder().ApplyDictionaryOperator(state, result, operands, operation, kind, cff2)) return null;
            operands.Clear();
        }
        if (operands.Count != 0) return new SvgCffOutlineDecoder().DictionaryFail(state);
        return result;
    }
bool ApplyDictionaryOperator(SvgCffDecodeState state, SvgCffDictionaryData result, System.Collections.Generic.List<double> operands, int operation, int kind, bool cff2)
    {
        if (kind == 0 && operation == 17)
        {
            if (!new SvgCffOutlineDecoder().OneInteger(operands)) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidDictionary);
            result.CharStringsOffset = (int)operands[0];
        }
        else if ((kind == 0 && !cff2 || kind == 1) && operation == 18)
        {
            if (!new SvgCffOutlineDecoder().TwoNonnegativeIntegers(operands)) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidDictionary);
            result.PrivateSize = (int)operands[0];
            result.PrivateOffset = (int)operands[1];
            result.HasPrivate = true;
        }
        else if (kind == 2 && operation == 19)
        {
            if (!new SvgCffOutlineDecoder().OneInteger(operands)) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidDictionary);
            result.LocalSubrOffset = (int)operands[0];
        }
        else if (kind == 0 && operation == 3108)
        {
            if (!new SvgCffOutlineDecoder().OneInteger(operands)) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidDictionary);
            result.FontDictOffset = (int)operands[0];
        }
        else if (kind == 0 && operation == 3109)
        {
            if (!new SvgCffOutlineDecoder().OneInteger(operands)) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidDictionary);
            result.FontDictSelectOffset = (int)operands[0];
        }
        else if (kind == 0 && operation == 24 && cff2)
        {
            if (!new SvgCffOutlineDecoder().OneInteger(operands)) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidDictionary);
            result.VariationStoreOffset = (int)operands[0];
        }
        else if (kind == 0 && operation == 3078 && !cff2)
        {
            if (!new SvgCffOutlineDecoder().OneInteger(operands)) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidDictionary);
            result.CharStringType = (int)operands[0];
        }
        else if (kind == 0 && operation == 3102 && !cff2)
        {
            if (operands.Count != 3) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidDictionary);
            result.Cid = true;
        }
        return true;
    }
bool Execute(SvgCffDecodeState state, int start, int length, bool subroutine, bool global, int subroutineIndex, int depth)
    {
        if (start < state.Start || length < 0 || (long)start + length > state.End) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        int position = start;
        int end = start + length;
        while (position < end)
        {
            int value = (state.Font.Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[position];
            if (new SvgCffOutlineDecoder().IsCharStringNumber(value))
            {
                SvgCffNumber number = new SvgCffOutlineDecoder().ReadNumber(state.Font.Bytes, position, end, false);
                if (!number.Valid) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
                if (state.Stack.Count >= new SvgCffOutlineDecoder().OperandLimit(state)) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.OperandLimitExceeded);
                state.Stack.Add(number.Value);
                position = number.Next;
                continue;
            }
            position = position + 1;
            int operation = value;
            if (value == 12)
            {
                if (position >= end) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
                operation = 3072 + state.Font.Bytes[position];
                position = position + 1;
            }
            if (operation == 10 || operation == 29)
            {
                if (!new SvgCffOutlineDecoder().CallSubroutine(state, operation == 29, depth)) return false;
                continue;
            }
            if (operation == 11)
            {
                if ((state.Parsed ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Cff2 || !subroutine) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
                return true;
            }
            if (operation == 14)
            {
                if ((state.Parsed ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Cff2 || subroutine || !new SvgCffOutlineDecoder().EndChar(state)) return false;
                state.Ended = true;
                return true;
            }
            if (operation == 15)
            {
                if (!(state.Parsed ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Cff2) return new SvgCffOutlineDecoder().Fallback(state, SvgCffFallbackReason.UnsupportedOperator, operation, SvgCffError.UnsupportedOperator);
                if (state.Parsed.VariationStoreOffset < 0) return new SvgCffOutlineDecoder().Fallback(state, SvgCffFallbackReason.VariationData, operation, SvgCffError.UnsupportedOperator);
                if (!new SvgCffOutlineDecoder().VariationIndex(state)) return false;
                continue;
            }
            if (operation == 16)
            {
                if (!(state.Parsed ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Cff2) return new SvgCffOutlineDecoder().Fallback(state, SvgCffFallbackReason.UnsupportedOperator, operation, SvgCffError.UnsupportedOperator);
                if (!new SvgCffOutlineDecoder().Blend(state)) return false;
                continue;
            }
            if (!new SvgCffOutlineDecoder().ApplyCharStringOperator(state, operation, position, end)) return false;
            if (operation == 19 || operation == 20)
            {
                int maskBytes = (state.StemCount + 7) / 8;
                if ((long)position + maskBytes > end) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
                position = position + maskBytes;
            }
        }
        if (!(state.Parsed ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Cff2 && subroutine) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        return true;
    }
bool ApplyCharStringOperator(SvgCffDecodeState state, int operation, int position, int end)
    {
        if (operation == 1 || operation == 3 || operation == 18 || operation == 23) return new SvgCffOutlineDecoder().Stems(state);
        if (operation == 19 || operation == 20) return new SvgCffOutlineDecoder().HintMask(state);
        if (operation == 4 || operation == 21 || operation == 22) return new SvgCffOutlineDecoder().Move(state, operation);
        if (operation == 5) return new SvgCffOutlineDecoder().RelativeLines(state);
        if (operation == 6 || operation == 7) return new SvgCffOutlineDecoder().AlternatingLines(state, operation == 6);
        if (operation == 8) return new SvgCffOutlineDecoder().RelativeCurves(state);
        if (operation == 24) return new SvgCffOutlineDecoder().CurveLine(state);
        if (operation == 25) return new SvgCffOutlineDecoder().LineCurve(state);
        if (operation == 26 || operation == 27) return new SvgCffOutlineDecoder().ShortCurves(state, operation == 27);
        if (operation == 30 || operation == 31) return new SvgCffOutlineDecoder().AlternatingCurves(state, operation == 31);
        if (operation == 3106 || operation == 3107 || operation == 3108 || operation == 3109) return new SvgCffOutlineDecoder().Flex(state, operation);
        return new SvgCffOutlineDecoder().Fallback(state, SvgCffFallbackReason.UnsupportedOperator, operation, SvgCffError.UnsupportedOperator);
    }
bool VariationIndex(SvgCffDecodeState state)
    {
        if (state.Stack.Count != 1) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        double value = state.Stack[0];
        int index = (int)value;
        if (value != index || index < 0) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        state.CurrentVariationDataIndex = index;
        state.Stack.Clear();
        return true;
    }
bool Blend(SvgCffDecodeState state)
    {
        if (state.Stack.Count < 1) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        double countValue = state.Stack[state.Stack.Count - 1];
        int count = (int)countValue;
        if (countValue != count || count < 0) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        int variationOffset = (state.Parsed ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).VariationStoreOffset;
        if (variationOffset < 0 || !new SvgCffOutlineDecoder().RelativeRange(state, variationOffset, 2)) return new SvgCffOutlineDecoder().Fallback(state, SvgCffFallbackReason.VariationData, 16, SvgCffError.UnsupportedOperator);
        int wrapper = state.Start + variationOffset;
        int length = new SvgCffOutlineDecoder().ReadUInt16(state.Font.Bytes, wrapper);
        if (length < 8 || (long)wrapper + 2 + length > state.End) return new SvgCffOutlineDecoder().Fallback(state, SvgCffFallbackReason.VariationData, 16, SvgCffError.UnsupportedOperator);
        double[]? scalars = new SvgItemVariationEvaluator().ScalarsFromStore((state.Font.Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), wrapper + 2, wrapper + 2 + length, state.CurrentVariationDataIndex, state.Options.VariationCoordinates);
        if (scalars == null) return new SvgCffOutlineDecoder().Fallback(state, SvgCffFallbackReason.VariationData, 16, SvgCffError.UnsupportedOperator);
        long expected = (long)count * (scalars.Length + 1) + 1;
        if (expected != state.Stack.Count) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        System.Collections.Generic.List<double> blended = new System.Collections.Generic.List<double>();
        int valueIndex = 0;
        while (valueIndex < count)
        {
            double value = state.Stack[valueIndex];
            int region = 0;
            while (region < scalars.Length)
            {
                value = value + state.Stack[count + region * count + valueIndex] * scalars[region];
                region = region + 1;
            }
            if (!SvgFiniteMath.IsFinite(value)) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.NonFiniteResult);
            blended.Add(value);
            valueIndex = valueIndex + 1;
        }
        state.Stack = blended;
        return true;
    }
bool Stems(SvgCffDecodeState state)
    {
        if (!(state.Parsed ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Cff2 && !state.WidthSeen && state.Stack.Count % 2 == 1) state.Stack.RemoveAt(0);
        state.WidthSeen = true;
        if (state.Stack.Count < 2 || state.Stack.Count % 2 != 0) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        state.StemCount = state.StemCount + state.Stack.Count / 2;
        state.HasHints = true;
        state.Stack.Clear();
        return true;
    }
bool HintMask(SvgCffDecodeState state)
    {
        if (state.Stack.Count != 0)
        {
            if (!new SvgCffOutlineDecoder().Stems(state)) return false;
        }
        if (state.StemCount < 1) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        state.HasHints = true;
        return true;
    }
bool Move(SvgCffDecodeState state, int operation)
    {
        int required = operation == 21 ? 2 : 1;
        if (!new SvgCffOutlineDecoder().PrepareWidth(state, required)) return false;
        new SvgCffOutlineDecoder().CloseContour(state);
        double dx = operation == 4 ? 0 : state.Stack[0];
        double dy = operation == 22 ? 0 : state.Stack[operation == 21 ? 1 : 0];
        state.Stack.Clear();
        if (!new SvgCffOutlineDecoder().AddCoordinate(state, dx, dy)) return false;
        state.Builder.MoveTo(state.X, state.Y);
        state.OpenContour = true;
        state.ContourCount = state.ContourCount + 1;
        return new SvgCffOutlineDecoder().AddPoints(state, 1);
    }
bool RelativeLines(SvgCffDecodeState state)
    {
        if (!state.OpenContour || state.Stack.Count < 2 || state.Stack.Count % 2 != 0) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        int index = 0;
        while (index < state.Stack.Count)
        {
            if (!new SvgCffOutlineDecoder().Line(state, state.Stack[index], state.Stack[index + 1])) return false;
            index = index + 2;
        }
        state.Stack.Clear();
        return true;
    }
bool AlternatingLines(SvgCffDecodeState state, bool horizontal)
    {
        if (!state.OpenContour || state.Stack.Count < 1) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        int index = 0;
        while (index < state.Stack.Count)
        {
            double dx = horizontal ? state.Stack[index] : 0;
            double dy = horizontal ? 0 : state.Stack[index];
            if (!new SvgCffOutlineDecoder().Line(state, dx, dy)) return false;
            horizontal = !horizontal;
            index = index + 1;
        }
        state.Stack.Clear();
        return true;
    }
bool RelativeCurves(SvgCffDecodeState state)
    {
        if (!state.OpenContour || state.Stack.Count < 6 || state.Stack.Count % 6 != 0) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        int index = 0;
        while (index < state.Stack.Count)
        {
            if (!new SvgCffOutlineDecoder().Curve(state, state.Stack[index], state.Stack[index + 1], state.Stack[index + 2], state.Stack[index + 3], state.Stack[index + 4], state.Stack[index + 5])) return false;
            index = index + 6;
        }
        state.Stack.Clear();
        return true;
    }
bool CurveLine(SvgCffDecodeState state)
    {
        int count = state.Stack.Count;
        if (!state.OpenContour || count < 8 || (count - 2) % 6 != 0) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        int index = 0;
        while (index < count - 2)
        {
            if (!new SvgCffOutlineDecoder().Curve(state, state.Stack[index], state.Stack[index + 1], state.Stack[index + 2], state.Stack[index + 3], state.Stack[index + 4], state.Stack[index + 5])) return false;
            index = index + 6;
        }
        if (!new SvgCffOutlineDecoder().Line(state, state.Stack[count - 2], state.Stack[count - 1])) return false;
        state.Stack.Clear();
        return true;
    }
bool LineCurve(SvgCffDecodeState state)
    {
        int count = state.Stack.Count;
        if (!state.OpenContour || count < 8 || (count - 6) % 2 != 0) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        int index = 0;
        while (index < count - 6)
        {
            if (!new SvgCffOutlineDecoder().Line(state, state.Stack[index], state.Stack[index + 1])) return false;
            index = index + 2;
        }
        bool success = new SvgCffOutlineDecoder().Curve(state, state.Stack[count - 6], state.Stack[count - 5], state.Stack[count - 4], state.Stack[count - 3], state.Stack[count - 2], state.Stack[count - 1]);
        state.Stack.Clear();
        return success;
    }
bool ShortCurves(SvgCffDecodeState state, bool horizontal)
    {
        int count = state.Stack.Count;
        if (!state.OpenContour || count < 4 || count % 4 != 0 && count % 4 != 1) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        int index = count % 4;
        double initial = index == 1 ? state.Stack[0] : 0;
        bool first = true;
        while (index < count)
        {
            double dx1 = horizontal ? state.Stack[index] : first ? initial : 0;
            double dy1 = horizontal ? first ? initial : 0 : state.Stack[index];
            double dx2 = state.Stack[index + 1];
            double dy2 = state.Stack[index + 2];
            double dx3 = horizontal ? state.Stack[index + 3] : 0;
            double dy3 = horizontal ? 0 : state.Stack[index + 3];
            if (!new SvgCffOutlineDecoder().Curve(state, dx1, dy1, dx2, dy2, dx3, dy3)) return false;
            first = false;
            index = index + 4;
        }
        state.Stack.Clear();
        return true;
    }
bool AlternatingCurves(SvgCffDecodeState state, bool horizontalFirst)
    {
        int count = state.Stack.Count;
        if (!state.OpenContour || count < 4 || count % 4 != 0 && count % 4 != 1) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        int index = 0;
        bool horizontal = horizontalFirst;
        while (count - index >= 4)
        {
            bool last = count - index == 4 || count - index == 5;
            double extra = last && count - index == 5 ? state.Stack[index + 4] : 0;
            double dx1 = horizontal ? state.Stack[index] : 0;
            double dy1 = horizontal ? 0 : state.Stack[index];
            double dx2 = state.Stack[index + 1];
            double dy2 = state.Stack[index + 2];
            double dx3 = horizontal ? extra : state.Stack[index + 3];
            double dy3 = horizontal ? state.Stack[index + 3] : extra;
            if (!new SvgCffOutlineDecoder().Curve(state, dx1, dy1, dx2, dy2, dx3, dy3)) return false;
            index = index + (last && count - index == 5 ? 5 : 4);
            horizontal = !horizontal;
        }
        if (index != count) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        state.Stack.Clear();
        return true;
    }
bool Flex(SvgCffDecodeState state, int operation)
    {
        if (!state.OpenContour) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        if (operation == 3107)
        {
            if (state.Stack.Count != 13) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
            bool first = new SvgCffOutlineDecoder().Curve(state, state.Stack[0], state.Stack[1], state.Stack[2], state.Stack[3], state.Stack[4], state.Stack[5]);
            bool second = first && new SvgCffOutlineDecoder().Curve(state, state.Stack[6], state.Stack[7], state.Stack[8], state.Stack[9], state.Stack[10], state.Stack[11]);
            state.Stack.Clear();
            return second;
        }
        if (operation == 3106)
        {
            if (state.Stack.Count != 7) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
            double dy = state.Stack[2];
            bool first = new SvgCffOutlineDecoder().Curve(state, state.Stack[0], 0, state.Stack[1], dy, state.Stack[3], 0);
            bool second = first && new SvgCffOutlineDecoder().Curve(state, state.Stack[4], 0, state.Stack[5], -dy, state.Stack[6], 0);
            state.Stack.Clear();
            return second;
        }
        if (operation == 3108)
        {
            if (state.Stack.Count != 9) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
            double finalY = -(state.Stack[1] + state.Stack[3] + state.Stack[7]);
            bool first = new SvgCffOutlineDecoder().Curve(state, state.Stack[0], state.Stack[1], state.Stack[2], state.Stack[3], state.Stack[4], 0);
            bool second = first && new SvgCffOutlineDecoder().Curve(state, state.Stack[5], 0, state.Stack[6], state.Stack[7], state.Stack[8], finalY);
            state.Stack.Clear();
            return second;
        }
        if (state.Stack.Count != 11) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        double sumX = state.Stack[0] + state.Stack[2] + state.Stack[4] + state.Stack[6] + state.Stack[8];
        double sumY = state.Stack[1] + state.Stack[3] + state.Stack[5] + state.Stack[7] + state.Stack[9];
        double finalX = System.Math.Abs(sumX) > System.Math.Abs(sumY) ? state.Stack[10] : -sumX;
        double finalYValue = System.Math.Abs(sumX) > System.Math.Abs(sumY) ? -sumY : state.Stack[10];
        bool firstFlex = new SvgCffOutlineDecoder().Curve(state, state.Stack[0], state.Stack[1], state.Stack[2], state.Stack[3], state.Stack[4], state.Stack[5]);
        bool secondFlex = firstFlex && new SvgCffOutlineDecoder().Curve(state, state.Stack[6], state.Stack[7], state.Stack[8], state.Stack[9], finalX, finalYValue);
        state.Stack.Clear();
        return secondFlex;
    }
bool CallSubroutine(SvgCffDecodeState state, bool global, int depth)
    {
        if (state.Stack.Count < 1) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        double operand = state.Stack[state.Stack.Count - 1];
        state.Stack.RemoveAt(state.Stack.Count - 1);
        if (!new SvgCffOutlineDecoder().Integer(operand)) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        SvgCffIndexData? index = global ? (state.Parsed ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GlobalSubrs : (state.Parsed ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).LocalSubrs;
        if (index == null) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        int bias = index.Count < 1240 ? 107 : index.Count < 33900 ? 1131 : 32768;
        long selectedValue = (long)operand + bias;
        if (selectedValue < 0 || selectedValue >= index.Count) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        if (depth >= state.Options.MaximumSubroutineDepth || state.SubroutineCalls >= state.Options.MaximumSubroutineCalls) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.SubroutineLimitExceeded);
        int selected = (int)selectedValue;
        bool[]? active = global ? state.ActiveGlobalSubrs : state.ActiveLocalSubrs;
        if ((active ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[selected]) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.SubroutineLimitExceeded);
        active[selected] = true;
        state.SubroutineCalls = state.SubroutineCalls + 1;
        bool success = new SvgCffOutlineDecoder().Execute(state, index.Starts[selected], index.Lengths[selected], true, global, selected, depth + 1);
        active[selected] = false;
        return success;
    }
bool EndChar(SvgCffDecodeState state)
    {
        if (!state.WidthSeen && state.Stack.Count == 1)
        {
            state.Stack.Clear();
            state.WidthSeen = true;
        }
        if (state.Stack.Count == 4 || state.Stack.Count == 5) return new SvgCffOutlineDecoder().Fallback(state, SvgCffFallbackReason.UnsupportedOperator, 14, SvgCffError.UnsupportedOperator);
        if (state.Stack.Count != 0) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        new SvgCffOutlineDecoder().CloseContour(state);
        return true;
    }
bool PrepareWidth(SvgCffDecodeState state, int required)
    {
        if (!(state.Parsed ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Cff2 && !state.WidthSeen && state.Stack.Count == required + 1) state.Stack.RemoveAt(0);
        state.WidthSeen = true;
        if (state.Stack.Count != required) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidCharString);
        return true;
    }
bool Line(SvgCffDecodeState state, double dx, double dy)
    {
        if (!new SvgCffOutlineDecoder().AddCoordinate(state, dx, dy)) return false;
        state.Builder.LineTo(state.X, state.Y);
        return new SvgCffOutlineDecoder().AddPoints(state, 1);
    }
bool Curve(SvgCffDecodeState state, double dx1, double dy1, double dx2, double dy2, double dx3, double dy3)
    {
        double control1X = state.X + dx1;
        double control1Y = state.Y + dy1;
        double control2X = control1X + dx2;
        double control2Y = control1Y + dy2;
        double endX = control2X + dx3;
        double endY = control2Y + dy3;
        if (!new SvgCffOutlineDecoder().Finite(control1X, control1Y, control2X, control2Y, endX, endY)) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.NonFiniteResult);
        state.Builder.CubicTo(control1X, control1Y, control2X, control2Y, endX, endY);
        state.X = endX;
        state.Y = endY;
        return new SvgCffOutlineDecoder().AddPoints(state, 3);
    }
bool AddCoordinate(SvgCffDecodeState state, double dx, double dy)
    {
        double x = state.X + dx;
        double y = state.Y + dy;
        if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y)) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.NonFiniteResult);
        state.X = SvgFiniteMath.NormalizeZero(x);
        state.Y = SvgFiniteMath.NormalizeZero(y);
        return true;
    }
bool AddPoints(SvgCffDecodeState state, int count)
    {
        if (state.PointCount > state.Options.Limits.MaxPointsPerGlyph - count) return new SvgCffOutlineDecoder().Fail(state, SvgCffError.PointLimitExceeded);
        state.PointCount = state.PointCount + count;
        return true;
    }
void CloseContour(SvgCffDecodeState state)
    {
        if (!state.OpenContour) return;
        state.Builder.Close();
        state.OpenContour = false;
    }
SvgCffNumber ReadNumber(byte[] bytes, int position, int end, bool dictionary)
    {
        int first = bytes[position];
        if (first >= 32 && first <= 246) return new SvgCffNumber(true, first - 139, position + 1);
        if (first >= 247 && first <= 250)
        {
            if (position + 2 > end) return new SvgCffNumber(false, 0, end);
            return new SvgCffNumber(true, (first - 247) * 256 + bytes[position + 1] + 108, position + 2);
        }
        if (first >= 251 && first <= 254)
        {
            if (position + 2 > end) return new SvgCffNumber(false, 0, end);
            return new SvgCffNumber(true, -(first - 251) * 256 - bytes[position + 1] - 108, position + 2);
        }
        if (first == 28)
        {
            if (position + 3 > end) return new SvgCffNumber(false, 0, end);
            int value = new SvgCffOutlineDecoder().ReadUInt16(bytes, position + 1);
            if (value >= 32768) value = value - 65536;
            return new SvgCffNumber(true, value, position + 3);
        }
        if (first == 29 && dictionary)
        {
            if (position + 5 > end) return new SvgCffNumber(false, 0, end);
            return new SvgCffNumber(true, new SvgCffOutlineDecoder().ReadInt32(bytes, position + 1), position + 5);
        }
        if (first == 30 && dictionary) return new SvgCffOutlineDecoder().ReadReal(bytes, position + 1, end);
        if (first == 255 && !dictionary)
        {
            if (position + 5 > end) return new SvgCffNumber(false, 0, end);
            return new SvgCffNumber(true, new SvgCffOutlineDecoder().ReadInt32(bytes, position + 1) / 65536.0, position + 5);
        }
        return new SvgCffNumber(false, 0, position + 1);
    }
SvgCffNumber ReadReal(byte[] bytes, int position, int end)
    {
        double mantissa = 0;
        int fractionDigits = 0;
        int exponent = 0;
        bool decimalPoint = false;
        bool exponentMode = false;
        bool exponentNegative = false;
        bool negative = false;
        bool digit = false;
        bool exponentDigit = false;
        int index = position;
        while (index < end)
        {
            int value = bytes[index];
            int part = 0;
            while (part < 2)
            {
                int nibble = part == 0 ? value >> 4 : value & 15;
                if (nibble == 15)
                {
                    if (exponentMode && !exponentDigit || !digit && exponentMode) return new SvgCffNumber(false, 0, index + 1);
                    int signedExponent = exponentNegative ? -exponent : exponent;
                    double result = mantissa * System.Math.Pow(10, signedExponent - fractionDigits);
                    if (negative) result = -result;
                    return new SvgCffNumber(SvgFiniteMath.IsFinite(result), result, index + 1);
                }
                if (nibble <= 9)
                {
                    if (exponentMode)
                    {
                        exponent = exponent * 10 + nibble;
                        exponentDigit = true;
                    }
                    else
                    {
                        mantissa = mantissa * 10 + nibble;
                        if (decimalPoint) fractionDigits = fractionDigits + 1;
                        digit = true;
                    }
                }
                else if (nibble == 10 && !decimalPoint && !exponentMode) decimalPoint = true;
                else if ((nibble == 11 || nibble == 12) && !exponentMode && digit)
                {
                    exponentMode = true;
                    exponentNegative = nibble == 12;
                }
                else if (nibble == 14 && !digit && !negative && !exponentMode) negative = true;
                else return new SvgCffNumber(false, 0, index + 1);
                part = part + 1;
            }
            index = index + 1;
        }
        return new SvgCffNumber(false, 0, end);
    }
bool IsDictNumber(int value)
    {
        return value == 28 || value == 29 || value == 30 || value >= 32;
    }
bool IsCharStringNumber(int value)
    {
        return value == 28 || value == 255 || value >= 32;
    }
bool OneInteger(System.Collections.Generic.List<double> values)
    {
        return values.Count == 1 && new SvgCffOutlineDecoder().NonnegativeInteger(values[0]);
    }
bool TwoNonnegativeIntegers(System.Collections.Generic.List<double> values)
    {
        return values.Count == 2 && new SvgCffOutlineDecoder().NonnegativeInteger(values[0]) && new SvgCffOutlineDecoder().NonnegativeInteger(values[1]);
    }
bool NonnegativeInteger(double value)
    {
        return new SvgCffOutlineDecoder().Integer(value) && value >= 0 && value <= System.Int32.MaxValue;
    }
bool Integer(double value)
    {
        return SvgFiniteMath.IsFinite(value) && value >= System.Int32.MinValue && value <= System.Int32.MaxValue && value == System.Math.Truncate(value);
    }
bool Finite(double firstX, double firstY, double secondX, double secondY, double endX, double endY)
    {
        return SvgFiniteMath.IsFinite(firstX) && SvgFiniteMath.IsFinite(firstY) && SvgFiniteMath.IsFinite(secondX) && SvgFiniteMath.IsFinite(secondY) && SvgFiniteMath.IsFinite(endX) && SvgFiniteMath.IsFinite(endY);
    }
int IndexLimit(SvgCffDecodeState state)
    {
        int high = state.Options.Limits.MaxGlyphsPerFont > state.Options.MaximumSubroutines ? state.Options.Limits.MaxGlyphsPerFont : state.Options.MaximumSubroutines;
        if (high > System.Int32.MaxValue / 4) return System.Int32.MaxValue;
        return high * 4;
    }
int OperandLimit(SvgCffDecodeState state)
    {
        int formatLimit = state.Font.OutlineKind == SvgOpenTypeOutlineKind.Cff ? 48 : 513;
        return state.Options.MaximumOperands < formatLimit ? state.Options.MaximumOperands : formatLimit;
    }
int Relative(SvgCffDecodeState state, int offset)
    {
        if (offset < 0 || (long)state.Start + offset > System.Int32.MaxValue) return -1;
        return state.Start + offset;
    }
bool RelativeRange(SvgCffDecodeState state, int offset, int length)
    {
        return offset >= 0 && length >= 0 && (long)offset + length <= state.Table.Length;
    }
bool TableRange(byte[]? bytes, SvgOpenTypeTable table)
    {
        return table.Offset >= 0 && table.Length >= 0 && (long)table.Offset + table.Length <= (bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length;
    }
uint ReadOffset(byte[] bytes, int offset, int size)
    {
        uint result = 0;
        int index = 0;
        while (index < size)
        {
            result = result << 8 | bytes[offset + index];
            index = index + 1;
        }
        return result;
    }
int ReadUInt16(byte[]? bytes, int offset)
    {
        return (bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[offset] << 8 | bytes[offset + 1];
    }
uint ReadUInt32(byte[]? bytes, int offset)
    {
        return (uint)(bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[offset] << 24 | (uint)bytes[offset + 1] << 16 | (uint)bytes[offset + 2] << 8 | bytes[offset + 3];
    }
int ReadInt32(byte[] bytes, int offset)
    {
        uint value = new SvgCffOutlineDecoder().ReadUInt32(bytes, offset);
        return value <= 2147483647u ? (int)value : (int)((long)value - 4294967296L);
    }
SvgCffIndexData? IndexFail(SvgCffDecodeState state)
    {
        new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidIndex);
        return null;
    }
SvgCffDictionaryData? DictionaryFail(SvgCffDecodeState state)
    {
        new SvgCffOutlineDecoder().Fail(state, SvgCffError.InvalidDictionary);
        return null;
    }
bool Fail(SvgCffDecodeState state, SvgCffError error)
    {
        if (state.Error == SvgCffError.None) state.Error = error;
        return false;
    }
bool Fallback(SvgCffDecodeState state, SvgCffFallbackReason reason, int operation, SvgCffError error)
    {
        if (state.Error == SvgCffError.None)
        {
            state.Error = error;
            state.RequiresFallback = true;
            state.FallbackReason = reason;
            state.UnsupportedOperator = operation;
        }
        return false;
    }
SvgCffGlyphResult StateResult(SvgCffDecodeState state, SvgCffGlyphOutline? outline)
    {
        return new SvgCffOutlineDecoder().Result(false, state.Error, outline, state.RequiresFallback, state.FallbackReason, state.UnsupportedOperator);
    }
SvgCffGlyphResult Result(bool success, SvgCffError error, SvgCffGlyphOutline? outline, bool fallback, SvgCffFallbackReason reason, int operation)
    {
        return new SvgCffGlyphResult(success, error, outline, fallback, reason, operation);
    }
}
