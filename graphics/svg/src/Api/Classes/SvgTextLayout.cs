namespace svg;

/// <summary>Positions shaped glyphs according to SVG text chunk semantics.</summary>
public class SvgTextLayout : ISvgTextLayout
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgTextLayout()
    {
    }

    /// <summary>Positions shaped glyphs with horizontal alphabetic defaults.</summary>
    /// <param name="shaping">A successful baseline or callback shaping result.</param>
    /// <returns>The positioned glyphs and chunks.</returns>
    public SvgTextLayoutResult LayoutWithShaping(SvgShapingResult shaping)
    {
        return new SvgTextLayout().LayoutWithShapingAndOptions(shaping, new SvgTextLayoutOptions());
    }

    /// <summary>Positions shaped glyphs with explicit SVG lists, spacing, baselines, and text length.</summary>
    /// <param name="shaping">A successful baseline or callback shaping result.</param>
    /// <param name="options">The bounded SVG text-positioning options.</param>
    /// <returns>The positioned glyphs and chunks.</returns>
    public SvgTextLayoutResult LayoutWithShapingAndOptions(SvgShapingResult shaping, SvgTextLayoutOptions options)
    {
        SvgTextLayoutResult result = new SvgTextLayoutResult();
        if (!new SvgTextLayout().Valid(shaping, options)) return new SvgTextLayout().Fail(result, SvgTextLayoutError.InvalidArgument, "SVG text layout arguments or options are invalid.");
        result.Text = shaping.Text;
        result.WritingMode = options.WritingMode;
        result.Direction = options.Direction;
        result.FontSize = options.FontSize;
        result.Ascent = options.Ascent;
        result.Descent = options.Descent;
        result.XHeight = options.XHeight;
        if (shaping.Glyphs.Count > options.MaxGlyphs) return new SvgTextLayout().Fail(result, SvgTextLayoutError.LimitExceeded, "SVG text layout exceeds MaxGlyphs.");
        if (options.PositioningRanges.Count > options.MaxPositioningRanges) return new SvgTextLayout().Fail(result, SvgTextLayoutError.LimitExceeded, "SVG text layout exceeds MaxPositioningRanges.");
        SvgUnicodeTextOptions unicodeOptions = new SvgUnicodeTextOptions();
        unicodeOptions.MaxScalars = options.MaxGlyphs > int.MaxValue / 16 ? int.MaxValue : options.MaxGlyphs * 16;
        unicodeOptions.MaxClusters = options.MaxGlyphs;
        unicodeOptions.MaxRuns = options.MaxGlyphs;
        unicodeOptions.CancellationToken = options.CancellationToken;
        SvgUnicodeTextResult unicodeResult = new SvgUnicodeTextAnalyzer().AnalyzeWithTextAndOptions(shaping.Text, unicodeOptions);
        if (!unicodeResult.Success)
        {
            if (unicodeResult.Error == SvgUnicodeTextError.Cancelled) return new SvgTextLayout().Fail(result, SvgTextLayoutError.Cancelled, unicodeResult.Message);
            if (unicodeResult.Error == SvgUnicodeTextError.LimitExceeded) return new SvgTextLayout().Fail(result, SvgTextLayoutError.LimitExceeded, unicodeResult.Message);
            return new SvgTextLayout().Fail(result, SvgTextLayoutError.InvalidArgument, unicodeResult.Message);
        }
        SvgUnicodeText text = unicodeResult.UnicodeText;
        int[] addresses = new SvgTextLayout().AddressMap(text, result);
        if (!new SvgTextLayout().ValidRanges(options, result.CharacterCount)) return new SvgTextLayout().Fail(result, SvgTextLayoutError.InvalidArgument, "An SVG text positioning range is invalid.");
        if (new SvgTextLayout().PositionValueCount(options) > options.MaxPositionValues) return new SvgTextLayout().Fail(result, SvgTextLayoutError.LimitExceeded, "SVG text layout exceeds MaxPositionValues.");
        System.Collections.Generic.List<SvgTextLayoutItem>? items = new SvgTextLayout().BuildItems(shaping, text, addresses, options, result);
        if (items == null) return result;
        if (!new SvgTextLayout().BuildChunks(items, options, result)) return result;
        if (!new SvgTextLayout().AdjustLengths(items, options, result)) return result;
        result.BaselineOffset = new SvgTextLayout().BaselineOffset(options);
        if (!new SvgTextLayout().Position(items, options, result)) return result;
        result.Success = true;
        return result;
    }
