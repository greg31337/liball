namespace svg;

/// <summary>Provides bounded geometry and resolved-document hit testing.</summary>
public class SvgHitTesting : ISvgHitTesting
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgHitTesting()
    {
    }

    /// <summary>Tests one path in local or transformed query coordinates.</summary>
    /// <param name="path">The valid path.</param>
    /// <param name="point">The finite query point.</param>
    /// <param name="options">The geometry, paint, visibility, transform, and effect options.</param>
    /// <returns>The component hit decision or stable failure.</returns>
    public SvgGeometryHitTestResult Path(SvgPathData? path, SvgPoint point, SvgGeometryHitTestOptions options)
    {
        if (path == null || !new SvgHitTesting().ValidWithPointAndOptions(point, options)) return new SvgHitTesting().GeometryFailure(SvgHitTestError.InvalidArgument);
        SvgPathNormalizeResult validity = path.Normalize();
        if (!validity.Success) return new SvgHitTesting().GeometryFailure(new SvgHitTesting().MapWithSvgPathNormalizeError(validity.Error));
        SvgPointerEventsMode mode = new SvgHitTesting().Normalize(options.PointerEvents);
        if (mode == SvgPointerEventsMode.None) return new SvgHitTesting().GeometrySuccess(false, false, false, false, false, false);
        SvgRegionHitResult clip = new SvgHitTesting().Regions(options.Clips, point);
        if (!clip.Success) return new SvgHitTesting().GeometryFailure(clip.Error);
        if (!clip.Contains) return new SvgHitTesting().GeometrySuccess(false, false, false, false, true, false);
        SvgRegionHitResult mask = new SvgHitTesting().Regions(options.Masks, point);
        if (!mask.Success) return new SvgHitTesting().GeometryFailure(mask.Error);
        if (!mask.Contains) return new SvgHitTesting().GeometrySuccess(false, false, false, false, false, true);
        if (new SvgHitTesting().RequiresVisibility(mode) && !options.Visible) return new SvgHitTesting().GeometrySuccess(false, false, false, false, false, false);
        bool fillEligible = new SvgHitTesting().FillEligible(mode, options.FillPainted);
        bool strokeEligible = new SvgHitTesting().StrokeEligible(mode, options.StrokePainted);
        bool fillHit = false;
        bool strokeHit = false;
        bool boundsHit = false;
        if (mode == SvgPointerEventsMode.BoundingBox)
        {
            SvgRectOperationResult bounds = new SvgHitTesting().QueryBounds(path, options);
            if (!bounds.Success) return new SvgHitTesting().GeometryFailure(new SvgHitTesting().MapWithSvgGeometryError(bounds.Error));
            boundsHit = (bounds.Rectangle ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Contains(point);
        }
        if (fillEligible)
        {
            SvgFillContainmentOptions fillOptions = new SvgHitTesting().CopyFill(options.FillOptions);
            fillOptions.Transform = options.Transform;
            SvgFillContainmentResult fill = path.ClassifyFillWithOptions(point, options.FillRule, fillOptions);
            if (!fill.Success) return new SvgHitTesting().GeometryFailure(new SvgHitTesting().MapWithSvgFillContainmentError(fill.Error));
            fillHit = fill.Contains;
        }
        if (strokeEligible && options.Stroke != null)
        {
            SvgStrokeGeometryOptions strokeOptions = new SvgHitTesting().CopyStroke(options.Stroke);
            strokeOptions.Transform = options.Transform;
            SvgStrokeContainmentResult stroke = path.ContainsStroke(point, strokeOptions);
            if (!stroke.Success) return new SvgHitTesting().GeometryFailure(new SvgHitTesting().MapWithSvgStrokeError(stroke.Error));
            strokeHit = stroke.Contains;
        }
        return new SvgHitTesting().GeometrySuccess(fillHit || strokeHit || boundsHit, fillHit, strokeHit, boundsHit, false, false);
    }

    /// <summary>Tests one resolved basic shape in local or transformed query coordinates.</summary>
    /// <param name="shape">The resolved shape.</param>
    /// <param name="point">The finite query point.</param>
    /// <param name="options">The geometry, paint, visibility, transform, and effect options.</param>
    /// <returns>The component hit decision or stable failure.</returns>
    public SvgGeometryHitTestResult Shape(SvgShapeResult? shape, SvgPoint point, SvgGeometryHitTestOptions options)
    {
        if (shape == null || !new SvgHitTesting().ValidWithPointAndOptions(point, options)) return new SvgHitTesting().GeometryFailure(SvgHitTestError.InvalidArgument);
        if (!shape.HasGeometry) return new SvgHitTesting().GeometrySuccess(false, false, false, false, false, false);
        if (shape.Kind == SvgShapeKind.Path) return shape.PathData == null ? new SvgHitTesting().GeometryFailure(SvgHitTestError.InvalidArgument) : new SvgHitTesting().Path(shape.PathData, point, options);
        SvgShapePathResult converted = shape.ToPath();
        if (!converted.Success) return new SvgHitTesting().GeometryFailure(new SvgHitTesting().MapWithSvgShapePathError(converted.Error));
        return new SvgHitTesting().Path(converted.PathData, point, options);
    }

    /// <summary>Tests resolved shapes in reverse painting order.</summary>
    /// <param name="document">The resolved document.</param>
    /// <param name="point">The finite world-space query point.</param>
    /// <param name="options">The traversal, result, viewport, and per-node options.</param>
    /// <returns>Topmost-first source and instance matches.</returns>
    public SvgDocumentHitTestResult Document(SvgResolvedDocument? document, SvgPoint point, SvgDocumentHitTestOptions? options)
    {
        if (document == null || document.Nodes == null || !new SvgHitTesting().ValidWithPoint(point) || options == null || options.Details == null || options.MaximumNodes < 1 || options.MaximumResults < 1) return new SvgHitTesting().DocumentFailure(SvgHitTestError.InvalidArgument);
        System.Collections.Generic.List<SvgHitTestMatch> matches = new System.Collections.Generic.List<SvgHitTestMatch>();
        int visited = 0;
        int candidates = 0;
        int index = document.Nodes.Count - 1;
        while (index >= 0)
        {
            if (visited >= options.MaximumNodes) return new SvgHitTesting().DocumentFailure(SvgHitTestError.LimitExceeded);
            SvgResolvedNode node = document.Nodes[index];
            visited = visited + 1;
            index = index - 1;
            if (!new SvgHitTesting().Candidate(node)) continue;
            candidates = candidates + 1;
            SvgResolvedNodeHitTestDetails? details = new SvgHitTesting().FindDetails(options.Details, node);
            if (details != null && !new SvgHitTesting().ValidWithDetails(details)) return new SvgHitTesting().DocumentFailure(SvgHitTestError.InvalidArgument);
            SvgPointerEventsMode mode = details != null && details.HasPointerEvents ? details.PointerEvents : new SvgHitTesting().PointerEvents(node);
            SvgGeometryHitTestOptions? geometryOptions = new SvgHitTesting().NodeOptions(document, node, details, mode);
            if (geometryOptions == null) return new SvgHitTesting().DocumentFailure(SvgHitTestError.NonFiniteResult);
            if (options.IncludeViewportClips)
            {
                SvgRectOperationResult viewport = new SvgDrawingBounds().ViewportClip(node);
                if (!viewport.Success) return new SvgHitTesting().DocumentFailure(new SvgHitTesting().MapWithSvgGeometryError(viewport.Error));
                if (viewport.Rectangle != null) geometryOptions.Clips.Add(new SvgHitTesting().RectRegion(viewport.Rectangle));
            }
            SvgGeometryHitTestResult geometry = new SvgHitTesting().Shape(node.Geometry, point, geometryOptions);
            if (!geometry.Success) return new SvgHitTesting().DocumentFailure(geometry.Error);
            if (!geometry.Hit) continue;
            SvgHitTestMatch match = new SvgHitTestMatch(node, mode, geometry);
            matches.Add(match);
            if (!options.ReturnAll) break;
            if (matches.Count >= options.MaximumResults)
            {
                return new SvgHitTesting().DocumentSuccess(matches, visited, candidates, index >= 0);
            }
        }
        return new SvgHitTesting().DocumentSuccess(matches, visited, candidates, false);
    }

    internal SvgGeometryHitTestResult GeometryFailure(SvgHitTestError error)
    {
        return new SvgGeometryHitTestResult(false, error, false, false, false, false, false, false);
    }

    internal SvgDocumentHitTestResult DocumentFailure(SvgHitTestError error)
    {
        return new SvgDocumentHitTestResult(false, error, null, null, 0, 0, false);
    }
SvgGeometryHitTestResult GeometrySuccess(bool hit, bool fill, bool stroke, bool bounds, bool clipped, bool masked)
    {
        return new SvgGeometryHitTestResult(true, SvgHitTestError.None, hit, fill, stroke, bounds, clipped, masked);
    }
SvgDocumentHitTestResult DocumentSuccess(System.Collections.Generic.List<SvgHitTestMatch> matches, int visited, int candidates, bool truncated)
    {
        SvgHitTestMatch? topmost = matches.Count == 0 ? null : matches[0];
        return new SvgDocumentHitTestResult(true, SvgHitTestError.None, topmost, matches, visited, candidates, truncated);
    }
SvgGeometryHitTestOptions? NodeOptions(SvgResolvedDocument document, SvgResolvedNode node, SvgResolvedNodeHitTestDetails? details, SvgPointerEventsMode mode)
    {
        SvgMatrixOperationResult world = new SvgDrawingBounds().World(node);
        if (!world.Success) return null;
        SvgGeometryHitTestOptions options = new SvgGeometryHitTestOptions();
        options.PointerEvents = mode;
        options.FillRule = new SvgHitTesting().FillRule(node);
        options.Visible = (node.Geometry ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Visible;
        options.FillPainted = new SvgHitTesting().Painted(node.Fill);
        options.StrokePainted = new SvgHitTesting().Painted(node.Stroke);
        options.Stroke = details != null && details.Stroke != null ? details.Stroke : new SvgDrawingBounds().ComputedStroke(node, document, false);
        options.Transform = world.Matrix;
        options.Bounds = details == null ? null : details.Bounds;
        if (details != null)
        {
            new SvgHitTesting().CopyRegions(details.Clips, options.Clips);
            new SvgHitTesting().CopyRegions(details.Masks, options.Masks);
        }
        return options;
    }
SvgRectOperationResult QueryBounds(SvgPathData path, SvgGeometryHitTestOptions options)
    {
        if (options.Bounds != null)
        {
            if (!new SvgHitTesting().ValidWithBounds(options.Bounds)) return new SvgRectOperationResult(false, SvgGeometryError.InvalidArgument, null);
            if (options.Transform == null || options.Bounds.IsEmpty) return new SvgRectOperationResult(true, SvgGeometryError.None, options.Bounds);
            SvgRectTransformResult transformed = options.Transform.TransformRect(options.Bounds);
            return new SvgRectOperationResult(transformed.Success, transformed.Error, transformed.Rectangle);
        }
        SvgDrawingBoundsOptions boundsOptions = new SvgDrawingBoundsOptions();
        boundsOptions.Transform = options.Transform;
        SvgDrawingBoundsResult bounds = path.GetDrawingBounds(boundsOptions);
        return bounds.Success ? new SvgRectOperationResult(true, SvgGeometryError.None, bounds.FillBounds) : new SvgRectOperationResult(false, bounds.Error == SvgDrawingBoundsError.InvalidArgument || bounds.Error == SvgDrawingBoundsError.InvalidPath ? SvgGeometryError.InvalidArgument : SvgGeometryError.NonFiniteResult, null);
    }
SvgRegionHitResult Regions(System.Collections.Generic.List<SvgHitTestRegion> regions, SvgPoint point)
    {
        int index = 0;
        while (index < regions.Count)
        {
            SvgRegionHitResult region = new SvgHitTesting().Region(regions[index], point);
            if (!region.Success || !region.Contains) return region;
            index = index + 1;
        }
        return new SvgRegionHitResult(true, SvgHitTestError.None, true);
    }
SvgRegionHitResult Region(SvgHitTestRegion? region, SvgPoint point)
    {
        if (region == null || region.FillOptions == null || region.PathData == null && region.Bounds == null || !new SvgHitTesting().Rule(region.FillRule)) return new SvgRegionHitResult(false, SvgHitTestError.InvalidArgument, false);
        if (region.Bounds != null)
        {
            if (!new SvgHitTesting().ValidWithBounds(region.Bounds)) return new SvgRegionHitResult(false, SvgHitTestError.InvalidArgument, false);
            if (!region.Bounds.Contains(point)) return new SvgRegionHitResult(true, SvgHitTestError.None, false);
        }
        if (region.PathData != null)
        {
            SvgFillContainmentResult contained = region.PathData.ClassifyFillWithOptions(point, region.FillRule, region.FillOptions);
            if (!contained.Success) return new SvgRegionHitResult(false, new SvgHitTesting().MapWithSvgFillContainmentError(contained.Error), false);
            if (!contained.Contains) return new SvgRegionHitResult(true, SvgHitTestError.None, false);
        }
        return new SvgRegionHitResult(true, SvgHitTestError.None, true);
    }
SvgFillContainmentOptions CopyFill(SvgFillContainmentOptions source)
    {
        SvgFillContainmentOptions result = new SvgFillContainmentOptions();
        result.Tolerance = source.Tolerance;
        result.Transform = source.Transform;
        result.MaximumSubdivisionDepth = source.MaximumSubdivisionDepth;
        result.MaximumSegments = source.MaximumSegments;
        return result;
    }
SvgStrokeGeometryOptions CopyStroke(SvgStrokeGeometryOptions source)
    {
        SvgStrokeGeometryOptions result = new SvgStrokeGeometryOptions();
        result.Style = source.Style;
        result.Transform = source.Transform;
        result.VectorEffect = source.VectorEffect;
        return result;
    }
void CopyRegions(System.Collections.Generic.List<SvgHitTestRegion> source, System.Collections.Generic.List<SvgHitTestRegion> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }
SvgHitTestRegion RectRegion(SvgRect bounds)
    {
        SvgHitTestRegion result = new SvgHitTestRegion();
        result.Bounds = bounds;
        return result;
    }
SvgResolvedNodeHitTestDetails? FindDetails(System.Collections.Generic.List<SvgResolvedNodeHitTestDetails> details, SvgResolvedNode node)
    {
        int index = 0;
        while (index < details.Count)
        {
            if (details[index] != null && object.ReferenceEquals(details[index].Node, node)) return details[index];
            index = index + 1;
        }
        return null;
    }
bool Candidate(SvgResolvedNode node)
    {
        return node != null && node.Active && node.Kind == SvgResolvedNodeKind.Shape && node.Geometry != null && node.Geometry.Displayed && node.Geometry.HasGeometry;
    }
bool Painted(SvgPaint? paint)
    {
        return paint != null && paint.Kind != SvgPaintKind.None;
    }
SvgFillRule FillRule(SvgResolvedNode node)
    {
        return node.ComputedStyle != null && node.ComputedStyle.GetKeyword(SvgCssPropertyNames.FillRule) == "evenodd" ? SvgFillRule.EvenOdd : SvgFillRule.Nonzero;
    }
SvgPointerEventsMode PointerEvents(SvgResolvedNode node)
    {
        string value = node.ComputedStyle == null ? "auto" : node.ComputedStyle.GetKeyword(SvgCssPropertyNames.PointerEvents);
        if (value == "none") return SvgPointerEventsMode.None;
        if (value == "bounding-box") return SvgPointerEventsMode.BoundingBox;
        if (value == "visiblepainted") return SvgPointerEventsMode.VisiblePainted;
        if (value == "visiblefill") return SvgPointerEventsMode.VisibleFill;
        if (value == "visiblestroke") return SvgPointerEventsMode.VisibleStroke;
        if (value == "visible") return SvgPointerEventsMode.Visible;
        if (value == "painted") return SvgPointerEventsMode.Painted;
        if (value == "fill") return SvgPointerEventsMode.Fill;
        if (value == "stroke") return SvgPointerEventsMode.Stroke;
        if (value == "all") return SvgPointerEventsMode.All;
        return SvgPointerEventsMode.Auto;
    }
SvgPointerEventsMode Normalize(SvgPointerEventsMode mode)
    {
        return mode == SvgPointerEventsMode.Auto ? SvgPointerEventsMode.VisiblePainted : mode;
    }
bool RequiresVisibility(SvgPointerEventsMode mode)
    {
        return mode == SvgPointerEventsMode.VisiblePainted || mode == SvgPointerEventsMode.VisibleFill || mode == SvgPointerEventsMode.VisibleStroke || mode == SvgPointerEventsMode.Visible;
    }
bool FillEligible(SvgPointerEventsMode mode, bool painted)
    {
        if (mode == SvgPointerEventsMode.VisiblePainted || mode == SvgPointerEventsMode.Painted) return painted;
        return mode == SvgPointerEventsMode.VisibleFill || mode == SvgPointerEventsMode.Visible || mode == SvgPointerEventsMode.Fill || mode == SvgPointerEventsMode.All;
    }
bool StrokeEligible(SvgPointerEventsMode mode, bool painted)
    {
        if (mode == SvgPointerEventsMode.VisiblePainted || mode == SvgPointerEventsMode.Painted) return painted;
        return mode == SvgPointerEventsMode.VisibleStroke || mode == SvgPointerEventsMode.Visible || mode == SvgPointerEventsMode.Stroke || mode == SvgPointerEventsMode.All;
    }
bool ValidWithPointAndOptions(SvgPoint point, SvgGeometryHitTestOptions options)
    {
        return new SvgHitTesting().ValidWithPoint(point) && options != null && new SvgHitTesting().Mode(options.PointerEvents) && new SvgHitTesting().Rule(options.FillRule) && new SvgHitTesting().ValidWithOptions(options.FillOptions) && options.Clips != null && options.Masks != null && new SvgHitTesting().ValidWithMatrix(options.Transform) && new SvgHitTesting().ValidWithBounds(options.Bounds);
    }
bool ValidWithDetails(SvgResolvedNodeHitTestDetails details)
    {
        return details.Clips != null && details.Masks != null && new SvgHitTesting().ValidWithBounds(details.Bounds) && (!details.HasPointerEvents || new SvgHitTesting().Mode(details.PointerEvents));
    }
bool ValidWithOptions(SvgFillContainmentOptions options)
    {
        return options != null && SvgFiniteMath.IsFinite(options.Tolerance) && options.Tolerance > 0 && options.MaximumSubdivisionDepth >= 0 && options.MaximumSegments > 0 && new SvgHitTesting().ValidWithMatrix(options.Transform);
    }
bool ValidWithPoint(SvgPoint point)
    {
        return point != null && SvgFiniteMath.IsFinite(point.X) && SvgFiniteMath.IsFinite(point.Y);
    }
bool ValidWithMatrix(SvgMatrix? matrix)
    {
        return matrix == null || SvgFiniteMath.IsFinite(matrix.A) && SvgFiniteMath.IsFinite(matrix.B) && SvgFiniteMath.IsFinite(matrix.C) && SvgFiniteMath.IsFinite(matrix.D) && SvgFiniteMath.IsFinite(matrix.E) && SvgFiniteMath.IsFinite(matrix.F);
    }
bool ValidWithBounds(SvgRect? bounds)
    {
        return bounds == null || SvgFiniteMath.IsFinite(bounds.X) && SvgFiniteMath.IsFinite(bounds.Y) && SvgFiniteMath.IsFinite(bounds.Width) && SvgFiniteMath.IsFinite(bounds.Height) && bounds.Width >= 0 && bounds.Height >= 0;
    }
bool Mode(SvgPointerEventsMode mode)
    {
        return mode >= SvgPointerEventsMode.Auto && mode <= SvgPointerEventsMode.All;
    }
bool Rule(SvgFillRule rule)
    {
        return rule == SvgFillRule.Nonzero || rule == SvgFillRule.EvenOdd;
    }
SvgHitTestError MapWithSvgPathNormalizeError(SvgPathNormalizeError error)
    {
        if (error == SvgPathNormalizeError.InvalidPath) return SvgHitTestError.InvalidPath;
        if (error == SvgPathNormalizeError.NonFiniteResult) return SvgHitTestError.NonFiniteResult;
        if (error == SvgPathNormalizeError.CommandLimitExceeded) return SvgHitTestError.LimitExceeded;
        return SvgHitTestError.InvalidArgument;
    }
SvgHitTestError MapWithSvgFillContainmentError(SvgFillContainmentError error)
    {
        if (error == SvgFillContainmentError.InvalidPath) return SvgHitTestError.InvalidPath;
        if (error == SvgFillContainmentError.NonFiniteResult) return SvgHitTestError.NonFiniteResult;
        if (error == SvgFillContainmentError.SegmentLimitExceeded || error == SvgFillContainmentError.SubdivisionLimitExceeded) return SvgHitTestError.LimitExceeded;
        return SvgHitTestError.InvalidArgument;
    }
SvgHitTestError MapWithSvgStrokeError(SvgStrokeError error)
    {
        if (error == SvgStrokeError.InvalidPath) return SvgHitTestError.InvalidPath;
        if (error == SvgStrokeError.NonFiniteResult) return SvgHitTestError.NonFiniteResult;
        if (error == SvgStrokeError.InvalidArgument) return SvgHitTestError.InvalidArgument;
        return SvgHitTestError.LimitExceeded;
    }
SvgHitTestError MapWithSvgShapePathError(SvgShapePathError error)
    {
        if (error == SvgShapePathError.CommandLimitExceeded) return SvgHitTestError.LimitExceeded;
        if (error == SvgShapePathError.NonFiniteResult) return SvgHitTestError.NonFiniteResult;
        if (error == SvgShapePathError.InvalidArgument) return SvgHitTestError.InvalidArgument;
        return SvgHitTestError.ShapeConversionFailed;
    }
SvgHitTestError MapWithSvgGeometryError(SvgGeometryError error)
    {
        return error == SvgGeometryError.InvalidArgument ? SvgHitTestError.InvalidArgument : SvgHitTestError.NonFiniteResult;
    }
}
