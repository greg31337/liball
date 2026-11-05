namespace asn1;

/// <summary>
/// Contains normalized value, encoded output, and diagnostics from one transcode operation.
/// </summary>
public class AsnTranscodeResult
{
    /// <summary>
    /// Indicates whether transcoding completed without losing source information.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Contains the source encoding rules.
    /// </summary>
    public AsnEncodingRules SourceEncodingRules;

    /// <summary>
    /// Contains the requested target encoding rules.
    /// </summary>
    public AsnEncodingRules TargetEncodingRules;

    /// <summary>
    /// Contains the normalized decoded value when source decoding succeeded.
    /// </summary>
    public AsnRuntimeValue? Value;

    /// <summary>
    /// Contains the transcoded bytes when Success is true.
    /// </summary>
    public byte[] Data = [];

    /// <summary>
    /// Contains the exact output byte count when Success is true.
    /// </summary>
    public int WrittenCount;

    /// <summary>
    /// Contains source decoding, transfer preflight, or target encoding diagnostics.
    /// </summary>
    public AsnDiagnostic[] Diagnostics = [];
}
