namespace icc;

/// <summary>Represents bidirectional reflectance parameters and transforms.</summary>
public class IccBrdf
{
    /// <summary>Gets or sets the BRDF parameterization type signature, either monochrome or full colour.</summary>
    public uint TypeSignature;
    /// <summary>Gets or sets the BRDF model signature.</summary>
    public uint ModelSignature;
    /// <summary>Gets or sets model parameters.</summary>
    public double[] Parameters;
    /// <summary>Gets or sets the colourimetric transform.</summary>
    public IccMultiProcessElements? ColorimetricTransform;
    /// <summary>Gets or sets the spectral transform.</summary>
    public IccMultiProcessElements? SpectralTransform;

    /// <summary>Initializes an unspecified BRDF.</summary>
    public IccBrdf()
    {
        TypeSignature = 0;
        ModelSignature = 0;
        Parameters = new double[0];
        ColorimetricTransform = null;
        SpectralTransform = null;
    }
}
