namespace sip;

/// <summary>
/// Parses, validates, compares, normalizes, and formats SIP-carried URIs.
/// </summary>
public interface ISipUriCodec
{
    /// <summary>Parses one SIP, SIPS, TEL, or extension absolute URI.</summary>
    /// <param name="source">The URI bytes.</param>
    /// <param name="options">The parse compatibility and limit options.</param>
    /// <returns>The parse outcome and parsed URI.</returns>
    SipParseResult ParseUri(byte[] source, SipParseOptions options);

    /// <summary>Validates one URI.</summary>
    /// <param name="uri">The URI to validate.</param>
    /// <returns>The validation issues.</returns>
    SipValidationIssue[] ValidateUri(SipUri uri);

    /// <summary>Compares two URIs using SIP URI comparison rules.</summary>
    /// <param name="left">The first URI.</param>
    /// <param name="right">The second URI.</param>
    /// <returns>True when the URIs are equivalent.</returns>
    bool CompareUris(SipUri left, SipUri right);

    /// <summary>Produces a normalized copy of one URI.</summary>
    /// <param name="uri">The URI to normalize.</param>
    /// <returns>The normalized URI.</returns>
    SipUri NormalizeUri(SipUri uri);

    /// <summary>Formats one URI.</summary>
    /// <param name="uri">The URI to format.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write outcome and formatted bytes.</returns>
    SipWriteResult FormatUri(SipUri uri, SipWriteOptions options);
}
