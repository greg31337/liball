namespace sip;

/// <summary>Parses, validates, compares, normalizes, and formats SIP-carried URIs.</summary>
public class SipUriCodec : ISipUriCodec
{
    /// <summary>Initializes a URI codec.</summary>
    public SipUriCodec()
    {
    }

    /// <summary>Parses one SIP, SIPS, TEL, or extension absolute URI.</summary>
    /// <param name="source">The URI bytes.</param>
    /// <param name="options">The parse compatibility and limit options.</param>
    /// <returns>The parse outcome and parsed URI.</returns>
    public SipParseResult ParseUri(byte[] source, SipParseOptions options)
    {
        return SipUriParser.ParseUriBytes(source, options);
    }

    /// <summary>Validates one URI.</summary>
    /// <param name="uri">The URI to validate.</param>
    /// <returns>The validation issues.</returns>
    public SipValidationIssue[] ValidateUri(SipUri uri)
    {
        return SipUriValidator.ValidateUriModel(uri);
    }

    /// <summary>Compares two URIs using the available URI comparison rules.</summary>
    /// <param name="left">The first URI.</param>
    /// <param name="right">The second URI.</param>
    /// <returns>True when the URIs are equivalent.</returns>
    public bool CompareUris(SipUri left, SipUri right)
    {
        return SipUriComparison.CompareUriModels(left, right);
    }

    /// <summary>Produces a normalized copy of one URI.</summary>
    /// <param name="uri">The URI to normalize.</param>
    /// <returns>The normalized URI.</returns>
    public SipUri NormalizeUri(SipUri uri)
    {
        return SipUriModelCopy.CreateNormalizedCopy(uri);
    }

    /// <summary>Formats one URI.</summary>
    /// <param name="uri">The URI to format.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write outcome and formatted bytes.</returns>
    public SipWriteResult FormatUri(SipUri uri, SipWriteOptions options)
    {
        return SipUriFormatter.FormatUriModel(uri, options);
    }
}
