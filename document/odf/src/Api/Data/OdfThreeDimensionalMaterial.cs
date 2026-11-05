namespace odf;

/// <summary>
/// Describes material and backface properties supplied by a graphic style.
/// </summary>
public class OdfThreeDimensionalMaterial
{
    /// <summary>The material ambient color.</summary>
    public string AmbientColor = "";
    /// <summary>The material diffuse color.</summary>
    public string DiffuseColor = "";
    /// <summary>The material emissive color.</summary>
    public string EmissiveColor = "";
    /// <summary>The material specular color.</summary>
    public string SpecularColor = "";
    /// <summary>The material shininess percentage.</summary>
    public string Shininess = "";
    /// <summary>The preserved backface-culling value.</summary>
    public string BackfaceCullingValue = "";
    /// <summary>The parsed backface-culling mode.</summary>
    public OdfBackfaceCulling BackfaceCulling;
    /// <summary>Whether the backface-culling value is valid when specified.</summary>
    public bool IsBackfaceCullingValid = true;
    /// <summary>The graphic or presentation style supplying the material.</summary>
    public OdfStyle? Style;
    internal string OriginalAmbientColor = "";
    internal string OriginalDiffuseColor = "";
    internal string OriginalEmissiveColor = "";
    internal string OriginalSpecularColor = "";
    internal string OriginalShininess = "";
    internal string OriginalBackfaceCullingValue = "";
    internal OdfBackfaceCulling OriginalBackfaceCulling;
}
