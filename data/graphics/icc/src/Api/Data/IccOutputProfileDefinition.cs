namespace icc;

/// <summary>Defines the intent transforms and optional characterization data of an Output profile.</summary>
public class IccOutputProfileDefinition
{
    /// <summary>Gets or sets the perceptual, media-relative colorimetric, and saturation device-to-PCS transforms, in that order.</summary>
    public IccOutputTransformDefinition[] ForwardTransforms;
    /// <summary>Gets or sets the perceptual, media-relative colorimetric, and saturation PCS-to-device transforms, in that order.</summary>
    public IccOutputTransformDefinition[] BackwardTransforms;
    /// <summary>Gets or sets the required PCS-to-one-channel gamut transform.</summary>
    public IccOutputTransformDefinition? GamutTransform;
    /// <summary>Gets or sets an optional device-space colorant table, required for xCLR data spaces.</summary>
    public IccColorantSet? Colorants;
    /// <summary>Gets or sets optional colorimetric measurement conditions.</summary>
    public IccMeasurement? Measurement;
    /// <summary>Gets or sets optional viewing conditions.</summary>
    public IccViewingConditions? ViewingConditions;

    /// <summary>Initializes an empty Output profile definition.</summary>
    public IccOutputProfileDefinition()
    {
        ForwardTransforms = new IccOutputTransformDefinition[0];
        BackwardTransforms = new IccOutputTransformDefinition[0];
        GamutTransform = null;
        Colorants = null;
        Measurement = null;
        ViewingConditions = null;
    }
}
