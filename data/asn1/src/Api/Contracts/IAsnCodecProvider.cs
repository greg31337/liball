namespace asn1;

/// <summary>
/// Creates configured schema-driven codecs.
/// </summary>
public interface IAsnCodecProvider
{
    /// <summary>
    /// Creates a codec for a reflection catalog and options snapshot.
    /// </summary>
    /// <param name="catalog">The compiled reflection catalog.</param>
    /// <param name="options">The codec options.</param>
    /// <returns>The configured codec.</returns>
    public IAsnCodec CreateCodec(AsnReflectionCatalog catalog, AsnCodecOptions options);
}
