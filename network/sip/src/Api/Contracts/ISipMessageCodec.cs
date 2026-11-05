namespace sip;

/// <summary>
/// Parses, validates, measures, and serializes SIP messages.
/// </summary>
public interface ISipMessageCodec
{
    /// <summary>Incrementally parses the first SIP message from stream bytes.</summary>
    /// <param name="source">The source bytes.</param>
    /// <param name="options">The parse compatibility and limit options.</param>
    /// <returns>The parse outcome and parsed message.</returns>
    SipParseResult ParseStreamMessage(byte[] source, SipParseOptions options);

    /// <summary>Parses exactly one SIP message from a datagram.</summary>
    /// <param name="source">The complete datagram bytes.</param>
    /// <param name="options">The parse compatibility and limit options.</param>
    /// <returns>The parse outcome and parsed message.</returns>
    SipParseResult ParseDatagramMessage(byte[] source, SipParseOptions options);

    /// <summary>Validates the semantic consistency of a SIP message.</summary>
    /// <param name="message">The message to validate.</param>
    /// <returns>The validation issues.</returns>
    SipValidationIssue[] ValidateMessage(SipMessage message);

    /// <summary>Measures the serialized size of a SIP message.</summary>
    /// <param name="message">The message to measure.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The required number of bytes.</returns>
    int MeasureMessage(SipMessage message, SipWriteOptions options);

    /// <summary>Serializes one SIP message.</summary>
    /// <param name="message">The message to serialize.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write outcome and serialized bytes.</returns>
    SipWriteResult SerializeMessage(SipMessage message, SipWriteOptions options);
}
