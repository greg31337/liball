namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgMarkerGeometry"/>.</summary>
public interface ISvgMarkerGeometry
{
    /// <summary>Extracts marker vertices with a positive result bound.</summary>
    /// <param name="path">The valid path.</param><param name="maximumVertices">The positive maximum output count.</param>
    /// <returns>The ordered marker vertices or a stable failure.</returns>
    SvgMarkerVertexResult Extract(SvgPathData? path, int maximumVertices);
    /// <summary>Resolves enabled start, mid, and end marker instances and bounds.</summary>
    /// <param name="path">The valid source path.</param><param name="options">The marker definitions, stroke width, and output bound.</param>
    /// <returns>The resolved marker geometry or a stable failure.</returns>
    SvgMarkerGeometryResult Resolve(SvgPathData path, SvgMarkerGeometryOptions options);
}