int[] AddressMap(SvgUnicodeText text, SvgTextLayoutResult result)
    {
        int[] addresses = new int[text.Scalars.Count];
        int address = 0;
        int index = 0;
        while (index < text.Scalars.Count)
        {
            if (text.Scalars[index].IsVariationSelector) addresses[index] = -1;
            else
            {
                addresses[index] = address;
                address = address + 1;
            }
            index = index + 1;
        }
        result.CharacterCount = address;
        return addresses;
    }
System.Collections.Generic.List<SvgTextLayoutItem>? BuildItems(SvgShapingResult shaping, SvgUnicodeText text, int[] addresses, SvgTextLayoutOptions options, SvgTextLayoutResult result)
    {
        System.Collections.Generic.List<SvgTextLayoutItem> items = new System.Collections.Generic.List<SvgTextLayoutItem>();
        bool horizontal = new SvgTextLayout().Horizontal(options.WritingMode);
        int previousAddress = -1;
        int index = 0;
        while (index < shaping.Glyphs.Count)
        {
            if (options.CancellationToken.IsCancellationRequested)
            {
                new SvgTextLayout().Fail(result, SvgTextLayoutError.Cancelled, "SVG text item construction was cancelled.");
                return null;
            }
            SvgShapedGlyph glyph = shaping.Glyphs[index];
            if (!new SvgTextLayout().ValidGlyph(glyph, shaping, text))
            {
                new SvgTextLayout().Fail(result, SvgTextLayoutError.InvalidArgument, "A shaped glyph has invalid source or font identity.");
                return null;
            }
            int character = new SvgTextLayout().FirstAddress(glyph, addresses);
            if (character < 0)
            {
                new SvgTextLayout().Fail(result, SvgTextLayoutError.InvalidArgument, "A shaped glyph contains no addressable source character.");
                return null;
            }
            SvgResolvedTextPosition position = new SvgTextLayout().ResolvePosition(options, character);
            if (character == previousAddress)
            {
                position.HasX = false;
                position.HasY = false;
                position.Dx = 0;
                position.Dy = 0;
            }
            SvgTextLayoutItem item = new SvgTextLayoutItem();
            item.Glyph = glyph;
            item.CharacterIndex = character;
            item.Position = position;
            item.WordSpace = new SvgTextLayout().WordSpace(shaping.Text, glyph.Utf16Start, glyph.Utf16Length);
            item.InlineDelta = horizontal ? position.Dx : position.Dy;
            item.CrossDelta = horizontal ? position.Dy : position.Dx;
            double advance = horizontal ? glyph.XAdvance : glyph.YAdvance;
            item.Advance = advance < 0 ? -advance : advance;
            items.Add(item);
            previousAddress = character;
            index = index + 1;
        }
        return items;
    }
bool BuildChunks(System.Collections.Generic.List<SvgTextLayoutItem> items, SvgTextLayoutOptions options, SvgTextLayoutResult result)
    {
        bool horizontal = new SvgTextLayout().Horizontal(options.WritingMode);
        int index = 0;
        while (index < items.Count)
        {
            bool absoluteInline = horizontal ? (items[index].Position ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).HasX : (items[index].Position ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).HasY;
            if (index == 0 || absoluteInline)
            {
                if (result.Chunks.Count >= options.MaxChunks)
                {
                    new SvgTextLayout().Fail(result, SvgTextLayoutError.LimitExceeded, "SVG text layout exceeds MaxChunks.");
                    return false;
                }
                SvgTextChunk chunk = new SvgTextChunk();
                chunk.Index = result.Chunks.Count;
                chunk.GlyphStart = index;
                result.Chunks.Add(chunk);
            }
            SvgTextChunk current = result.Chunks[result.Chunks.Count - 1];
            current.GlyphCount = current.GlyphCount + 1;
            items[index].ChunkIndex = current.Index;
            index = index + 1;
        }
        if (items.Count == 0)
        {
            SvgTextChunk empty = new SvgTextChunk();
            empty.Index = 0;
            empty.X = options.InitialX;
            empty.Y = options.InitialY;
            result.Chunks.Add(empty);
        }
        new SvgTextLayout().AssignNaturalSpacing(items, options, result);
        return true;
    }
