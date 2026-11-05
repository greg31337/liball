using System;

namespace uri;

internal static class IriComparerCore
{
    internal static UriComparisonResult CompareExact(
        ReadOnlySpan<char> left,
        ReadOnlySpan<char> right,
        OperationLimits? limits)
    {
        UriComparisonLevel level = UriComparisonLevel.Exact;
        if (limits == null)
        {
            return CreateFailure(
                level,
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri));
        }

        IriParseResult parsedLeft = ParseComparisonInput(left, limits);
        if (!parsedLeft.Success)
        {
            return CreateFailure(level, parsedLeft.Diagnostic);
        }

        IriParseResult parsedRight = ParseComparisonInput(right, limits);
        if (!parsedRight.Success)
        {
            return CreateFailure(level, parsedRight.Diagnostic);
        }

        return CompareReferences(parsedLeft.Reference, parsedRight.Reference, level);
    }

    internal static UriComparisonResult CompareUnicode(
        ReadOnlySpan<char> left,
        ReadOnlySpan<char> right,
        OperationLimits? limits)
    {
        UriComparisonLevel level = UriComparisonLevel.IriUnicode;
        UriTextResult normalizedLeft = IriNormalizerCore.NormalizeIri(
            left,
            UnicodeNormalizationMode.NormalizeNfc,
            limits);
        if (!normalizedLeft.Success)
        {
            return CreateFailure(level, normalizedLeft.Diagnostic);
        }

        UriTextResult normalizedRight = IriNormalizerCore.NormalizeIri(
            right,
            UnicodeNormalizationMode.NormalizeNfc,
            limits);
        if (!normalizedRight.Success)
        {
            return CreateFailure(level, normalizedRight.Diagnostic);
        }

        IriParseResult parsedLeft = IriParserCore.ParseReference(
            normalizedLeft.Text.AsSpan());
        IriParseResult parsedRight = IriParserCore.ParseReference(
            normalizedRight.Text.AsSpan());
        if (!parsedLeft.Success)
        {
            return CreateFailure(level, parsedLeft.Diagnostic);
        }

        if (!parsedRight.Success)
        {
            return CreateFailure(level, parsedRight.Diagnostic);
        }

        return CompareReferences(parsedLeft.Reference, parsedRight.Reference, level);
    }

    internal static UriComparisonResult CompareAsUri(
        ReadOnlySpan<char> left,
        ReadOnlySpan<char> right,
        OperationLimits? limits)
    {
        UriComparisonLevel level = UriComparisonLevel.IriMappedUri;
        UriTextResult mappedLeft = IriConverterCore.ConvertIriToUri(left, limits);
        if (!mappedLeft.Success)
        {
            return CreateFailure(level, mappedLeft.Diagnostic);
        }

        UriTextResult mappedRight = IriConverterCore.ConvertIriToUri(right, limits);
        if (!mappedRight.Success)
        {
            return CreateFailure(level, mappedRight.Diagnostic);
        }

        UriParseResult parsedLeft = UriParserCore.ParseReference(mappedLeft.Text.AsSpan());
        UriParseResult parsedRight = UriParserCore.ParseReference(mappedRight.Text.AsSpan());
        if (!parsedLeft.Success)
        {
            return CreateFailure(
                level,
                IriComponentScanner.MapDiagnostic(parsedLeft.Diagnostic));
        }

        if (!parsedRight.Success)
        {
            return CreateFailure(
                level,
                IriComponentScanner.MapDiagnostic(parsedRight.Diagnostic));
        }

        UriComparisonCoreResult comparison = UriComparerCore.CompareSyntax(
            parsedLeft.Reference,
            parsedRight.Reference);
        return new UriComparisonResult(
            true,
            comparison.Equal,
            level,
            comparison.DifferentComponent,
            null);
    }

    internal static IriParseResult ParseComparisonInput(
        ReadOnlySpan<char> text,
        OperationLimits limits)
    {
        UriDiagnostic? inputLimit = LimitGuard.CheckInputCharacters(
            text.Length,
            limits,
            DiagnosticDomain.Iri);
        if (inputLimit != null)
        {
            return IriParserCore.CreateFailureResult(inputLimit);
        }

        return IriParserCore.ParseReference(text);
    }

    internal static UriComparisonResult CompareReferences(
        IriReference left,
        IriReference right,
        UriComparisonLevel level)
    {
        UriComponent difference = FindFirstDifferentComponent(left, right);
        return new UriComparisonResult(
            true,
            difference == UriComponent.None,
            level,
            difference,
            null);
    }

    internal static UriComponent FindFirstDifferentComponent(
        IriReference left,
        IriReference right)
    {
        if (left.HasScheme != right.HasScheme)
        {
            return UriComponent.Scheme;
        }

        if (left.HasScheme && left.Scheme != right.Scheme)
        {
            return UriComponent.Scheme;
        }

        if (left.HasAuthority != right.HasAuthority)
        {
            return UriComponent.Authority;
        }

        if (left.HasAuthority)
        {
            UriAuthority leftAuthority = left.Authority;
            UriAuthority rightAuthority = right.Authority;
            if (leftAuthority.HasUserInfo != rightAuthority.HasUserInfo)
            {
                return UriComponent.UserInfo;
            }

            if (leftAuthority.HasUserInfo &&
                leftAuthority.UserInfo != rightAuthority.UserInfo)
            {
                return UriComponent.UserInfo;
            }

            if (leftAuthority.HostKind != rightAuthority.HostKind ||
                leftAuthority.Host != rightAuthority.Host)
            {
                return UriComponent.Host;
            }

            if (leftAuthority.HasPort != rightAuthority.HasPort)
            {
                return UriComponent.Port;
            }

            if (leftAuthority.HasPort && leftAuthority.Port != rightAuthority.Port)
            {
                return UriComponent.Port;
            }
        }

        if (left.Path != right.Path)
        {
            return UriComponent.Path;
        }

        if (left.HasQuery != right.HasQuery)
        {
            return UriComponent.Query;
        }

        if (left.HasQuery && left.Query != right.Query)
        {
            return UriComponent.Query;
        }

        if (left.HasFragment != right.HasFragment)
        {
            return UriComponent.Fragment;
        }

        if (left.HasFragment && left.Fragment != right.Fragment)
        {
            return UriComponent.Fragment;
        }

        return UriComponent.None;
    }

    internal static UriComparisonResult CreateFailure(
        UriComparisonLevel level,
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

        return new UriComparisonResult(
            false,
            false,
            level,
            UriComponent.None,
            failure);
    }
}
