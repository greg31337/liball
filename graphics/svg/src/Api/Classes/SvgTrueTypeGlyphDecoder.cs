namespace svg;

/// <summary>Expands bounded unhinted TrueType simple and composite glyph outlines.</summary>
public class SvgTrueTypeGlyphDecoder : ISvgTrueTypeGlyphDecoder
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgTrueTypeGlyphDecoder()
    {
    }

    /// <summary>Decodes one TrueType glyph using bounded defaults.</summary>
    /// <param name="font">A successfully parsed TrueType OpenType font.</param>
    /// <param name="glyphIndex">The zero-based glyph index.</param>
    /// <returns>The expanded glyph outline and quadratic SVG path.</returns>
    public SvgTrueTypeGlyphResult DecodeWithFontAndGlyphIndex(SvgFont font, int glyphIndex)
    {
        return new SvgTrueTypeGlyphDecoder().DecodeWithFontAndGlyphIndexAndOptions(font, glyphIndex, new SvgTrueTypeGlyphOptions());
    }

    /// <summary>Decodes one TrueType glyph using explicit point, component, and recursion limits.</summary>
    /// <param name="font">A successfully parsed TrueType OpenType font.</param>
    /// <param name="glyphIndex">The zero-based glyph index.</param>
    /// <param name="options">The bounded glyph expansion options.</param>
    /// <returns>The expanded glyph outline and quadratic SVG path.</returns>
    public SvgTrueTypeGlyphResult DecodeWithFontAndGlyphIndexAndOptions(SvgFont font, int glyphIndex, SvgTrueTypeGlyphOptions options)
    {
        if (!new SvgTrueTypeGlyphDecoder().Valid(font, glyphIndex, options)) return new SvgTrueTypeGlyphResult(false, SvgTrueTypeGlyphError.InvalidArgument, null);
        if (font.OutlineKind != SvgOpenTypeOutlineKind.TrueType) return new SvgTrueTypeGlyphResult(false, SvgTrueTypeGlyphError.UnsupportedOutline, null);
        SvgTrueTypeGlyphDecodeState state = new SvgTrueTypeGlyphDecodeState(font, options);
        if (!new SvgTrueTypeGlyphDecoder().ReadLocations(state)) return new SvgTrueTypeGlyphResult(false, state.Error, null);
        SvgTrueTypeGlyphOutline? outline = new SvgTrueTypeGlyphDecoder().ReadGlyph(state, glyphIndex, 0);
        if (outline == null) return new SvgTrueTypeGlyphResult(false, state.Error, null);
        outline.Path = new SvgTrueTypeGlyphDecoder().BuildPath(state, outline);
        if (outline.Path == null) return new SvgTrueTypeGlyphResult(false, state.Error, null);
        outline.Empty = outline.Points.Count == 0;
        return new SvgTrueTypeGlyphResult(true, SvgTrueTypeGlyphError.None, outline);
    }
internal SvgPathData? RebuildPathWithFontAndOutlineAndOptions(SvgFont font, SvgTrueTypeGlyphOutline outline, SvgTrueTypeGlyphOptions options)
    {
        if (font == null || outline == null || options == null || font.MaximumProfile == null) return null;
        SvgTrueTypeGlyphDecodeState state = new SvgTrueTypeGlyphDecodeState(font, options);
        return new SvgTrueTypeGlyphDecoder().BuildPath(state, outline);
    }