void AssignNaturalSpacing(System.Collections.Generic.List<SvgTextLayoutItem> items, SvgTextLayoutOptions options, SvgTextLayoutResult result)
    {
        int index = 0;
        while (index < items.Count)
        {
            bool adjustable = index + 1 < items.Count && items[index + 1].ChunkIndex == items[index].ChunkIndex && items[index + 1].CharacterIndex != items[index].CharacterIndex;
            items[index].Gap = adjustable ? options.LetterSpacing + (items[index].WordSpace ? options.WordSpacing : 0) : 0;
            index = index + 1;
        }
        new SvgTextLayout().Recalculate(items, result, true);
        result.NaturalTextLength = result.ActualTextLength;
    }
bool AdjustLengths(System.Collections.Generic.List<SvgTextLayoutItem> items, SvgTextLayoutOptions options, SvgTextLayoutResult result)
    {
        if (!options.HasTextLength)
        {
            result.GlyphScale = 1;
            return true;
        }
        double natural = result.NaturalTextLength;
        if (options.LengthAdjust == SvgTextLengthAdjust.Spacing)
        {
            int gaps = 0;
            int index = 0;
            while (index + 1 < items.Count)
            {
                if (items[index + 1].ChunkIndex == items[index].ChunkIndex && items[index + 1].CharacterIndex != items[index].CharacterIndex) gaps = gaps + 1;
                index = index + 1;
            }
            if (gaps > 0)
            {
                result.SpacingAdjustment = (options.TextLength - natural) / gaps;
                index = 0;
                while (index + 1 < items.Count)
                {
                    if (items[index + 1].ChunkIndex == items[index].ChunkIndex && items[index + 1].CharacterIndex != items[index].CharacterIndex) items[index].Gap = items[index].Gap + result.SpacingAdjustment;
                    index = index + 1;
                }
            }
        }
        else
        {
            if (natural == 0 && options.TextLength != 0) return new SvgTextLayout().FailBoolean(result, SvgTextLayoutError.InvalidArgument, "spacingAndGlyphs cannot scale zero natural text length.");
            double scale = natural == 0 ? 1 : options.TextLength / natural;
            result.GlyphScale = scale;
            int index = 0;
            while (index < items.Count)
            {
                items[index].InlineDelta = items[index].InlineDelta * scale;
                items[index].Advance = items[index].Advance * scale;
                items[index].Gap = items[index].Gap * scale;
                items[index].Scale = scale;
                index = index + 1;
            }
        }
        new SvgTextLayout().Recalculate(items, result, false);
        return true;
    }
void Recalculate(System.Collections.Generic.List<SvgTextLayoutItem> items, SvgTextLayoutResult result, bool natural)
    {
        int chunk = 0;
        while (chunk < result.Chunks.Count)
        {
            double advance = 0;
            int start = result.Chunks[chunk].GlyphStart;
            int end = start + result.Chunks[chunk].GlyphCount;
            int index = start;
            while (index < end)
            {
                advance = advance + items[index].InlineDelta + items[index].Advance + items[index].Gap;
                index = index + 1;
            }
            if (natural) result.Chunks[chunk].NaturalAdvance = advance;
            result.Chunks[chunk].Advance = advance;
            chunk = chunk + 1;
        }
        double total = 0;
        chunk = 0;
        while (chunk < result.Chunks.Count)
        {
            total = total + result.Chunks[chunk].Advance;
            chunk = chunk + 1;
        }
        result.ActualTextLength = total;
    }
