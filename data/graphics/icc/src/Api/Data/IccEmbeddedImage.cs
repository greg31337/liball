namespace icc;

/// <summary>Represents an ICC.2 embedded height or normal image.</summary>
public class IccEmbeddedImage
{
    /// <summary>Gets or sets image width in pixels.</summary>
    public int Width;
    /// <summary>Gets or sets image height in pixels.</summary>
    public int Height;
    /// <summary>Gets or sets the component count.</summary>
    public int Components;
    /// <summary>Gets or sets the image encoding signature.</summary>
    public uint EncodingSignature;
    /// <summary>Gets or sets encoded image bytes.</summary>
    public byte[] Bytes;
    /// <summary>Gets or sets whether the image payload is compressed.</summary>
    public bool IsCompressed;
    /// <summary>Gets or sets whether opposite image edges tile without a visible seam.</summary>
    public bool IsSeamless;
    /// <summary>Gets or sets the height in metres represented by the minimum height-map sample.</summary>
    public double MinimumHeightMeters;
    /// <summary>Gets or sets the height in metres represented by the maximum height-map sample.</summary>
    public double MaximumHeightMeters;

    /// <summary>Initializes an empty image.</summary>
    public IccEmbeddedImage()
    {
        Width = 0;
        Height = 0;
        Components = 0;
        EncodingSignature = 0;
        Bytes = new byte[0];
        IsCompressed = false;
        IsSeamless = false;
        MinimumHeightMeters = 0.0;
        MaximumHeightMeters = 0.0;
    }
}