bool Valid(SvgFont? font, int glyphIndex, SvgTrueTypeGlyphOptions? options)
    {
        if (font == null || !font.Success || font.Bytes == null || font.Head == null || font.MaximumProfile == null || font.HorizontalMetrics == null) return false;
        if (glyphIndex < 0 || glyphIndex >= font.MaximumProfile.GlyphCount || font.HorizontalMetrics.Count != font.MaximumProfile.GlyphCount) return false;
        if (options == null || options.Limits == null || options.Limits.MaxGlyphsPerFont < 1 || options.Limits.MaxPointsPerGlyph < 1 || options.MaximumCompositeDepth < 0 || options.MaximumComponents < 1) return false;
        if (font.MaximumProfile.GlyphCount > options.Limits.MaxGlyphsPerFont || font.Head.UnitsPerEm < 1 || font.MaximumProfile.MaximumPoints < 0 || font.MaximumProfile.MaximumContours < 0 || font.MaximumProfile.MaximumCompositePoints < 0 || font.MaximumProfile.MaximumCompositeContours < 0 || font.MaximumProfile.MaximumComponentElements < 0 || font.MaximumProfile.MaximumComponentDepth < 0) return false;
        return true;
    }
bool ReadLocations(SvgTrueTypeGlyphDecodeState state)
    {
        SvgOpenTypeTable? locationTable = state.Font.FindTable("loca");
        SvgOpenTypeTable? glyphTable = state.Font.FindTable("glyf");
        if (locationTable == null || glyphTable == null) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidLocation);
        if (!new SvgTrueTypeGlyphDecoder().TableRange(state.Font.Bytes, locationTable) || !new SvgTrueTypeGlyphDecoder().TableRange(state.Font.Bytes, glyphTable)) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidLocation);
        int count = (state.Font.MaximumProfile ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GlyphCount + 1;
        int width = (state.Font.Head ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IndexToLocationFormat == 0 ? 2 : state.Font.Head.IndexToLocationFormat == 1 ? 4 : 0;
        if (width == 0 || (long)count * width > locationTable.Length) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidLocation);
        state.Locations = new int[count];
        int preceding = 0;
        int index = 0;
        while (index < count)
        {
            int absolute = locationTable.Offset + index * width;
            long value = width == 2 ? new SvgTrueTypeGlyphDecoder().ReadUInt16(state.Font.Bytes, absolute) * 2L : new SvgTrueTypeGlyphDecoder().ReadUInt32(state.Font.Bytes, absolute);
            if (value > System.Int32.MaxValue || value < preceding || value > glyphTable.Length) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidLocation);
            state.Locations[index] = (int)value;
            preceding = (int)value;
            index = index + 1;
        }
        return true;
    }
