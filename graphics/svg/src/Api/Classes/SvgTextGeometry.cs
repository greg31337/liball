namespace svg;

/// <summary>Converts positioned SVG text into paths, bounds, extraction, and hit-test geometry.</summary>
public class SvgTextGeometry : ISvgTextGeometry
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgTextGeometry()
    {
    }

    /// <summary>Converts planar positioned text into bounded glyph and decoration geometry.</summary>
    /// <param name="layout">The successful planar text layout.</param>
    /// <param name="options">The fonts, decorations, stroke, and security bounds.</param>
    /// <returns>The converted geometry, extraction, and bounds.</returns>
    public SvgTextGeometryResult BuildWithSvgTextLayoutResultAndSvgTextGeometryOptions(SvgTextLayoutResult? layout, SvgTextGeometryOptions options)
    {
        if (layout == null || !layout.Success || layout.Glyphs == null) return new SvgTextGeometry().Failure(SvgTextGeometryError.InvalidArgument, "The planar text layout is invalid.");
        SvgTextGeometryInput input = new SvgTextGeometryInput();
        input.Text = layout.Text;
        input.WritingMode = layout.WritingMode;
        input.Direction = layout.Direction;
        input.FontSize = layout.FontSize;
        input.Ascent = layout.Ascent;
        input.Descent = layout.Descent;
        input.XHeight = layout.XHeight;
        int index = 0;
        while (index < layout.Glyphs.Count)
        {
            SvgPositionedGlyph glyph = layout.Glyphs[index];
            SvgTextGeometryInputGlyph item = new SvgTextGeometryInputGlyph();
            item.Glyph = glyph;
            item.Point = glyph == null ? null : new SvgPoint(glyph.X, glyph.Y);
            item.Rotation = glyph == null ? 0 : glyph.Rotation;
            input.Glyphs.Add(item);
            index = index + 1;
        }
        return new SvgTextGeometry().BuildCore(input, options);
    }

    /// <summary>Converts projected textPath glyphs into bounded glyph and decoration geometry.</summary>
    /// <param name="layout">The successful textPath layout.</param>
    /// <param name="options">The fonts, decorations, stroke, and security bounds.</param>
    /// <returns>The converted geometry, extraction, and bounds.</returns>
    public SvgTextGeometryResult BuildWithSvgTextPathResultAndSvgTextGeometryOptions(SvgTextPathResult? layout, SvgTextGeometryOptions options)
    {
        if (layout == null || !layout.Success || layout.Glyphs == null) return new SvgTextGeometry().Failure(SvgTextGeometryError.InvalidArgument, "The textPath layout is invalid.");
        SvgTextGeometryInput input = new SvgTextGeometryInput();
        input.Text = layout.Text;
        input.WritingMode = layout.WritingMode;
        input.Direction = layout.Direction;
        input.FontSize = layout.FontSize;
        input.Ascent = layout.Ascent;
        input.Descent = layout.Descent;
        input.XHeight = layout.XHeight;
        int index = 0;
        while (index < layout.Glyphs.Count)
        {
            SvgTextPathGlyph projected = layout.Glyphs[index];
            SvgTextGeometryInputGlyph item = new SvgTextGeometryInputGlyph();
            item.Glyph = projected == null ? null : projected.Glyph;
            item.Point = projected == null ? null : projected.Point;
            item.Rotation = projected == null ? 0 : projected.Rotation;
            item.MethodScale = projected == null ? 1 : projected.MethodScale;
            item.Visible = projected != null && projected.Visible;
            input.Glyphs.Add(item);
            index = index + 1;
        }
        return new SvgTextGeometry().BuildCore(input, options);
    }

    /// <summary>Hit-tests converted text geometry.</summary>
    /// <param name="geometry">The successful converted text geometry.</param>
    /// <param name="point">The finite query point.</param>
    /// <param name="options">The query mode and bounded fill options.</param>
    /// <returns>The topmost matching glyph or cluster.</returns>
    public SvgTextHitTestResult HitTest(SvgTextGeometryResult? geometry, SvgPoint? point, SvgTextHitTestOptions? options)
    {
        SvgTextHitTestResult result = new SvgTextHitTestResult(point == null ? new SvgPoint(0, 0) : point);
        if (geometry == null || !geometry.Success || point == null || options == null || !new SvgTextGeometry().Finite(point.X) || !new SvgTextGeometry().Finite(point.Y) || !new SvgTextGeometry().ValidHitOptions(options)) return new SvgTextGeometry().HitFailure(result, SvgTextGeometryError.InvalidArgument);
        if (options.Mode == SvgTextHitTestMode.ClusterLogical)
        {
            int cluster = geometry.Clusters.Count - 1;
            while (cluster >= 0)
            {
                SvgTextClusterGeometry candidate = geometry.Clusters[cluster];
                if (new SvgTextGeometry().Contains(candidate.LogicalBounds, point))
                {
                    result.Success = true;
                    result.Hit = true;
                    result.ClusterIndex = candidate.Index;
                    if (candidate.GlyphIndices.Count > 0)
                    {
                        result.GlyphIndex = candidate.GlyphIndices[candidate.GlyphIndices.Count - 1];
                        result.CharacterIndex = geometry.Glyphs[result.GlyphIndex].CharacterIndex;
                    }
                    return result;
                }
                cluster = cluster - 1;
            }
            result.Success = true;
            return result;
        }
        int index = geometry.Glyphs.Count - 1;
        while (index >= 0)
        {
            SvgTextGlyphGeometry glyph = geometry.Glyphs[index];
            bool hit = false;
            if (glyph.Visible && options.Mode == SvgTextHitTestMode.GlyphLogical) hit = new SvgTextGeometry().Contains(glyph.LogicalBounds, point);
            else if (glyph.Visible && glyph.Path != null && glyph.Path.Commands.Count > 0)
            {
                SvgFillContainmentOptions fill = new SvgFillContainmentOptions();
                fill.Tolerance = options.Tolerance;
                fill.MaximumSubdivisionDepth = options.MaximumSubdivisionDepth;
                fill.MaximumSegments = options.MaximumSegments;
                SvgFillContainmentResult contained = glyph.Path.ClassifyFillWithOptions(point, SvgFillRule.Nonzero, fill);
                if (!contained.Success) return new SvgTextGeometry().HitFailure(result, new SvgTextGeometry().MapWithSvgFillContainmentError(contained.Error));
                hit = contained.Contains;
            }
            if (hit)
            {
                result.Success = true;
                result.Hit = true;
                result.GlyphIndex = glyph.Index;
                result.ClusterIndex = glyph.ClusterIndex;
                result.CharacterIndex = glyph.CharacterIndex;
                return result;
            }
            index = index - 1;
        }
        result.Success = true;
        return result;
    }

    internal SvgRect EmptyBounds()
    {
        return SvgRect.Create(0, 0, 0, 0, true);
    }
