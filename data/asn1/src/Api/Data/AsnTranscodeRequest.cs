namespace asn1;

/// <summary>
/// Contains all inputs for one schema-directed transcode operation.
/// </summary>
public class AsnTranscodeRequest
{
    /// <summary>
    /// Contains the qualified schema type name.
    /// </summary>
    public string TypeName = "";

    /// <summary>
    /// Contains the source encoded bytes.
    /// </summary>
    public byte[] Data = [];

    /// <summary>
    /// Contains the zero-based starting byte offset.
    /// </summary>
    public int Offset;

    /// <summary>
    /// Indicates whether Count limits the source input region.
    /// </summary>
    public bool HasCount;

    /// <summary>
    /// Contains the source input byte count when HasCount is true.
    /// </summary>
    public int Count;

    /// <summary>
    /// Contains source decode options when supplied.
    /// </summary>
    public AsnCodecOptions? SourceOptions;

    /// <summary>
    /// Contains the required target encoding options.
    /// </summary>
    public AsnCodecOptions? TargetOptions;
}
