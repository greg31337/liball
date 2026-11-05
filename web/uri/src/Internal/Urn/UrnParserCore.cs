using System;

namespace uri;

internal static class UrnParserCore
{
    internal const string InvalidSchemeMessage = "The URN must start with the case-insensitive urn scheme.";
    internal const string InvalidNamespaceIdentifierMessage = "The URN namespace identifier must contain 2 through 32 LDH characters and start and end with an alphanumeric character.";
    internal const string RegisteredNamespaceIdentifierMessage = "The URN namespace identifier is reserved or is not eligible for namespace registration.";
    internal const string InvalidNamespaceSpecificStringMessage = "The URN namespace-specific string must be non-empty ASCII pchar text with slash allowed after the first character.";
    internal const string InvalidComponentOrderMessage = "URN optional components must use ?+ for r, then ?= for q, then # for f.";
    internal const string InvalidRComponentMessage = "The URN r-component must be non-empty ASCII pchar, slash, or question-mark text.";
    internal const string InvalidQComponentMessage = "The URN q-component must be non-empty ASCII pchar, slash, or question-mark text.";
    internal const string InvalidFComponentMessage = "The URN f-component must contain only ASCII pchar, slash, or question-mark text.";

    internal static UrnParseResult ParseUrn(
        ReadOnlySpan<char> text,
        UrnValidationMode validationMode)
    {
        UrnAssignedNameParseResult assignedName = ParseAssignedName(
            text,
            validationMode);
        if (!assignedName.Success)
        {
            return CreateUrnFailure(assignedName.Diagnostic);
        }

        Urn urn = new Urn();
        urn.Scheme = assignedName.Scheme;
        urn.NamespaceIdentifier = assignedName.NamespaceIdentifier;
        urn.NamespaceSpecificString =
            assignedName.NamespaceSpecificString;
        urn.AssignedName = assignedName.AssignedName;

        int offset = assignedName.NextOffset;
        if (offset < text.Length && text[offset] == '?')
        {
            if (offset + 1 >= text.Length ||
                (text[offset + 1] != '+' && text[offset + 1] != '='))
            {
                return CreateUrnFailure(CreateUrnDiagnostic(
                    UriDiagnosticCode.InvalidUrnComponentOrder,
                    UriComponent.Urn,
                    offset,
                    1,
                    InvalidComponentOrderMessage));
            }

            if (text[offset + 1] == '+')
            {
                urn.HasRComponent = true;
                UrnComponentParseResult rComponent = ParseRComponent(
                    text,
                    offset + 2);
                if (!rComponent.Success)
                {
                    return CreateUrnFailure(rComponent.Diagnostic);
                }

                urn.RComponent = rComponent.Value;
                offset = rComponent.NextOffset;
            }

            if (offset < text.Length && text[offset] == '?' &&
                offset + 1 < text.Length && text[offset + 1] == '=')
            {
                urn.HasQComponent = true;
                UrnComponentParseResult qComponent = ParseQComponent(
                    text,
                    offset + 2);
                if (!qComponent.Success)
                {
                    return CreateUrnFailure(qComponent.Diagnostic);
                }

                urn.QComponent = qComponent.Value;
                offset = qComponent.NextOffset;
            }
        }

        if (offset < text.Length && text[offset] == '#')
        {
            urn.HasFComponent = true;
            UrnComponentParseResult fComponent = ParseFComponent(
                text,
                offset + 1);
            if (!fComponent.Success)
            {
                return CreateUrnFailure(fComponent.Diagnostic);
            }

            urn.FComponent = fComponent.Value;
            offset = fComponent.NextOffset;
        }

        if (offset != text.Length)
        {
            return CreateUrnFailure(CreateUrnDiagnostic(
                UriDiagnosticCode.InvalidUrnComponentOrder,
                UriComponent.Urn,
                offset,
                1,
                InvalidComponentOrderMessage));
        }

        urn.OriginalText = text.ToString();
        return new UrnParseResult(true, urn, null);
    }

