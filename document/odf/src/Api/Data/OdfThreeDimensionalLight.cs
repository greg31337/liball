namespace odf;

/// <summary>
/// Describes one directional light in a three-dimensional scene.
/// </summary>
public class OdfThreeDimensionalLight
{
    /// <summary>The diffuse color emitted by the light.</summary>
    public string DiffuseColor = "";
    /// <summary>The light direction.</summary>
    public OdfThreeDimensionalVector Direction =
        new OdfThreeDimensionalVector();
    /// <summary>The preserved enabled value.</summary>
    public string EnabledValue = "";
    /// <summary>Whether the light is enabled.</summary>
    public bool IsEnabled;
    /// <summary>Whether the enabled value is valid when specified.</summary>
    public bool IsEnabledValid = true;
    /// <summary>The preserved specular value.</summary>
    public string SpecularValue = "";
    /// <summary>Whether the light contributes specular highlights.</summary>
    public bool IsSpecular;
    /// <summary>Whether the specular value is valid when specified.</summary>
    public bool IsSpecularValid = true;
    /// <summary>The underlying light element.</summary>
    public OdfNode? Node;
    internal string OriginalDiffuseColor = "";
    internal string OriginalEnabledValue = "";
    internal bool OriginalIsEnabled;
    internal string OriginalSpecularValue = "";
    internal bool OriginalIsSpecular;
}
