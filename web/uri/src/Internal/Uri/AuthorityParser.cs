using System;

namespace uri;

internal static class AuthorityParser
{
    internal static AuthorityParseResult ParseBoundaries(
        ReadOnlySpan<char> authority,
        int baseOffset)
    {
        return ParseAuthorityBoundaries(authority, baseOffset, true);
    }

    internal static AuthorityParseResult ParseStructuralBoundaries(
        ReadOnlySpan<char> authority,
        int baseOffset)
    {
        return ParseAuthorityBoundaries(authority, baseOffset, false);
    }

    internal static AuthorityParseResult ParseAuthorityBoundaries(
        ReadOnlySpan<char> authority,
        int baseOffset,
        bool validatePort)
    {
        int userInfoDelimiter = FindLastCharacter(authority, '@');
        bool hasUserInfo = userInfoDelimiter >= 0;
        int userInfoLength = 0;
        int hostStart = 0;
        if (hasUserInfo)
        {
            userInfoLength = userInfoDelimiter;
            hostStart = userInfoDelimiter + 1;
        }

        if (hostStart < authority.Length && authority[hostStart] == '[')
        {
            return ParseBracketedHost(
                authority,
                baseOffset,
                hasUserInfo,
                userInfoLength,
                hostStart,
                validatePort);
        }

        return ParseUnbracketedHost(
            authority,
            baseOffset,
            hasUserInfo,
            userInfoLength,
            hostStart,
            validatePort);
    }

    internal static AuthorityParseResult ParseBracketedHost(
        ReadOnlySpan<char> authority,
        int baseOffset,
        bool hasUserInfo,
        int userInfoLength,
        int hostStart,
        bool validatePort)
    {
        int closeBracket = -1;
        int offset = hostStart + 1;
        while (offset < authority.Length)
        {
            if (authority[offset] == '[')
            {
                return CreateHostFailure(
                    baseOffset,
                    offset,
                    1);
            }

            if (authority[offset] == ']')
            {
                closeBracket = offset;
                break;
            }

            offset = offset + 1;
        }

        if (closeBracket < 0)
        {
            return CreateHostFailure(
                baseOffset,
                authority.Length,
                0);
        }

        int hostOffset = hostStart + 1;
        int hostLength = closeBracket - hostOffset;
        int afterHost = closeBracket + 1;
        bool hasPort = false;
        int portOffset = afterHost;
        int portLength = 0;
        if (afterHost < authority.Length)
        {
            if (authority[afterHost] != ':')
            {
                return CreateHostFailure(
                    baseOffset,
                    afterHost,
                    1);
            }

            hasPort = true;
            portOffset = afterHost + 1;
            portLength = authority.Length - portOffset;
            if (validatePort)
            {
                UriDiagnostic? invalidPort = ValidatePort(
                    authority,
                    portOffset,
                    portLength,
                    baseOffset);
                if (invalidPort != null)
                {
                    return CreateFailure(invalidPort);
                }
            }
        }

        return new AuthorityParseResult(
            true,
            hasUserInfo,
            0,
            userInfoLength,
            true,
            hostOffset,
            hostLength,
            hasPort,
            portOffset,
            portLength,
            null);
    }

    internal static AuthorityParseResult ParseUnbracketedHost(
        ReadOnlySpan<char> authority,
        int baseOffset,
        bool hasUserInfo,
        int userInfoLength,
        int hostStart,
        bool validatePort)
    {
        int portDelimiter = -1;
        int offset = hostStart;
        while (offset < authority.Length)
        {
            char value = authority[offset];
            if (value == '[' || value == ']')
            {
                return CreateHostFailure(baseOffset, offset, 1);
            }

            if (value == ':' && portDelimiter < 0)
            {
                portDelimiter = offset;
            }

            offset = offset + 1;
        }

        int hostLength = authority.Length - hostStart;
        bool hasPort = portDelimiter >= 0;
        int portOffset = authority.Length;
        int portLength = 0;
        if (hasPort)
        {
            hostLength = portDelimiter - hostStart;
            portOffset = portDelimiter + 1;
            portLength = authority.Length - portOffset;
            if (validatePort)
            {
                UriDiagnostic? invalidPort = ValidatePort(
                    authority,
                    portOffset,
                    portLength,
                    baseOffset);
                if (invalidPort != null)
                {
                    return CreateFailure(invalidPort);
                }
            }
        }

        return new AuthorityParseResult(
            true,
            hasUserInfo,
            0,
            userInfoLength,
            false,
            hostStart,
            hostLength,
            hasPort,
            portOffset,
            portLength,
            null);
    }

