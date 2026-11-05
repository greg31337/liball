namespace asn1;

/// <summary>
/// Exposes configured codec creation through a composed provider implementation.
/// </summary>
public class AsnCodecProvider : IAsnCodecProvider
{
    internal IAsnCodecProvider Implementation;

    /// <summary>
    /// Initializes a codec-provider facade over an operation implementation.
    /// </summary>
    /// <param name="implementation">The implementation that creates configured codecs.</param>
    public AsnCodecProvider(IAsnCodecProvider implementation)
    {
        this.Implementation = implementation == null ? throw new System.ArgumentNullException("implementation") : implementation;
    }

    /// <summary>
    /// Creates a codec for a reflection catalog and options snapshot.
    /// </summary>
    /// <param name="catalog">The reflection catalog.</param>
    /// <param name="options">The codec options.</param>
    /// <returns>The configured codec.</returns>
    public IAsnCodec CreateCodec(AsnReflectionCatalog catalog, AsnCodecOptions options)
    {
        return this.Implementation.CreateCodec(catalog, options);
    }
}
