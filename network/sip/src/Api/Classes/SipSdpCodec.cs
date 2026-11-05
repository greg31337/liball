namespace sip;

/// <summary>Parses, validates, and formats Session Description Protocol bodies.</summary>
public class SipSdpCodec : ISipSdpCodec
{
    /// <summary>Initializes a SIP SDP codec.</summary>
    public SipSdpCodec()
    {
    }

    /// <summary>Parses one SDP session description.</summary>
    /// <param name="source">The SDP bytes.</param>
    /// <param name="options">The parse compatibility and limit options.</param>
    /// <returns>The parse outcome and parsed session description.</returns>
    public SipParseResult ParseSessionDescription(byte[] source, SipParseOptions options)
    {
        return SipSdpParser.ParseDescriptionBytes(source, options);
    }

    /// <summary>Validates one SDP session description.</summary>
    /// <param name="description">The session description to validate.</param>
    /// <returns>The validation issues.</returns>
    public SipValidationIssue[] ValidateSessionDescription(SipSessionDescription description)
    {
        return SipSdpValidator.ValidateDescriptionModel(description, true);
    }

    /// <summary>Formats one SDP session description.</summary>
    /// <param name="description">The session description to format.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write outcome and formatted bytes.</returns>
    public SipWriteResult FormatSessionDescription(SipSessionDescription description, SipWriteOptions options)
    {
        return SipSdpFormatter.FormatDescriptionModel(description, options);
    }
}
