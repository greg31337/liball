namespace cbor;

/// <summary>
/// Selects how JSON strings and preserved byte wrappers are converted to CBOR.
/// </summary>
public enum CborJsonByteStringConvention
{
    /// <summary>Converts ordinary JSON strings to CBOR text strings.</summary>
    TextString = 0,
    /// <summary>Decodes every ordinary JSON string as unpadded base64url bytes.</summary>
    Base64UrlString = 1,
    /// <summary>Converts ordinary strings to text and recognizes preserved byte wrappers.</summary>
    PreserveWrapper = 2
}