bool Position(System.Collections.Generic.List<SvgTextLayoutItem> items, SvgTextLayoutOptions options, SvgTextLayoutResult result)
    {
        bool horizontal = new SvgTextLayout().Horizontal(options.WritingMode);
        double direction = options.Direction == SvgTextDirection.RightToLeft ? -1 : 1;
        double x = options.InitialX;
        double y = options.InitialY;
        int index = 0;
        while (index < items.Count)
        {
            if (options.CancellationToken.IsCancellationRequested) return new SvgTextLayout().FailBoolean(result, SvgTextLayoutError.Cancelled, "SVG text positioning was cancelled.");
            SvgTextLayoutItem item = items[index];
            if ((item.Position ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).HasX) x = item.Position.X;
            if (item.Position.HasY) y = item.Position.Y;
            SvgTextChunk chunk = result.Chunks[item.ChunkIndex];
            if (chunk.GlyphStart == index)
            {
                chunk.X = x;
                chunk.Y = y;
                chunk.AnchorOffset = new SvgTextLayout().AnchorOffset(options.TextAnchor, chunk.Advance, direction);
                if (horizontal) x = x + chunk.AnchorOffset;
                else y = y + chunk.AnchorOffset;
            }
            if (horizontal)
            {
                x = x + item.InlineDelta;
                y = y + item.CrossDelta;
            }
            else
            {
                x = x + item.CrossDelta;
                y = y + item.InlineDelta;
            }
            SvgPositionedGlyph positioned = new SvgPositionedGlyph();
            positioned.Glyph = new SvgTextLayout().CopyGlyph(item.Glyph);
            positioned.CharacterIndex = item.CharacterIndex;
            positioned.ChunkIndex = item.ChunkIndex;
            positioned.X = x + (item.Glyph ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).XOffset + (horizontal ? 0 : result.BaselineOffset);
            positioned.Y = y + item.Glyph.YOffset + (horizontal ? result.BaselineOffset : 0);
            positioned.Rotation = item.Position.Rotation;
            positioned.InlineAdvance = item.Advance;
            positioned.SpacingAfter = item.Gap;
            positioned.InlineScale = item.Scale;
            positioned.WordSpace = item.WordSpace;
            result.Glyphs.Add(positioned);
            double step = direction * (item.Advance + item.Gap);
            if (horizontal) x = x + step;
            else y = y + step;
            index = index + 1;
        }
        return true;
    }
SvgResolvedTextPosition ResolvePosition(SvgTextLayoutOptions options, int character)
    {
        SvgResolvedTextPosition result = new SvgResolvedTextPosition();
        if (character < options.X.Count) { result.HasX = true; result.X = options.X[character]; }
        if (character < options.Y.Count) { result.HasY = true; result.Y = options.Y[character]; }
        if (character < options.Dx.Count) result.Dx = options.Dx[character];
        if (character < options.Dy.Count) result.Dy = options.Dy[character];
        if (options.Rotate.Count > 0) result.Rotation = options.Rotate[character < options.Rotate.Count ? character : options.Rotate.Count - 1];
        int rangeIndex = 0;
        while (rangeIndex < options.PositioningRanges.Count)
        {
            SvgTextPositioningRange range = options.PositioningRanges[rangeIndex];
            int local = character - range.CharacterStart;
            if (local >= 0 && local < range.CharacterCount)
            {
                if (local < range.X.Count) { result.HasX = true; result.X = range.X[local]; }
                if (local < range.Y.Count) { result.HasY = true; result.Y = range.Y[local]; }
                if (local < range.Dx.Count) result.Dx = range.Dx[local];
                if (local < range.Dy.Count) result.Dy = range.Dy[local];
                if (range.Rotate.Count > 0) result.Rotation = range.Rotate[local < range.Rotate.Count ? local : range.Rotate.Count - 1];
            }
            rangeIndex = rangeIndex + 1;
        }
        return result;
    }
