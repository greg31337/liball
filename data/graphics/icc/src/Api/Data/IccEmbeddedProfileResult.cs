namespace icc;

/// <summary>Reports an embedded-profile discovery or mutation operation.</summary>
public class IccEmbeddedProfileResult
{
    /// <summary>Gets or sets the requested operation.</summary>
    public IccEmbeddedOperation Operation;
    /// <summary>Gets or sets operation options.</summary>
    public IccEmbeddingOptions Options;
    /// <summary>Gets or sets profiles found or produced.</summary>
    public IccEmbeddedProfile[] Profiles;
    /// <summary>Gets or sets updated carrier bytes when produced.</summary>
    public byte[] CarrierBytes;
    /// <summary>Gets or sets the operation outcome.</summary>
    public IccOperationResult Result;

    /// <summary>Initializes an empty detection result.</summary>
    public IccEmbeddedProfileResult()
    {
        Operation = IccEmbeddedOperation.Detect;
        Options = new IccEmbeddingOptions();
        Profiles = new IccEmbeddedProfile[0];
        CarrierBytes = new byte[0];
        Result = new IccOperationResult();
    }
}
