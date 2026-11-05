using System;

namespace uri;

internal static class IriAuthorityParser
{
    internal static AuthorityValueParseResult Parse(
        ReadOnlySpan<char> authorityText,
        int baseOffset)
    {
        AuthorityParseResult boundaries = AuthorityParser.ParseStructuralBoundaries(
            authorityText,
            baseOffset);
        if (!boundaries.Success)
        {
            return CreateFailure(boundaries.Diagnostic);
        }

        if (boundaries.HasUserInfo)
        {
            UriDiagnostic? userInfo = IriComponentScanner.ValidateUserInfo(
                authorityText.Slice(boundaries.UserInfoOffset, boundaries.UserInfoLength),
                UriParserCore.GetAbsoluteOffset(baseOffset, boundaries.UserInfoOffset));
            if (userInfo != null)
            {
                return CreateFailure(userInfo);
            }
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
                IpFutureParseResult future = IpFutureParser.Parse(
                    hostText,
                    UriParserCore.GetAbsoluteOffset(baseOffset, boundaries.HostOffset));
                if (!future.Success)
                {
                    return CreateFailure(future.Diagnostic);
                }

                hostKind = UriHostKind.IpvFuture;
            }
            else
            {
                IpV6ParseResult ipv6 = IpV6Parser.Parse(
                    hostText,
                    UriParserCore.GetAbsoluteOffset(baseOffset, boundaries.HostOffset));
                if (!ipv6.Success)
                {
                    return CreateFailure(ipv6.Diagnostic);
                }

                hostKind = UriHostKind.Ipv6;
                addressBytes = ipv6.AddressBytes;
            }
        }
        else
        {
            IpV4ParseResult ipv4 = IpV4Parser.Parse(
                hostText,
                UriParserCore.GetAbsoluteOffset(baseOffset, boundaries.HostOffset));
            if (ipv4.Success)
            {
                hostKind = UriHostKind.Ipv4;
                addressBytes = ipv4.AddressBytes;
            }
            else
            {
                UriDiagnostic? registeredName = IriComponentScanner.ValidateRegisteredName(
                    hostText,
                    UriParserCore.GetAbsoluteOffset(baseOffset, boundaries.HostOffset));
                if (registeredName != null)
                {
                    return CreateFailure(registeredName);
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
                baseOffset);
            if (port != null)
            {
                return CreateFailure(port);
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

    internal static AuthorityValueParseResult CreateFailure(UriDiagnostic? diagnostic)
    {
        return new AuthorityValueParseResult(
            false,
            new UriAuthority(),
            IriComponentScanner.MapDiagnostic(diagnostic));
    }
}
