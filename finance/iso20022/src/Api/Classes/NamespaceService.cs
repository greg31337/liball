using System;
using System.Collections.Generic;

namespace iso20022;

/// <summary>Provides explicit ISO 20022 namespace parsing, repair, classification, and construction.</summary>
public class NamespaceService : INamespaceService
{
    /// <summary>Initializes a namespace service.</summary>
    public NamespaceService()
    {
    }

    /// <summary>Parses a namespace URI using the requested strict or lenient policy.</summary>
    /// <param name="namespaceUri">The namespace URI.</param>
    /// <param name="mode">The namespace-processing mode.</param>
    /// <returns>The namespace result and any ordered repairs.</returns>
    public NamespaceServiceResult ParseNamespace(string? namespaceUri, NamespaceMode mode)
    {
        List<NamespaceRepair> repairs = new List<NamespaceRepair>();
        if (mode != NamespaceMode.Strict && mode != NamespaceMode.Lenient)
        {
            return CreateFailure("namespace.mode", "The namespace mode is not supported.", repairs);
        }

        if (namespaceUri == null)
        {
            return CreateFailure("namespace.null", "A namespace URI is required.", repairs);
        }

        string candidate = namespaceUri;
        if (mode == NamespaceMode.Lenient)
        {
            string trimmed = TrimXmlWhitespace(candidate);
            if (trimmed != candidate)
            {
                AddRepair(repairs, NamespaceRepairKind.TrimXmlWhitespace, candidate, trimmed);
                candidate = trimmed;
            }

            if (IsSchemaNamespaceWithTrailingSlash(candidate))
            {
                string withoutSlash = candidate.Substring(0, candidate.Length - 1);
                AddRepair(repairs, NamespaceRepairKind.RemoveTrailingSlash, candidate, withoutSlash);
                candidate = withoutSlash;
            }

            string prefixNormalized = NormalizeTechnicalPrefix(candidate);
            if (prefixNormalized != candidate)
            {
                AddRepair(repairs, NamespaceRepairKind.NormalizeTechnicalPrefixCase, candidate, prefixNormalized);
                candidate = prefixNormalized;
            }

            string areaNormalized = NormalizeBusinessArea(candidate);
            if (areaNormalized != candidate)
            {
                AddRepair(repairs, NamespaceRepairKind.NormalizeBusinessAreaCase, candidate, areaNormalized);
                candidate = areaNormalized;
            }
        }
        else if (HasTechnicalPrefixIgnoringCase(candidate) && !candidate.StartsWith(Iso20022Namespaces.SchemaUrnPrefix, StringComparison.Ordinal))
        {
            return CreateFailure("namespace.noncanonical", "The ISO 20022 technical namespace prefix does not use its registered casing.", repairs);
        }

        if (candidate == Iso20022Namespaces.Ecore2013 || candidate == Iso20022Namespaces.Ecore2013Extension)
        {
            return CreateSuccess(candidate, NamespaceKind.Repository, mode, null, repairs);
        }

        if (candidate.StartsWith(Iso20022Namespaces.SchemaUrnPrefix, StringComparison.Ordinal))
        {
            string identifierText = candidate.Substring(Iso20022Namespaces.SchemaUrnPrefix.Length);
            MessageIdentifier? identifier = MessageIdentifierLexical.ParseMessageIdentifier(identifierText.AsSpan());
            if (identifier == null)
            {
                return CreateFailure("namespace.message_identifier", "The ISO 20022 schema namespace does not contain a valid message identifier.", repairs);
            }

            return CreateSuccess(candidate, ClassifyIdentifier(identifier), mode, identifier, repairs);
        }

        if (!UriLexical.IsNamespaceUri(candidate.AsSpan()))
        {
            return CreateFailure("namespace.invalid_uri", "The namespace is not a valid non-empty URI reference.", repairs);
        }

        return CreateSuccess(candidate, NamespaceKind.Unknown, mode, null, repairs);
    }

    /// <summary>Constructs the canonical namespace for a message identifier.</summary>
    /// <param name="identifier">The message identifier.</param>
    /// <returns>The namespace result.</returns>
    public NamespaceServiceResult BuildNamespaceFromIdentifier(MessageIdentifier? identifier)
    {
        List<NamespaceRepair> repairs = new List<NamespaceRepair>();
        if (identifier == null)
        {
            return CreateFailure("namespace.identifier_null", "A message identifier is required.", repairs);
        }

        string? identifierText = MessageIdentifierLexical.FormatMessageIdentifier(identifier);
        if (identifierText == null)
        {
            return CreateFailure("namespace.identifier_invalid", "The message identifier components are invalid.", repairs);
        }

        if (identifier.Value.Length > 0 && identifier.Value != identifierText)
        {
            return CreateFailure("namespace.identifier_conflict", "The complete message identifier conflicts with its individual components.", repairs);
        }

        MessageIdentifier canonicalIdentifier = new MessageIdentifier();
        canonicalIdentifier.BusinessArea = identifier.BusinessArea;
        canonicalIdentifier.MessageFunctionality = identifier.MessageFunctionality;
        canonicalIdentifier.Variant = identifier.Variant;
        canonicalIdentifier.Version = identifier.Version;
        canonicalIdentifier.Value = identifierText;

        string namespaceUri = Iso20022Namespaces.SchemaUrnPrefix + identifierText;
        return CreateSuccess(namespaceUri, ClassifyIdentifier(canonicalIdentifier), NamespaceMode.Strict, canonicalIdentifier, repairs);
    }

