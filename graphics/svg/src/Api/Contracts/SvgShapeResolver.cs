namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgShapeResolver"/>.</summary>
public interface ISvgShapeResolver
{
    /// <summary>Resolves one supported SVG shape using secure defaults.</summary>
    /// <param name="node">The circle, ellipse, line, path, polygon, polyline, or rectangle.</param>
    /// <returns>The resolved shape geometry.</returns>
    SvgShapeResult ResolveWithNode(SvgNode node);
    /// <summary>Resolves one supported SVG shape using explicit style, length, and security context.</summary>
    /// <param name="node">The circle, ellipse, line, path, polygon, polyline, or rectangle.</param>
    /// <param name="options">The computed style, length context, parsers, and bounds.</param>
    /// <returns>The resolved shape geometry or a located failure.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when the node is not a supported SVG shape.</exception>
    SvgShapeResult ResolveWithNodeAndOptions(SvgNode node, SvgShapeResolveOptions options);
}
