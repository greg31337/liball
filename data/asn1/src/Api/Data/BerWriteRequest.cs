namespace asn1;

/// <summary>
/// Contains all inputs for one schema-less BER TLV write operation.
/// </summary>
public class BerWriteRequest
{
    /// <summary>
    /// Contains the TLV tree to write when supplied.
    /// </summary>
    public BerTlv? Tlv;

    /// <summary>
    /// Contains TLV events to write when supplied.
    /// </summary>
    public BerTlvEvent[] Events = [];

    /// <summary>
    /// Selects BER, DER, or CER output rules.
    /// </summary>
    public AsnEncodingRules EncodingRules;
}
