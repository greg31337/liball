namespace svg;

/// <summary>Defines one optional rectangular or path-shaped hit-test constraint.</summary>
public class SvgHitTestRegion
{
    /// <summary>Optional region path geometry.</summary>
    public SvgPathData? PathData;
    /// <summary>Optional region rectangle in query coordinates.</summary>
    public SvgRect? Bounds;
    /// <summary>The path fill rule.</summary>
    public SvgFillRule FillRule;
    /// <summary>The path containment tolerance, transform, and limits.</summary>
    public SvgFillContainmentOptions FillOptions;

    /// <summary>Creates an unset region with deterministic containment defaults.</summary>
    public SvgHitTestRegion()
    {
        PathData = null;
        Bounds = null;
        FillRule = SvgFillRule.Nonzero;
        FillOptions = new SvgFillContainmentOptions();
    }
}

/// <summary>Configures one path or resolved-shape hit test.</summary>
public class SvgGeometryHitTestOptions
{
    /// <summary>The pointer-event geometry selection mode.</summary>
    public SvgPointerEventsMode PointerEvents;
    /// <summary>The fill winding rule.</summary>
    public SvgFillRule FillRule;
    /// <summary>Whether visibility permits visible-prefixed modes.</summary>
    public bool Visible;
    /// <summary>Whether fill paint is present.</summary>
    public bool FillPainted;
    /// <summary>Whether stroke paint is present.</summary>
    public bool StrokePainted;
    /// <summary>Optional stroke geometry and limits.</summary>
    public SvgStrokeGeometryOptions? Stroke;
    /// <summary>Optional local-to-query transform.</summary>
    public SvgMatrix? Transform;
    /// <summary>Optional local object bounds used by bounding-box mode.</summary>
    public SvgRect? Bounds;
    /// <summary>Fill containment tolerance and limits.</summary>
    public SvgFillContainmentOptions FillOptions;
    /// <summary>Ordered intersecting clip constraints.</summary>
    public System.Collections.Generic.List<SvgHitTestRegion> Clips;
    /// <summary>Ordered intersecting mask constraints.</summary>
    public System.Collections.Generic.List<SvgHitTestRegion> Masks;

    /// <summary>Creates visible painted-fill defaults.</summary>
    public SvgGeometryHitTestOptions()
    {
        PointerEvents = SvgPointerEventsMode.Auto;
        FillRule = SvgFillRule.Nonzero;
        Visible = true;
        FillPainted = true;
        StrokePainted = false;
        Stroke = null;
        Transform = null;
        Bounds = null;
        FillOptions = new SvgFillContainmentOptions();
        Clips = new System.Collections.Generic.List<SvgHitTestRegion>();
        Masks = new System.Collections.Generic.List<SvgHitTestRegion>();
    }
}

/// <summary>Contains one geometry hit-test decision and component details.</summary>
public class SvgGeometryHitTestResult
{
    /// <summary>Whether hit testing completed successfully.</summary>
    public bool Success;
    /// <summary>The stable failure.</summary>
    public SvgHitTestError Error;
    /// <summary>Whether the selected pointer-event geometry was hit.</summary>
    public bool Hit;
    /// <summary>Whether fill geometry contained the point.</summary>
    public bool FillHit;
    /// <summary>Whether stroke geometry contained the point.</summary>
    public bool StrokeHit;
    /// <summary>Whether bounding-box geometry contained the point.</summary>
    public bool BoundingBoxHit;
    /// <summary>Whether a clip rejected an otherwise eligible query.</summary>
    public bool ClippedOut;
    /// <summary>Whether a mask rejected an otherwise eligible query.</summary>
    public bool MaskedOut;

    /// <summary>Creates a geometry hit-test result.</summary>
    /// <param name="success">Whether testing succeeded.</param>
    /// <param name="error">The stable failure.</param>
    /// <param name="hit">Whether selected geometry was hit.</param>
    /// <param name="fillHit">Whether fill geometry was hit.</param>
    /// <param name="strokeHit">Whether stroke geometry was hit.</param>
    /// <param name="boundingBoxHit">Whether object bounds were hit.</param>
    /// <param name="clippedOut">Whether clipping rejected the query.</param>
    /// <param name="maskedOut">Whether masking rejected the query.</param>
    public SvgGeometryHitTestResult(bool success, SvgHitTestError error, bool hit, bool fillHit, bool strokeHit, bool boundingBoxHit, bool clippedOut, bool maskedOut)
    {
        Success = success;
        Error = error;
        Hit = hit;
        FillHit = fillHit;
        StrokeHit = strokeHit;
        BoundingBoxHit = boundingBoxHit;
        ClippedOut = clippedOut;
        MaskedOut = maskedOut;
    }
}

