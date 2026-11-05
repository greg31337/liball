using System;

namespace uri;

/// <summary>
/// Provides strict, bounded RFC 8141 URN parsing with optional namespace validation.
/// </summary>
public class UrnParser : IUrnParser
{
    /// <summary>Initializes a stateless URN parser.</summary>
    public UrnParser()
    {
    }

    /// <summary>Parses a URN and throws when parsing fails.</summary>
    /// <param name="text">The complete URN text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsed URN model.</returns>
    /// <exception cref="UrnSyntaxException">The input, namespace rules, or configured limits are invalid.</exception>
    public Urn ParseUrn(string text, UrnParseOptions options)
    {
        UrnParseResult result = TryParseUrn(text, options);
        if (!result.Success)
        {
            throw CreateSyntaxException(result.Diagnostic);
        }

        return result.Urn;
    }

    /// <summary>Parses a URN without throwing for expected failure.</summary>
    /// <param name="text">The complete URN text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    public UrnParseResult TryParseUrn(
        string text,
        UrnParseOptions options)
    {
        return ParseNullableText(text, options);
    }

    internal UrnParseResult ParseNullableText(
        string? text,
        UrnParseOptions? options)
    {
        if (text == null)
        {
            return CreateParseFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn));
        }

        return ParseSpanWithOptions(text.AsSpan(), options);
    }

    /// <summary>Parses a URN span without requiring a source string.</summary>
    /// <param name="text">The complete URN span.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    public UrnParseResult TryParseUrnSpan(
        ReadOnlySpan<char> text,
        UrnParseOptions options)
    {
        return ParseSpanWithOptions(text, options);
    }

    internal UrnParseResult ParseSpanWithOptions(
        ReadOnlySpan<char> text,
        UrnParseOptions? options)
    {
        UriDiagnostic? precondition = CheckParsePreconditions(
            text.Length,
            options);
        if (precondition != null || options == null)
        {
            UriDiagnostic failure = precondition ??
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn);
            return CreateParseFailure(failure);
        }

        UrnParseResult result = UrnParserCore.ParseUrn(
            text,
            options.ValidationMode);
        if (!result.Success)
        {
            return result;
        }

        UriDiagnostic? componentLimit =
            UrnLocatorCore.GetComponentLimitDiagnostic(
                result.Urn,
                options.Limits);
        if (componentLimit != null)
        {
            return CreateParseFailure(componentLimit);
        }

        UriDiagnostic? namespaceDiagnostic =
            GetNamespaceValidationDiagnostic(result.Urn, options);
        if (namespaceDiagnostic != null)
        {
            return CreateParseFailure(namespaceDiagnostic);
        }

        return result;
    }

    internal UriDiagnostic? CheckParsePreconditions(
        int inputLength,
        UrnParseOptions? options)
    {
        if (options == null || options.Limits == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn);
        }

        return LimitGuard.CheckInputCharacters(
            inputLength,
            options.Limits,
            DiagnosticDomain.Urn);
    }

    internal UriDiagnostic? GetNamespaceValidationDiagnostic(
        Urn urn,
        UrnParseOptions options)
    {
        if (options.NamespaceHandler == null)
        {
            return null;
        }

        string namespaceIdentifier =
            UrnComparerCore.NormalizeNamespaceIdentifier(
                urn.NamespaceIdentifier);
        return options.NamespaceHandler.ValidateNamespaceSpecificString(
            namespaceIdentifier,
            urn.NamespaceSpecificString);
    }

    internal UrnParseResult CreateParseFailure(UriDiagnostic diagnostic)
    {
        return new UrnParseResult(false, new Urn(), diagnostic);
    }

    internal UrnSyntaxException CreateSyntaxException(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = diagnostic ??
            DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn);
        return new UrnSyntaxException(failure);
    }
}
