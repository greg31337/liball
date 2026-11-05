using System;

namespace uri;

internal static class UriParserCore
{
    internal static UriParseResult ParseReference(ReadOnlySpan<char> text)
    {
        SchemeScanResult scheme = ScanOptionalScheme(text);
        if (scheme.HasScheme)
        {
            return ParseUri(text);
        }

        return ParseRelativeReference(text);
    }

    internal static UriParseResult ParseUri(ReadOnlySpan<char> text)
    {
        return ParseWithRequiredScheme(text, UriReferenceKind.Uri, true);
    }

    internal static UriParseResult ParseAbsoluteUri(ReadOnlySpan<char> text)
    {
        return ParseWithRequiredScheme(text, UriReferenceKind.AbsoluteUri, false);
    }

    internal static UriParseResult ParseRelativeReference(ReadOnlySpan<char> text)
    {
        UriReferenceKind kind = ClassifyReference(text);
        return ParseComponents(
            text,
            0,
            0,
            false,
            false,
            true,
            kind);
    }

    internal static UriParseResult ParseWithRequiredScheme(
        ReadOnlySpan<char> text,
        UriReferenceKind kind,
        bool fragmentAllowed)
    {
        SchemeScanResult scheme = ScanRequiredScheme(text, 0);
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

    internal static UriParseResult ParseComponents(
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
            split = SplitHierarchicalPart(text, partOffset);
        }
        else
        {
            split = SplitRelativePart(text, partOffset);
        }

        if (!split.Success)
        {
            return CreateFailureResult(split.Diagnostic);
        }

        UriAuthority authority = new UriAuthority();
        if (split.HasAuthority)
        {
            AuthorityValueParseResult authorityResult = ParseAuthorityValue(
                text,
                split.AuthorityOffset,
                split.AuthorityLength);
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
        QueryFragmentScanResult query = QueryFragmentParser.ScanQuery(
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
            UriDiagnostic forbiddenFragment = DiagnosticFactory.CreateInvalidFragment(
                query.NextOffset,
                1);
            return CreateFailureResult(forbiddenFragment);
        }

        QueryFragmentScanResult fragment = QueryFragmentParser.ScanFragment(
            text,
            query.NextOffset,
            0);
        if (!fragment.Success)
        {
            return CreateFailureResult(fragment.Diagnostic);
        }

        if (fragment.NextOffset != text.Length)
        {
            UriDiagnostic unconsumed = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
            return CreateFailureResult(unconsumed);
        }

        UriReference reference = new UriReference();
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
        return new UriParseResult(true, reference, null);
    }

    internal static PathParseResult ParseSelectedPath(
        ReadOnlySpan<char> text,
        UriReferenceSplitResult split,
        bool hierarchical)
    {
        ReadOnlySpan<char> path = text.Slice(split.PathOffset, split.PathLength);
        if (split.HasAuthority)
        {
            return PathParser.ParseAbEmpty(path, split.PathOffset);
        }

        if (path.Length == 0)
        {
            return PathParser.ParseEmpty(path, split.PathOffset);
        }

        if (path[0] == '/')
        {
            return PathParser.ParseAbsolute(path, split.PathOffset);
        }

        if (hierarchical)
        {
            return PathParser.ParseRootless(path, split.PathOffset);
        }

        return PathParser.ParseNoScheme(path, split.PathOffset);
    }

    internal static AuthorityValueParseResult ParseAuthorityValue(
        ReadOnlySpan<char> text,
        int authorityOffset,
        int authorityLength)
    {
        ReadOnlySpan<char> authorityText = text.Slice(authorityOffset, authorityLength);
        int userInfoDelimiter = AuthorityParser.FindLastCharacter(authorityText, '@');
        if (userInfoDelimiter >= 0)
        {
            UriDiagnostic? userInfoDiagnostic = AuthorityParser.ValidateUserInfo(
                authorityText.Slice(0, userInfoDelimiter),
                authorityOffset);
            if (userInfoDiagnostic != null)
            {
                return CreateAuthorityFailure(userInfoDiagnostic);
            }
        }

        AuthorityParseResult boundaries = AuthorityParser.ParseStructuralBoundaries(
            authorityText,
            authorityOffset);
        if (!boundaries.Success)
        {
            return CreateAuthorityFailure(boundaries.Diagnostic);
        }

        ReadOnlySpan<char> hostText = authorityText.Slice(
            boundaries.HostOffset,
            boundaries.HostLength);
        UriHostKind hostKind;
        byte[] addressBytes = new byte[0];
        if (boundaries.HostBracketed)
        {
            if (hostText.Length > 0 && AsciiCase.FoldToLower(hostText[0]) == 'v')
            {
                IpFutureParseResult ipvFuture = IpFutureParser.Parse(
                    hostText,
                    authorityOffset + boundaries.HostOffset);
                if (!ipvFuture.Success)
                {
                    return CreateAuthorityFailure(ipvFuture.Diagnostic);
                }

                hostKind = UriHostKind.IpvFuture;
            }
            else
            {
                IpV6ParseResult ipv6 = IpV6Parser.Parse(
                    hostText,
                    authorityOffset + boundaries.HostOffset);
                if (!ipv6.Success)
                {
                    return CreateAuthorityFailure(ipv6.Diagnostic);
                }

                hostKind = UriHostKind.Ipv6;
                addressBytes = ipv6.AddressBytes;
            }
        }
        else
        {
            IpV4ParseResult ipv4 = IpV4Parser.Parse(
                hostText,
                authorityOffset + boundaries.HostOffset);
            if (ipv4.Success)
            {
                hostKind = UriHostKind.Ipv4;
                addressBytes = ipv4.AddressBytes;
            }
            else
            {
                UriDiagnostic? registeredName = AuthorityParser.ValidateRegisteredName(
                    hostText,
                    authorityOffset + boundaries.HostOffset);
                if (registeredName != null)
                {
                    return CreateAuthorityFailure(registeredName);
                }

                hostKind = UriHostKind.RegisteredName;
            }
        }

        if (boundaries.HasPort)
        {
            UriDiagnostic? port = AuthorityParser.ValidatePort(
                authorityText,
                boundaries.PortOffset,
                boundaries.PortLength,
                authorityOffset);
            if (port != null)
            {
                return CreateAuthorityFailure(port);
            }
        }

        UriAuthority authority = new UriAuthority();
        authority.HasUserInfo = boundaries.HasUserInfo;
        if (boundaries.HasUserInfo)
        {
            authority.UserInfo = authorityText.Slice(
                boundaries.UserInfoOffset,
                boundaries.UserInfoLength).ToString();
        }

        authority.HostKind = hostKind;
        authority.Host = hostText.ToString();
        authority.AddressBytes = addressBytes;
        authority.HasPort = boundaries.HasPort;
        if (boundaries.HasPort)
        {
            authority.Port = authorityText.Slice(
                boundaries.PortOffset,
                boundaries.PortLength).ToString();
        }

        return new AuthorityValueParseResult(true, authority, null);
    }

    internal static AuthorityValueParseResult CreateAuthorityFailure(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }
        else
        {
            failure = diagnostic;
        }

        return new AuthorityValueParseResult(false, new UriAuthority(), failure);
    }

