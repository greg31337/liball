namespace svg;

/// <summary>Projects positioned SVG text onto resolved, referenced-shape, or inline path geometry.</summary>
public class SvgTextPathLayout : ISvgTextPathLayout
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgTextPathLayout()
    {
    }

    /// <summary>Projects positioned text onto a caller-resolved referenced path.</summary>
    /// <param name="text">The successful planar text layout.</param>
    /// <param name="path">The referenced SVG path.</param>
    /// <returns>The projected text path glyphs.</returns>
    public SvgTextPathResult LayoutWithTextAndPath(SvgTextLayoutResult text, SvgPathData path)
    {
        return new SvgTextPathLayout().LayoutWithTextAndPathAndOptions(text, path, new SvgTextPathOptions());
    }

    /// <summary>Projects positioned text onto a caller-resolved referenced path.</summary>
    /// <param name="text">The successful planar text layout.</param>
    /// <param name="path">The referenced SVG path.</param>
    /// <param name="options">The start, calibration, method, side, overflow, and bounds.</param>
    /// <returns>The projected text path glyphs.</returns>
    public SvgTextPathResult LayoutWithTextAndPathAndOptions(SvgTextLayoutResult text, SvgPathData path, SvgTextPathOptions options)
    {
        return new SvgTextPathLayout().LayoutCore(text, path, options, SvgTextPathSourceKind.ReferencedPath, null, false);
    }

    /// <summary>Converts a referenced SVG shape and projects positioned text onto it.</summary>
    /// <param name="text">The successful planar text layout.</param>
    /// <param name="shape">The resolved referenced basic shape or path.</param>
    /// <param name="options">The start, calibration, method, side, overflow, and bounds.</param>
    /// <returns>The projected text path glyphs.</returns>
    public SvgTextPathResult LayoutShape(SvgTextLayoutResult text, SvgShapeResult? shape, SvgTextPathOptions? options)
    {
        SvgTextPathResult failure = new SvgTextPathResult(SvgTextPathSourceKind.ReferencedShape);
        if (shape == null || options == null || !shape.Success || !shape.HasGeometry) return new SvgTextPathLayout().Fail(failure, SvgTextPathError.InvalidArgument, "The referenced textPath shape is invalid or empty.");
        SvgPathData path;
        if (shape.Kind == SvgShapeKind.Path) path = (shape.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        else
        {
            SvgShapePathResult converted = shape.ToPathWithLimit(options.MaximumPathCommands);
            if (!converted.Success)
            {
                SvgTextPathError error = converted.Error == SvgShapePathError.CommandLimitExceeded ? SvgTextPathError.LimitExceeded : SvgTextPathError.InvalidPath;
                return new SvgTextPathLayout().Fail(failure, error, "The referenced shape could not be converted to a text path.");
            }
            path = (converted.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        }
        SvgPathLength? calibration = !options.HasPathLength && shape.HasPathLength ? shape.PathLength : null;
        return new SvgTextPathLayout().LayoutCore(text, path, options, SvgTextPathSourceKind.ReferencedShape, calibration, shape.Closed);
    }

    /// <summary>Parses inline SVG path data and projects positioned text onto it.</summary>
    /// <param name="text">The successful planar text layout.</param>
    /// <param name="pathData">The complete inline SVG path data.</param>
    /// <param name="options">The start, calibration, method, side, overflow, parsing, and measurement bounds.</param>
    /// <returns>The projected text path glyphs.</returns>
    public SvgTextPathResult LayoutInline(SvgTextLayoutResult text, string? pathData, SvgTextPathOptions? options)
    {
        SvgTextPathResult failure = new SvgTextPathResult(SvgTextPathSourceKind.InlinePathData);
        if (pathData == null || options == null) return new SvgTextPathLayout().Fail(failure, SvgTextPathError.InvalidArgument, "Inline textPath arguments are invalid.");
        SvgPathParseResult parsed = new SvgPathParser().Parse(pathData, options.MaximumPathCommands, options.MaximumTokenCharacters);
        if (!parsed.Success)
        {
            SvgTextPathError error = parsed.Error == SvgPathParseError.SegmentLimitExceeded ? SvgTextPathError.LimitExceeded : SvgTextPathError.InvalidPath;
            return new SvgTextPathLayout().Fail(failure, error, "Inline textPath data is invalid or exceeds its command limit.");
        }
        return new SvgTextPathLayout().LayoutCore(text, parsed.PathData, options, SvgTextPathSourceKind.InlinePathData, null, false);
    }
SvgTextPathResult LayoutCore(SvgTextLayoutResult text, SvgPathData? path, SvgTextPathOptions options, SvgTextPathSourceKind sourceKind, SvgPathLength? inheritedCalibration, bool closedHint)
    {
        SvgTextPathResult result = new SvgTextPathResult(sourceKind);
        if (!new SvgTextPathLayout().Valid(text, path, options)) return new SvgTextPathLayout().Fail(result, SvgTextPathError.InvalidArgument, "TextPath layout arguments or options are invalid.");
        if (text.Glyphs.Count > options.MaximumGlyphs) return new SvgTextPathLayout().Fail(result, SvgTextPathError.LimitExceeded, "TextPath layout exceeds MaximumGlyphs.");
        if ((long)text.Glyphs.Count * 2 > options.MaximumPathQueries) return new SvgTextPathLayout().Fail(result, SvgTextPathError.LimitExceeded, "TextPath layout exceeds MaximumPathQueries.");
        if (options.CancellationToken.IsCancellationRequested) return new SvgTextPathLayout().Fail(result, SvgTextPathError.Cancelled, "TextPath layout was cancelled.");
        SvgPathMeasureOptions measurement = new SvgPathMeasureOptions();
        measurement.Tolerance = options.Tolerance;
        measurement.MaximumSubdivisionDepth = options.MaximumSubdivisionDepth;
        measurement.MaximumFlattenedSegments = options.MaximumFlattenedSegments;
        if (options.HasPathLength) measurement.PathLength = new SvgPathLength(options.PathLength);
        else measurement.PathLength = inheritedCalibration;
        SvgPathMeasureState state = SvgPathMeasureState.Create(path, measurement);
        if (state.Error != SvgPathMetricError.None) return new SvgTextPathLayout().Fail(result, new SvgTextPathLayout().MetricError(state.Error), "TextPath geometry could not be measured.");
        SvgPathMetricResult metric = state.MetricResult();
        if (!metric.Success) return new SvgTextPathLayout().Fail(result, new SvgTextPathLayout().MetricError(metric.Error), "TextPath geometry could not be measured.");
        if (metric.ActualLength <= 0 || metric.CalibratedLength <= 0) return new SvgTextPathLayout().Fail(result, SvgTextPathError.InvalidPath, "TextPath geometry has no positive drawable length.");
        result.Path = path;
        result.Text = text.Text;
        result.WritingMode = text.WritingMode;
        result.Direction = text.Direction;
        result.FontSize = text.FontSize;
        result.Ascent = text.Ascent;
        result.Descent = text.Descent;
        result.XHeight = text.XHeight;
        result.ActualPathLength = metric.ActualLength;
        result.CalibratedPathLength = metric.CalibratedLength;
        result.PathLengthScale = metric.Scale;
        result.Closed = closedHint || new SvgTextPathLayout().Closed(path);
        result.ResolvedStartOffset = options.StartOffsetIsPercentage ? options.StartOffset * metric.CalibratedLength / 100 : options.StartOffset;
        double available = result.Closed ? metric.CalibratedLength : metric.CalibratedLength - result.ResolvedStartOffset;
        if (available < 0) available = 0;
        if (!new SvgTextPathLayout().Adjust(text, options, result, available)) return result;
        if (!new SvgTextPathLayout().Project(text, options, state, result)) return result;
        result.Success = true;
        return result;
    }
bool Adjust(SvgTextLayoutResult text, SvgTextPathOptions options, SvgTextPathResult result, double available)
    {
        if (options.Method == SvgTextPathMethod.Stretch)
        {
            if (text.ActualTextLength == 0 && available != 0) return new SvgTextPathLayout().FailBoolean(result, SvgTextPathError.InvalidArgument, "TextPath stretch cannot scale zero text length.");
            result.MethodScale = text.ActualTextLength == 0 ? 1 : available / text.ActualTextLength;
        }
        if (options.Spacing == SvgTextPathSpacing.Auto && options.Method == SvgTextPathMethod.Align)
        {
            int gaps = new SvgTextPathLayout().AddressableGaps(text);
            if (gaps > 0) result.SpacingAdjustment = (available - text.ActualTextLength) / gaps;
        }
        return new SvgTextPathLayout().Finite(result.MethodScale) && new SvgTextPathLayout().Finite(result.SpacingAdjustment) || new SvgTextPathLayout().FailBoolean(result, SvgTextPathError.InvalidArgument, "TextPath adjustment produced a non-finite value.");
    }
bool Project(SvgTextLayoutResult text, SvgTextPathOptions options, SvgPathMeasureState state, SvgTextPathResult result)
    {
        bool horizontal = new SvgTextPathLayout().Horizontal(text);
        int gapsBefore = 0;
        int index = 0;
        while (index < text.Glyphs.Count)
        {
            if (options.CancellationToken.IsCancellationRequested) return new SvgTextPathLayout().FailBoolean(result, SvgTextPathError.Cancelled, "TextPath glyph projection was cancelled.");
            SvgPositionedGlyph source = text.Glyphs[index];
            if (!new SvgTextPathLayout().ValidGlyph(source)) return new SvgTextPathLayout().FailBoolean(result, SvgTextPathError.InvalidArgument, "A positioned text glyph is invalid.");
            if (index > 0 && text.Glyphs[index - 1].CharacterIndex != source.CharacterIndex) gapsBefore = gapsBefore + 1;
            double inline = horizontal ? source.X : source.Y;
            double cross = horizontal ? source.Y : source.X;
            double distance = result.ResolvedStartOffset + inline * result.MethodScale + gapsBefore * result.SpacingAdjustment;
            double advance = source.InlineAdvance * result.MethodScale;
            double midpoint = distance + advance * 0.5;
            bool overflowed = !result.Closed && (midpoint < 0 || midpoint > result.CalibratedPathLength);
            SvgTextPathFrame? origin = new SvgTextPathLayout().Frame(state, distance, result, options.Side);
            SvgTextPathFrame? middle = new SvgTextPathLayout().Frame(state, midpoint, result, options.Side);
            if (origin == null || middle == null) return new SvgTextPathLayout().FailBoolean(result, SvgTextPathError.InvalidPath, "A textPath point or tangent query failed.");
            SvgTextPathGlyph glyph = new SvgTextPathGlyph();
            glyph.Glyph = new SvgTextPathLayout().CopyPositioned(source);
            glyph.Point = new SvgPoint((origin.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X + (origin.Normal ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X * cross, origin.Point.Y + origin.Normal.Y * cross);
            glyph.Tangent = middle.Tangent;
            glyph.Normal = middle.Normal;
            double angle = System.Math.Atan2((middle.Tangent ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Y, middle.Tangent.X) * 180 / System.Math.PI;
            glyph.Rotation = angle + source.Rotation + (horizontal ? 0 : -90);
            glyph.PathDistance = origin.Distance;
            glyph.ActualPathDistance = origin.ActualDistance;
            glyph.MethodScale = result.MethodScale;
            glyph.Wrapped = origin.Wrapped;
            glyph.Overflowed = overflowed;
            glyph.Visible = !overflowed || options.Overflow == SvgTextPathOverflow.Visible;
            if (overflowed)
            {
                result.OverflowedGlyphs = result.OverflowedGlyphs + 1;
                if (!glyph.Visible) result.HiddenGlyphs = result.HiddenGlyphs + 1;
            }
            result.Glyphs.Add(glyph);
            index = index + 1;
        }
        return true;
    }
SvgTextPathFrame? Frame(SvgPathMeasureState state, double distance, SvgTextPathResult result, SvgTextPathSide side)
    {
        double length = result.CalibratedPathLength;
        double progression = distance;
        bool wrapped = false;
        if (result.Closed)
        {
            progression = progression % length;
            if (progression < 0) progression = progression + length;
            wrapped = progression != distance;
        }
        double clamped = progression < 0 ? 0 : progression > length ? length : progression;
        double query = side == SvgTextPathSide.Right ? length - clamped : clamped;
        SvgPathLocationResult location = state.Location(query);
        if (!location.Success || location.Point == null || location.Tangent == null) return null;
        double direction = side == SvgTextPathSide.Right ? -1 : 1;
        SvgVector tangent = new SvgVector(location.Tangent.X * direction, location.Tangent.Y * direction);
        SvgVector normal = new SvgVector(-tangent.Y, tangent.X);
        double excess = progression < 0 ? progression : progression > length ? progression - length : 0;
        double actualExcess = excess * result.PathLengthScale;
        SvgTextPathFrame frame = new SvgTextPathFrame();
        frame.Point = new SvgPoint(location.Point.X + tangent.X * actualExcess, location.Point.Y + tangent.Y * actualExcess);
        frame.Tangent = tangent;
        frame.Normal = normal;
        frame.Distance = progression;
        frame.ActualDistance = location.ActualDistance;
        frame.Wrapped = wrapped;
        return frame;
    }
int AddressableGaps(SvgTextLayoutResult text)
    {
        int result = 0;
        int index = 1;
        while (index < text.Glyphs.Count)
        {
            if (text.Glyphs[index - 1].CharacterIndex != text.Glyphs[index].CharacterIndex) result = result + 1;
            index = index + 1;
        }
        return result;
    }
bool Horizontal(SvgTextLayoutResult text)
    {
        return text.WritingMode == SvgWritingMode.HorizontalTopToBottom;
    }
bool Closed(SvgPathData? path)
    {
        SvgPathNormalizeResult normalized = (path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Normalize();
        if (!normalized.Success || (normalized.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GetSubpathCount() != 1 || normalized.PathData.Commands.Count == 0) return false;
        return normalized.PathData.Commands[normalized.PathData.Commands.Count - 1].Kind == SvgPathCommandKind.ClosePath;
    }
SvgTextPathError MetricError(SvgPathMetricError error)
    {
        if (error == SvgPathMetricError.SegmentLimitExceeded || error == SvgPathMetricError.SubdivisionLimitExceeded) return SvgTextPathError.LimitExceeded;
        if (error == SvgPathMetricError.InvalidArgument) return SvgTextPathError.InvalidArgument;
        return SvgTextPathError.InvalidPath;
    }
bool Valid(SvgTextLayoutResult? text, SvgPathData? path, SvgTextPathOptions? options)
    {
        if (text == null || !text.Success || text.Glyphs == null || path == null || options == null) return false;
        if (!new SvgTextPathLayout().Finite(options.StartOffset) || !new SvgTextPathLayout().Finite(options.PathLength) || !new SvgTextPathLayout().Finite(options.Tolerance)) return false;
        if (options.HasPathLength && options.PathLength <= 0 || options.Tolerance <= 0) return false;
        if (options.MaximumSubdivisionDepth < 0 || options.MaximumFlattenedSegments < 1 || options.MaximumPathCommands < 1 || options.MaximumTokenCharacters < 1 || options.MaximumGlyphs < 1 || options.MaximumPathQueries < 1) return false;
        int method = (int)options.Method;
        int spacing = (int)options.Spacing;
        int side = (int)options.Side;
        int overflow = (int)options.Overflow;
        return method >= 0 && method <= 1 && spacing >= 0 && spacing <= 1 && side >= 0 && side <= 1 && overflow >= 0 && overflow <= 1;
    }
bool ValidGlyph(SvgPositionedGlyph glyph)
    {
        return glyph != null && glyph.Glyph != null && glyph.CharacterIndex >= 0 && new SvgTextPathLayout().Finite(glyph.X) && new SvgTextPathLayout().Finite(glyph.Y) && new SvgTextPathLayout().Finite(glyph.Rotation) && new SvgTextPathLayout().Finite(glyph.InlineAdvance) && new SvgTextPathLayout().Finite(glyph.SpacingAfter) && new SvgTextPathLayout().Finite(glyph.InlineScale);
    }
bool Finite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
SvgPositionedGlyph CopyPositioned(SvgPositionedGlyph source)
    {
        SvgPositionedGlyph result = new SvgPositionedGlyph();
        result.Glyph = new SvgTextPathLayout().CopyShaped(source.Glyph);
        result.CharacterIndex = source.CharacterIndex;
        result.ChunkIndex = source.ChunkIndex;
        result.X = source.X;
        result.Y = source.Y;
        result.Rotation = source.Rotation;
        result.InlineAdvance = source.InlineAdvance;
        result.SpacingAfter = source.SpacingAfter;
        result.InlineScale = source.InlineScale;
        result.WordSpace = source.WordSpace;
        return result;
    }
SvgShapedGlyph CopyShaped(SvgShapedGlyph? source)
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
bool FailBoolean(SvgTextPathResult result, SvgTextPathError error, string message)
    {
        new SvgTextPathLayout().Fail(result, error, message);
        return false;
    }
SvgTextPathResult Fail(SvgTextPathResult result, SvgTextPathError error, string message)
    {
        if (result.Error == SvgTextPathError.None) result.Error = error;
        result.Message = message;
        return result;
    }
}
