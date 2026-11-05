namespace sip;

/// <summary>Parses, validates, and formats generic SIP header fields.</summary>
public class SipHeaderCodec : ISipHeaderCodec
{
    /// <summary>Initializes a SIP header codec.</summary>
    public SipHeaderCodec()
    {
    }

    /// <summary>Parses one complete SIP header field.</summary>
    /// <param name="source">The complete header field bytes.</param>
    /// <param name="options">The parse compatibility and limit options.</param>
    /// <returns>The generic parse outcome.</returns>
    public SipHeaderParseResult ParseHeader(byte[] source, SipParseOptions options)
    {
        return SipHeaderParser.ParseHeaderBytes(source, options);
    }

    /// <summary>Validates one generic SIP header field.</summary>
    /// <param name="header">The header field to validate.</param>
    /// <returns>The validation issues.</returns>
    public SipValidationIssue[] ValidateHeader(SipHeader header)
    {
        return SipHeaderValidator.ValidateHeaderModel(header);
    }

    /// <summary>Formats one generic SIP header field.</summary>
    /// <param name="header">The header field to format.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The formatted header field bytes.</returns>
    public SipWriteResult FormatHeader(SipHeader header, SipWriteOptions options)
    {
        return SipHeaderFormatter.FormatHeaderModel(header, options);
    }

    /// <summary>Finds generic header fields by case-insensitive field name.</summary>
    /// <param name="message">The message containing the header fields.</param>
    /// <param name="name">The header field name to find.</param>
    /// <returns>The matching fields in message order.</returns>
    public SipHeader[] FindHeaders(SipMessage message, string name)
    {
        return SipGenericValueAccess.FindMessageHeaders(message, name);
    }

    /// <summary>Finds generic parameters by case-insensitive parameter name.</summary>
    /// <param name="parameters">The parameter collection to search.</param>
    /// <param name="name">The parameter name to find.</param>
    /// <returns>The matching parameters in collection order.</returns>
    public SipUriParameter[] FindParameters(SipUriParameter[] parameters, string name)
    {
        return SipGenericValueAccess.FindNamedParameters(parameters, name);
    }
}
