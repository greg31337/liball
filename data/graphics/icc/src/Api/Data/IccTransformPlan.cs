namespace icc;

/// <summary>Describes an immutable-ready compiled transform plan.</summary>
public class IccTransformPlan
{
    /// <summary>Gets or sets transform options.</summary>
    public IccTransformOptions Options;
    /// <summary>Gets or sets the source channel count.</summary>
    public int SourceChannels;
    /// <summary>Gets or sets the destination channel count.</summary>
    public int DestinationChannels;
    /// <summary>Gets or sets ordered transform stages.</summary>
    public IccTransformStage[] Stages;
    /// <summary>Gets or sets an optional execution trace.</summary>
    public IccTransformTrace? Trace;
    /// <summary>Gets or sets the compilation outcome.</summary>
    public IccOperationResult Result;
    /// <summary>Gets or sets optional device-link construction options.</summary>
    public IccProfileLinkOptions? LinkOptions;
    /// <summary>Gets or sets optional proofing configuration.</summary>
    public IccProofingOptions? ProofingOptions;
    /// <summary>Gets or sets the separately compiled gamut-check stages.</summary>
    public IccTransformStage[] GamutStages;
    /// <summary>Gets or sets the source colour-space signature for a direct device link.</summary>
    public uint SourceColorSpaceSignature;
    /// <summary>Gets or sets the destination colour-space signature for a direct device link.</summary>
    public uint DestinationColorSpaceSignature;
    /// <summary>Gets or sets DeviceLink profile-sequence descriptions.</summary>
    public IccProfileSequenceEntry[] ProfileSequence;
    /// <summary>Gets or sets DeviceLink profile-sequence identifiers.</summary>
    public IccProfileSequenceIdentifier[] ProfileSequenceIdentifiers;

    /// <summary>Initializes an empty transform plan.</summary>
    public IccTransformPlan()
    {
        Options = new IccTransformOptions();
        SourceChannels = 0;
        DestinationChannels = 0;
        Stages = new IccTransformStage[0];
        Trace = null;
        Result = new IccOperationResult();
        LinkOptions = null;
        ProofingOptions = null;
        GamutStages = new IccTransformStage[0];
        SourceColorSpaceSignature = 0;
        DestinationColorSpaceSignature = 0;
        ProfileSequence = new IccProfileSequenceEntry[0];
        ProfileSequenceIdentifiers = new IccProfileSequenceIdentifier[0];
    }
}
