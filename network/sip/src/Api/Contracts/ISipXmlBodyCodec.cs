namespace sip;

/// <summary>
/// Securely parses, validates, and formats supported SIP XML body families.
/// </summary>
public interface ISipXmlBodyCodec
{
    /// <summary>Parses one XML body with external entities disabled.</summary>
    /// <param name="source">The XML body bytes.</param>
    /// <param name="mediaType">The XML media type.</param>
    /// <param name="options">The parse compatibility and limit options.</param>
    /// <returns>The parse outcome and parsed XML body.</returns>
    SipParseResult ParseXmlBody(byte[] source, SipMediaType mediaType, SipParseOptions options);

    /// <summary>Validates one parsed XML body.</summary>
    /// <param name="body">The XML body to validate.</param>
    /// <returns>The validation issues.</returns>
    SipValidationIssue[] ValidateXmlBody(SipXmlBody body);

    /// <summary>Formats one XML body.</summary>
    /// <param name="body">The XML body to format.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write outcome and formatted bytes.</returns>
    SipWriteResult FormatXmlBody(SipXmlBody body, SipWriteOptions options);
}
