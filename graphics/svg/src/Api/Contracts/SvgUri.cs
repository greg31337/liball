namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgUri"/>.</summary>
public interface ISvgUri
{
    /// <summary>Normalizes one URI or Unicode IRI reference.</summary>
    /// <param name="value">The URI or IRI reference, which may be empty.</param>
    /// <returns>The normalized forms and resource cache key.</returns>
    SvgUriResult NormalizeWithValue(string value);
    /// <summary>Normalizes one URI or Unicode IRI reference with an explicit character bound.</summary>
    /// <param name="value">The URI or IRI reference, which may be empty.</param>
    /// <param name="maxCharacters">The maximum input and normalized character count.</param>
    /// <returns>The normalized forms or a stable failure.</returns>
    SvgUriResult NormalizeWithValueAndMaxCharacters(string? value, int maxCharacters);
    /// <summary>Resolves one URI or IRI reference against a base and normalizes the target.</summary>
    /// <param name="baseUri">The base URI or IRI, or an empty string when no base is available.</param>
    /// <param name="reference">The relative or absolute reference.</param>
    /// <returns>The normalized target and resource cache key.</returns>
    SvgUriResult ResolveWithBaseUriAndReference(string baseUri, string reference);
    /// <summary>Resolves one URI or IRI reference with an explicit character bound.</summary>
    /// <param name="baseUri">The base URI or IRI, or an empty string when no base is available.</param>
    /// <param name="reference">The relative or absolute reference.</param>
    /// <param name="maxCharacters">The maximum input and normalized character count.</param>
    /// <returns>The normalized target or a stable failure.</returns>
    SvgUriResult ResolveWithBaseUriAndReferenceAndMaxCharacters(string? baseUri, string? reference, int maxCharacters);
    /// <summary>Escapes an IRI as an ASCII URI using UTF-8 percent encoding.</summary>
    /// <param name="value">The URI or IRI reference.</param>
    /// <returns>The normalized ASCII URI, or an empty string for invalid input.</returns>
    string EscapeIri(string value);
    /// <summary>Decodes a valid percent-encoded fragment as UTF-8.</summary>
    /// <param name="fragment">Fragment text with or without a leading number sign.</param>
    /// <returns>The decoded fragment, or an empty string for malformed input.</returns>
    string DecodeFragment(string? fragment);
    /// <summary>Creates a fragment-free normalized ASCII cache key for a resolved resource.</summary>
    /// <param name="baseUri">The base URI or IRI, or an empty string.</param>
    /// <param name="reference">The resource reference.</param>
    /// <returns>The cache key, or an empty string for invalid input.</returns>
    string GetCacheKey(string baseUri, string reference);
}
