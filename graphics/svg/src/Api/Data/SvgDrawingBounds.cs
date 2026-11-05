namespace svg;

/// <summary>Configures fill, stroke, marker, world, and effect-region bounds.</summary>
public class SvgDrawingBoundsOptions
{
    /// <summary>Whether fill geometry contributes.</summary>
    public bool IncludeFill;
    /// <summary>Optional stroke geometry; a missing value omits stroke.</summary>
    public SvgStrokeGeometryOptions? Stroke;
    /// <summary>Optional marker geometry; a missing value omits markers.</summary>
    public SvgMarkerGeometryOptions? Markers;
    /// <summary>Optional local-to-world transform.</summary>
    public SvgMatrix? Transform;
    /// <summary>Optional world-space clipping bounds.</summary>
    public SvgRect? ClipBounds;
    /// <summary>Optional world-space mask painting bounds.</summary>
    public SvgRect? MaskBounds;
    /// <summary>Optional world-space filter output region.</summary>
    public SvgRect? FilterBounds;

    /// <summary>Creates fill-only local bounds defaults.</summary>
    public SvgDrawingBoundsOptions() { IncludeFill = true; Stroke = null; Markers = null; Transform = null; ClipBounds = null; MaskBounds = null; FilterBounds = null; }
}

/// <summary>Contains component and final world-space drawing bounds.</summary>
public class SvgDrawingBoundsResult
{
    /// <summary>Whether bounds computation succeeded.</summary>
    public bool Success;
    /// <summary>The stable failure.</summary>
    public SvgDrawingBoundsError Error;
    /// <summary>The transformed fill bounds.</summary>
    public SvgRect? FillBounds;
    /// <summary>The transformed stroke bounds.</summary>
    public SvgRect? StrokeBounds;
    /// <summary>The transformed marker bounds.</summary>
    public SvgRect? MarkerBounds;
    /// <summary>The final union after filter, clip, and mask regions.</summary>
    public SvgRect? Bounds;
    /// <summary>Whether clipping made the final bounds empty.</summary>
    public bool ClippedOut;
    /// <summary>Whether masking made the final bounds empty.</summary>
    public bool MaskedOut;
    /// <summary>Whether a filter output region replaced the unfiltered union.</summary>
    public bool Filtered;

    /// <summary>Creates a drawing-bounds result.</summary>
    /// <param name="success">Whether computation succeeded.</param><param name="error">The failure.</param><param name="fillBounds">The fill bounds.</param><param name="strokeBounds">The stroke bounds.</param><param name="markerBounds">The marker bounds.</param><param name="bounds">The final bounds.</param><param name="clippedOut">Whether clipping emptied the result.</param><param name="maskedOut">Whether masking emptied the result.</param><param name="filtered">Whether a filter region applied.</param>
    public SvgDrawingBoundsResult(bool success, SvgDrawingBoundsError error, SvgRect? fillBounds, SvgRect? strokeBounds, SvgRect? markerBounds, SvgRect? bounds, bool clippedOut, bool maskedOut, bool filtered) { Success = success; Error = error; FillBounds = fillBounds; StrokeBounds = strokeBounds; MarkerBounds = markerBounds; Bounds = bounds; ClippedOut = clippedOut; MaskedOut = maskedOut; Filtered = filtered; }
}

/// <summary>Supplies optional resolved-node drawing details not retained by scene resolution.</summary>
public class SvgResolvedNodeBoundsDetails
{
    /// <summary>The resolved node receiving these details.</summary>
    public SvgResolvedNode Node;
    /// <summary>Optional explicit stroke geometry.</summary>
    public SvgStrokeGeometryOptions? Stroke;
    /// <summary>Optional explicit marker geometry.</summary>
    public SvgMarkerGeometryOptions? Markers;
    /// <summary>Optional world-space clip bounds.</summary>
    public SvgRect? ClipBounds;
    /// <summary>Optional world-space mask bounds.</summary>
    public SvgRect? MaskBounds;
    /// <summary>Optional world-space filter output region.</summary>
    public SvgRect? FilterBounds;

    /// <summary>Creates empty details for one resolved node.</summary>
    /// <param name="node">The resolved node.</param>
    public SvgResolvedNodeBoundsDetails(SvgResolvedNode node) { Node = node; Stroke = null; Markers = null; ClipBounds = null; MaskBounds = null; FilterBounds = null; }
}

/// <summary>Configures aggregate resolved-document drawing bounds.</summary>
public class SvgDocumentBoundsOptions
{
    /// <summary>Whether resolved ancestor viewport overflow clips apply.</summary>
    public bool ClipToViewports;
    /// <summary>Whether ordinary computed strokes are included.</summary>
    public bool IncludeComputedStroke;
    /// <summary>Whether resolved embedded-content rectangles are included.</summary>
    public bool IncludeEmbeddedContent;
    /// <summary>The positive resolved-node traversal limit.</summary>
    public int MaximumNodes;
    /// <summary>Optional per-node marker and effect-region details.</summary>
    public System.Collections.Generic.List<SvgResolvedNodeBoundsDetails> Details;

    /// <summary>Creates deterministic aggregate bounds defaults.</summary>
    public SvgDocumentBoundsOptions() { ClipToViewports = true; IncludeComputedStroke = true; IncludeEmbeddedContent = true; MaximumNodes = 2000000; Details = new System.Collections.Generic.List<SvgResolvedNodeBoundsDetails>(); }
}

/// <summary>Contains aggregate resolved-document drawing bounds and contribution counts.</summary>
public class SvgDocumentBoundsResult
{
    /// <summary>Whether aggregation succeeded.</summary>
    public bool Success;
    /// <summary>The stable failure.</summary>
    public SvgDrawingBoundsError Error;
    /// <summary>The aggregate world-space drawing bounds.</summary>
    public SvgRect? Bounds;
    /// <summary>The number of resolved nodes that contributed nonempty bounds.</summary>
    public int ContributingNodeCount;
    /// <summary>The number of active renderable nodes with empty drawing bounds.</summary>
    public int EmptyNodeCount;
    /// <summary>The number of inactive, non-renderable, or resource nodes skipped.</summary>
    public int SkippedNodeCount;
    /// <summary>The number of node bounds emptied by a clip or viewport.</summary>
    public int ClippedNodeCount;
    /// <summary>The number of node bounds emptied by a mask.</summary>
    public int MaskedNodeCount;
    /// <summary>The number of contributing nodes using filter regions.</summary>
    public int FilteredNodeCount;

    /// <summary>Creates an aggregate document-bounds result.</summary>
    /// <param name="success">Whether aggregation succeeded.</param><param name="error">The failure.</param><param name="bounds">The aggregate bounds.</param><param name="contributingNodeCount">The contributing count.</param><param name="emptyNodeCount">The empty count.</param><param name="skippedNodeCount">The skipped count.</param><param name="clippedNodeCount">The clipped count.</param><param name="maskedNodeCount">The masked count.</param><param name="filteredNodeCount">The filtered count.</param>
    public SvgDocumentBoundsResult(bool success, SvgDrawingBoundsError error, SvgRect? bounds, int contributingNodeCount, int emptyNodeCount, int skippedNodeCount, int clippedNodeCount, int maskedNodeCount, int filteredNodeCount) { Success = success; Error = error; Bounds = bounds; ContributingNodeCount = contributingNodeCount; EmptyNodeCount = emptyNodeCount; SkippedNodeCount = skippedNodeCount; ClippedNodeCount = clippedNodeCount; MaskedNodeCount = maskedNodeCount; FilteredNodeCount = filteredNodeCount; }
}
