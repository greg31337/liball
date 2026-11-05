namespace odf;

/// <summary>
/// Describes the camera and lighting environment of a three-dimensional scene.
/// </summary>
public class OdfThreeDimensionalCamera
{
    /// <summary>The scene view-reference-point vector.</summary>
    public OdfThreeDimensionalVector ViewReferencePoint =
        new OdfThreeDimensionalVector();
    /// <summary>The scene view-plane-normal vector.</summary>
    public OdfThreeDimensionalVector ViewPlaneNormal =
        new OdfThreeDimensionalVector();
    /// <summary>The scene view-up vector.</summary>
    public OdfThreeDimensionalVector ViewUpVector =
        new OdfThreeDimensionalVector();
    /// <summary>The preserved projection value.</summary>
    public string ProjectionValue = "";
    /// <summary>The parsed projection mode.</summary>
    public OdfThreeDimensionalProjection Projection;
    /// <summary>Whether the projection value is valid when specified.</summary>
    public bool IsProjectionValid = true;
    /// <summary>The distance between the camera and projection plane.</summary>
    public string Distance = "";
    /// <summary>The camera focal length.</summary>
    public string FocalLength = "";
    /// <summary>The scene shadow-slant angle.</summary>
    public string ShadowSlant = "";
    /// <summary>The preserved shading mode.</summary>
    public string ShadeMode = "";
    /// <summary>The scene ambient-light color.</summary>
    public string AmbientColor = "";
    /// <summary>The preserved lighting-mode value.</summary>
    public string LightingModeValue = "";
    /// <summary>Whether lighting mode is enabled.</summary>
    public bool IsLightingModeEnabled;
    /// <summary>Whether the lighting-mode value is valid when specified.</summary>
    public bool IsLightingModeValid = true;
    internal string OriginalProjectionValue = "";
    internal OdfThreeDimensionalProjection OriginalProjection;
    internal string OriginalDistance = "";
    internal string OriginalFocalLength = "";
    internal string OriginalShadowSlant = "";
    internal string OriginalShadeMode = "";
    internal string OriginalAmbientColor = "";
    internal string OriginalLightingModeValue = "";
    internal bool OriginalIsLightingModeEnabled;
}
