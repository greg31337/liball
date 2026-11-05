namespace raw;

/// <summary>Reports independently verified operations for a document or profile.</summary>
public class RawCapabilities
{
    /// <summary>Whether the format and basic identity can be recognized.</summary>
    public bool Recognized;
    /// <summary>Whether structure, metadata, previews, and payload descriptions can be inspected.</summary>
    public bool Inspected;
    /// <summary>Whether stored raw samples can be reconstructed.</summary>
    public bool Decoded;
    /// <summary>Whether a documented displayable or linear image can be rendered.</summary>
    public bool Rendered;
    /// <summary>Whether supported edits can preserve unknown and undecoded data.</summary>
    public bool PreservingRewrite;
    /// <summary>Whether a new payload and container can be emitted.</summary>
    public bool Encoded;
    /// <summary>Whether output has passed the compatibility checks recorded by its profile.</summary>
    public bool VendorConfirmed;

    /// <summary>Creates an independent capability set.</summary>
    /// <param name="recognized">Whether recognition is supported.</param>
    /// <param name="inspected">Whether inspection is supported.</param>
    /// <param name="decoded">Whether decoding is supported.</param>
    /// <param name="rendered">Whether rendering is supported.</param>
    /// <param name="preservingRewrite">Whether preserving rewrite is supported.</param>
    /// <param name="encoded">Whether new encoding is supported.</param>
    /// <param name="vendorConfirmed">Whether encoding is vendor-confirmed.</param>
    public RawCapabilities(bool recognized, bool inspected, bool decoded, bool rendered, bool preservingRewrite, bool encoded, bool vendorConfirmed)
    {
        Recognized = recognized;
        Inspected = inspected;
        Decoded = decoded;
        Rendered = rendered;
        PreservingRewrite = preservingRewrite;
        Encoded = encoded;
        VendorConfirmed = vendorConfirmed;
    }

    /// <summary>Returns whether a specific support level is available.</summary>
    /// <param name="level">The level to query.</param>
    /// <returns><c>true</c> when the level is independently available; otherwise <c>false</c>.</returns>
    internal bool Supports(RawSupportLevel level)
    {
        switch (level)
        {
            case RawSupportLevel.Recognized:
                return Recognized;
            case RawSupportLevel.Inspected:
                return Inspected;
            case RawSupportLevel.Decoded:
                return Decoded;
            case RawSupportLevel.Rendered:
                return Rendered;
            case RawSupportLevel.PreservingRewrite:
                return PreservingRewrite;
            case RawSupportLevel.Encoded:
                return Encoded;
            case RawSupportLevel.VendorConfirmed:
                return VendorConfirmed;
            default:
                return false;
        }
    }

    /// <summary>Returns the greatest available level in compatibility-contract order.</summary>
    /// <returns>The greatest available support level, or <see cref="RawSupportLevel.Unsupported"/>.</returns>
    internal RawSupportLevel GetHighestSupportLevel()
    {
        if (VendorConfirmed)
        {
            return RawSupportLevel.VendorConfirmed;
        }
        if (Encoded)
        {
            return RawSupportLevel.Encoded;
        }
        if (PreservingRewrite)
        {
            return RawSupportLevel.PreservingRewrite;
        }
        if (Rendered)
        {
            return RawSupportLevel.Rendered;
        }
        if (Decoded)
        {
            return RawSupportLevel.Decoded;
        }
        if (Inspected)
        {
            return RawSupportLevel.Inspected;
        }
        if (Recognized)
        {
            return RawSupportLevel.Recognized;
        }
        return RawSupportLevel.Unsupported;
    }
}