    internal static UriParseResult CreateFailureResult(UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }
        else
        {
            failure = diagnostic;
        }

        return new UriParseResult(false, new UriReference(), failure);
    }

    internal static SchemeScanResult ScanOptionalScheme(ReadOnlySpan<char> text)
    {
        if (text.Length == 0 || !UriGrammar.IsSchemeStartCharacter(text[0]))
        {
            return new SchemeScanResult(true, false, 0, 0, null);
        }

        int offset = 1;
        while (offset < text.Length)
        {
            char value = text[offset];
            if (value == ':')
            {
                return new SchemeScanResult(true, true, offset, offset + 1, null);
            }

            if (!UriGrammar.IsSchemeCharacter(value))
            {
                return new SchemeScanResult(true, false, 0, 0, null);
            }

            offset = offset + 1;
        }

        return new SchemeScanResult(true, false, 0, 0, null);
    }

    internal static SchemeScanResult ScanRequiredScheme(ReadOnlySpan<char> text, int baseOffset)
    {
        if (text.Length == 0)
        {
            UriDiagnostic empty = DiagnosticFactory.CreateInvalidScheme(
                GetAbsoluteOffset(baseOffset, 0),
                0);
            return new SchemeScanResult(false, false, 0, 0, empty);
        }

        if (!UriGrammar.IsSchemeStartCharacter(text[0]))
        {
            UriDiagnostic invalidStart = DiagnosticFactory.CreateInvalidScheme(
                GetAbsoluteOffset(baseOffset, 0),
                1);
            return new SchemeScanResult(false, false, 0, 0, invalidStart);
        }

        int offset = 1;
        while (offset < text.Length)
        {
            char value = text[offset];
            if (value == ':')
            {
                return new SchemeScanResult(true, true, offset, offset + 1, null);
            }

            if (!UriGrammar.IsSchemeCharacter(value))
            {
                UriDiagnostic invalidCharacter = DiagnosticFactory.CreateInvalidScheme(
                    GetAbsoluteOffset(baseOffset, offset),
                    1);
                return new SchemeScanResult(false, false, 0, 0, invalidCharacter);
            }

            offset = offset + 1;
        }

        UriDiagnostic missingDelimiter = DiagnosticFactory.CreateInvalidScheme(
            GetAbsoluteOffset(baseOffset, text.Length),
            0);
        return new SchemeScanResult(false, false, 0, 0, missingDelimiter);
    }

    internal static UriReferenceKind ClassifyReference(ReadOnlySpan<char> text)
    {
        SchemeScanResult scheme = ScanOptionalScheme(text);
        if (scheme.HasScheme)
        {
            return UriReferenceKind.Uri;
        }

        if (text.Length == 0)
        {
            return UriReferenceKind.EmptyReference;
        }

        if (text[0] == '#')
        {
            return UriReferenceKind.SameDocumentReference;
        }

        if (text[0] == '/')
        {
            if (text.Length > 1 && text[1] == '/')
            {
                return UriReferenceKind.NetworkPathReference;
            }

            return UriReferenceKind.AbsolutePathReference;
        }

        if (text[0] == '?')
        {
            return UriReferenceKind.RelativeReference;
        }

        return UriReferenceKind.RelativePathReference;
    }

    internal static UriReferenceSplitResult SplitHierarchicalPart(
        ReadOnlySpan<char> text,
        int partOffset)
    {
        return SplitReferencePart(text, partOffset);
    }

    internal static UriReferenceSplitResult SplitRelativePart(
        ReadOnlySpan<char> text,
        int partOffset)
    {
        return SplitReferencePart(text, partOffset);
    }

    internal static UriReferenceSplitResult SplitReferencePart(
        ReadOnlySpan<char> text,
        int partOffset)
    {
        if (partOffset < 0 || partOffset > text.Length)
        {
            return new UriReferenceSplitResult(
                false,
                false,
                0,
                0,
                0,
                0,
                false,
                0,
                0,
                false,
                0,
                0,
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        int fragmentDelimiter = FindFirstCharacter(text, partOffset, text.Length, '#');
        int beforeFragmentEnd = fragmentDelimiter;
        if (fragmentDelimiter < 0)
        {
            beforeFragmentEnd = text.Length;
        }

        int queryDelimiter = FindFirstCharacter(text, partOffset, beforeFragmentEnd, '?');
        int partEnd = queryDelimiter;
        if (queryDelimiter < 0)
        {
            partEnd = beforeFragmentEnd;
        }

        bool hasAuthority = partEnd - partOffset >= 2 &&
            text[partOffset] == '/' &&
            text[partOffset + 1] == '/';
        int authorityOffset = partOffset;
        int authorityLength = 0;
        int pathOffset = partOffset;
        if (hasAuthority)
        {
            authorityOffset = partOffset + 2;
            int pathDelimiter = FindFirstCharacter(text, authorityOffset, partEnd, '/');
            if (pathDelimiter < 0)
            {
                pathOffset = partEnd;
            }
            else
            {
                pathOffset = pathDelimiter;
            }

            authorityLength = pathOffset - authorityOffset;
        }

        int pathLength = partEnd - pathOffset;
        bool hasQuery = queryDelimiter >= 0;
        int queryOffset = beforeFragmentEnd;
        int queryLength = 0;
        if (hasQuery)
        {
            queryOffset = queryDelimiter + 1;
            queryLength = beforeFragmentEnd - queryOffset;
        }

        bool hasFragment = fragmentDelimiter >= 0;
        int fragmentOffset = text.Length;
        int fragmentLength = 0;
        if (hasFragment)
        {
            fragmentOffset = fragmentDelimiter + 1;
            fragmentLength = text.Length - fragmentOffset;
        }

        return new UriReferenceSplitResult(
            true,
            hasAuthority,
            authorityOffset,
            authorityLength,
            pathOffset,
            pathLength,
            hasQuery,
            queryOffset,
            queryLength,
            hasFragment,
            fragmentOffset,
            fragmentLength,
            null);
    }

    internal static int FindFirstCharacter(
        ReadOnlySpan<char> text,
        int startOffset,
        int endOffset,
        char target)
    {
        int offset = startOffset;
        while (offset < endOffset)
        {
            if (text[offset] == target)
            {
                return offset;
            }

            offset = offset + 1;
        }

        return -1;
    }

    internal static int GetAbsoluteOffset(int baseOffset, int relativeOffset)
    {
        if (baseOffset < 0)
        {
            return relativeOffset;
        }

        if (relativeOffset > int.MaxValue - baseOffset)
        {
            return int.MaxValue;
        }

        return baseOffset + relativeOffset;
    }
}
