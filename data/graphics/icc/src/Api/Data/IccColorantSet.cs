namespace icc;

/// <summary>Represents an ordered colorant table.</summary>
public class IccColorantSet
{
    /// <summary>Gets or sets the associated colour-space signature.</summary>
    public uint ColorSpaceSignature;
    /// <summary>Gets or sets the ordered colorants.</summary>
    public IccColorant[] Colorants;

    /// <summary>Initializes an empty colorant table.</summary>
    public IccColorantSet()
    {
        ColorSpaceSignature = 0;
        Colorants = new IccColorant[0];
    }
}
