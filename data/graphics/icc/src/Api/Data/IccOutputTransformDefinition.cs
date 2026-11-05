namespace icc;

/// <summary>Defines one classic or multi-stage ICC.1 output-profile transform.</summary>
public class IccOutputTransformDefinition
{
    /// <summary>Gets or sets an optional classic LUT8 or LUT16 pipeline.</summary>
    public IccLut? Lut;
    /// <summary>Gets or sets an optional version-4 lutAToB or lutBToA pipeline.</summary>
    public IccMultiStageLut? MultiStageLut;

    /// <summary>Initializes an empty output transform definition.</summary>
    public IccOutputTransformDefinition()
    {
        Lut = null;
        MultiStageLut = null;
    }
}
