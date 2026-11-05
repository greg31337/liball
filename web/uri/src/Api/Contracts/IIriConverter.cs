namespace uri;

/// <summary>
/// Converts between RFC 3987 IRI text, RFC 3986 URI text, and DNS host labels.
/// </summary>
public interface IIriConverter
{
    /// <summary>Maps an IRI reference to an ASCII URI reference.</summary>
    /// <param name="text">The complete IRI-reference text.</param>
    /// <param name="options">The conversion options.</param>
    /// <returns>The mapped URI-reference text.</returns>
    /// <exception cref="IriConversionException">Conversion or a configured limit fails.</exception>
    string ConvertIriToUri(string text, IriConversionOptions options);

    /// <summary>Maps an IRI reference to an ASCII URI reference without throwing.</summary>
    /// <param name="text">The complete IRI-reference text.</param>
    /// <param name="options">The conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    UriTextResult TryConvertIriToUri(string text, IriConversionOptions options);

    /// <summary>Converts safe UTF-8 percent sequences in a URI reference to IRI text.</summary>
    /// <param name="text">The complete URI-reference text.</param>
    /// <param name="options">The conversion options.</param>
    /// <returns>The converted IRI-reference text.</returns>
    /// <exception cref="IriConversionException">Conversion or a configured limit fails.</exception>
    string ConvertUriToIri(string text, IriConversionOptions options);

    /// <summary>Converts a DNS-oriented Unicode host to platform A-label form.</summary>
    /// <param name="host">The registered-name host text.</param>
    /// <param name="options">The conversion options and explicit IDN mode.</param>
    /// <returns>The ASCII host text.</returns>
    /// <exception cref="IriConversionException">IDN conversion or a configured limit fails.</exception>
    string ConvertHostToAscii(string host, IriConversionOptions options);

    /// <summary>Converts a DNS-oriented A-label host to platform Unicode form.</summary>
    /// <param name="host">The ASCII registered-name host text.</param>
    /// <param name="options">The conversion options and explicit IDN mode.</param>
    /// <returns>The Unicode host text.</returns>
    /// <exception cref="IriConversionException">IDN conversion or a configured limit fails.</exception>
    string ConvertHostToUnicode(string host, IriConversionOptions options);
}
