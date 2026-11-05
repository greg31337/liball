using System;

namespace iso20022;

internal static class UriLexical
{
    internal static bool IsUriReference(ReadOnlySpan<char> text)
    {
        int fragmentIndex = -1;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character > 127 || character <= 32 || character == 127)
            {
                return false;
            }

            if (character == '%')
            {
                if (index + 2 >= text.Length || DecodeHexDigit(text[index + 1]) < 0 || DecodeHexDigit(text[index + 2]) < 0)
                {
                    return false;
                }

                index = index + 3;
                continue;
            }

            if (!IsUriCharacter(character))
            {
                return false;
            }

            if (character == '#')
            {
                if (fragmentIndex >= 0)
                {
                    return false;
                }

                fragmentIndex = index;
            }

            index++;
        }

        int schemeSeparator = FindSchemeSeparator(text);
        if (schemeSeparator >= 0 && !IsScheme(text.Slice(0, schemeSeparator)))
        {
            return false;
        }

        int pathStart = schemeSeparator >= 0 ? schemeSeparator + 1 : 0;
        bool hasAuthority = pathStart + 1 < text.Length && text[pathStart] == '/' && text[pathStart + 1] == '/';
        if (hasAuthority)
        {
            int authorityStart = pathStart + 2;
            int authorityEnd = FindComponentEnd(text, authorityStart);
            if (!IsAuthority(text.Slice(authorityStart, authorityEnd - authorityStart)))
            {
                return false;
            }

            if (authorityEnd < text.Length && text[authorityEnd] != '/' && text[authorityEnd] != '?' && text[authorityEnd] != '#')
            {
                return false;
            }

            if (ContainsSquareBracket(text.Slice(authorityEnd, text.Length - authorityEnd)))
            {
                return false;
            }
        }
        else if (schemeSeparator < 0)
        {
            if (ContainsSquareBracket(text))
            {
                return false;
            }

            int firstSegmentEnd = FindFirstSegmentEnd(text);
            index = 0;
            while (index < firstSegmentEnd)
            {
                if (text[index] == ':')
                {
                    return false;
                }

                index++;
            }
        }
        else if (ContainsSquareBracket(text.Slice(pathStart, text.Length - pathStart)))
        {
            return false;
        }

        return true;
    }

    internal static bool IsAbsoluteUri(ReadOnlySpan<char> text)
    {
        int separator = FindSchemeSeparator(text);
        return separator > 0 && IsScheme(text.Slice(0, separator)) && IsUriReference(text);
    }

    internal static bool IsNamespaceUri(ReadOnlySpan<char> text)
    {
        return text.Length > 0 && IsUriReference(text);
    }

    internal static bool IsUrn(ReadOnlySpan<char> text)
    {
        if (text.Length < 7 || !IsAsciiEqualIgnoreCase(text.Slice(0, 4), "urn:".AsSpan()))
        {
            return false;
        }

        int nidEnd = 4;
        while (nidEnd < text.Length && text[nidEnd] != ':')
        {
            nidEnd++;
        }

        if (nidEnd == text.Length)
        {
            return false;
        }

        ReadOnlySpan<char> nid = text.Slice(4, nidEnd - 4);
        if (!IsNamespaceIdentifier(nid))
        {
            return false;
        }

        int nssStart = nidEnd + 1;
        int nssEnd = FindUrnComponentStart(text, nssStart);
        if (nssEnd == nssStart || !IsUrnPathComponent(text.Slice(nssStart, nssEnd - nssStart), false))
        {
            return false;
        }

        int index = nssEnd;
        bool hasResourceComponent = false;
        bool hasQueryComponent = false;
        if (index + 1 < text.Length && text[index] == '?' && text[index + 1] == '+')
        {
            hasResourceComponent = true;
            int componentStart = index + 2;
            int componentEnd = FindQueryOrFragmentStart(text, componentStart);
            if (componentEnd == componentStart || !IsUrnPathComponent(text.Slice(componentStart, componentEnd - componentStart), true))
            {
                return false;
            }

            index = componentEnd;
        }

        if (index + 1 < text.Length && text[index] == '?' && text[index + 1] == '=')
        {
            hasQueryComponent = true;
            int componentStart = index + 2;
            int componentEnd = FindFragmentStart(text, componentStart);
            if (componentEnd == componentStart || !IsUrnPathComponent(text.Slice(componentStart, componentEnd - componentStart), true))
            {
                return false;
            }

            index = componentEnd;
        }

        if (index < text.Length && text[index] == '#')
        {
            index++;
            if (!IsUrnFragmentComponent(text.Slice(index, text.Length - index)))
            {
                return false;
            }

            index = text.Length;
        }

        if (!hasResourceComponent && !hasQueryComponent && nssEnd < text.Length && text[nssEnd] != '#')
        {
            return false;
        }

        return index == text.Length;
    }

    internal static int FindSchemeSeparator(ReadOnlySpan<char> text)
    {
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character == ':')
            {
                return index;
            }

            if (character == '/' || character == '?' || character == '#')
            {
                return -1;
            }

            index++;
        }

        return -1;
    }

    internal static bool IsScheme(ReadOnlySpan<char> text)
    {
        if (text.Length == 0 || !IsAsciiLetter(text[0]))
        {
            return false;
        }

        int index = 1;
        while (index < text.Length)
        {
            char character = text[index];
            if (!IsAsciiLetter(character) && !IsAsciiDigit(character) && character != '+' && character != '-' && character != '.')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool IsNamespaceIdentifier(ReadOnlySpan<char> text)
    {
        if (text.Length < 2 || text.Length > 32 || !IsAsciiLetterOrDigit(text[0]) || !IsAsciiLetterOrDigit(text[text.Length - 1]))
        {
            return false;
        }

        if (IsAsciiEqualIgnoreCase(text, "urn".AsSpan()))
        {
            return false;
        }

        int index = 1;
        while (index < text.Length - 1)
        {
            char character = text[index];
            if (!IsAsciiLetterOrDigit(character) && character != '-')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool IsUrnPathComponent(ReadOnlySpan<char> text, bool allowQuestionMark)
    {
        if (text.Length == 0 || text[0] == '/' || text[0] == '?')
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character == '%')
            {
                if (index + 2 >= text.Length || DecodeHexDigit(text[index + 1]) < 0 || DecodeHexDigit(text[index + 2]) < 0)
                {
                    return false;
                }

                index = index + 3;
                continue;
            }

            if (!IsUnreserved(character) && !IsSubDelimiter(character) && character != ':' && character != '@' && character != '/' && (!allowQuestionMark || character != '?'))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool IsUrnFragmentComponent(ReadOnlySpan<char> text)
    {
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character == '%')
            {
                if (index + 2 >= text.Length || DecodeHexDigit(text[index + 1]) < 0 || DecodeHexDigit(text[index + 2]) < 0)
                {
                    return false;
                }

                index = index + 3;
                continue;
            }

            if (!IsUnreserved(character) && !IsSubDelimiter(character) && character != ':' && character != '@' && character != '/' && character != '?')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static int FindUrnComponentStart(ReadOnlySpan<char> text, int start)
    {
        int index = start;
        while (index < text.Length)
        {
            if (text[index] == '#' || (text[index] == '?' && index + 1 < text.Length && (text[index + 1] == '+' || text[index + 1] == '=')))
            {
                return index;
            }

            index++;
        }

        return text.Length;
    }

    internal static int FindQueryOrFragmentStart(ReadOnlySpan<char> text, int start)
    {
        int index = start;
        while (index < text.Length)
        {
            if (text[index] == '#' || (text[index] == '?' && index + 1 < text.Length && text[index + 1] == '='))
            {
                return index;
            }

            index++;
        }

        return text.Length;
    }

    internal static int FindFragmentStart(ReadOnlySpan<char> text, int start)
    {
        int index = start;
        while (index < text.Length)
        {
            if (text[index] == '#')
            {
                return index;
            }

            index++;
        }

        return text.Length;
    }

    internal static int FindComponentEnd(ReadOnlySpan<char> text, int start)
    {
        int index = start;
        while (index < text.Length && text[index] != '/' && text[index] != '?' && text[index] != '#')
        {
            index++;
        }

        return index;
    }

    internal static int FindFirstSegmentEnd(ReadOnlySpan<char> text)
    {
        int index = 0;
        while (index < text.Length && text[index] != '/' && text[index] != '?' && text[index] != '#')
        {
            index++;
        }

        return index;
    }

    internal static bool IsAuthority(ReadOnlySpan<char> text)
    {
        int atIndex = -1;
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '@')
            {
                if (atIndex >= 0)
                {
                    return false;
                }

                atIndex = index;
            }

            index++;
        }

        int hostStart = 0;
        if (atIndex >= 0)
        {
            if (!IsUserInfo(text.Slice(0, atIndex)))
            {
                return false;
            }

            hostStart = atIndex + 1;
        }

        ReadOnlySpan<char> hostAndPort = text.Slice(hostStart, text.Length - hostStart);
        if (hostAndPort.Length > 0 && hostAndPort[0] == '[')
        {
            int closingBracket = -1;
            index = 1;
            while (index < hostAndPort.Length)
            {
                if (hostAndPort[index] == ']')
                {
                    closingBracket = index;
                    break;
                }

                index++;
            }

            if (closingBracket < 0 || !IsIpLiteral(hostAndPort.Slice(1, closingBracket - 1)))
            {
                return false;
            }

            if (closingBracket == hostAndPort.Length - 1)
            {
                return true;
            }

            if (hostAndPort[closingBracket + 1] != ':')
            {
                return false;
            }

            return IsPort(hostAndPort.Slice(closingBracket + 2, hostAndPort.Length - closingBracket - 2));
        }

        if (ContainsSquareBracket(hostAndPort))
        {
            return false;
        }

        int portSeparator = -1;
        index = 0;
        while (index < hostAndPort.Length)
        {
            if (hostAndPort[index] == ':')
            {
                if (portSeparator >= 0)
                {
                    return false;
                }

                portSeparator = index;
            }

            index++;
        }

        if (portSeparator < 0)
        {
            return IsRegName(hostAndPort);
        }

        return IsRegName(hostAndPort.Slice(0, portSeparator)) && IsPort(hostAndPort.Slice(portSeparator + 1, hostAndPort.Length - portSeparator - 1));
    }

    internal static bool IsUserInfo(ReadOnlySpan<char> text)
    {
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character == '%')
            {
                index = index + 3;
                continue;
            }

            if (!IsUnreserved(character) && !IsSubDelimiter(character) && character != ':')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool IsRegName(ReadOnlySpan<char> text)
    {
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character == '%')
            {
                index = index + 3;
                continue;
            }

            if (!IsUnreserved(character) && !IsSubDelimiter(character))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool IsPort(ReadOnlySpan<char> text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (!IsAsciiDigit(text[index]))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool IsIpLiteral(ReadOnlySpan<char> text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        if (text[0] == 'v' || text[0] == 'V')
        {
            return IsIpvFuture(text);
        }

        return IsIpv6Address(text);
    }

    internal static bool IsIpvFuture(ReadOnlySpan<char> text)
    {
        int index = 1;
        int versionStart = index;
        while (index < text.Length && DecodeHexDigit(text[index]) >= 0)
        {
            index++;
        }

        if (index == versionStart || index >= text.Length || text[index] != '.')
        {
            return false;
        }

        index++;
        if (index == text.Length)
        {
            return false;
        }

        while (index < text.Length)
        {
            char character = text[index];
            if (!IsUnreserved(character) && !IsSubDelimiter(character) && character != ':')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool IsIpv6Address(ReadOnlySpan<char> text)
    {
        int index = 0;
        int groupCount = 0;
        bool compressed = false;
        if (text.Length >= 2 && text[0] == ':' && text[1] == ':')
        {
            compressed = true;
            index = 2;
            if (index == text.Length)
            {
                return true;
            }
        }
        else if (text[0] == ':')
        {
            return false;
        }

        while (index < text.Length)
        {
            int segmentStart = index;
            bool hasPeriod = false;
            while (index < text.Length && text[index] != ':')
            {
                if (text[index] == '.')
                {
                    hasPeriod = true;
                }

                index++;
            }

            ReadOnlySpan<char> segment = text.Slice(segmentStart, index - segmentStart);
            if (segment.Length == 0)
            {
                return false;
            }

            if (hasPeriod)
            {
                if (index != text.Length || !IsIpv4Address(segment))
                {
                    return false;
                }

                groupCount = groupCount + 2;
            }
            else
            {
                if (segment.Length > 4 || !IsHexSequence(segment))
                {
                    return false;
                }

                groupCount++;
            }

            if (groupCount > 8)
            {
                return false;
            }

            if (index < text.Length)
            {
                if (index + 1 < text.Length && text[index + 1] == ':')
                {
                    if (compressed)
                    {
                        return false;
                    }

                    compressed = true;
                    index = index + 2;
                    if (index == text.Length)
                    {
                        break;
                    }
                }
                else
                {
                    index++;
                    if (index == text.Length)
                    {
                        return false;
                    }
                }
            }
        }

        if (compressed)
        {
            return groupCount < 8;
        }

        return groupCount == 8;
    }

    internal static bool IsIpv4Address(ReadOnlySpan<char> text)
    {
        int partCount = 0;
        int index = 0;
        while (index < text.Length)
        {
            int partStart = index;
            int value = 0;
            while (index < text.Length && text[index] != '.')
            {
                if (!IsAsciiDigit(text[index]))
                {
                    return false;
                }

                value = value * 10 + text[index] - '0';
                if (value > 255)
                {
                    return false;
                }

                index++;
            }

            int partLength = index - partStart;
            if (partLength == 0 || partLength > 3 || (partLength > 1 && text[partStart] == '0'))
            {
                return false;
            }

            partCount++;
            if (index < text.Length)
            {
                index++;
                if (index == text.Length)
                {
                    return false;
                }
            }
        }

        return partCount == 4;
    }

    internal static bool IsHexSequence(ReadOnlySpan<char> text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (DecodeHexDigit(text[index]) < 0)
            {
                return false;
            }

            index++;
        }

        return text.Length > 0;
    }

    internal static bool ContainsSquareBracket(ReadOnlySpan<char> text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '[' || text[index] == ']')
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static bool IsUriCharacter(char character)
    {
        return IsUnreserved(character)
            || IsSubDelimiter(character)
            || character == ':'
            || character == '/'
            || character == '?'
            || character == '#'
            || character == '['
            || character == ']'
            || character == '@';
    }

    internal static bool IsUnreserved(char character)
    {
        return IsAsciiLetterOrDigit(character) || character == '-' || character == '.' || character == '_' || character == '~';
    }

    internal static bool IsSubDelimiter(char character)
    {
        return character == '!' || character == '$' || character == '&' || character == '\'' || character == '(' || character == ')' || character == '*' || character == '+' || character == ',' || character == ';' || character == '=';
    }

    internal static bool IsAsciiLetter(char character)
    {
        return (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z');
    }

    internal static bool IsAsciiDigit(char character)
    {
        return character >= '0' && character <= '9';
    }

    internal static bool IsAsciiLetterOrDigit(char character)
    {
        return IsAsciiLetter(character) || IsAsciiDigit(character);
    }

    internal static int DecodeHexDigit(char character)
    {
        if (character >= '0' && character <= '9')
        {
            return character - '0';
        }

        if (character >= 'A' && character <= 'F')
        {
            return character - 'A' + 10;
        }

        if (character >= 'a' && character <= 'f')
        {
            return character - 'a' + 10;
        }

        return -1;
    }

    internal static bool IsAsciiEqualIgnoreCase(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            char leftCharacter = ToLowerAscii(left[index]);
            char rightCharacter = ToLowerAscii(right[index]);
            if (leftCharacter != rightCharacter)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static char ToLowerAscii(char character)
    {
        if (character >= 'A' && character <= 'Z')
        {
            return (char)(character + 32);
        }

        return character;
    }
}
