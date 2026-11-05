namespace raw;

/// <summary>Contains optional format-specific inspection results without placing vendor fields directly on <see cref="RawDocument"/>.</summary>
public class RawFormatDetails
{
    /// <summary>The parsed CR2 header, or <c>null</c> for non-CR2 inputs.</summary>
    public RawCr2Header? Cr2Header;
    /// <summary>Profile-qualified Canon sensor metadata, or <c>null</c> for non-Canon or unmatched inputs.</summary>
    public RawCanonCr2Metadata? Canon;
    /// <summary>Profile-qualified Nikon sensor metadata, or <c>null</c> for non-Nikon or unmatched inputs.</summary>
    public RawNikonNefMetadata? Nikon;

    /// <summary>Creates an empty format-details model.</summary>
    public RawFormatDetails()
    {
        Cr2Header = null;
        Canon = null;
        Nikon = null;
    }
}
