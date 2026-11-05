namespace icc;

/// <summary>Represents ICC.2 surface height and normal maps.</summary>
public class IccSurfaceMap
{
    /// <summary>Gets or sets the surface width.</summary>
    public int Width;
    /// <summary>Gets or sets the surface height.</summary>
    public int Height;
    /// <summary>Gets or sets the height image when present.</summary>
    public IccEmbeddedImage? HeightImage;
    /// <summary>Gets or sets the normal image when present.</summary>
    public IccEmbeddedImage? NormalImage;
    /// <summary>Gets or sets physical scaling values.</summary>
    public double[] Scale;

    /// <summary>Initializes an empty surface map.</summary>
    public IccSurfaceMap()
    {
        Width = 0;
        Height = 0;
        HeightImage = null;
        NormalImage = null;
        Scale = new double[0];
    }
}
