namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgHitTesting"/>.</summary>
public interface ISvgHitTesting
{
    /// <summary>Tests one path in local or transformed query coordinates.</summary>
    /// <param name="path">The valid path.</param>
    /// <param name="point">The finite query point.</param>
    /// <param name="options">The geometry, paint, visibility, transform, and effect options.</param>
    /// <returns>The component hit decision or stable failure.</returns>
    SvgGeometryHitTestResult Path(SvgPathData? path, SvgPoint point, SvgGeometryHitTestOptions options);
    /// <summary>Tests one resolved basic shape in local or transformed query coordinates.</summary>
    /// <param name="shape">The resolved shape.</param>
    /// <param name="point">The finite query point.</param>
    /// <param name="options">The geometry, paint, visibility, transform, and effect options.</param>
    /// <returns>The component hit decision or stable failure.</returns>
    SvgGeometryHitTestResult Shape(SvgShapeResult? shape, SvgPoint point, SvgGeometryHitTestOptions options);
    /// <summary>Tests resolved shapes in reverse painting order.</summary>
    /// <param name="document">The resolved document.</param>
    /// <param name="point">The finite world-space query point.</param>
    /// <param name="options">The traversal, result, viewport, and per-node options.</param>
    /// <returns>Topmost-first source and instance matches.</returns>
    SvgDocumentHitTestResult Document(SvgResolvedDocument? document, SvgPoint point, SvgDocumentHitTestOptions? options);
}
