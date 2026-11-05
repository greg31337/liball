namespace sip;

/// <summary>
/// Parses, validates, and formats MIME entities carried by SIP.
/// </summary>
public interface ISipMimeCodec
{
    /// <summary>Parses one MIME entity, including multipart and sipfrag bodies.</summary>
    /// <param name="source">The MIME entity bytes.</param>
    /// <param name="options">The parse compatibility and limit options.</param>
    /// <returns>The parse outcome and parsed MIME entity.</returns>
    SipParseResult ParseMimeEntity(byte[] source, SipParseOptions options);

    /// <summary>Validates one MIME entity.</summary>
    /// <param name="entity">The MIME entity to validate.</param>
    /// <returns>The validation issues.</returns>
    SipValidationIssue[] ValidateMimeEntity(SipMimeEntity entity);

    /// <summary>Formats one MIME entity.</summary>
    /// <param name="entity">The MIME entity to format.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write outcome and formatted bytes.</returns>
    SipWriteResult FormatMimeEntity(SipMimeEntity entity, SipWriteOptions options);
}
