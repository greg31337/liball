namespace asn1;

/// <summary>
/// Creates schema-driven codecs using the built-in runtime implementations.
/// </summary>
public class DefaultAsnCodecProvider : IAsnCodecProvider
{
    /// <summary>
    /// Initializes the built-in codec provider.
    /// </summary>
    public DefaultAsnCodecProvider()
    {
    }

    /// <summary>
    /// Creates a codec for a reflection catalog and options snapshot.
    /// </summary>
    /// <param name="catalog">The compiled reflection catalog.</param>
    /// <param name="options">The codec options.</param>
    /// <returns>The configured built-in codec.</returns>
    public IAsnCodec CreateCodec(
        AsnReflectionCatalog catalog,
        AsnCodecOptions options)
    {
        return new CodecProviderImplementation().CreateCodec(catalog, options);
    }
}
