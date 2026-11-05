namespace svg;

/// <summary>Extracts marker vertices and resolves automatic marker geometry.</summary>
public class SvgMarkerGeometry : ISvgMarkerGeometry
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgMarkerGeometry()
    {
    }

    /// <summary>Extracts marker vertices with a positive result bound.</summary>
    /// <param name="path">The valid path.</param><param name="maximumVertices">The positive maximum output count.</param>
    /// <returns>The ordered marker vertices or a stable failure.</returns>
    public SvgMarkerVertexResult Extract(SvgPathData? path, int maximumVertices)
    {
        if (path == null || maximumVertices < 1) return new SvgMarkerGeometry().VertexFailure(SvgMarkerGeometryError.InvalidArgument);
        SvgPathNormalizeResult normalized = path.Normalize(); if (!normalized.Success) return new SvgMarkerGeometry().VertexFailure(new SvgMarkerGeometry().Map(normalized.Error));
        SvgMarkerExtractState state = new SvgMarkerExtractState(normalized.PathData, maximumVertices); SvgMarkerGeometryError error = state.Run();
        return error == SvgMarkerGeometryError.None ? new SvgMarkerVertexResult(true, SvgMarkerGeometryError.None, state.Vertices) : new SvgMarkerGeometry().VertexFailure(error);
    }

    /// <summary>Resolves enabled start, mid, and end marker instances and bounds.</summary>
    /// <param name="path">The valid source path.</param><param name="options">The marker definitions, stroke width, and output bound.</param>
    /// <returns>The resolved marker geometry or a stable failure.</returns>
    public SvgMarkerGeometryResult Resolve(SvgPathData path, SvgMarkerGeometryOptions options)
    {
        if (!new SvgMarkerGeometry().ValidWithOptions(options)) return new SvgMarkerGeometry().Failure(SvgMarkerGeometryError.InvalidArgument);
        SvgMarkerVertexResult extracted = new SvgMarkerGeometry().Extract(path, options.MaximumVertices); if (!extracted.Success) return new SvgMarkerGeometry().Failure(extracted.Error);
        System.Collections.Generic.List<SvgMarkerInstance> instances = new System.Collections.Generic.List<SvgMarkerInstance>(); SvgRect aggregate = SvgRect.Create(0, 0, 0, 0, true);
        int index = 0;
        while (index < (extracted.Vertices ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            SvgMarkerVertex vertex = extracted.Vertices[index]; SvgMarkerDefinition? definition = new SvgMarkerGeometry().Definition(vertex.Placement, options);
            if (definition != null && (definition.Units != SvgMarkerUnits.StrokeWidth || options.StrokeWidth > 0))
            {
                if (instances.Count >= options.MaximumVertices) return new SvgMarkerGeometry().Failure(SvgMarkerGeometryError.VertexLimitExceeded);
                SvgMarkerInstanceResult placed = new SvgMarkerGeometry().Place(vertex, definition, options.StrokeWidth); if (placed.Error != SvgMarkerGeometryError.None) return new SvgMarkerGeometry().Failure(placed.Error);
                instances.Add((placed.Instance ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); SvgRectOperationResult? union = (aggregate ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Union((placed.Instance.Bounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); if (!union.Success) return new SvgMarkerGeometry().Failure(SvgMarkerGeometryError.NonFiniteResult); aggregate = (union.Rectangle ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            }
            index = index + 1;
        }
        return new SvgMarkerGeometryResult(true, SvgMarkerGeometryError.None, extracted.Vertices, instances, aggregate);
    }

    internal SvgMarkerVertexResult VertexFailure(SvgMarkerGeometryError error) { return new SvgMarkerVertexResult(false, error, null); }
    internal SvgMarkerGeometryResult Failure(SvgMarkerGeometryError error) { return new SvgMarkerGeometryResult(false, error, null, null, null); }
SvgMarkerInstanceResult Place(SvgMarkerVertex vertex, SvgMarkerDefinition definition, double strokeWidth)
    {
        double angle = definition.OrientKind == SvgMarkerOrientKind.Angle ? definition.Angle : vertex.AutoAngle;
        if (definition.OrientKind == SvgMarkerOrientKind.AutoStartReverse && vertex.Placement == SvgMarkerPlacement.Start) angle = angle + 180;
        double scale = definition.Units == SvgMarkerUnits.StrokeWidth ? strokeWidth : 1; double radians = angle * System.Math.PI / 180; double cosine = System.Math.Cos(radians); double sine = System.Math.Sin(radians);
        SvgMatrix translation = new SvgMatrix(1, 0, 0, 1, vertex.Position.X, vertex.Position.Y); SvgMatrix rotation = new SvgMatrix(cosine, sine, -sine, cosine, 0, 0); SvgMatrix scaling = new SvgMatrix(scale, 0, 0, scale, 0, 0); SvgMatrix reference = new SvgMatrix(1, 0, 0, 1, -definition.RefX, -definition.RefY);
        SvgMatrixOperationResult first = translation.Multiply(rotation); if (!first.Success) return new SvgMarkerInstanceResult(SvgMarkerGeometryError.NonFiniteResult, null); SvgMatrixOperationResult second = (first.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Multiply(scaling); if (!second.Success) return new SvgMarkerInstanceResult(SvgMarkerGeometryError.NonFiniteResult, null); SvgMatrixOperationResult third = (second.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Multiply(reference); if (!third.Success) return new SvgMarkerInstanceResult(SvgMarkerGeometryError.NonFiniteResult, null); SvgMatrix? transform = third.Matrix;
        if (definition.ContentTransform != null) { SvgMatrixOperationResult? content = (transform ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Multiply(definition.ContentTransform); if (!content.Success) return new SvgMarkerInstanceResult(SvgMarkerGeometryError.NonFiniteResult, null); transform = content.Matrix; }
        SvgRectTransformResult bounds = (transform ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformRect(definition.Bounds); if (!bounds.Success) return new SvgMarkerInstanceResult(SvgMarkerGeometryError.NonFiniteResult, null);
        return new SvgMarkerInstanceResult(SvgMarkerGeometryError.None, new SvgMarkerInstance(vertex, SvgFiniteMath.NormalizeZero(angle), scale, transform, bounds.Rectangle));
    }
SvgMarkerDefinition? Definition(SvgMarkerPlacement placement, SvgMarkerGeometryOptions options) { if (placement == SvgMarkerPlacement.Start) return options.StartMarker; if (placement == SvgMarkerPlacement.End) return options.EndMarker; return options.MidMarker; }
bool ValidWithOptions(SvgMarkerGeometryOptions options) { return options != null && options.MaximumVertices > 0 && SvgFiniteMath.IsFinite(options.StrokeWidth) && options.StrokeWidth >= 0 && new SvgMarkerGeometry().ValidWithDefinition(options.StartMarker) && new SvgMarkerGeometry().ValidWithDefinition(options.MidMarker) && new SvgMarkerGeometry().ValidWithDefinition(options.EndMarker); }
bool ValidWithDefinition(SvgMarkerDefinition? definition) { if (definition == null) return true; if (definition.Bounds == null || !SvgFiniteMath.IsFinite(definition.Angle) || !SvgFiniteMath.IsFinite(definition.RefX) || !SvgFiniteMath.IsFinite(definition.RefY)) return false; if (definition.Units != SvgMarkerUnits.StrokeWidth && definition.Units != SvgMarkerUnits.UserSpaceOnUse) return false; if (definition.OrientKind != SvgMarkerOrientKind.Angle && definition.OrientKind != SvgMarkerOrientKind.Auto && definition.OrientKind != SvgMarkerOrientKind.AutoStartReverse) return false; if (!new SvgMarkerGeometry().ValidWithRect(definition.Bounds)) return false; return definition.ContentTransform == null || new SvgMarkerGeometry().ValidWithMatrix(definition.ContentTransform); }
bool ValidWithRect(SvgRect rect) { return SvgFiniteMath.IsFinite(rect.X) && SvgFiniteMath.IsFinite(rect.Y) && SvgFiniteMath.IsFinite(rect.Width) && SvgFiniteMath.IsFinite(rect.Height) && rect.Width >= 0 && rect.Height >= 0; }
bool ValidWithMatrix(SvgMatrix matrix) { return SvgFiniteMath.IsFinite(matrix.A) && SvgFiniteMath.IsFinite(matrix.B) && SvgFiniteMath.IsFinite(matrix.C) && SvgFiniteMath.IsFinite(matrix.D) && SvgFiniteMath.IsFinite(matrix.E) && SvgFiniteMath.IsFinite(matrix.F); }
SvgMarkerGeometryError Map(SvgPathNormalizeError error) { if (error == SvgPathNormalizeError.InvalidPath) return SvgMarkerGeometryError.InvalidPath; if (error == SvgPathNormalizeError.NonFiniteResult) return SvgMarkerGeometryError.NonFiniteResult; if (error == SvgPathNormalizeError.CommandLimitExceeded) return SvgMarkerGeometryError.VertexLimitExceeded; return SvgMarkerGeometryError.InvalidArgument; }
}