double BaselineOffset(SvgTextLayoutOptions options)
    {
        double result;
        SvgAlignmentBaseline alignment = options.AlignmentBaseline;
        if (alignment == SvgAlignmentBaseline.Auto || alignment == SvgAlignmentBaseline.Baseline) result = new SvgTextLayout().DominantOffset(options);
        else if (alignment == SvgAlignmentBaseline.Alphabetic) result = 0;
        else if (alignment == SvgAlignmentBaseline.Ideographic || alignment == SvgAlignmentBaseline.AfterEdge || alignment == SvgAlignmentBaseline.TextAfterEdge || alignment == SvgAlignmentBaseline.Bottom) result = -options.Descent;
        else if (alignment == SvgAlignmentBaseline.Hanging) result = options.Ascent * 0.8;
        else if (alignment == SvgAlignmentBaseline.Mathematical) result = options.XHeight * 0.5;
        else if (alignment == SvgAlignmentBaseline.Middle) result = options.XHeight * 0.5;
        else if (alignment == SvgAlignmentBaseline.Central || alignment == SvgAlignmentBaseline.Center) result = (options.Ascent - options.Descent) * 0.5;
        else result = options.Ascent;
        if (options.BaselineShift == SvgBaselineShiftKind.Sub) result = result + options.FontSize * 0.2;
        else if (options.BaselineShift == SvgBaselineShiftKind.Super) result = result - options.FontSize * 0.6;
        else if (options.BaselineShift == SvgBaselineShiftKind.Length) result = result - options.BaselineShiftValue;
        return result;
    }
double DominantOffset(SvgTextLayoutOptions options)
    {
        if (options.DominantBaseline == SvgDominantBaseline.Ideographic || options.DominantBaseline == SvgDominantBaseline.TextBottom) return -options.Descent;
        if (options.DominantBaseline == SvgDominantBaseline.Hanging) return options.Ascent * 0.8;
        if (options.DominantBaseline == SvgDominantBaseline.Mathematical) return options.XHeight * 0.5;
        if (options.DominantBaseline == SvgDominantBaseline.Central) return (options.Ascent - options.Descent) * 0.5;
        if (options.DominantBaseline == SvgDominantBaseline.Middle) return options.XHeight * 0.5;
        if (options.DominantBaseline == SvgDominantBaseline.TextTop) return options.Ascent;
        return 0;
    }
double AnchorOffset(SvgTextAnchor anchor, double advance, double direction)
    {
        if (anchor == SvgTextAnchor.Middle) return -direction * advance * 0.5;
        if (anchor == SvgTextAnchor.End) return -direction * advance;
        return 0;
    }
int FirstAddress(SvgShapedGlyph glyph, int[] addresses)
    {
        int scalar = glyph.ScalarStart;
        int end = glyph.ScalarStart + glyph.ScalarCount;
        while (scalar < end)
        {
            if (addresses[scalar] >= 0) return addresses[scalar];
            scalar = scalar + 1;
        }
        return -1;
    }
bool WordSpace(string text, int start, int length)
    {
        int end = start + length;
        int index = start;
        while (index < end)
        {
            char value = text[index];
            if (value == ' ' || value == '\t' || value == '\n' || value == '\r') return true;
            index = index + 1;
        }
        return false;
    }
bool Horizontal(SvgWritingMode mode)
    {
        return mode == SvgWritingMode.HorizontalTopToBottom;
    }
bool Valid(SvgShapingResult? shaping, SvgTextLayoutOptions? options)
    {
        if (shaping == null || !shaping.Success || shaping.Text == null || shaping.Glyphs == null || options == null) return false;
        if (!new SvgTextLayout().Finite(options.InitialX) || !new SvgTextLayout().Finite(options.InitialY) || !new SvgTextLayout().Finite(options.LetterSpacing) || !new SvgTextLayout().Finite(options.WordSpacing) || !new SvgTextLayout().Finite(options.FontSize) || !new SvgTextLayout().Finite(options.Ascent) || !new SvgTextLayout().Finite(options.Descent) || !new SvgTextLayout().Finite(options.XHeight) || !new SvgTextLayout().Finite(options.BaselineShiftValue) || !new SvgTextLayout().Finite(options.TextLength)) return false;
        if (options.FontSize <= 0 || options.Ascent < 0 || options.Descent < 0 || options.XHeight < 0 || options.HasTextLength && options.TextLength < 0) return false;
        if (options.MaxGlyphs < 1 || options.MaxPositioningRanges < 1 || options.MaxPositionValues < 1 || options.MaxChunks < 1) return false;
        if (options.X == null || options.Y == null || options.Dx == null || options.Dy == null || options.Rotate == null || options.PositioningRanges == null) return false;
        if (!new SvgTextLayout().ValidList(options.X) || !new SvgTextLayout().ValidList(options.Y) || !new SvgTextLayout().ValidList(options.Dx) || !new SvgTextLayout().ValidList(options.Dy) || !new SvgTextLayout().ValidList(options.Rotate)) return false;
        int mode = (int)options.WritingMode;
        int direction = (int)options.Direction;
        int anchor = (int)options.TextAnchor;
        int dominant = (int)options.DominantBaseline;
        int alignment = (int)options.AlignmentBaseline;
        int shift = (int)options.BaselineShift;
        int adjust = (int)options.LengthAdjust;
        return mode >= 0 && mode <= 4 && direction >= 0 && direction <= 2 && anchor >= 0 && anchor <= 2 && dominant >= 0 && dominant <= 8 && alignment >= 0 && alignment <= 14 && shift >= 0 && shift <= 3 && adjust >= 0 && adjust <= 1;
    }
