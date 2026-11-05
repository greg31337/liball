namespace icc;

/// <summary>Pairs a device encoding with its characterized CIELAB reference.</summary>
public class IccCharacterizationSample
{
    /// <summary>Gets or sets the normalized device-space encoding.</summary>
    public IccColorVector DeviceColor;
    /// <summary>Gets or sets the characterized CIELAB reference.</summary>
    public IccLabColor ReferenceLab;
    /// <summary>Gets or sets whether the reference is media-relative and requires perfect-diffuser normalization.</summary>
    public bool ReferenceIsMediaRelative;

    /// <summary>Initializes an empty perfect-diffuser-relative characterization sample.</summary>
    public IccCharacterizationSample()
    {
        DeviceColor = new IccColorVector();
        ReferenceLab = new IccLabColor();
        ReferenceIsMediaRelative = false;
    }
}