SvgTrueTypeGlyphOutline? ReadGlyph(SvgTrueTypeGlyphDecodeState state, int glyphIndex, int depth)
    {
        if (depth > state.Options.MaximumCompositeDepth) return new SvgTrueTypeGlyphDecoder().GlyphFail(state, SvgTrueTypeGlyphError.RecursionLimitExceeded);
        int declaredDepth = (state.Font.MaximumProfile ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaximumComponentDepth;
        if (depth > declaredDepth) return new SvgTrueTypeGlyphDecoder().GlyphFail(state, SvgTrueTypeGlyphError.RecursionLimitExceeded);
        if (state.ActiveGlyphs[glyphIndex]) return new SvgTrueTypeGlyphDecoder().GlyphFail(state, SvgTrueTypeGlyphError.CyclicComposite);
        SvgOpenTypeTable? glyphTable = state.Font.FindTable("glyf");
        int relativeStart = (state.Locations ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[glyphIndex];
        int relativeEnd = state.Locations[glyphIndex + 1];
        SvgTrueTypeGlyphOutline outline = new SvgTrueTypeGlyphOutline(glyphIndex);
        if (state.Font.HorizontalMetrics[glyphIndex] == null) return new SvgTrueTypeGlyphDecoder().GlyphFail(state, SvgTrueTypeGlyphError.InvalidArgument);
        if (state.Font.VerticalHeader != null && (state.Font.VerticalMetrics == null || state.Font.VerticalMetrics.Count != state.Font.MaximumProfile.GlyphCount || state.Font.VerticalMetrics[glyphIndex] == null)) return new SvgTrueTypeGlyphDecoder().GlyphFail(state, SvgTrueTypeGlyphError.InvalidArgument);
        if (relativeStart == relativeEnd)
        {
            new SvgTrueTypeGlyphDecoder().BuildPhantoms(state.Font, outline);
            outline.Empty = true;
            return outline;
        }
        if (relativeEnd - relativeStart < 10) return new SvgTrueTypeGlyphDecoder().GlyphFail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
        SvgTrueTypeGlyphCursor cursor = new SvgTrueTypeGlyphCursor(state.Font.Bytes, (glyphTable ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Offset + relativeStart, glyphTable.Offset + relativeEnd);
        int contourCount = cursor.ReadInt16();
        outline.XMin = cursor.ReadInt16();
        outline.YMin = cursor.ReadInt16();
        outline.XMax = cursor.ReadInt16();
        outline.YMax = cursor.ReadInt16();
        if (outline.XMin > outline.XMax || outline.YMin > outline.YMax) return new SvgTrueTypeGlyphDecoder().GlyphFail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
        new SvgTrueTypeGlyphDecoder().BuildPhantoms(state.Font, outline);
        state.ActiveGlyphs[glyphIndex] = true;
        bool success;
        if (contourCount >= 0) success = new SvgTrueTypeGlyphDecoder().ReadSimple(state, outline, contourCount, cursor);
        else success = new SvgTrueTypeGlyphDecoder().ReadComposite(state, outline, cursor, depth);
        state.ActiveGlyphs[glyphIndex] = false;
        if (!success) return null;
        outline.Empty = outline.Points.Count == 0;
        return outline;
    }
bool ReadSimple(SvgTrueTypeGlyphDecodeState state, SvgTrueTypeGlyphOutline outline, int contourCount, SvgTrueTypeGlyphCursor cursor)
    {
        outline.Composite = false;
        if (contourCount > (state.Font.MaximumProfile ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaximumContours) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
        if (contourCount == 0)
        {
            if (cursor.Has(2))
            {
                int instructions = cursor.ReadUInt16();
                if (!cursor.Advance(instructions)) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
                outline.InstructionLength = instructions;
            }
            return true;
        }
        if (!cursor.Has(contourCount * 2 + 2)) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
        int[] ends = new int[contourCount];
        int preceding = -1;
        int index = 0;
        while (index < contourCount)
        {
            ends[index] = cursor.ReadUInt16();
            if (ends[index] <= preceding) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
            preceding = ends[index];
            index = index + 1;
        }
        int pointCount = ends[contourCount - 1] + 1;
        if (pointCount > state.Options.Limits.MaxPointsPerGlyph) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.PointLimitExceeded);
        if (pointCount > state.Font.MaximumProfile.MaximumPoints) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
        int instructionLength = cursor.ReadUInt16();
        if (!cursor.Advance(instructionLength)) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
        outline.InstructionLength = instructionLength;
        byte[] flags = new byte[pointCount];
        int point = 0;
        while (point < pointCount)
        {
            if (!cursor.Has(1)) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
            int flag = cursor.ReadByte();
            if ((flag & 128) != 0) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
            flags[point] = (byte)flag;
            point = point + 1;
            if ((flag & 8) != 0)
            {
                if (!cursor.Has(1)) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
                int repeats = cursor.ReadByte();
                if (repeats > pointCount - point) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
                int repeat = 0;
                while (repeat < repeats)
                {
                    flags[point] = (byte)flag;
                    point = point + 1;
                    repeat = repeat + 1;
                }
            }
        }
        int[] xValues = new int[pointCount];
        int coordinate = 0;
        index = 0;
        while (index < pointCount)
        {
            int delta = new SvgTrueTypeGlyphDecoder().ReadCoordinate(cursor, flags[index], 2, 16);
            if (cursor.Position > cursor.End) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
            long next = (long)coordinate + delta;
            if (next < System.Int32.MinValue || next > System.Int32.MaxValue) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
            coordinate = (int)next;
            xValues[index] = coordinate;
            index = index + 1;
        }
        int[] yValues = new int[pointCount];
        coordinate = 0;
        index = 0;
        while (index < pointCount)
        {
            int delta = new SvgTrueTypeGlyphDecoder().ReadCoordinate(cursor, flags[index], 4, 32);
            if (cursor.Position > cursor.End) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
            long next = (long)coordinate + delta;
            if (next < System.Int32.MinValue || next > System.Int32.MaxValue) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
            coordinate = (int)next;
            yValues[index] = coordinate;
            index = index + 1;
        }
        index = 0;
        while (index < pointCount)
        {
            outline.Points.Add(new SvgTrueTypeGlyphPoint(xValues[index], yValues[index], (flags[index] & 1) != 0, false));
            index = index + 1;
        }
        int start = 0;
        index = 0;
        while (index < contourCount)
        {
            SvgTrueTypeGlyphContour contour = new SvgTrueTypeGlyphContour(start, ends[index]);
            point = start;
            while (point <= ends[index])
            {
                contour.Points.Add(outline.Points[point]);
                point = point + 1;
            }
            outline.Contours.Add(contour);
            start = ends[index] + 1;
            index = index + 1;
        }
        return true;
    }
int ReadCoordinate(SvgTrueTypeGlyphCursor cursor, int flag, int shortMask, int sameMask)
    {
        bool shortValue = (flag & shortMask) != 0;
        bool positiveOrSame = (flag & sameMask) != 0;
        if (shortValue)
        {
            if (!cursor.Has(1)) return new SvgTrueTypeGlyphDecoder().CoordinateFail(cursor);
            int value = cursor.ReadByte();
            return positiveOrSame ? value : -value;
        }
        if (positiveOrSame) return 0;
        if (!cursor.Has(2)) return new SvgTrueTypeGlyphDecoder().CoordinateFail(cursor);
        return cursor.ReadInt16();
    }
int CoordinateFail(SvgTrueTypeGlyphCursor cursor)
    {
        cursor.Position = cursor.End + 1;
        return 0;
    }
bool ReadComposite(SvgTrueTypeGlyphDecodeState state, SvgTrueTypeGlyphOutline outline, SvgTrueTypeGlyphCursor cursor, int depth)
    {
        outline.Composite = true;
        bool more = true;
        bool instructions = false;
        bool first = true;
        bool metricsUsed = false;
        int localComponents = 0;
        while (more)
        {
            if (!cursor.Has(4)) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
            int flags = cursor.ReadUInt16();
            int childGlyph = cursor.ReadUInt16();
            if ((flags & 57360) != 0 || childGlyph < 0 || childGlyph >= (state.Font.MaximumProfile ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GlyphCount) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
            int transformKinds = ((flags & 8) != 0 ? 1 : 0) + ((flags & 64) != 0 ? 1 : 0) + ((flags & 128) != 0 ? 1 : 0);
            if (transformKinds > 1 || (flags & 6144) == 6144) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
            bool words = (flags & 1) != 0;
            bool xy = (flags & 2) != 0;
            if (first && !xy) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
            int argumentBytes = words ? 4 : 2;
            if (!cursor.Has(argumentBytes)) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
            int argument1;
            int argument2;
            if (words)
            {
                argument1 = xy ? cursor.ReadInt16() : cursor.ReadUInt16();
                argument2 = xy ? cursor.ReadInt16() : cursor.ReadUInt16();
            }
            else
            {
                argument1 = cursor.ReadByte();
                argument2 = cursor.ReadByte();
                if (xy)
                {
                    if (argument1 >= 128) argument1 = argument1 - 256;
                    if (argument2 >= 128) argument2 = argument2 - 256;
                }
            }
            SvgTrueTypeGlyphComponent component = new SvgTrueTypeGlyphComponent();
            component.GlyphIndex = childGlyph;
            component.Flags = flags;
            component.UsesXYOffset = xy;
            component.UsesMetrics = (flags & 512) != 0;
            if (!new SvgTrueTypeGlyphDecoder().ReadTransform(state, cursor, flags, component)) return false;
            state.ComponentCount = state.ComponentCount + 1;
            localComponents = localComponents + 1;
            if (state.ComponentCount > state.Options.MaximumComponents) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.ComponentLimitExceeded);
            int declaredComponents = state.Font.MaximumProfile.MaximumComponentElements;
            if (localComponents > declaredComponents) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
            SvgTrueTypeGlyphOutline? child = new SvgTrueTypeGlyphDecoder().ReadGlyph(state, childGlyph, depth + 1);
            if (child == null) return false;
            if (!new SvgTrueTypeGlyphDecoder().PlaceComponent(state, outline, child, component, argument1, argument2, flags)) return false;
            if (component.UsesMetrics)
            {
                if (metricsUsed) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
                metricsUsed = true;
                outline.PhantomPoints.Clear();
                int phantom = 0;
                while (phantom < child.PhantomPoints.Count)
                {
                    SvgTrueTypeGlyphPoint? transformed = new SvgTrueTypeGlyphDecoder().Transform(child.PhantomPoints[phantom], component, true);
                    if (transformed == null) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.NonFiniteResult);
                    outline.PhantomPoints.Add(transformed);
                    phantom = phantom + 1;
                }
                outline.MetricsGlyphIndex = child.MetricsGlyphIndex;
                outline.HasVerticalMetrics = child.HasVerticalMetrics;
            }
            outline.Components.Add(component);
            more = (flags & 32) != 0;
            if ((flags & 256) != 0) instructions = true;
            first = false;
        }
        if (instructions)
        {
            if (!cursor.Has(2)) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
            int instructionLength = cursor.ReadUInt16();
            if (!cursor.Advance(instructionLength)) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
            outline.InstructionLength = instructionLength;
        }
        if (outline.Points.Count > state.Options.Limits.MaxPointsPerGlyph) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.PointLimitExceeded);
        int maximumPoints = (state.Font.MaximumProfile ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaximumCompositePoints;
        int maximumContours = state.Font.MaximumProfile.MaximumCompositeContours;
        if (outline.Points.Count > maximumPoints) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
        if (outline.Contours.Count > maximumContours) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
        return true;
    }
bool ReadTransform(SvgTrueTypeGlyphDecodeState state, SvgTrueTypeGlyphCursor cursor, int flags, SvgTrueTypeGlyphComponent component)
    {
        if ((flags & 8) != 0)
        {
            if (!cursor.Has(2)) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
            double scale = cursor.ReadInt16() / 16384.0;
            component.A = scale;
            component.D = scale;
        }
        else if ((flags & 64) != 0)
        {
            if (!cursor.Has(4)) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
            component.A = cursor.ReadInt16() / 16384.0;
            component.D = cursor.ReadInt16() / 16384.0;
        }
        else if ((flags & 128) != 0)
        {
            if (!cursor.Has(8)) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
            component.A = cursor.ReadInt16() / 16384.0;
            component.B = cursor.ReadInt16() / 16384.0;
            component.C = cursor.ReadInt16() / 16384.0;
            component.D = cursor.ReadInt16() / 16384.0;
        }
        return true;
    }
bool PlaceComponent(SvgTrueTypeGlyphDecodeState state, SvgTrueTypeGlyphOutline parent, SvgTrueTypeGlyphOutline child, SvgTrueTypeGlyphComponent component, int argument1, int argument2, int flags)
    {
        double offsetX;
        double offsetY;
        if (component.UsesXYOffset)
        {
            offsetX = argument1;
            offsetY = argument2;
            if ((flags & 2048) != 0)
            {
                double transformedX = component.A * offsetX + component.C * offsetY;
                double transformedY = component.B * offsetX + component.D * offsetY;
                offsetX = transformedX;
                offsetY = transformedY;
            }
        }
        else
        {
            SvgTrueTypeGlyphPoint? parentPoint = new SvgTrueTypeGlyphDecoder().IndexedPoint(parent, argument1);
            SvgTrueTypeGlyphPoint? childPoint = new SvgTrueTypeGlyphDecoder().IndexedPoint(child, argument2);
            if (parentPoint == null || childPoint == null) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
            double childX = component.A * childPoint.X + component.C * childPoint.Y;
            double childY = component.B * childPoint.X + component.D * childPoint.Y;
            offsetX = parentPoint.X - childX;
            offsetY = parentPoint.Y - childY;
        }
        if (!SvgFiniteMath.IsFinite(offsetX) || !SvgFiniteMath.IsFinite(offsetY)) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.NonFiniteResult);
        component.OffsetX = SvgFiniteMath.NormalizeZero(offsetX);
        component.OffsetY = SvgFiniteMath.NormalizeZero(offsetY);
        int basePoint = parent.Points.Count;
        int index = 0;
        while (index < child.Points.Count)
        {
            SvgTrueTypeGlyphPoint? transformed = new SvgTrueTypeGlyphDecoder().Transform(child.Points[index], component, false);
            if (transformed == null) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.NonFiniteResult);
            parent.Points.Add(transformed);
            if (parent.Points.Count > state.Options.Limits.MaxPointsPerGlyph) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.PointLimitExceeded);
            index = index + 1;
        }
        index = 0;
        while (index < child.Contours.Count)
        {
            SvgTrueTypeGlyphContour childContour = child.Contours[index];
            SvgTrueTypeGlyphContour contour = new SvgTrueTypeGlyphContour(basePoint + childContour.StartPointIndex, basePoint + childContour.EndPointIndex);
            int point = childContour.StartPointIndex;
            while (point <= childContour.EndPointIndex)
            {
                contour.Points.Add(parent.Points[basePoint + point]);
                point = point + 1;
            }
            parent.Contours.Add(contour);
            index = index + 1;
        }
        return true;
    }