    internal static NamespaceServiceResult CreateSuccess(string uri, NamespaceKind kind, NamespaceMode mode, MessageIdentifier? identifier, List<NamespaceRepair> repairs)
    {
        NamespaceDescriptor descriptor = new NamespaceDescriptor();
        descriptor.Uri = uri;
        descriptor.Kind = kind;
        descriptor.Mode = mode;
        descriptor.MessageIdentifier = identifier;
        NamespaceServiceResult result = new NamespaceServiceResult();
        result.Success = true;
        result.Descriptor = descriptor;
        result.Repairs = repairs;
        return result;
    }

    internal static NamespaceServiceResult CreateFailure(string errorCode, string errorMessage, List<NamespaceRepair> repairs)
    {
        NamespaceServiceResult result = new NamespaceServiceResult();
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        result.Repairs = repairs;
        return result;
    }

    internal static NamespaceKind ClassifyIdentifier(MessageIdentifier identifier)
    {
        if (identifier.BusinessArea == "head")
        {
            return NamespaceKind.Header;
        }

        if (identifier.BusinessArea == "nvlp")
        {
            return NamespaceKind.Envelope;
        }

        if (identifier.BusinessArea == "supl")
        {
            return NamespaceKind.Supplementary;
        }

        return NamespaceKind.Message;
    }

    internal static string TrimXmlWhitespace(string value)
    {
        int start = 0;
        while (start < value.Length && IsXmlWhitespace(value[start]))
        {
            start++;
        }

        int end = value.Length;
        while (end > start && IsXmlWhitespace(value[end - 1]))
        {
            end--;
        }

        if (start == 0 && end == value.Length)
        {
            return value;
        }

        return value.AsSpan().Slice(start, end - start).ToString();
    }

    internal static bool IsXmlWhitespace(char character)
    {
        return character == ' ' || character == '\t' || character == '\r' || character == '\n';
    }

    internal static bool IsSchemaNamespaceWithTrailingSlash(string value)
    {
        int expectedLength = Iso20022Namespaces.SchemaUrnPrefix.Length + 15 + 1;
        if (value.Length != expectedLength || value[value.Length - 1] != '/')
        {
            return false;
        }

        return UriLexical.IsAsciiEqualIgnoreCase(value.AsSpan().Slice(0, Iso20022Namespaces.SchemaUrnPrefix.Length), Iso20022Namespaces.SchemaUrnPrefix.AsSpan());
    }

    internal static bool HasTechnicalPrefixIgnoringCase(string value)
    {
        if (value.Length < Iso20022Namespaces.SchemaUrnPrefix.Length)
        {
            return false;
        }

        return UriLexical.IsAsciiEqualIgnoreCase(value.AsSpan().Slice(0, Iso20022Namespaces.SchemaUrnPrefix.Length), Iso20022Namespaces.SchemaUrnPrefix.AsSpan());
    }

    internal static string NormalizeTechnicalPrefix(string value)
    {
        if (!HasTechnicalPrefixIgnoringCase(value) || value.StartsWith(Iso20022Namespaces.SchemaUrnPrefix, StringComparison.Ordinal))
        {
            return value;
        }

        return Iso20022Namespaces.SchemaUrnPrefix + value.Substring(Iso20022Namespaces.SchemaUrnPrefix.Length);
    }

    internal static string NormalizeBusinessArea(string value)
    {
        int expectedLength = Iso20022Namespaces.SchemaUrnPrefix.Length + 15;
        if (value.Length != expectedLength || !value.StartsWith(Iso20022Namespaces.SchemaUrnPrefix, StringComparison.Ordinal))
        {
            return value;
        }

        int areaStart = Iso20022Namespaces.SchemaUrnPrefix.Length;
        char[] characters = value.ToCharArray();
        bool changed = false;
        int index = areaStart;
        while (index < areaStart + 4)
        {
            if (characters[index] >= 'A' && characters[index] <= 'Z')
            {
                characters[index] = (char)(characters[index] + 32);
                changed = true;
            }

            index++;
        }

        if (!changed)
        {
            return value;
        }

        return new string(characters);
    }

    internal static void AddRepair(List<NamespaceRepair> repairs, NamespaceRepairKind kind, string originalValue, string normalizedValue)
    {
        NamespaceRepair repair = new NamespaceRepair();
        repair.Kind = kind;
        repair.OriginalValue = originalValue;
        repair.NormalizedValue = normalizedValue;
        repairs.Add(repair);
    }
}
