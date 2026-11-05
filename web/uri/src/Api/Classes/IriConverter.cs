using System;

namespace uri;

/// <summary>
/// Provides bounded RFC 3987 IRI, RFC 3986 URI, and explicit IDN conversion.
/// </summary>
public class IriConverter : IIriConverter
{
    /// <summary>Initializes a stateless IRI converter.</summary>
    public IriConverter()
    {
    }

    /// <summary>Maps an IRI reference to an ASCII URI reference.</summary>
    /// <param name="text">The complete IRI-reference text.</param>
    /// <param name="options">The conversion options.</param>
    /// <returns>The mapped URI-reference text.</returns>
    /// <exception cref="IriConversionException">Conversion or a configured limit fails.</exception>
    public string ConvertIriToUri(string text, IriConversionOptions options)
    {
        UriTextResult result = TryConvertIriToUri(text, options);
        if (!result.Success)
        {
            throw CreateConversionException(result.Diagnostic);
        }

        return result.Text;
    }

    /// <summary>Maps an IRI reference to an ASCII URI reference without throwing.</summary>
    /// <param name="text">The complete IRI-reference text.</param>
    /// <param name="options">The conversion options.</param>
    /// <returns>The conversion outcome.</returns>
    public UriTextResult TryConvertIriToUri(
        string text,
        IriConversionOptions options)
    {
        UriDiagnostic? optionFailure = GetOptionsDiagnostic(options);
        if (optionFailure != null)
        {
            return IriConverterCore.CreateFailure(optionFailure);
        }

        if (text == null)
        {
            return IriConverterCore.CreateFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri));
        }

        UriTextResult prepared = IriNormalizerCore.NormalizeIri(
            text.AsSpan(),
            options.NormalizationMode,
            options.Limits);
        if (!prepared.Success)
        {
            return prepared;
        }

        return IriConverterCore.ConvertIriToUri(
            prepared.Text.AsSpan(),
            options.Limits);
    }

    /// <summary>Converts safe UTF-8 percent sequences in a URI reference to IRI text.</summary>
    /// <param name="text">The complete URI-reference text.</param>
    /// <param name="options">The conversion options.</param>
    /// <returns>The converted IRI-reference text.</returns>
    /// <exception cref="IriConversionException">Conversion or a configured limit fails.</exception>
    public string ConvertUriToIri(string text, IriConversionOptions options)
    {
        UriDiagnostic? optionFailure = GetOptionsDiagnostic(options);
        if (optionFailure != null)
        {
            throw new IriConversionException(optionFailure);
        }

        if (text == null)
        {
            throw new IriConversionException(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri));
        }

        UriTextResult converted = IriConverterCore.ConvertUriToIri(
            text.AsSpan(),
            options.Limits);
        if (!converted.Success)
        {
            throw CreateConversionException(converted.Diagnostic);
        }

        UriTextResult normalized = IriNormalizerCore.NormalizeIri(
            converted.Text.AsSpan(),
            options.NormalizationMode,
            options.Limits);
        if (!normalized.Success)
        {
            throw CreateConversionException(normalized.Diagnostic);
        }

        return normalized.Text;
    }

    /// <summary>Converts a DNS-oriented Unicode host to platform A-label form.</summary>
    /// <param name="host">The registered-name host text.</param>
    /// <param name="options">The conversion options and explicit IDN mode.</param>
    /// <returns>The ASCII host text.</returns>
    /// <exception cref="IriConversionException">IDN conversion or a configured limit fails.</exception>
    public string ConvertHostToAscii(
        string host,
        IriConversionOptions options)
    {
        UriDiagnostic? precondition = GetHostPrecondition(host, options);
        if (precondition != null)
        {
            throw new IriConversionException(precondition);
        }

        UriTextResult result = IdnHostCore.ConvertHostToAscii(
            host.AsSpan(),
            options.IdnMode,
            options.Limits);
        if (!result.Success)
        {
            throw CreateConversionException(result.Diagnostic);
        }

        return result.Text;
    }

    /// <summary>Converts a DNS-oriented A-label host to platform Unicode form.</summary>
    /// <param name="host">The ASCII registered-name host text.</param>
    /// <param name="options">The conversion options and explicit IDN mode.</param>
    /// <returns>The Unicode host text.</returns>
    /// <exception cref="IriConversionException">IDN conversion or a configured limit fails.</exception>
    public string ConvertHostToUnicode(
        string host,
        IriConversionOptions options)
    {
        UriDiagnostic? precondition = GetHostPrecondition(host, options);
        if (precondition != null)
        {
            throw new IriConversionException(precondition);
        }

        UriTextResult result = IdnHostCore.ConvertHostToUnicode(
            host.AsSpan(),
            options.IdnMode,
            options.Limits);
        if (!result.Success)
        {
            throw CreateConversionException(result.Diagnostic);
        }

        return result.Text;
    }

    internal static UriDiagnostic? GetOptionsDiagnostic(
        IriConversionOptions? options)
    {
        if (options == null || options.Limits == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri);
        }

        if (options.NormalizationMode != UnicodeNormalizationMode.Preserve &&
            options.NormalizationMode != UnicodeNormalizationMode.NormalizeNfc &&
            options.NormalizationMode != UnicodeNormalizationMode.DiagnoseNfc)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri);
        }

        if (options.IdnMode != IriIdnMode.None &&
            options.IdnMode != IriIdnMode.Platform)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri);
        }

        return null;
    }

    internal UriDiagnostic? GetHostPrecondition(
        string? host,
        IriConversionOptions? options)
    {
        UriDiagnostic? optionFailure = GetOptionsDiagnostic(options);
        if (optionFailure != null)
        {
            return optionFailure;
        }

        if (host == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri);
        }

        return null;
    }

    internal static IriConversionException CreateConversionException(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri);
        }
        else
        {
            failure = IriComponentScanner.MapDiagnostic(diagnostic);
        }

        return new IriConversionException(failure);
    }
}