SvgTrueTypeGlyphPoint? IndexedPoint(SvgTrueTypeGlyphOutline outline, int index)
    {
        if (index < 0) return null;
        if (index < outline.Points.Count) return outline.Points[index];
        int phantom = index - outline.Points.Count;
        if (phantom < outline.PhantomPoints.Count) return outline.PhantomPoints[phantom];
        return null;
    }
SvgTrueTypeGlyphPoint? Transform(SvgTrueTypeGlyphPoint point, SvgTrueTypeGlyphComponent component, bool phantom)
    {
        double x = component.A * point.X + component.C * point.Y + component.OffsetX;
        double y = component.B * point.X + component.D * point.Y + component.OffsetY;
        if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y)) return null;
        return new SvgTrueTypeGlyphPoint(SvgFiniteMath.NormalizeZero(x), SvgFiniteMath.NormalizeZero(y), point.OnCurve, phantom);
    }
void BuildPhantoms(SvgFont font, SvgTrueTypeGlyphOutline outline)
    {
        SvgOpenTypeHorizontalMetric horizontal = font.HorizontalMetrics[outline.GlyphIndex];
        double left = outline.XMin - horizontal.LeftSideBearing;
        double right = left + horizontal.AdvanceWidth;
        outline.PhantomPoints.Add(new SvgTrueTypeGlyphPoint(left, 0, true, true));
        outline.PhantomPoints.Add(new SvgTrueTypeGlyphPoint(right, 0, true, true));
        double top = outline.YMax;
        double bottom = top - (font.Head ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).UnitsPerEm;
        if (font.VerticalHeader != null && font.VerticalMetrics != null && font.VerticalMetrics.Count == (font.MaximumProfile ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GlyphCount)
        {
            SvgOpenTypeVerticalMetric vertical = font.VerticalMetrics[outline.GlyphIndex];
            top = outline.YMax + vertical.TopSideBearing;
            bottom = top - vertical.AdvanceHeight;
            outline.HasVerticalMetrics = true;
        }
        outline.PhantomPoints.Add(new SvgTrueTypeGlyphPoint(0, top, true, true));
        outline.PhantomPoints.Add(new SvgTrueTypeGlyphPoint(0, bottom, true, true));
    }
SvgPathData? BuildPath(SvgTrueTypeGlyphDecodeState state, SvgTrueTypeGlyphOutline outline)
    {
        SvgGeneratedPathBuilder builder = new SvgGeneratedPathBuilder();
        int index = 0;
        while (index < outline.Contours.Count)
        {
            if (!new SvgTrueTypeGlyphDecoder().AddContour(state, builder, outline.Contours[index])) return null;
            index = index + 1;
        }
        return builder.Build();
    }
bool AddContour(SvgTrueTypeGlyphDecodeState state, SvgGeneratedPathBuilder builder, SvgTrueTypeGlyphContour contour)
    {
        int count = contour.Points.Count;
        if (count < 1) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.InvalidGlyphData);
        SvgTrueTypeGlyphPoint first = contour.Points[0];
        SvgTrueTypeGlyphPoint last = contour.Points[count - 1];
        SvgTrueTypeGlyphPoint start;
        int index;
        int remaining;
        if (first.OnCurve)
        {
            start = first;
            index = 1;
            remaining = count - 1;
        }
        else if (last.OnCurve)
        {
            start = last;
            index = 0;
            remaining = count - 1;
        }
        else
        {
            start = (new SvgTrueTypeGlyphDecoder().Midpoint(last, first) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            index = 0;
            remaining = count;
        }
        if (start == null) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.NonFiniteResult);
        builder.MoveTo(start.X, start.Y);
        while (remaining > 0)
        {
            SvgTrueTypeGlyphPoint point = contour.Points[index % count];
            if (point.OnCurve)
            {
                builder.LineTo(point.X, point.Y);
                index = index + 1;
                remaining = remaining - 1;
            }
            else
            {
                SvgTrueTypeGlyphPoint next = remaining > 1 ? contour.Points[(index + 1) % count] : start;
                if (next.OnCurve)
                {
                    builder.QuadraticTo(point.X, point.Y, next.X, next.Y);
                    if (remaining > 1)
                    {
                        index = index + 2;
                        remaining = remaining - 2;
                    }
                    else
                    {
                        index = index + 1;
                        remaining = remaining - 1;
                    }
                }
                else
                {
                    SvgTrueTypeGlyphPoint? middle = new SvgTrueTypeGlyphDecoder().Midpoint(point, next);
                    if (middle == null) return new SvgTrueTypeGlyphDecoder().Fail(state, SvgTrueTypeGlyphError.NonFiniteResult);
                    builder.QuadraticTo(point.X, point.Y, middle.X, middle.Y);
                    index = index + 1;
                    remaining = remaining - 1;
                }
            }
        }
        builder.Close();
        return true;
    }
