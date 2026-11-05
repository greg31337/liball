namespace sip;

/// <summary>Securely parses, validates, and formats supported SIP event bodies.</summary>
public class SipXmlBodyCodec : ISipXmlBodyCodec
{
    /// <summary>Initializes a SIP XML event body codec.</summary>
    public SipXmlBodyCodec()
    {
    }

    /// <summary>Parses one XML or message-summary event body.</summary>
    /// <param name="source">The event body bytes.</param>
    /// <param name="mediaType">The event body media type.</param>
    /// <param name="options">The parse compatibility and limit options.</param>
    /// <returns>The parse outcome and decoded event body.</returns>
    public SipParseResult ParseXmlBody(byte[] source, SipMediaType mediaType, SipParseOptions options)
    {
        return SipXmlBodyParser.ParseBodyBytes(source, mediaType, options);
    }

    /// <summary>Validates one parsed XML or message-summary event body.</summary>
    /// <param name="body">The event body to validate.</param>
    /// <returns>The validation issues.</returns>
    public SipValidationIssue[] ValidateXmlBody(SipXmlBody body)
    {
        return SipXmlBodyValidator.ValidateBodyModel(body, true);
    }

    /// <summary>Formats one XML or message-summary event body.</summary>
    /// <param name="body">The event body to format.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write outcome and formatted bytes.</returns>
    public SipWriteResult FormatXmlBody(SipXmlBody body, SipWriteOptions options)
    {
        return SipXmlBodyFormatter.FormatBodyModel(body, options);
    }
}