/// <summary>Supplies optional resolved-node hit-test geometry not retained by resolution.</summary>
public class SvgResolvedNodeHitTestDetails
{
    /// <summary>The resolved node receiving these details.</summary>
    public SvgResolvedNode Node;
    /// <summary>Optional explicit stroke geometry.</summary>
    public SvgStrokeGeometryOptions? Stroke;
    /// <summary>Optional local object bounds override.</summary>
    public SvgRect? Bounds;
    /// <summary>Additional intersecting clip constraints.</summary>
    public System.Collections.Generic.List<SvgHitTestRegion> Clips;
    /// <summary>Additional intersecting mask constraints.</summary>
    public System.Collections.Generic.List<SvgHitTestRegion> Masks;
    /// <summary>Whether the pointer-event mode override applies.</summary>
    public bool HasPointerEvents;
    /// <summary>The optional pointer-event mode override.</summary>
    public SvgPointerEventsMode PointerEvents;

    /// <summary>Creates empty details for one resolved node.</summary>
    /// <param name="node">The resolved node.</param>
    public SvgResolvedNodeHitTestDetails(SvgResolvedNode node)
    {
        Node = node;
        Stroke = null;
        Bounds = null;
        Clips = new System.Collections.Generic.List<SvgHitTestRegion>();
        Masks = new System.Collections.Generic.List<SvgHitTestRegion>();
        HasPointerEvents = false;
        PointerEvents = SvgPointerEventsMode.Auto;
    }
}

/// <summary>Configures topmost or all resolved-document hit testing.</summary>
public class SvgDocumentHitTestOptions
{
    /// <summary>Whether every hit is returned instead of only the topmost hit.</summary>
    public bool ReturnAll;
    /// <summary>Whether resolved ancestor viewport clips apply.</summary>
    public bool IncludeViewportClips;
    /// <summary>The positive maximum visited resolved-node count.</summary>
    public int MaximumNodes;
    /// <summary>The positive maximum returned hit count.</summary>
    public int MaximumResults;
    /// <summary>Optional per-node stroke, clip, mask, bounds, and mode details.</summary>
    public System.Collections.Generic.List<SvgResolvedNodeHitTestDetails> Details;

    /// <summary>Creates topmost-hit defaults with secure traversal limits.</summary>
    public SvgDocumentHitTestOptions()
    {
        ReturnAll = false;
        IncludeViewportClips = true;
        MaximumNodes = 1000000;
        MaximumResults = 1000000;
        Details = new System.Collections.Generic.List<SvgResolvedNodeHitTestDetails>();
    }
}

/// <summary>Identifies one target or ancestor in a hit-test event route.</summary>
public class SvgHitTestRouteNode
{
    /// <summary>The resolved route node.</summary>
    public SvgResolvedNode Node;
    /// <summary>The corresponding authored source node.</summary>
    public SvgNode? SourceNode;
    /// <summary>The stable authored source path.</summary>
    public string SourcePath;
    /// <summary>The stable use-instance path, or an empty string.</summary>
    public string InstancePath;
    /// <summary>Whether this route entry is inside a generated use instance.</summary>
    public bool IsInstance;

    /// <summary>Creates one source- and instance-identified route entry.</summary>
    /// <param name="node">The resolved target or ancestor.</param>
    public SvgHitTestRouteNode(SvgResolvedNode node)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        Node = node;
        SourceNode = node.SourceNode;
        SourcePath = node.SourcePath;
        InstancePath = node.InstancePath;
        IsInstance = node.InstancePath != null && node.InstancePath.Length != 0;
    }
}

/// <summary>Contains the computed cursor selected for one hit target.</summary>
public class SvgHitTestCursor
{
    /// <summary>The resolved node supplying the effective cursor.</summary>
    public SvgResolvedNode Node;
    /// <summary>The cursor node's authored source identity.</summary>
    public SvgNode? SourceNode;
    /// <summary>The cursor node's authored source path.</summary>
    public string SourcePath;
    /// <summary>The cursor node's use-instance path, or an empty string.</summary>
    public string InstancePath;
    /// <summary>The ordered custom cursor URL references.</summary>
    public System.Collections.Generic.List<SvgIriReference> Urls;
    /// <summary>The normalized terminal cursor keyword.</summary>
    public string Keyword;
    /// <summary>The complete computed cursor text.</summary>
    public string CssText;

    internal SvgHitTestCursor(SvgResolvedNode node, SvgComputedCursor? cursor)
    {
        Node = node;
        SourceNode = node.SourceNode;
        SourcePath = node.SourcePath;
        InstancePath = node.InstancePath;
        Urls = new System.Collections.Generic.List<SvgIriReference>();
        Keyword = "auto";
        CssText = "auto";
        if (cursor != null)
        {
            int index = 0;
            while (index < cursor.Urls.Count)
            {
                Urls.Add(cursor.Urls[index]);
                index = index + 1;
            }
            Keyword = cursor.Keyword;
            CssText = cursor.CssText;
        }
    }
}