bool ValidRanges(SvgTextLayoutOptions options, int characters)
    {
        int index = 0;
        while (index < options.PositioningRanges.Count)
        {
            SvgTextPositioningRange range = options.PositioningRanges[index];
            if (range == null || range.CharacterStart < 0 || range.CharacterCount < 0 || (long)range.CharacterStart + range.CharacterCount > characters) return false;
            if (range.X == null || range.Y == null || range.Dx == null || range.Dy == null || range.Rotate == null) return false;
            if (!new SvgTextLayout().ValidList(range.X) || !new SvgTextLayout().ValidList(range.Y) || !new SvgTextLayout().ValidList(range.Dx) || !new SvgTextLayout().ValidList(range.Dy) || !new SvgTextLayout().ValidList(range.Rotate)) return false;
            index = index + 1;
        }
        return true;
    }
long PositionValueCount(SvgTextLayoutOptions options)
    {
        long result = (long)options.X.Count + options.Y.Count + options.Dx.Count + options.Dy.Count + options.Rotate.Count;
        int index = 0;
        while (index < options.PositioningRanges.Count)
        {
            SvgTextPositioningRange range = options.PositioningRanges[index];
            result = result + (long)range.X.Count + range.Y.Count + range.Dx.Count + range.Dy.Count + range.Rotate.Count;
            index = index + 1;
        }
        return result;
    }
bool ValidList(System.Collections.Generic.List<double> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (!new SvgTextLayout().Finite(values[index])) return false;
            index = index + 1;
        }
        return true;
    }
bool ValidGlyph(SvgShapedGlyph? glyph, SvgShapingResult shaping, SvgUnicodeText text)
    {
        if (glyph == null || glyph.FontIdentifier != shaping.FontIdentifier || glyph.ScalarStart < 0 || glyph.ScalarCount < 1 || (long)glyph.ScalarStart + glyph.ScalarCount > text.Scalars.Count) return false;
        if (glyph.Utf16Start < 0 || glyph.Utf16Length < 1 || (long)glyph.Utf16Start + glyph.Utf16Length > shaping.Text.Length) return false;
        SvgUnicodeScalar first = text.Scalars[glyph.ScalarStart];
        SvgUnicodeScalar last = text.Scalars[glyph.ScalarStart + glyph.ScalarCount - 1];
        if (glyph.Utf16Start != first.Utf16Start || glyph.Utf16Length != last.Utf16Start + last.Utf16Length - first.Utf16Start) return false;
        return new SvgTextLayout().Finite(glyph.XAdvance) && new SvgTextLayout().Finite(glyph.YAdvance) && new SvgTextLayout().Finite(glyph.XOffset) && new SvgTextLayout().Finite(glyph.YOffset);
    }
bool Finite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
SvgShapedGlyph CopyGlyph(SvgShapedGlyph? source)
    {
        SvgShapedGlyph result = new SvgShapedGlyph();
        result.FontIdentifier = (source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FontIdentifier;
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
bool FailBoolean(SvgTextLayoutResult result, SvgTextLayoutError error, string message)
    {
        new SvgTextLayout().Fail(result, error, message);
        return false;
    }
SvgTextLayoutResult Fail(SvgTextLayoutResult result, SvgTextLayoutError error, string message)
    {
        if (result.Error == SvgTextLayoutError.None) result.Error = error;
        result.Message = message;
        return result;
    }
}