    internal static UrnComponentParseResult ParseRComponent(
        ReadOnlySpan<char> text,
        int startOffset)
    {
        int endOffset = startOffset;
        while (endOffset < text.Length)
        {
            if (text[endOffset] == '#')
            {
                break;
            }

            if (text[endOffset] == '?' &&
                endOffset + 1 < text.Length &&
                text[endOffset + 1] == '=')
            {
                break;
            }

            endOffset = endOffset + 1;
        }

        return ParseOptionalComponentValue(
            text,
            startOffset,
            endOffset,
            UriComponent.UrnRComponent,
            false);
    }

    internal static UrnComponentParseResult ParseQComponent(
        ReadOnlySpan<char> text,
        int startOffset)
    {
        int endOffset = startOffset;
        while (endOffset < text.Length && text[endOffset] != '#')
        {
            endOffset = endOffset + 1;
        }

        return ParseOptionalComponentValue(
            text,
            startOffset,
            endOffset,
            UriComponent.UrnQComponent,
            false);
    }

    internal static UrnComponentParseResult ParseFComponent(
        ReadOnlySpan<char> text,
        int startOffset)
    {
        return ParseOptionalComponentValue(
            text,
            startOffset,
            text.Length,
            UriComponent.UrnFComponent,
            true);
    }