/// <summary>Identifies one hit resolved node and its source or instance identity.</summary>
public class SvgHitTestMatch
{
    /// <summary>The hit resolved node.</summary>
    public SvgResolvedNode Node;
    /// <summary>The corresponding authored source node.</summary>
    public SvgNode? SourceNode;
    /// <summary>The stable authored source path.</summary>
    public string SourcePath;
    /// <summary>The stable use-instance path, or an empty string.</summary>
    public string InstancePath;
    /// <summary>Whether this is a generated use instance.</summary>
    public bool IsInstance;
    /// <summary>The effective pointer-event mode.</summary>
    public SvgPointerEventsMode PointerEvents;
    /// <summary>Whether fill geometry produced the match.</summary>
    public bool FillHit;
    /// <summary>Whether stroke geometry produced the match.</summary>
    public bool StrokeHit;
    /// <summary>Whether bounding-box geometry produced the match.</summary>
    public bool BoundingBoxHit;
    /// <summary>The target-first resolved ancestor route through the document root.</summary>
    public System.Collections.Generic.List<SvgHitTestRouteNode> Route;
    /// <summary>The effective computed cursor for this match.</summary>
    public SvgHitTestCursor Cursor;

    /// <summary>Creates one source- or instance-identified hit.</summary>
    /// <param name="node">The resolved hit node.</param>
    /// <param name="pointerEvents">The effective pointer-event mode.</param>
    /// <param name="geometry">The successful geometry decision.</param>
    public SvgHitTestMatch(SvgResolvedNode node, SvgPointerEventsMode pointerEvents, SvgGeometryHitTestResult geometry)
    {
        Node = node;
        SourceNode = node.SourceNode;
        SourcePath = node.SourcePath;
        InstancePath = node.InstancePath;
        IsInstance = node.InstancePath != null && node.InstancePath.Length != 0;
        PointerEvents = pointerEvents;
        FillHit = geometry.FillHit;
        StrokeHit = geometry.StrokeHit;
        BoundingBoxHit = geometry.BoundingBoxHit;
        Route = CreateRoute(node);
        Cursor = ResolveCursor(node);
    }

    internal static System.Collections.Generic.List<SvgHitTestRouteNode> CreateRoute(SvgResolvedNode node)
    {
        System.Collections.Generic.List<SvgHitTestRouteNode> result = new System.Collections.Generic.List<SvgHitTestRouteNode>();
        System.Collections.Generic.HashSet<SvgResolvedNode> visited = new System.Collections.Generic.HashSet<SvgResolvedNode>();
        SvgResolvedNode? current = node;
        while (current != null && visited.Add(current))
        {
            result.Add(new SvgHitTestRouteNode(current));
            current = current.Parent;
        }
        return result;
    }

    internal static SvgHitTestCursor ResolveCursor(SvgResolvedNode node)
    {
        SvgResolvedNode? current = node;
        System.Collections.Generic.HashSet<SvgResolvedNode> visited = new System.Collections.Generic.HashSet<SvgResolvedNode>();
        while (current != null && visited.Add(current))
        {
            SvgComputedValue? value = current.ComputedStyle == null ? null : current.ComputedStyle.Find(SvgCssPropertyNames.Cursor);
            if (value != null && value.Kind == SvgComputedValueKind.Cursor && value.Cursor != null) return new SvgHitTestCursor(current, value.Cursor);
            current = current.Parent;
        }
        return new SvgHitTestCursor(node, null);
    }
}

/// <summary>Contains topmost-first document hit-test matches.</summary>
public class SvgDocumentHitTestResult
{
    /// <summary>Whether document hit testing completed successfully.</summary>
    public bool Success;
    /// <summary>The stable failure.</summary>
    public SvgHitTestError Error;
    /// <summary>The topmost match, or <c>null</c>.</summary>
    public SvgHitTestMatch? Topmost;
    /// <summary>Topmost-first matches, bounded by the requested result limit.</summary>
    public System.Collections.Generic.List<SvgHitTestMatch>? Matches;
    /// <summary>The visited resolved-node count.</summary>
    public int VisitedNodeCount;
    /// <summary>The tested display-active shape count.</summary>
    public int CandidateNodeCount;
    /// <summary>Whether additional matches may exist beyond the result limit.</summary>
    public bool Truncated;
    /// <summary>The effective cursor of the topmost match, or <c>null</c> when nothing was hit.</summary>
    public SvgHitTestCursor? Cursor;

    /// <summary>Creates a document hit-test result.</summary>
    /// <param name="success">Whether testing succeeded.</param>
    /// <param name="error">The stable failure.</param>
    /// <param name="topmost">The topmost match.</param>
    /// <param name="matches">The topmost-first match list.</param>
    /// <param name="visitedNodeCount">The visited node count.</param>
    /// <param name="candidateNodeCount">The candidate count.</param>
    /// <param name="truncated">Whether results were truncated.</param>
    public SvgDocumentHitTestResult(bool success, SvgHitTestError error, SvgHitTestMatch? topmost, System.Collections.Generic.List<SvgHitTestMatch>? matches, int visitedNodeCount, int candidateNodeCount, bool truncated)
    {
        Success = success;
        Error = error;
        Topmost = topmost;
        Matches = matches;
        VisitedNodeCount = visitedNodeCount;
        CandidateNodeCount = candidateNodeCount;
        Truncated = truncated;
        Cursor = topmost == null ? null : topmost.Cursor;
    }
}