    internal static UriDiagnostic? ValidatePort(
        ReadOnlySpan<char> authority,
        int portOffset,
        int portLength,
        int baseOffset)
    {
        int offset = portOffset;
        int endOffset = portOffset + portLength;
        while (offset < endOffset)
        {
            if (!Ascii.IsDigit(authority[offset]))
            {
                return DiagnosticFactory.CreateInvalidPort(
                    UriParserCore.GetAbsoluteOffset(baseOffset, offset),
                    1);
            }

            offset = offset + 1;
        }

        return null;
    }

    internal static UriDiagnostic? ValidateUserInfo(
        ReadOnlySpan<char> userInfo,
        int baseOffset)
    {
        int offset = 0;
        while (offset < userInfo.Length)
        {
            if (userInfo[offset] == '%')
            {
                PercentTripletResult triplet = PercentScanner.ScanTriplet(
                    userInfo,
                    offset,
                    baseOffset,
                    UriComponent.UserInfo);
                if (!triplet.Success)
                {
                    return CreateUriPercentFailure(
                        triplet,
                        UriComponent.UserInfo,
                        baseOffset,
                        offset);
                }

                offset = triplet.NextOffset;
                continue;
            }

            if (!PercentAllowlist.IsUserInfoCharacter(userInfo[offset]))
            {
                return DiagnosticFactory.CreateInvalidUserInfo(
                    UriParserCore.GetAbsoluteOffset(baseOffset, offset),
                    1);
            }

            offset = offset + 1;
        }

        return null;
    }

    internal static UriDiagnostic? ValidateRegisteredName(
        ReadOnlySpan<char> registeredName,
        int baseOffset)
    {
        int offset = 0;
        while (offset < registeredName.Length)
        {
            if (registeredName[offset] == '%')
            {
                PercentTripletResult triplet = PercentScanner.ScanTriplet(
                    registeredName,
                    offset,
                    baseOffset,
                    UriComponent.Host);
                if (!triplet.Success)
                {
                    return CreateUriPercentFailure(
                        triplet,
                        UriComponent.Host,
                        baseOffset,
                        offset);
                }

                offset = triplet.NextOffset;
                continue;
            }

            if (!PercentAllowlist.IsRegisteredNameCharacter(registeredName[offset]))
            {
                return DiagnosticFactory.CreateInvalidHost(
                    UriParserCore.GetAbsoluteOffset(baseOffset, offset),
                    1);
            }

            offset = offset + 1;
        }

        return null;
    }

    internal static bool AreRegisteredNamesEqual(
        ReadOnlySpan<char> left,
        ReadOnlySpan<char> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int offset = 0;
        while (offset < left.Length)
        {
            if (AsciiCase.FoldToLower(left[offset]) != AsciiCase.FoldToLower(right[offset]))
            {
                return false;
            }

            offset = offset + 1;
        }

        return true;
    }

    internal static UriDiagnostic CreateUriPercentFailure(
        PercentTripletResult triplet,
        UriComponent component,
        int baseOffset,
        int relativeOffset)
    {
        UriDiagnostic? diagnostic = triplet.Diagnostic;
        if (diagnostic != null)
        {
            return DiagnosticFactory.CreateUriInvalidPercentEncoding(
                component,
                diagnostic.Range.Offset,
                diagnostic.Range.Length);
        }

        return DiagnosticFactory.CreateUriInvalidPercentEncoding(
            component,
            UriParserCore.GetAbsoluteOffset(baseOffset, relativeOffset),
            1);
    }

    internal static int FindLastCharacter(ReadOnlySpan<char> text, char target)
    {
        int offset = text.Length - 1;
        while (offset >= 0)
        {
            if (text[offset] == target)
            {
                return offset;
            }

            offset = offset - 1;
        }

        return -1;
    }

    internal static AuthorityParseResult CreateHostFailure(
        int baseOffset,
        int relativeOffset,
        int length)
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateInvalidHost(
            UriParserCore.GetAbsoluteOffset(baseOffset, relativeOffset),
            length);
        return CreateFailure(diagnostic);
    }

    internal static AuthorityParseResult CreateFailure(UriDiagnostic diagnostic)
    {
        return new AuthorityParseResult(
            false,
            false,
            0,
            0,
            false,
            0,
            0,
            false,
            0,
            0,
            diagnostic);
    }
}