    internal static UrnComponentParseResult ParseOptionalComponentValue(
        ReadOnlySpan<char> text,
        int startOffset,
        int endOffset,
        UriComponent component,
        bool allowEmpty)
    {
        if (startOffset < 0 || endOffset < startOffset ||
            endOffset > text.Length ||
            (component != UriComponent.UrnRComponent &&
                component != UriComponent.UrnQComponent &&
                component != UriComponent.UrnFComponent))
        {
            return CreateComponentFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn));
        }

        if (!allowEmpty && startOffset == endOffset)
        {
            return CreateComponentFailure(CreateUrnDiagnostic(
                UriDiagnosticCode.InvalidUrn,
                component,
                startOffset,
                0,
                GetOptionalComponentMessage(component)));
        }

        ReadOnlySpan<char> value = text.Slice(
            startOffset,
            endOffset - startOffset);
        int offset = 0;
        while (offset < value.Length)
        {
            if (value[offset] == '%')
            {
                PercentTripletResult triplet = PercentScanner.ScanTriplet(
                    value,
                    offset,
                    startOffset,
                    component);
                if (!triplet.Success)
                {
                    return CreateComponentFailure(
                        CreateUrnComponentPercentDiagnostic(
                            triplet.Diagnostic,
                            component));
                }

                offset = triplet.NextOffset;
                continue;
            }

            if (!Ascii.IsAscii(value[offset]) ||
                !PercentAllowlist.IsFragmentCharacter(value[offset]) ||
                (!allowEmpty && offset == 0 &&
                    (value[offset] == '/' || value[offset] == '?')))
            {
                return CreateComponentFailure(CreateUrnDiagnostic(
                    UriDiagnosticCode.InvalidUrn,
                    component,
                    startOffset + offset,
                    1,
                    GetOptionalComponentMessage(component)));
            }

            offset = offset + 1;
        }

        return new UrnComponentParseResult(
            true,
            value.ToString(),
            endOffset,
            null);
    }

    internal static UrnAssignedNameParseResult ParseAssignedName(
        ReadOnlySpan<char> text,
        UrnValidationMode validationMode)
    {
        UrnPrefixParseResult prefix = ParsePrefix(text, validationMode);
        if (!prefix.Success)
        {
            return CreateAssignedNameFailure(prefix.Diagnostic);
        }

        UrnNamespaceSpecificStringParseResult namespaceSpecificString =
            ParseNamespaceSpecificString(text, prefix.NextOffset);
        if (!namespaceSpecificString.Success)
        {
            return CreateAssignedNameFailure(
                namespaceSpecificString.Diagnostic);
        }

        return new UrnAssignedNameParseResult(
            true,
            prefix.Scheme,
            prefix.NamespaceIdentifier,
            namespaceSpecificString.NamespaceSpecificString,
            text.Slice(0, namespaceSpecificString.NextOffset).ToString(),
            namespaceSpecificString.NextOffset,
            null);
    }

    internal static UrnNamespaceSpecificStringParseResult
        ParseNamespaceSpecificString(
            ReadOnlySpan<char> text,
            int startOffset)
    {
        if (startOffset < 0 || startOffset > text.Length)
        {
            return CreateNamespaceSpecificStringFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn));
        }

        int endOffset = startOffset;
        while (endOffset < text.Length &&
            text[endOffset] != '?' &&
            text[endOffset] != '#')
        {
            endOffset = endOffset + 1;
        }

        int length = endOffset - startOffset;
        if (length == 0)
        {
            return CreateNamespaceSpecificStringFailure(
                CreateUrnDiagnostic(
                    UriDiagnosticCode.InvalidNamespaceSpecificString,
                    UriComponent.UrnNamespaceSpecificString,
                    startOffset,
                    0,
                    InvalidNamespaceSpecificStringMessage));
        }

        ReadOnlySpan<char> namespaceSpecificString = text.Slice(
            startOffset,
            length);
        int offset = 0;
        while (offset < namespaceSpecificString.Length)
        {
            char value = namespaceSpecificString[offset];
            if (value == '%')
            {
                PercentTripletResult triplet = PercentScanner.ScanTriplet(
                    namespaceSpecificString,
                    offset,
                    startOffset,
                    UriComponent.UrnNamespaceSpecificString);
                if (!triplet.Success)
                {
                    return CreateNamespaceSpecificStringFailure(
                        CreateUrnPercentDiagnostic(triplet.Diagnostic));
                }

                offset = triplet.NextOffset;
                continue;
            }

            if (!Ascii.IsAscii(value) ||
                !PercentAllowlist.IsUrnNamespaceSpecificStringCharacter(
                    value) ||
                (offset == 0 && value == '/'))
            {
                return CreateNamespaceSpecificStringFailure(
                    CreateUrnDiagnostic(
                        UriDiagnosticCode.InvalidNamespaceSpecificString,
                        UriComponent.UrnNamespaceSpecificString,
                        startOffset + offset,
                        1,
                        InvalidNamespaceSpecificStringMessage));
            }

            offset = offset + 1;
        }

        return new UrnNamespaceSpecificStringParseResult(
            true,
            namespaceSpecificString.ToString(),
            endOffset,
            null);
    }

    internal static UrnPrefixParseResult ParsePrefix(
        ReadOnlySpan<char> text,
        UrnValidationMode validationMode)
    {
        if (validationMode != UrnValidationMode.GenericSyntax &&
            validationMode != UrnValidationMode.RegisteredNamespace)
        {
            return CreateFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn));
        }

        UriDiagnostic? schemeDiagnostic = ValidateScheme(text);
        if (schemeDiagnostic != null)
        {
            return CreateFailure(schemeDiagnostic);
        }

        int namespaceOffset = 4;
        int offset = namespaceOffset;
        while (offset < text.Length && text[offset] != ':')
        {
            if (!UrnGrammar.IsNamespaceIdentifierCharacter(text[offset]))
            {
                return CreateNamespaceFailure(offset, 1,
                    InvalidNamespaceIdentifierMessage);
            }

            if (offset - namespaceOffset == 32)
            {
                return CreateNamespaceFailure(offset, 1,
                    InvalidNamespaceIdentifierMessage);
            }

            offset = offset + 1;
        }

        int namespaceLength = offset - namespaceOffset;
        if (namespaceLength < 2)
        {
            return CreateNamespaceFailure(
                namespaceOffset,
                namespaceLength,
                InvalidNamespaceIdentifierMessage);
        }

        if (!Ascii.IsAlphaNumeric(text[namespaceOffset]))
        {
            return CreateNamespaceFailure(
                namespaceOffset,
                1,
                InvalidNamespaceIdentifierMessage);
        }

        if (!Ascii.IsAlphaNumeric(text[offset - 1]))
        {
            return CreateNamespaceFailure(
                offset - 1,
                1,
                InvalidNamespaceIdentifierMessage);
        }

        if (offset == text.Length)
        {
            return CreateNamespaceFailure(
                offset,
                0,
                InvalidNamespaceIdentifierMessage);
        }

        ReadOnlySpan<char> namespaceIdentifier = text.Slice(
            namespaceOffset,
            namespaceLength);
        if (validationMode == UrnValidationMode.RegisteredNamespace)
        {
            UriDiagnostic? registrationDiagnostic =
                ValidateRegisteredNamespaceIdentifier(
                    namespaceIdentifier,
                    namespaceOffset);
            if (registrationDiagnostic != null)
            {
                return CreateFailure(registrationDiagnostic);
            }
        }

        return new UrnPrefixParseResult(
            true,
            text.Slice(0, 3).ToString(),
            namespaceIdentifier.ToString(),
            offset + 1,
            null);
    }

    internal static UriDiagnostic? ValidateScheme(ReadOnlySpan<char> text)
    {
        const string expected = "urn";
        int index = 0;
        while (index < expected.Length)
        {
            if (index >= text.Length)
            {
                return CreateUrnDiagnostic(
                    UriDiagnosticCode.InvalidUrn,
                    UriComponent.Urn,
                    index,
                    0,
                    InvalidSchemeMessage);
            }

            if (AsciiCase.FoldToLower(text[index]) != expected[index])
            {
                return CreateUrnDiagnostic(
                    UriDiagnosticCode.InvalidUrn,
                    UriComponent.Urn,
                    index,
                    1,
                    InvalidSchemeMessage);
            }

            index = index + 1;
        }

        if (text.Length == 3)
        {
            return CreateUrnDiagnostic(
                UriDiagnosticCode.InvalidUrn,
                UriComponent.Urn,
                3,
                0,
                InvalidSchemeMessage);
        }

        if (text[3] != ':')
        {
            return CreateUrnDiagnostic(
                UriDiagnosticCode.InvalidUrn,
                UriComponent.Urn,
                3,
                1,
                InvalidSchemeMessage);
        }

        return null;
    }

    internal static UriDiagnostic? ValidateRegisteredNamespaceIdentifier(
        ReadOnlySpan<char> namespaceIdentifier,
        int namespaceOffset)
    {
        if (UrnGrammar.StartsWithIgnoreCase(namespaceIdentifier, "urn-"))
        {
            if (UrnGrammar.IsInformalNamespaceIdentifier(
                namespaceIdentifier))
            {
                return null;
            }

            return CreateUrnDiagnostic(
                UriDiagnosticCode.InvalidNamespaceIdentifier,
                UriComponent.UrnNamespaceIdentifier,
                namespaceOffset,
                namespaceIdentifier.Length,
                RegisteredNamespaceIdentifierMessage);
        }

        if (namespaceIdentifier.Length < 3)
        {
            return CreateUrnDiagnostic(
                UriDiagnosticCode.InvalidNamespaceIdentifier,
                UriComponent.UrnNamespaceIdentifier,
                namespaceOffset,
                namespaceIdentifier.Length,
                RegisteredNamespaceIdentifierMessage);
        }

        if (UrnGrammar.StartsWithIgnoreCase(namespaceIdentifier, "x-"))
        {
            return CreateUrnDiagnostic(
                UriDiagnosticCode.InvalidNamespaceIdentifier,
                UriComponent.UrnNamespaceIdentifier,
                namespaceOffset,
                2,
                RegisteredNamespaceIdentifierMessage);
        }

        if (namespaceIdentifier.Length >= 3 &&
            Ascii.IsAlphabetic(namespaceIdentifier[0]) &&
            Ascii.IsAlphabetic(namespaceIdentifier[1]) &&
            namespaceIdentifier[2] == '-')
        {
            return CreateUrnDiagnostic(
                UriDiagnosticCode.InvalidNamespaceIdentifier,
                UriComponent.UrnNamespaceIdentifier,
                namespaceOffset,
                3,
                RegisteredNamespaceIdentifierMessage);
        }

        return null;
    }

    internal static UrnPrefixParseResult CreateNamespaceFailure(
        int offset,
        int length,
        string message)
    {
        return CreateFailure(CreateUrnDiagnostic(
            UriDiagnosticCode.InvalidNamespaceIdentifier,
            UriComponent.UrnNamespaceIdentifier,
            offset,
            length,
            message));
    }

    internal static UrnAssignedNameParseResult CreateAssignedNameFailure(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = diagnostic ??
            DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn);
        return new UrnAssignedNameParseResult(
            false,
            "",
            "",
            "",
            "",
            0,
            failure);
    }

    internal static UrnNamespaceSpecificStringParseResult
        CreateNamespaceSpecificStringFailure(UriDiagnostic diagnostic)
    {
        return new UrnNamespaceSpecificStringParseResult(
            false,
            "",
            0,
            diagnostic);
    }

    internal static UriDiagnostic CreateUrnPercentDiagnostic(
        UriDiagnostic? diagnostic)
    {
        if (diagnostic == null)
        {
            return DiagnosticFactory.CreateError(
                DiagnosticDomain.Urn,
                UriDiagnosticCode.InvalidPercentEncoding,
                UriComponent.UrnNamespaceSpecificString,
                0,
                0,
                DiagnosticFactory.InvalidPercentEncodingMessage);
        }

        return DiagnosticFactory.CreateError(
            DiagnosticDomain.Urn,
            UriDiagnosticCode.InvalidPercentEncoding,
            UriComponent.UrnNamespaceSpecificString,
            diagnostic.Range.Offset,
            diagnostic.Range.Length,
            DiagnosticFactory.InvalidPercentEncodingMessage);
    }

    internal static UrnParseResult CreateUrnFailure(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = diagnostic ??
            DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn);
        return new UrnParseResult(false, new Urn(), failure);
    }

    internal static UrnComponentParseResult CreateComponentFailure(
        UriDiagnostic diagnostic)
    {
        return new UrnComponentParseResult(false, "", 0, diagnostic);
    }

    internal static UriDiagnostic CreateUrnComponentPercentDiagnostic(
        UriDiagnostic? diagnostic,
        UriComponent component)
    {
        int offset = 0;
        int length = 0;
        if (diagnostic != null)
        {
            offset = diagnostic.Range.Offset;
            length = diagnostic.Range.Length;
        }

        return DiagnosticFactory.CreateError(
            DiagnosticDomain.Urn,
            UriDiagnosticCode.InvalidPercentEncoding,
            component,
            offset,
            length,
            DiagnosticFactory.InvalidPercentEncodingMessage);
    }

    internal static string GetOptionalComponentMessage(
        UriComponent component)
    {
        if (component == UriComponent.UrnRComponent)
        {
            return InvalidRComponentMessage;
        }

        if (component == UriComponent.UrnQComponent)
        {
            return InvalidQComponentMessage;
        }

        return InvalidFComponentMessage;
    }

    internal static UrnPrefixParseResult CreateFailure(
        UriDiagnostic diagnostic)
    {
        return new UrnPrefixParseResult(false, "", "", 0, diagnostic);
    }

    internal static UriDiagnostic CreateUrnDiagnostic(
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.Urn,
            code,
            component,
            offset,
            length,
            message);
    }
}
