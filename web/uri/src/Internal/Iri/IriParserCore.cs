using System;

namespace uri;

internal static class IriParserCore
{
    internal static IriParseResult ParseReference(ReadOnlySpan<char> text)
    {
        SchemeScanResult scheme = UriParserCore.ScanOptionalScheme(text);
        if (scheme.HasScheme)
        {
            return ParseIri(text);
        }

        return ParseRelativeReference(text);
    }

    internal static IriParseResult ParseIri(ReadOnlySpan<char> text)
    {
        return ParseWithRequiredScheme(text, UriReferenceKind.Uri, true);
    }

    internal static IriParseResult ParseAbsoluteIri(ReadOnlySpan<char> text)
    {
        return ParseWithRequiredScheme(text, UriReferenceKind.AbsoluteUri, false);
    }

    internal static IriParseResult ParseRelativeReference(ReadOnlySpan<char> text)
    {
        UriReferenceKind kind = UriParserCore.ClassifyReference(text);
        return ParseComponents(text, 0, 0, false, false, true, kind);
    }

    internal static IriReference ParseReferenceOrThrow(ReadOnlySpan<char> text)
    {
        return GetReferenceOrThrow(ParseReference(text));
    }

    internal static IriReference ParseIriOrThrow(ReadOnlySpan<char> text)
    {
        return GetReferenceOrThrow(ParseIri(text));
    }

    internal static IriReference ParseAbsoluteIriOrThrow(ReadOnlySpan<char> text)
    {
        return GetReferenceOrThrow(ParseAbsoluteIri(text));
    }

    internal static IriReference ParseRelativeReferenceOrThrow(ReadOnlySpan<char> text)
    {
        return GetReferenceOrThrow(ParseRelativeReference(text));
    }

    internal static IriParseResult ParseWithRequiredScheme(
        ReadOnlySpan<char> text,
        UriReferenceKind kind,
        bool fragmentAllowed)
    {
        SchemeScanResult scheme = UriParserCore.ScanRequiredScheme(text, 0);
        if (!scheme.Success)
        {
            return CreateFailureResult(scheme.Diagnostic);
        }

        return ParseComponents(
            text,
            scheme.NextOffset,
            scheme.SchemeLength,
            true,
            true,
            fragmentAllowed,
            kind);
    }

    internal static IriParseResult ParseComponents(
        ReadOnlySpan<char> text,
        int partOffset,
        int schemeLength,
        bool hasScheme,
        bool hierarchical,
        bool fragmentAllowed,
        UriReferenceKind kind)
    {
        UriReferenceSplitResult split;
        if (hierarchical)
        {
            split = UriParserCore.SplitHierarchicalPart(text, partOffset);
        }
        else
        {
            split = UriParserCore.SplitRelativePart(text, partOffset);
        }

        if (!split.Success)
        {
            return CreateFailureResult(split.Diagnostic);
        }

        UriAuthority authority = new UriAuthority();
        if (split.HasAuthority)
        {
            AuthorityValueParseResult authorityResult = IriAuthorityParser.Parse(
                text.Slice(split.AuthorityOffset, split.AuthorityLength),
                split.AuthorityOffset);
            if (!authorityResult.Success)
            {
                return CreateFailureResult(authorityResult.Diagnostic);
            }

            authority = authorityResult.Authority;
        }

        PathParseResult path = ParseSelectedPath(text, split, hierarchical);
        if (!path.Success)
        {
            return CreateFailureResult(path.Diagnostic);
        }

        int optionalOffset = split.PathOffset + split.PathLength;
        QueryFragmentScanResult query = IriQueryFragmentParser.ScanQuery(
            text,
            optionalOffset,
            0);
        if (!query.Success)
        {
            return CreateFailureResult(query.Diagnostic);
        }

        if (!fragmentAllowed &&
            query.NextOffset < text.Length &&
            text[query.NextOffset] == '#')
        {
            UriDiagnostic forbiddenFragment = IriComponentScanner.CreateComponentFailure(
                UriComponent.Fragment,
                query.NextOffset,
                1);
            return CreateFailureResult(forbiddenFragment);
        }

        QueryFragmentScanResult fragment = IriQueryFragmentParser.ScanFragment(
            text,
            query.NextOffset,
            0);
        if (!fragment.Success)
        {
            return CreateFailureResult(fragment.Diagnostic);
        }

        if (fragment.NextOffset != text.Length)
        {
            return CreateFailureResult(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri));
        }

        IriReference reference = new IriReference();
        reference.Kind = kind;
        reference.HasScheme = hasScheme;
        if (hasScheme)
        {
            reference.Scheme = text.Slice(0, schemeLength).ToString();
        }

        reference.HasAuthority = split.HasAuthority;
        reference.Authority = authority;
        reference.Path = text.Slice(split.PathOffset, split.PathLength).ToString();
        reference.HasQuery = query.Present;
        if (query.Present)
        {
            reference.Query = text.Slice(query.ValueOffset, query.ValueLength).ToString();
        }

        reference.HasFragment = fragment.Present;
        if (fragment.Present)
        {
            reference.Fragment = text.Slice(
                fragment.ValueOffset,
                fragment.ValueLength).ToString();
        }

        reference.OriginalText = text.ToString();
        return new IriParseResult(true, reference, null);
    }

    internal static PathParseResult ParseSelectedPath(
        ReadOnlySpan<char> text,
        UriReferenceSplitResult split,
        bool hierarchical)
    {
        ReadOnlySpan<char> path = text.Slice(split.PathOffset, split.PathLength);
        if (split.HasAuthority)
        {
            return IriPathParser.ParseAbEmpty(path, split.PathOffset);
        }

        if (path.Length == 0)
        {
            return IriPathParser.ParseEmpty(path, split.PathOffset);
        }

        if (path[0] == '/')
        {
            return IriPathParser.ParseAbsolute(path, split.PathOffset);
        }

        if (hierarchical)
        {
            return IriPathParser.ParseRootless(path, split.PathOffset);
        }

        return IriPathParser.ParseNoScheme(path, split.PathOffset);
    }

    internal static IriParseResult CreateFailureResult(UriDiagnostic? diagnostic)
    {
        return new IriParseResult(
            false,
            new IriReference(),
            IriComponentScanner.MapDiagnostic(diagnostic));
    }

    internal static IriReference GetReferenceOrThrow(IriParseResult result)
    {
        if (!result.Success)
        {
            throw CreateSyntaxException(result.Diagnostic);
        }

        return result.Reference;
    }

    internal static UriSyntaxException CreateSyntaxException(UriDiagnostic? diagnostic)
    {
        return new UriSyntaxException(IriComponentScanner.MapDiagnostic(diagnostic));
    }
}
