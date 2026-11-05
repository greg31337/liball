namespace sip;

/// <summary>
/// Parses, validates, and formats generic and typed SIP header field values.
/// </summary>
public interface ISipHeaderCodec
{
    /// <summary>Parses one complete SIP header field.</summary>
    /// <param name="source">The complete header field bytes.</param>
    /// <param name="options">The parse compatibility and limit options.</param>
    /// <returns>The generic and typed parse outcome.</returns>
    SipHeaderParseResult ParseHeader(byte[] source, SipParseOptions options);

    /// <summary>Validates one generic SIP header field.</summary>
    /// <param name="header">The header field to validate.</param>
    /// <returns>The validation issues.</returns>
    SipValidationIssue[] ValidateHeader(SipHeader header);

    /// <summary>Formats one generic SIP header field.</summary>
    /// <param name="header">The header field to format.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write outcome and formatted bytes.</returns>
    SipWriteResult FormatHeader(SipHeader header, SipWriteOptions options);

    /// <summary>Finds generic header fields by case-insensitive field name.</summary>
    /// <param name="message">The message containing the header fields.</param>
    /// <param name="name">The header field name to find.</param>
    /// <returns>The matching fields in message order.</returns>
    SipHeader[] FindHeaders(SipMessage message, string name);

    /// <summary>Finds generic parameters by case-insensitive parameter name.</summary>
    /// <param name="parameters">The parameter collection to search.</param>
    /// <param name="name">The parameter name to find.</param>
    /// <returns>The matching parameters in collection order.</returns>
    SipUriParameter[] FindParameters(SipUriParameter[] parameters, string name);
}
