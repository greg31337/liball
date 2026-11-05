namespace asn1;

/// <summary>
/// Contains all inputs for one schema-directed encode operation.
/// </summary>
public class AsnEncodeRequest
{
    /// <summary>
    /// Contains the qualified schema type name.
    /// </summary>
    public string TypeName = "";

    /// <summary>
    /// Contains the runtime value to encode when supplied.
    /// </summary>
    public AsnRuntimeValue? Value;

    /// <summary>
    /// Contains codec options when supplied.
    /// </summary>
    public AsnCodecOptions? Options;
}
