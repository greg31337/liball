namespace sip;

/// <summary>
/// Parses, validates, and formats Session Description Protocol bodies.
/// </summary>
public interface ISipSdpCodec
{
    /// <summary>Parses one SDP session description.</summary>
    /// <param name="source">The SDP bytes.</param>
    /// <param name="options">The parse compatibility and limit options.</param>
    /// <returns>The parse outcome and parsed session description.</returns>
    SipParseResult ParseSessionDescription(byte[] source, SipParseOptions options);

    /// <summary>Validates one SDP session description.</summary>
    /// <param name="description">The session description to validate.</param>
    /// <returns>The validation issues.</returns>
    SipValidationIssue[] ValidateSessionDescription(SipSessionDescription description);

    /// <summary>Formats one SDP session description.</summary>
    /// <param name="description">The session description to format.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write outcome and formatted bytes.</returns>
    SipWriteResult FormatSessionDescription(SipSessionDescription description, SipWriteOptions options);
}
