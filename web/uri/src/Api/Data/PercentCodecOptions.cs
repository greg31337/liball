namespace uri;

/// <summary>
/// Defines options shared by percent encoding and decoding operations.
/// </summary>
public class PercentCodecOptions
{
    /// <summary>The processing limits for the operation.</summary>
    public OperationLimits Limits;

    /// <summary>The ASCII character set that encoding preserves literally.</summary>
    public PercentEncodeSet EncodeSet;

    /// <summary>The policy used when decoded octets are not legal strict UTF-8.</summary>
    public PercentDecodeMode DecodeMode;

    /// <summary>Initializes percent-codec options with default processing limits.</summary>
    public PercentCodecOptions()
    {
        Limits = new OperationLimits();
        EncodeSet = PercentEncodeSet.Unreserved;
        DecodeMode = PercentDecodeMode.StrictUtf8;
    }
}
