namespace icc;

/// <summary>Describes a recognized private or vendor tag while retaining its encoded payload separately.</summary>
public class IccVendorTagData
{
    /// <summary>Gets or sets the registered vendor name.</summary>
    public string VendorName;
    /// <summary>Gets or sets the recognized payload format name.</summary>
    public string FormatName;
    /// <summary>Gets or sets the owning tag signature.</summary>
    public uint TagSignature;
    /// <summary>Gets or sets the payload type signature.</summary>
    public uint TypeSignature;
    /// <summary>Gets or sets optional decoded Apple video-card gamma metadata.</summary>
    public IccVideoCardGamma? VideoCardGamma;
    /// <summary>Gets or sets optional decoded Microsoft hardware-calibration metadata.</summary>
    public IccMicrosoftHardwareCalibration? MicrosoftHardwareCalibration;

    /// <summary>Initializes an empty vendor-tag description.</summary>
    public IccVendorTagData()
    {
        VendorName = string.Empty;
        FormatName = string.Empty;
        TagSignature = 0;
        TypeSignature = 0;
        VideoCardGamma = null;
        MicrosoftHardwareCalibration = null;
    }
}
