namespace sip;

/// <summary>Parses, validates, and formats MIME entities carried by SIP.</summary>
public class SipMimeCodec : ISipMimeCodec
{
    /// <summary>Initializes a SIP MIME codec.</summary>
    public SipMimeCodec()
    {
    }

    /// <summary>Parses one MIME entity, including multipart and sipfrag bodies.</summary>
    /// <param name="source">The MIME entity bytes.</param>
    /// <param name="options">The parse compatibility and limit options.</param>
    /// <returns>The parse outcome and parsed MIME entity.</returns>
    public SipParseResult ParseMimeEntity(byte[] source, SipParseOptions options)
    {
        return SipMimeParser.ParseEntityBytes(source, options);
    }

    /// <summary>Validates one MIME entity.</summary>
    /// <param name="entity">The MIME entity to validate.</param>
    /// <returns>The validation issues.</returns>
    public SipValidationIssue[] ValidateMimeEntity(SipMimeEntity entity)
    {
        return SipMimeValidator.ValidateEntityModel(entity, true);
    }

    /// <summary>Formats one MIME entity.</summary>
    /// <param name="entity">The MIME entity to format.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write outcome and formatted bytes.</returns>
    public SipWriteResult FormatMimeEntity(SipMimeEntity entity, SipWriteOptions options)
    {
        return SipMimeFormatter.FormatEntityModel(entity, options);
    }
}