SvgTrueTypeGlyphPoint? Midpoint(SvgTrueTypeGlyphPoint first, SvgTrueTypeGlyphPoint second)
    {
        double x = first.X / 2 + second.X / 2;
        double y = first.Y / 2 + second.Y / 2;
        if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y)) return null;
        return new SvgTrueTypeGlyphPoint(SvgFiniteMath.NormalizeZero(x), SvgFiniteMath.NormalizeZero(y), true, false);
    }
bool Fail(SvgTrueTypeGlyphDecodeState state, SvgTrueTypeGlyphError error)
    {
        if (state.Error == SvgTrueTypeGlyphError.None) state.Error = error;
        return false;
    }
SvgTrueTypeGlyphOutline? GlyphFail(SvgTrueTypeGlyphDecodeState state, SvgTrueTypeGlyphError error)
    {
        new SvgTrueTypeGlyphDecoder().Fail(state, error);
        return null;
    }
int ReadUInt16(byte[]? bytes, int offset)
    {
        return (bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[offset] << 8 | bytes[offset + 1];
    }
uint ReadUInt32(byte[]? bytes, int offset)
    {
        return (uint)(bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[offset] << 24 | (uint)bytes[offset + 1] << 16 | (uint)bytes[offset + 2] << 8 | bytes[offset + 3];
    }
bool TableRange(byte[]? bytes, SvgOpenTypeTable table)
    {
        return table.Offset >= 0 && table.Length >= 0 && (long)table.Offset + table.Length <= (bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length;
    }
}
