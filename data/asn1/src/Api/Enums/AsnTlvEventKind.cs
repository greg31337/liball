namespace asn1;

/// <summary>
/// Identifies a schema-less BER TLV reader event.
/// </summary>
public enum AsnTlvEventKind
{
    /// <summary>
    /// Represents end of input.
    /// </summary>
    EndOfInput = 0,

    /// <summary>
    /// Represents primitive.
    /// </summary>
    Primitive = 1,

    /// <summary>
    /// Represents start constructed.
    /// </summary>
    StartConstructed = 2,

    /// <summary>
    /// Represents end constructed.
    /// </summary>
    EndConstructed = 3
}
