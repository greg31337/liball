namespace icc;

/// <summary>Represents an ordered colour-space channel vector.</summary>
public class IccColorVector
{
    /// <summary>Gets or sets the decoded colour-space family.</summary>
    public IccColorSpaceKind ColorSpace;
    /// <summary>Gets or sets the encoded colour-space signature.</summary>
    public uint ColorSpaceSignature;
    /// <summary>Gets or sets the ordered channel components.</summary>
    public double[] Components;

    /// <summary>Initializes an empty vector with unknown colour space.</summary>
    public IccColorVector()
    {
        ColorSpace = IccColorSpaceKind.Unknown;
        ColorSpaceSignature = 0;
        Components = new double[0];
    }
}