SvgTextGeometryResult BuildCore(SvgTextGeometryInput input, SvgTextGeometryOptions options)
    {
        SvgTextGeometryResult result = new SvgTextGeometryResult();
        if (!new SvgTextGeometry().ValidWithInputAndOptions(input, options)) return new SvgTextGeometry().Fail(result, SvgTextGeometryError.InvalidArgument, "Text geometry arguments or options are invalid.");
        if (input.Glyphs.Count > options.MaximumGlyphs) return new SvgTextGeometry().Fail(result, SvgTextGeometryError.LimitExceeded, "Text geometry exceeds MaximumGlyphs.");
        if (options.CancellationToken.IsCancellationRequested) return new SvgTextGeometry().Fail(result, SvgTextGeometryError.Cancelled, "Text geometry was cancelled.");
        System.Collections.Generic.List<SvgTextOutlineCacheEntry> cache = new System.Collections.Generic.List<SvgTextOutlineCacheEntry>();
        SvgGeneratedPathBuilder combined = new SvgGeneratedPathBuilder();
        int commands = 0;
        int index = 0;
        while (index < input.Glyphs.Count)
        {
            if (options.CancellationToken.IsCancellationRequested) return new SvgTextGeometry().Fail(result, SvgTextGeometryError.Cancelled, "Text geometry was cancelled during glyph conversion.");
            SvgTextGeometryInputGlyph source = input.Glyphs[index];
            if (!new SvgTextGeometry().ValidWithSourceAndText(source, input.Text)) return new SvgTextGeometry().Fail(result, SvgTextGeometryError.InvalidArgument, "A positioned glyph or source cluster is invalid.");
            SvgTextGlyphGeometry? glyph = new SvgTextGeometry().ConvertGlyph(source, input, options, cache, index, result);
            if (glyph == null) return result;
            if (glyph.Visible)
            {
                if (!new SvgTextGeometry().Append(combined, glyph.Path, options.MaximumPathCommands, commands)) return new SvgTextGeometry().Fail(result, SvgTextGeometryError.LimitExceeded, "Text geometry exceeds MaximumPathCommands.");
                commands = commands + (glyph.Path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count;
            }
            result.Glyphs.Add(glyph);
            result.LogicalBounds = (new SvgTextGeometry().Union(result.LogicalBounds, glyph.LogicalBounds, result) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            if (result.Error != SvgTextGeometryError.None) return result;
            if (glyph.Visible)
            {
                result.InkBounds = (new SvgTextGeometry().Union(result.InkBounds, glyph.InkBounds, result) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
                result.StrokeBounds = (new SvgTextGeometry().Union(result.StrokeBounds, glyph.StrokeBounds, result) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
                if (result.Error != SvgTextGeometryError.None) return result;
            }
            if (glyph.Visible && !new SvgTextGeometry().Decorations(source, input, options, index, result, combined, commands)) return result;
            int decorationIndex = result.Decorations.Count - 1;
            while (decorationIndex >= 0 && result.Decorations[decorationIndex].GlyphIndex == index)
            {
                commands = commands + (result.Decorations[decorationIndex].Path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count;
                decorationIndex = decorationIndex - 1;
            }
            index = index + 1;
        }
        if (!new SvgTextGeometry().Clusters(input.Text, result)) return result;
        result.Path = combined.Build();
        result.Success = true;
        return result;
    }
SvgTextGlyphGeometry? ConvertGlyph(SvgTextGeometryInputGlyph source, SvgTextGeometryInput input, SvgTextGeometryOptions options, System.Collections.Generic.List<SvgTextOutlineCacheEntry> cache, int index, SvgTextGeometryResult result)
    {
        SvgTextFontResource? resource = new SvgTextGeometry().Font(options, ((source.Glyph ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Glyph ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FontIdentifier);
        if (resource == null) return new SvgTextGeometry().GlyphFail(result, SvgTextGeometryError.FontNotFound, "No font resource matches a positioned glyph.");
        SvgPathData? outline = new SvgTextGeometry().Outline(resource, (source.Glyph.Glyph ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GlyphIndex, options, cache, result);
        if (outline == null) return null;
        SvgMatrix? matrix = new SvgTextGeometry().GlyphMatrix(source, input.WritingMode, resource);
        if (matrix == null) return new SvgTextGeometry().GlyphFail(result, SvgTextGeometryError.InvalidOutline, "A glyph transformation was not finite.");
        SvgPathTransformOptions transformOptions = new SvgPathTransformOptions();
        transformOptions.MaximumCommands = options.MaximumPathCommands;
        SvgPathTransformResult transformed = outline.TransformWithOptions(matrix, transformOptions);
        if (!transformed.Success) return new SvgTextGeometry().GlyphFail(result, transformed.Error == SvgPathTransformError.CommandLimitExceeded ? SvgTextGeometryError.LimitExceeded : SvgTextGeometryError.InvalidOutline, "A glyph outline could not be transformed.");
        SvgTextGlyphGeometry glyph = new SvgTextGlyphGeometry();
        glyph.Index = index;
        glyph.CharacterIndex = source.Glyph.CharacterIndex;
        glyph.Utf16Start = source.Glyph.Glyph.Utf16Start;
        glyph.Utf16Length = source.Glyph.Glyph.Utf16Length;
        glyph.Text = input.Text.Substring(glyph.Utf16Start, glyph.Utf16Length);
        glyph.Path = transformed.PathData;
        glyph.Visible = source.Visible || options.IncludeInvisibleGlyphs;
        glyph.LogicalBounds = new SvgTextGeometry().LogicalBounds(source, input);
        if (glyph.LogicalBounds == null) return new SvgTextGeometry().GlyphFail(result, SvgTextGeometryError.InvalidOutline, "A logical glyph bound was not finite.");
        SvgDrawingBoundsResult ink = (glyph.Path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GetFillBounds();
        if (!ink.Success) return new SvgTextGeometry().GlyphFail(result, new SvgTextGeometry().MapWithSvgDrawingBoundsError(ink.Error), "Glyph ink bounds could not be computed.");
        glyph.InkBounds = ink.Bounds;
        glyph.StrokeBounds = new SvgTextGeometry().StrokeBounds(glyph.Path, options, result);
        if (glyph.StrokeBounds == null) return null;
        return glyph;
    }
SvgPathData? Outline(SvgTextFontResource resource, int glyphIndex, SvgTextGeometryOptions options, System.Collections.Generic.List<SvgTextOutlineCacheEntry> cache, SvgTextGeometryResult result)
    {
        int index = 0;
        while (index < cache.Count)
        {
            if (cache[index].Resource == resource && cache[index].GlyphIndex == glyphIndex) return cache[index].Path;
            index = index + 1;
        }
        SvgPathData? path = null;
        if (resource.Font.OutlineKind == SvgOpenTypeOutlineKind.TrueType)
        {
            SvgTrueTypeGlyphResult decoded = resource.Font.DecodeTrueTypeGlyphWithOptions(glyphIndex, options.TrueTypeOptions);
            if (!decoded.Success)
            {
                SvgTextGeometryError error = decoded.Error == SvgTrueTypeGlyphError.PointLimitExceeded || decoded.Error == SvgTrueTypeGlyphError.ComponentLimitExceeded || decoded.Error == SvgTrueTypeGlyphError.RecursionLimitExceeded ? SvgTextGeometryError.LimitExceeded : decoded.Error == SvgTrueTypeGlyphError.UnsupportedOutline ? SvgTextGeometryError.UnsupportedOutline : SvgTextGeometryError.InvalidOutline;
                new SvgTextGeometry().Fail(result, error, "A TrueType glyph outline could not be decoded.");
                return null;
            }
            path = (decoded.Outline ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Path;
        }
        else if (resource.Font.OutlineKind == SvgOpenTypeOutlineKind.Cff || resource.Font.OutlineKind == SvgOpenTypeOutlineKind.Cff2)
        {
            SvgCffGlyphResult decoded = resource.Font.DecodeCffGlyphWithOptions(glyphIndex, options.CffOptions);
            if (!decoded.Success)
            {
                SvgTextGeometryError error = decoded.Error == SvgCffError.OperandLimitExceeded || decoded.Error == SvgCffError.SubroutineLimitExceeded || decoded.Error == SvgCffError.PointLimitExceeded ? SvgTextGeometryError.LimitExceeded : decoded.RequiresFallback || decoded.Error == SvgCffError.UnsupportedOperator || decoded.Error == SvgCffError.UnsupportedStructure ? SvgTextGeometryError.UnsupportedOutline : SvgTextGeometryError.InvalidOutline;
                new SvgTextGeometry().Fail(result, error, "A CFF glyph outline could not be decoded.");
                return null;
            }
            path = (decoded.Outline ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Path;
        }
        else
        {
            new SvgTextGeometry().Fail(result, SvgTextGeometryError.UnsupportedOutline, "The font outline format is unsupported.");
            return null;
        }
        if (path == null)
        {
            new SvgTextGeometry().Fail(result, SvgTextGeometryError.InvalidOutline, "The decoded glyph has no path.");
            return null;
        }
        cache.Add(new SvgTextOutlineCacheEntry(resource, glyphIndex, path));
        return path;
    }
SvgMatrix? GlyphMatrix(SvgTextGeometryInputGlyph source, SvgWritingMode writingMode, SvgTextFontResource resource)
    {
        double scale = resource.EmSize / (resource.Font.Head ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).UnitsPerEm;
        bool horizontal = new SvgTextGeometry().Horizontal(writingMode);
        double scaleX = scale * (horizontal ? (source.Glyph ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).InlineScale * source.MethodScale : 1);
        double scaleY = scale * (horizontal ? 1 : (source.Glyph ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).InlineScale * source.MethodScale);
        double radians = source.Rotation * System.Math.PI / 180;
        double cosine = System.Math.Cos(radians);
        double sine = System.Math.Sin(radians);
        double a = cosine * scaleX;
        double b = sine * scaleX;
        double c = sine * scaleY;
        double d = -cosine * scaleY;
        if (!new SvgTextGeometry().Finite(a) || !new SvgTextGeometry().Finite(b) || !new SvgTextGeometry().Finite(c) || !new SvgTextGeometry().Finite(d)) return null;
        return new SvgMatrix(a, b, c, d, (source.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, source.Point.Y);
    }
SvgRect? LogicalBounds(SvgTextGeometryInputGlyph source, SvgTextGeometryInput input)
    {
        bool horizontal = new SvgTextGeometry().Horizontal(input.WritingMode);
        double direction = input.Direction == SvgTextDirection.RightToLeft ? -1 : 1;
        double length = ((source.Glyph ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).InlineAdvance + source.Glyph.SpacingAfter) * source.MethodScale * direction;
        double x = horizontal ? System.Math.Min(0, length) : -input.Ascent;
        double y = horizontal ? -input.Ascent : System.Math.Min(0, length);
        double width = horizontal ? System.Math.Abs(length) : input.Ascent + input.Descent;
        double height = horizontal ? input.Ascent + input.Descent : System.Math.Abs(length);
        SvgRect local = new SvgRect(x, y, width, height);
        SvgMatrix matrix = new SvgTextGeometry().RotationMatrix(source.Point, source.Rotation);
        SvgRectTransformResult transformed = matrix.TransformRect(local);
        return transformed.Success ? transformed.Rectangle : null;
    }
bool Decorations(SvgTextGeometryInputGlyph source, SvgTextGeometryInput input, SvgTextGeometryOptions options, int glyphIndex, SvgTextGeometryResult result, SvgGeneratedPathBuilder combined, int commands)
    {
        SvgTextDecorationOptions decoration = options.Decorations;
        if (!decoration.Underline && !decoration.Overline && !decoration.LineThrough) return true;
        double thickness = input.FontSize * decoration.ThicknessFactor;
        if (thickness == 0) return true;
        int count = result.Decorations.Count;
        if (decoration.Underline && !new SvgTextGeometry().Decoration(source, input, options, glyphIndex, SvgTextDecorationKind.Underline, input.FontSize * decoration.UnderlineOffsetFactor, thickness, result, combined, commands)) return false;
        if (result.Decorations.Count > count) commands = commands + (result.Decorations[result.Decorations.Count - 1].Path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count;
        count = result.Decorations.Count;
        if (decoration.Overline && !new SvgTextGeometry().Decoration(source, input, options, glyphIndex, SvgTextDecorationKind.Overline, input.FontSize * decoration.OverlineOffsetFactor, thickness, result, combined, commands)) return false;
        if (result.Decorations.Count > count) commands = commands + (result.Decorations[result.Decorations.Count - 1].Path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count;
        if (decoration.LineThrough && !new SvgTextGeometry().Decoration(source, input, options, glyphIndex, SvgTextDecorationKind.LineThrough, input.FontSize * decoration.LineThroughOffsetFactor, thickness, result, combined, commands)) return false;
        return true;
    }
bool Decoration(SvgTextGeometryInputGlyph source, SvgTextGeometryInput input, SvgTextGeometryOptions options, int glyphIndex, SvgTextDecorationKind kind, double offset, double thickness, SvgTextGeometryResult result, SvgGeneratedPathBuilder combined, int commands)
    {
        bool horizontal = new SvgTextGeometry().Horizontal(input.WritingMode);
        double direction = input.Direction == SvgTextDirection.RightToLeft ? -1 : 1;
        double length = ((source.Glyph ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).InlineAdvance + source.Glyph.SpacingAfter) * source.MethodScale * direction;
        double x = horizontal ? System.Math.Min(0, length) : offset;
        double y = horizontal ? offset : System.Math.Min(0, length);
        double width = horizontal ? System.Math.Abs(length) : thickness;
        double height = horizontal ? thickness : System.Math.Abs(length);
        if (width == 0 || height == 0) return true;
        SvgGeneratedPathBuilder builder = new SvgGeneratedPathBuilder();
        builder.MoveTo(x, y);
        builder.LineTo(x + width, y);
        builder.LineTo(x + width, y + height);
        builder.LineTo(x, y + height);
        builder.Close();
        SvgPathTransformOptions transformOptions = new SvgPathTransformOptions();
        transformOptions.MaximumCommands = options.MaximumPathCommands;
        SvgPathTransformResult transformed = builder.Build().TransformWithOptions(new SvgTextGeometry().RotationMatrix(source.Point, source.Rotation), transformOptions);
        if (!transformed.Success) return new SvgTextGeometry().FailBoolean(result, new SvgTextGeometry().MapWithSvgPathTransformError(transformed.Error), "A text decoration could not be transformed.");
        if (!new SvgTextGeometry().Append(combined, transformed.PathData, options.MaximumPathCommands, commands)) return new SvgTextGeometry().FailBoolean(result, SvgTextGeometryError.LimitExceeded, "Text geometry exceeds MaximumPathCommands.");
        SvgDrawingBoundsResult bounds = (transformed.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GetFillBounds();
        if (!bounds.Success) return new SvgTextGeometry().FailBoolean(result, new SvgTextGeometry().MapWithSvgDrawingBoundsError(bounds.Error), "Text decoration bounds could not be computed.");
        SvgTextDecorationGeometry geometry = new SvgTextDecorationGeometry();
        geometry.Kind = kind;
        geometry.GlyphIndex = glyphIndex;
        geometry.Path = transformed.PathData;
        geometry.Bounds = bounds.Bounds;
        geometry.StrokeBounds = new SvgTextGeometry().StrokeBounds(transformed.PathData, options, result);
        if (geometry.StrokeBounds == null) return false;
        result.Decorations.Add(geometry);
        result.InkBounds = (new SvgTextGeometry().Union(result.InkBounds, geometry.Bounds, result) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        result.StrokeBounds = (new SvgTextGeometry().Union(result.StrokeBounds, geometry.StrokeBounds, result) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        return result.Error == SvgTextGeometryError.None;
    }
SvgRect? StrokeBounds(SvgPathData path, SvgTextGeometryOptions options, SvgTextGeometryResult result)
    {
        if (options.StrokeWidth == 0) return new SvgTextGeometry().EmptyBounds();
        SvgStrokeGeometryOptions stroke = new SvgStrokeGeometryOptions();
        stroke.Style.Width = options.StrokeWidth;
        SvgStrokeBoundsResult bounds = path.GetStrokeBounds(stroke);
        if (!bounds.Success)
        {
            new SvgTextGeometry().Fail(result, new SvgTextGeometry().MapWithSvgStrokeError(bounds.Error), "Text stroke bounds could not be computed.");
            return null;
        }
        return bounds.Bounds;
    }
bool Clusters(string text, SvgTextGeometryResult result)
    {
        int glyphIndex = 0;
        while (glyphIndex < result.Glyphs.Count)
        {
            SvgTextGlyphGeometry glyph = result.Glyphs[glyphIndex];
            int clusterIndex = new SvgTextGeometry().FindCluster(result.Clusters, glyph.Utf16Start, glyph.Utf16Length);
            SvgTextClusterGeometry cluster;
            if (clusterIndex < 0)
            {
                cluster = new SvgTextClusterGeometry();
                cluster.Index = result.Clusters.Count;
                cluster.Utf16Start = glyph.Utf16Start;
                cluster.Utf16Length = glyph.Utf16Length;
                cluster.Text = text.Substring(glyph.Utf16Start, glyph.Utf16Length);
                cluster.LogicalBounds = new SvgTextGeometry().EmptyBounds();
                cluster.InkBounds = new SvgTextGeometry().EmptyBounds();
                result.Clusters.Add(cluster);
            }
            else cluster = result.Clusters[clusterIndex];
            glyph.ClusterIndex = cluster.Index;
            cluster.GlyphIndices.Add(glyphIndex);
            cluster.LogicalBounds = new SvgTextGeometry().Union(cluster.LogicalBounds, glyph.LogicalBounds, result);
            if (glyph.Visible) cluster.InkBounds = new SvgTextGeometry().Union(cluster.InkBounds, glyph.InkBounds, result);
            if (result.Error != SvgTextGeometryError.None) return false;
            glyphIndex = glyphIndex + 1;
        }
        System.Text.StringBuilder extracted = new System.Text.StringBuilder();
        int clusterPosition = 0;
        while (clusterPosition < result.Clusters.Count)
        {
            extracted.Append(result.Clusters[clusterPosition].Text);
            new SvgTextGeometry().InsertReadingOrder(result.AccessibleReadingOrder, result.Clusters, clusterPosition);
            clusterPosition = clusterPosition + 1;
        }
        result.ExtractedText = extracted.ToString();
        System.Text.StringBuilder accessible = new System.Text.StringBuilder();
        int order = 0;
        while (order < result.AccessibleReadingOrder.Count)
        {
            int index = result.AccessibleReadingOrder[order];
            result.Clusters[index].ReadingOrder = order;
            accessible.Append(result.Clusters[index].Text);
            order = order + 1;
        }
        result.AccessibleText = accessible.ToString();
        return true;
    }
void InsertReadingOrder(System.Collections.Generic.List<int> order, System.Collections.Generic.List<SvgTextClusterGeometry> clusters, int clusterIndex)
    {
        int position = order.Count;
        while (position > 0)
        {
            SvgTextClusterGeometry previous = clusters[order[position - 1]];
            SvgTextClusterGeometry current = clusters[clusterIndex];
            if (previous.Utf16Start < current.Utf16Start || previous.Utf16Start == current.Utf16Start && previous.Utf16Length <= current.Utf16Length) break;
            position = position - 1;
        }
        order.Insert(position, clusterIndex);
    }
int FindCluster(System.Collections.Generic.List<SvgTextClusterGeometry> clusters, int start, int length)
    {
        int index = 0;
        while (index < clusters.Count)
        {
            if (clusters[index].Utf16Start == start && clusters[index].Utf16Length == length) return index;
            index = index + 1;
        }
        return -1;
    }
SvgTextFontResource? Font(SvgTextGeometryOptions options, int identifier)
    {
        int index = 0;
        while (index < options.Fonts.Count)
        {
            if (options.Fonts[index].Identifier == identifier) return options.Fonts[index];
            index = index + 1;
        }
        return null;
    }
SvgMatrix RotationMatrix(SvgPoint? point, double rotation)
    {
        double radians = rotation * System.Math.PI / 180;
        double cosine = System.Math.Cos(radians);
        double sine = System.Math.Sin(radians);
        return new SvgMatrix(cosine, sine, -sine, cosine, (point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, point.Y);
    }
bool Append(SvgGeneratedPathBuilder builder, SvgPathData? path, int maximum, int preceding)
    {
        if (path == null || (long)preceding + path.Commands.Count > maximum) return false;
        int index = 0;
        while (index < path.Commands.Count)
        {
            SvgPathCommand command = path.Commands[index];
            if (command.Kind == SvgPathCommandKind.MoveTo) builder.MoveTo(command.EndX, command.EndY);
            else if (command.Kind == SvgPathCommandKind.LineTo) builder.LineTo(command.EndX, command.EndY);
            else if (command.Kind == SvgPathCommandKind.QuadraticCurveTo) builder.QuadraticTo(command.Values[0], command.Values[1], command.EndX, command.EndY);
            else if (command.Kind == SvgPathCommandKind.CubicCurveTo) builder.CubicTo(command.Values[0], command.Values[1], command.Values[2], command.Values[3], command.EndX, command.EndY);
            else if (command.Kind == SvgPathCommandKind.ArcTo) builder.ArcToWithParameters(command.Values[0], command.Values[1], command.Values[2], command.Values[3] == 1, command.Values[4] == 1, command.EndX, command.EndY);
            else if (command.Kind == SvgPathCommandKind.ClosePath) builder.Close();
            else return false;
            index = index + 1;
        }
        return true;
    }
SvgRect? Union(SvgRect? first, SvgRect? second, SvgTextGeometryResult result)
    {
        SvgRectOperationResult union = (first ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Union((second ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        if (!union.Success)
        {
            new SvgTextGeometry().Fail(result, SvgTextGeometryError.InvalidOutline, "Text bounds produced a non-finite union.");
            return first;
        }
        return union.Rectangle;
    }
bool Contains(SvgRect? bounds, SvgPoint point)
    {
        if (bounds == null || bounds.IsEmpty) return false;
        return point.X >= bounds.X && point.X <= bounds.X + bounds.Width && point.Y >= bounds.Y && point.Y <= bounds.Y + bounds.Height;
    }
bool ValidWithInputAndOptions(SvgTextGeometryInput? input, SvgTextGeometryOptions? options)
    {
        if (input == null || input.Text == null || input.Glyphs == null || options == null || options.Fonts == null || options.Decorations == null || options.TrueTypeOptions == null || options.CffOptions == null) return false;
        if (!new SvgTextGeometry().Finite(input.FontSize) || !new SvgTextGeometry().Finite(input.Ascent) || !new SvgTextGeometry().Finite(input.Descent) || !new SvgTextGeometry().Finite(input.XHeight) || input.FontSize <= 0 || input.Ascent < 0 || input.Descent < 0 || input.XHeight < 0) return false;
        if (!new SvgTextGeometry().Finite(options.StrokeWidth) || options.StrokeWidth < 0 || options.MaximumGlyphs < 1 || options.MaximumPathCommands < 1) return false;
        SvgTextDecorationOptions decoration = options.Decorations;
        if (!new SvgTextGeometry().Finite(decoration.ThicknessFactor) || !new SvgTextGeometry().Finite(decoration.UnderlineOffsetFactor) || !new SvgTextGeometry().Finite(decoration.OverlineOffsetFactor) || !new SvgTextGeometry().Finite(decoration.LineThroughOffsetFactor) || decoration.ThicknessFactor < 0) return false;
        int mode = (int)input.WritingMode;
        int direction = (int)input.Direction;
        if (mode < 0 || mode > 4 || direction < 0 || direction > 2) return false;
        int index = 0;
        while (index < options.Fonts.Count)
        {
            SvgTextFontResource resource = options.Fonts[index];
            if (resource == null || resource.Font == null || !resource.Font.Success || resource.Font.Head == null || resource.Font.Head.UnitsPerEm < 1 || !new SvgTextGeometry().Finite(resource.EmSize) || resource.EmSize <= 0) return false;
            int preceding = 0;
            while (preceding < index)
            {
                if (options.Fonts[preceding].Identifier == resource.Identifier) return false;
                preceding = preceding + 1;
            }
            index = index + 1;
        }
        return true;
    }
bool ValidWithSourceAndText(SvgTextGeometryInputGlyph? source, string text)
    {
        if (source == null || source.Glyph == null || source.Glyph.Glyph == null || source.Point == null) return false;
        SvgShapedGlyph glyph = source.Glyph.Glyph;
        if (source.Glyph.CharacterIndex < 0 || glyph.GlyphIndex < 0 || glyph.Utf16Start < 0 || glyph.Utf16Length < 1 || (long)glyph.Utf16Start + glyph.Utf16Length > text.Length) return false;
        return new SvgTextGeometry().Finite(source.Point.X) && new SvgTextGeometry().Finite(source.Point.Y) && new SvgTextGeometry().Finite(source.Rotation) && new SvgTextGeometry().Finite(source.MethodScale) && source.MethodScale >= 0 && new SvgTextGeometry().Finite(source.Glyph.InlineAdvance) && source.Glyph.InlineAdvance >= 0 && new SvgTextGeometry().Finite(source.Glyph.SpacingAfter) && new SvgTextGeometry().Finite(source.Glyph.InlineScale) && source.Glyph.InlineScale >= 0;
    }
bool ValidHitOptions(SvgTextHitTestOptions options)
    {
        int mode = (int)options.Mode;
        return mode >= 0 && mode <= 2 && new SvgTextGeometry().Finite(options.Tolerance) && options.Tolerance > 0 && options.MaximumSubdivisionDepth >= 0 && options.MaximumSegments > 0;
    }
bool Horizontal(SvgWritingMode mode)
    {
        return mode == SvgWritingMode.HorizontalTopToBottom;
    }
bool Finite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
SvgTextGeometryError MapWithSvgDrawingBoundsError(SvgDrawingBoundsError error)
    {
        if (error == SvgDrawingBoundsError.LimitExceeded) return SvgTextGeometryError.LimitExceeded;
        if (error == SvgDrawingBoundsError.InvalidArgument) return SvgTextGeometryError.InvalidArgument;
        return SvgTextGeometryError.InvalidOutline;
    }
SvgTextGeometryError MapWithSvgStrokeError(SvgStrokeError error)
    {
        if (error == SvgStrokeError.SubdivisionLimitExceeded || error == SvgStrokeError.DashLimitExceeded || error == SvgStrokeError.OutputLimitExceeded) return SvgTextGeometryError.LimitExceeded;
        if (error == SvgStrokeError.InvalidArgument) return SvgTextGeometryError.InvalidArgument;
        return SvgTextGeometryError.InvalidOutline;
    }
SvgTextGeometryError MapWithSvgFillContainmentError(SvgFillContainmentError error)
    {
        if (error == SvgFillContainmentError.SegmentLimitExceeded || error == SvgFillContainmentError.SubdivisionLimitExceeded) return SvgTextGeometryError.LimitExceeded;
        if (error == SvgFillContainmentError.InvalidArgument) return SvgTextGeometryError.InvalidArgument;
        return SvgTextGeometryError.InvalidOutline;
    }
SvgTextGeometryError MapWithSvgPathTransformError(SvgPathTransformError error)
    {
        if (error == SvgPathTransformError.CommandLimitExceeded) return SvgTextGeometryError.LimitExceeded;
        if (error == SvgPathTransformError.InvalidArgument) return SvgTextGeometryError.InvalidArgument;
        return SvgTextGeometryError.InvalidOutline;
    }
SvgTextGlyphGeometry? GlyphFail(SvgTextGeometryResult result, SvgTextGeometryError error, string message)
    {
        new SvgTextGeometry().Fail(result, error, message);
        return null;
    }
bool FailBoolean(SvgTextGeometryResult result, SvgTextGeometryError error, string message)
    {
        new SvgTextGeometry().Fail(result, error, message);
        return false;
    }
SvgTextGeometryResult Failure(SvgTextGeometryError error, string message)
    {
        return new SvgTextGeometry().Fail(new SvgTextGeometryResult(), error, message);
    }
SvgTextGeometryResult Fail(SvgTextGeometryResult result, SvgTextGeometryError error, string message)
    {
        if (result.Error == SvgTextGeometryError.None) result.Error = error;
        result.Message = message;
        return result;
    }
SvgTextHitTestResult HitFailure(SvgTextHitTestResult result, SvgTextGeometryError error)
    {
        result.Error = error;
        return result;
    }
}
