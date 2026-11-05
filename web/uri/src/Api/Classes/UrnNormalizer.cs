namespace uri;

/// <summary>
/// Provides bounded generic and explicit namespace-specific URN normalization.
/// </summary>
public class UrnNormalizer : IUrnNormalizer
{
    /// <summary>Initializes a stateless URN normalizer.</summary>
    public UrnNormalizer()
    {
    }

    /// <summary>Normalizes a complete URN while preserving its optional components.</summary>
    /// <param name="text">The complete URN text.</param>
    /// <param name="options">The validation, namespace-handler, and processing options.</param>
    /// <returns>The normalized complete URN text.</returns>
    /// <exception cref="UrnEquivalenceException">Validation, normalization, or a configured limit fails.</exception>
    public string NormalizeUrn(string text, UrnParseOptions options)
    {
        return NormalizeAtLevel(text, options, false);
    }

    /// <summary>Normalizes and returns only the assigned-name portion of a URN.</summary>
    /// <param name="text">The complete URN text.</param>
    /// <param name="options">The validation, namespace-handler, and processing options.</param>
    /// <returns>The normalized assigned-name text without optional components.</returns>
    /// <exception cref="UrnEquivalenceException">Validation, normalization, or a configured limit fails.</exception>
    public string NormalizeAssignedName(
        string text,
        UrnParseOptions options)
    {
        return NormalizeAtLevel(text, options, true);
    }

    internal string NormalizeAtLevel(
        string? text,
        UrnParseOptions? options,
        bool assignedNameOnly)
    {
        UrnParser parser = new UrnParser();
        UrnParseResult parsed = parser.ParseNullableText(text, options);
        if (!parsed.Success || options == null)
        {
            throw CreateEquivalenceException(parsed.Diagnostic);
        }

        UrnParseResult prepared = PrepareNormalizationModel(
            parsed.Urn,
            options);
        if (!prepared.Success)
        {
            throw CreateEquivalenceException(prepared.Diagnostic);
        }

        UriTextResult normalized;
        if (assignedNameOnly)
        {
            normalized = UrnNormalizerCore.NormalizeAssignedName(
                prepared.Urn,
                options.Limits);
        }
        else
        {
            normalized = UrnNormalizerCore.NormalizeUrn(
                prepared.Urn,
                options.Limits);
        }

        if (!normalized.Success)
        {
            throw CreateEquivalenceException(normalized.Diagnostic);
        }

        return normalized.Text;
    }

    internal UrnParseResult PrepareNormalizationModel(
        Urn urn,
        UrnParseOptions options)
    {
        if (options.NamespaceHandler == null)
        {
            return new UrnParseResult(true, urn, null);
        }

        string namespaceIdentifier =
            UrnComparerCore.NormalizeNamespaceIdentifier(
                urn.NamespaceIdentifier);
        UriTextResult normalized =
            options.NamespaceHandler.NormalizeNamespaceSpecificString(
                namespaceIdentifier,
                urn.NamespaceSpecificString);
        if (normalized == null ||
            !normalized.Success ||
            normalized.Text == null ||
            normalized.Length != normalized.Text.Length ||
            normalized.Diagnostic != null)
        {
            UriDiagnostic? diagnostic = null;
            if (normalized != null)
            {
                diagnostic = normalized.Diagnostic;
            }

            UriDiagnostic failure = diagnostic ??
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn);
            return new UrnParseResult(false, new Urn(), failure);
        }

        Urn prepared = CopyUrn(urn);
        prepared.NamespaceSpecificString = normalized.Text;
        UriDiagnostic? componentLimit =
            UrnLocatorCore.GetComponentLimitDiagnostic(
                prepared,
                options.Limits);
        if (componentLimit != null)
        {
            return new UrnParseResult(
                false,
                new Urn(),
                componentLimit);
        }

        return new UrnParseResult(true, prepared, null);
    }

    internal Urn CopyUrn(Urn source)
    {
        Urn copy = new Urn();
        copy.Scheme = source.Scheme;
        copy.NamespaceIdentifier = source.NamespaceIdentifier;
        copy.NamespaceSpecificString = source.NamespaceSpecificString;
        copy.AssignedName = source.AssignedName;
        copy.HasRComponent = source.HasRComponent;
        copy.RComponent = source.RComponent;
        copy.HasQComponent = source.HasQComponent;
        copy.QComponent = source.QComponent;
        copy.HasFComponent = source.HasFComponent;
        copy.FComponent = source.FComponent;
        copy.OriginalText = source.OriginalText;
        return copy;
    }

    internal UrnEquivalenceException CreateEquivalenceException(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = diagnostic ??
            DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn);
        return new UrnEquivalenceException(failure);
    }
}
