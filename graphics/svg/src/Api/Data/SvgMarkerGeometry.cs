namespace svg;

/// <summary>Describes one ordered start, mid, or end marker vertex.</summary>
public class SvgMarkerVertex
{
    /// <summary>The marker position in path user coordinates.</summary>
    public SvgPoint Position;
    /// <summary>The normalized incoming tangent, or zero when unavailable.</summary>
    public SvgVector IncomingTangent;
    /// <summary>The normalized outgoing tangent, or zero when unavailable.</summary>
    public SvgVector OutgoingTangent;
    /// <summary>The path-derived placement angle in degrees.</summary>
    public double AutoAngle;
    /// <summary>The marker placement role.</summary>
    public SvgMarkerPlacement Placement;
    /// <summary>The zero-based extracted subpath index.</summary>
    public int SubpathIndex;
    /// <summary>The zero-based geometric vertex index in the subpath.</summary>
    public int VertexIndex;

    /// <summary>Creates one marker vertex.</summary>
    /// <param name="position">The placement point.</param><param name="incoming">The incoming unit tangent.</param><param name="outgoing">The outgoing unit tangent.</param>
    /// <param name="autoAngle">The automatic angle in degrees.</param><param name="placement">The placement role.</param><param name="subpathIndex">The subpath index.</param><param name="vertexIndex">The vertex index.</param>
    public SvgMarkerVertex(SvgPoint position, SvgVector incoming, SvgVector outgoing, double autoAngle, SvgMarkerPlacement placement, int subpathIndex, int vertexIndex)
    {
        Position = position; IncomingTangent = incoming; OutgoingTangent = outgoing; AutoAngle = autoAngle; Placement = placement; SubpathIndex = subpathIndex; VertexIndex = vertexIndex;
    }
}

/// <summary>Contains ordered path marker vertices.</summary>
public class SvgMarkerVertexResult
{
    /// <summary>Whether extraction succeeded.</summary>
    public bool Success;
    /// <summary>The stable marker failure.</summary>
    public SvgMarkerGeometryError Error;
    /// <summary>The ordered vertices, or <c>null</c> after failure.</summary>
    public System.Collections.Generic.List<SvgMarkerVertex>? Vertices;

    /// <summary>Creates a marker-vertex result.</summary>
    /// <param name="success">Whether extraction succeeded.</param><param name="error">The failure kind.</param><param name="vertices">The ordered vertices.</param>
    public SvgMarkerVertexResult(bool success, SvgMarkerGeometryError error, System.Collections.Generic.List<SvgMarkerVertex>? vertices) { Success = success; Error = error; Vertices = vertices; }
}

/// <summary>Defines one local marker resource for a placement role.</summary>
public class SvgMarkerDefinition
{
    /// <summary>The local marker content bounds.</summary>
    public SvgRect Bounds;
    /// <summary>The marker coordinate scaling mode.</summary>
    public SvgMarkerUnits Units;
    /// <summary>The marker orientation mode.</summary>
    public SvgMarkerOrientKind OrientKind;
    /// <summary>The fixed angle in degrees when orientation uses an angle.</summary>
    public double Angle;
    /// <summary>The local reference x coordinate placed on the path vertex.</summary>
    public double RefX;
    /// <summary>The local reference y coordinate placed on the path vertex.</summary>
    public double RefY;
    /// <summary>An optional marker-content transform applied before reference placement.</summary>
    public SvgMatrix? ContentTransform;

    /// <summary>Creates an empty user-space marker with fixed zero-degree orientation.</summary>
    public SvgMarkerDefinition()
    {
        Bounds = SvgRect.Create(0, 0, 0, 0, true); Units = SvgMarkerUnits.UserSpaceOnUse; OrientKind = SvgMarkerOrientKind.Angle; Angle = 0; RefX = 0; RefY = 0; ContentTransform = null;
    }
}

/// <summary>Configures start, mid, and end marker placement.</summary>
public class SvgMarkerGeometryOptions
{
    /// <summary>The optional start marker definition.</summary>
    public SvgMarkerDefinition? StartMarker;
    /// <summary>The optional mid marker definition.</summary>
    public SvgMarkerDefinition? MidMarker;
    /// <summary>The optional end marker definition.</summary>
    public SvgMarkerDefinition? EndMarker;
    /// <summary>The finite nonnegative used stroke width for stroke-width marker units.</summary>
    public double StrokeWidth;
    /// <summary>The positive maximum vertex and marker-instance count.</summary>
    public int MaximumVertices;

    /// <summary>Creates empty marker-placement defaults.</summary>
    public SvgMarkerGeometryOptions() { StartMarker = null; MidMarker = null; EndMarker = null; StrokeWidth = 1; MaximumVertices = 2000000; }
}

/// <summary>Contains one resolved marker placement transform and bounds.</summary>
public class SvgMarkerInstance
{
    /// <summary>The source marker vertex.</summary>
    public SvgMarkerVertex Vertex;
    /// <summary>The placement-specific applied angle in degrees.</summary>
    public double AppliedAngle;
    /// <summary>The marker-unit scale.</summary>
    public double Scale;
    /// <summary>The local-content to path-user-space transform.</summary>
    public SvgMatrix Transform;
    /// <summary>The transformed marker content bounds.</summary>
    public SvgRect? Bounds;

    /// <summary>Creates a resolved marker instance.</summary>
    /// <param name="vertex">The source vertex.</param><param name="appliedAngle">The applied angle.</param><param name="scale">The unit scale.</param><param name="transform">The placement transform.</param><param name="bounds">The transformed bounds.</param>
    public SvgMarkerInstance(SvgMarkerVertex vertex, double appliedAngle, double scale, SvgMatrix transform, SvgRect? bounds) { Vertex = vertex; AppliedAngle = appliedAngle; Scale = scale; Transform = transform; Bounds = bounds; }
}

/// <summary>Contains resolved marker vertices, instances, and aggregate bounds.</summary>
public class SvgMarkerGeometryResult
{
    /// <summary>Whether marker geometry resolution succeeded.</summary>
    public bool Success;
    /// <summary>The stable marker failure.</summary>
    public SvgMarkerGeometryError Error;
    /// <summary>The ordered path vertices, or <c>null</c> after failure.</summary>
    public System.Collections.Generic.List<SvgMarkerVertex>? Vertices;
    /// <summary>The ordered enabled marker instances, or <c>null</c> after failure.</summary>
    public System.Collections.Generic.List<SvgMarkerInstance>? Instances;
    /// <summary>The aggregate marker bounds, explicitly empty when no marker contributes.</summary>
    public SvgRect? Bounds;

    /// <summary>Creates a marker geometry result.</summary>
    /// <param name="success">Whether resolution succeeded.</param><param name="error">The failure kind.</param><param name="vertices">The vertices.</param><param name="instances">The marker instances.</param><param name="bounds">The aggregate bounds.</param>
    public SvgMarkerGeometryResult(bool success, SvgMarkerGeometryError error, System.Collections.Generic.List<SvgMarkerVertex>? vertices, System.Collections.Generic.List<SvgMarkerInstance>? instances, SvgRect? bounds) { Success = success; Error = error; Vertices = vertices; Instances = instances; Bounds = bounds; }
}
