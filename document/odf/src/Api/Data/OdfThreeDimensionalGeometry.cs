namespace odf;

/// <summary>
/// Describes scene and primitive metadata for a three-dimensional drawing object.
/// </summary>
public class OdfThreeDimensionalGeometry
{
    /// <summary>The preserved three-dimensional transform expression.</summary>
    public string Transform = "";
    /// <summary>The scene camera and lighting environment.</summary>
    public OdfThreeDimensionalCamera Camera =
        new OdfThreeDimensionalCamera();
    /// <summary>The directional lights declared directly by a scene.</summary>
    public OdfThreeDimensionalLight[] Lights = [];
    /// <summary>The minimum edge of a cube.</summary>
    public OdfThreeDimensionalVector MinimumEdge =
        new OdfThreeDimensionalVector();
    /// <summary>The maximum edge of a cube.</summary>
    public OdfThreeDimensionalVector MaximumEdge =
        new OdfThreeDimensionalVector();
    /// <summary>The center of a sphere.</summary>
    public OdfThreeDimensionalVector Center =
        new OdfThreeDimensionalVector();
    /// <summary>The size of a sphere.</summary>
    public OdfThreeDimensionalVector Size =
        new OdfThreeDimensionalVector();
    /// <summary>The style-backed material and backface metadata.</summary>
    public OdfThreeDimensionalMaterial Material =
        new OdfThreeDimensionalMaterial();
    /// <summary>The underlying three-dimensional drawing element.</summary>
    public OdfNode? Node;
    internal string OriginalTransform = "";
}
