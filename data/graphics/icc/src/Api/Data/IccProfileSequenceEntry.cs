namespace icc;

/// <summary>Describes one profile in a profile sequence.</summary>
public class IccProfileSequenceEntry
{
    /// <summary>Gets or sets the device manufacturer signature.</summary>
    public uint DeviceManufacturerSignature;
    /// <summary>Gets or sets the device model signature.</summary>
    public uint DeviceModelSignature;
    /// <summary>Gets or sets device attributes.</summary>
    public ulong DeviceAttributes;
    /// <summary>Gets or sets the technology signature.</summary>
    public uint TechnologySignature;
    /// <summary>Gets or sets the manufacturer description.</summary>
    public IccTextData ManufacturerDescription;
    /// <summary>Gets or sets all localized manufacturer descriptions for an ICC version 4 sequence.</summary>
    public IccLocalizedString[] ManufacturerLocalizedDescriptions;
    /// <summary>Gets or sets the model description.</summary>
    public IccTextData ModelDescription;
    /// <summary>Gets or sets all localized model descriptions for an ICC version 4 sequence.</summary>
    public IccLocalizedString[] ModelLocalizedDescriptions;

    /// <summary>Initializes an empty profile-sequence entry.</summary>
    public IccProfileSequenceEntry()
    {
        DeviceManufacturerSignature = 0;
        DeviceModelSignature = 0;
        DeviceAttributes = 0;
        TechnologySignature = 0;
        ManufacturerDescription = new IccTextData();
        ManufacturerLocalizedDescriptions = new IccLocalizedString[0];
        ModelDescription = new IccTextData();
        ModelLocalizedDescriptions = new IccLocalizedString[0];
    }
}
