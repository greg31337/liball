namespace uri;

internal static class UrnComparerCore
{
    internal const string UnsupportedNamespaceMessage = "No namespace handler is available for namespace-specific URN equivalence.";

    internal static UrnEquivalenceResult CompareExact(
        Urn? left,
        Urn? right)
    {
        UriDiagnostic? precondition = ValidateInputs(left, right);
        if (precondition != null || left == null || right == null)
        {
            return CreateFailure(UrnEquivalenceLevel.Exact, precondition);
        }

        UriComponent difference = FindExactDifference(left, right);
        return CreateCompleted(UrnEquivalenceLevel.Exact, difference);
    }

    internal static UrnEquivalenceResult CompareAssignedName(
        Urn? left,
        Urn? right)
    {
        UriDiagnostic? precondition = ValidateInputs(left, right);
        if (precondition != null || left == null || right == null)
        {
            return CreateFailure(
                UrnEquivalenceLevel.AssignedName,
                precondition);
        }

        UriComponent difference = FindAssignedNameDifference(left, right);
        return CreateCompleted(
            UrnEquivalenceLevel.AssignedName,
            difference);
    }

    internal static UrnEquivalenceResult CompareNamespaceSpecific(
        Urn? left,
        Urn? right,
        IUrnNamespaceHandler? handler)
    {
        UriDiagnostic? precondition = ValidateInputs(left, right);
        if (precondition != null || left == null || right == null)
        {
            return CreateFailure(
                UrnEquivalenceLevel.NamespaceSpecific,
                precondition);
        }

        if (handler == null)
        {
            return CreateFailure(
                UrnEquivalenceLevel.NamespaceSpecific,
                CreateUnsupportedNamespaceDiagnostic());
        }

        UriComponent genericDifference = FindAssignedNameDifference(
            left,
            right);
        if (genericDifference == UriComponent.None)
        {
            return CreateCompleted(
                UrnEquivalenceLevel.NamespaceSpecific,
                UriComponent.None);
        }

        if (genericDifference != UriComponent.UrnNamespaceSpecificString)
        {
            return CreateCompleted(
                UrnEquivalenceLevel.NamespaceSpecific,
                genericDifference);
        }

        string namespaceIdentifier = NormalizeNamespaceIdentifier(
            left.NamespaceIdentifier);
        UrnEquivalenceResult? handled =
            handler.CompareNamespaceSpecificStrings(
                namespaceIdentifier,
                left.NamespaceSpecificString,
                right.NamespaceSpecificString);
        if (handled == null || !handled.Success)
        {
            UriDiagnostic? diagnostic = null;
            if (handled != null)
            {
                diagnostic = handled.Diagnostic;
            }

            return CreateFailure(
                UrnEquivalenceLevel.NamespaceSpecific,
                diagnostic);
        }

        UriComponent difference =
            UriComponent.UrnNamespaceSpecificString;
        if (handled.Equal)
        {
            difference = UriComponent.None;
        }

        return CreateCompleted(
            UrnEquivalenceLevel.NamespaceSpecific,
            difference);
    }

    internal static UriComponent FindExactDifference(
        Urn left,
        Urn right)
    {
        if (left.Scheme != right.Scheme)
        {
            return UriComponent.Scheme;
        }

        if (left.NamespaceIdentifier != right.NamespaceIdentifier)
        {
            return UriComponent.UrnNamespaceIdentifier;
        }

        if (left.NamespaceSpecificString != right.NamespaceSpecificString)
        {
            return UriComponent.UrnNamespaceSpecificString;
        }

        if (left.HasRComponent != right.HasRComponent ||
            (left.HasRComponent && left.RComponent != right.RComponent))
        {
            return UriComponent.UrnRComponent;
        }

        if (left.HasQComponent != right.HasQComponent ||
            (left.HasQComponent && left.QComponent != right.QComponent))
        {
            return UriComponent.UrnQComponent;
        }

        if (left.HasFComponent != right.HasFComponent ||
            (left.HasFComponent && left.FComponent != right.FComponent))
        {
            return UriComponent.UrnFComponent;
        }

        return UriComponent.None;
    }

    internal static UriComponent FindAssignedNameDifference(
        Urn left,
        Urn right)
    {
        if (!AsciiCase.EqualsIgnoreCase(left.Scheme, right.Scheme))
        {
            return UriComponent.Scheme;
        }

        if (!AsciiCase.EqualsIgnoreCase(
            left.NamespaceIdentifier,
            right.NamespaceIdentifier))
        {
            return UriComponent.UrnNamespaceIdentifier;
        }

        if (!NamespaceSpecificStringsAreEquivalent(
            left.NamespaceSpecificString,
            right.NamespaceSpecificString))
        {
            return UriComponent.UrnNamespaceSpecificString;
        }

        return UriComponent.None;
    }

    internal static bool NamespaceSpecificStringsAreEquivalent(
        string left,
        string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int offset = 0;
        while (offset < left.Length)
        {
            if (left[offset] == '%' && right[offset] == '%' &&
                offset + 2 < left.Length &&
                Ascii.IsHexDigit(left[offset + 1]) &&
                Ascii.IsHexDigit(left[offset + 2]) &&
                Ascii.IsHexDigit(right[offset + 1]) &&
                Ascii.IsHexDigit(right[offset + 2]))
            {
                if (AsciiCase.FoldToUpper(left[offset + 1]) !=
                    AsciiCase.FoldToUpper(right[offset + 1]) ||
                    AsciiCase.FoldToUpper(left[offset + 2]) !=
                    AsciiCase.FoldToUpper(right[offset + 2]))
                {
                    return false;
                }

                offset = offset + 3;
                continue;
            }

            if (left[offset] != right[offset])
            {
                return false;
            }

            offset = offset + 1;
        }

        return true;
    }

    internal static string NormalizeNamespaceIdentifier(string value)
    {
        TextOutput output = new TextOutput(value.Length);
        UrnNormalizerCore.AppendLowercaseAscii(value, output);
        return output.GetText();
    }

    internal static UriDiagnostic? ValidateInputs(Urn? left, Urn? right)
    {
        if (!HasRequiredFields(left) || !HasRequiredFields(right))
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn);
        }

        return null;
    }

    internal static bool HasRequiredFields(Urn? urn)
    {
        return urn != null &&
            urn.Scheme != null &&
            urn.NamespaceIdentifier != null &&
            urn.NamespaceSpecificString != null &&
            urn.RComponent != null &&
            urn.QComponent != null &&
            urn.FComponent != null;
    }

    internal static UrnEquivalenceResult CreateCompleted(
        UrnEquivalenceLevel level,
        UriComponent difference)
    {
        return new UrnEquivalenceResult(
            true,
            difference == UriComponent.None,
            level,
            difference,
            null);
    }

    internal static UrnEquivalenceResult CreateFailure(
        UrnEquivalenceLevel level,
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = diagnostic ??
            DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn);
        return new UrnEquivalenceResult(
            false,
            false,
            level,
            UriComponent.None,
            failure);
    }

    internal static UriDiagnostic CreateUnsupportedNamespaceDiagnostic()
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.Urn,
            UriDiagnosticCode.UnsupportedNamespace,
            UriComponent.UrnNamespaceSpecificString,
            0,
            0,
            UnsupportedNamespaceMessage);
    }
}
