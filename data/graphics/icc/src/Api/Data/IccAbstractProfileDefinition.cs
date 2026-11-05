namespace icc;

/// <summary>Defines the fallback and optional MPE transforms of an abstract profile.</summary>
public class IccAbstractProfileDefinition
{
    /// <summary>Gets or sets an optional classic LUT8 or LUT16 fallback transform.</summary>
    public IccLut? Lut;
    /// <summary>Gets or sets an optional version-4 lutAToB fallback transform.</summary>
    public IccMultiStageLut? MultiStageLut;
    /// <summary>Gets or sets an optional version-4 preferred perceptual MPE transform.</summary>
    public IccMultiProcessElements? MultiProcessElements;

    /// <summary>Initializes an empty abstract-profile definition.</summary>
    public IccAbstractProfileDefinition()
    {
        Lut = null;
        MultiStageLut = null;
        MultiProcessElements = null;
    }
}
