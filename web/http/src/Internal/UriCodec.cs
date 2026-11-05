namespace http;

internal static class UriCodec
{
    internal static UriValidationResult ValidateUriReference(string value, int maximumSize)
    {
        UriValidationResult inputResult = ValidateInput(value, maximumSize);
        if (inputResult.Status != HttpOperationStatus.Done)
        {
            return inputResult;
        }

        UriValidationResult result = CreateDone(value.Length);
        int fragmentPosition = FindCharacter(value, 0, value.Length, '#');
        int mainEnd = value.Length;
        if (fragmentPosition >= 0)
        {
            result.HasFragment = true;
            result.Fragment = value.Substring(fragmentPosition + 1, value.Length - fragmentPosition - 1);
            UriValidationResult fragmentResult = ValidateQueryOrFragment(
                result.Fragment,
                maximumSize,
                true);
            if (fragmentResult.Status != HttpOperationStatus.Done)
            {
                fragmentResult.ErrorPosition = fragmentPosition + 1 + fragmentResult.ErrorPosition;
                return fragmentResult;
            }

            mainEnd = fragmentPosition;
        }

        int extraFragment = FindCharacter(value, fragmentPosition + 1, value.Length, '#');
        if (fragmentPosition >= 0 && extraFragment >= 0)
        {
            return CreateMalformed(extraFragment);
        }

        int queryPosition = FindCharacter(value, 0, mainEnd, '?');
        int hierarchyEnd = mainEnd;
        if (queryPosition >= 0)
        {
            result.HasQuery = true;
            result.Query = value.Substring(queryPosition + 1, mainEnd - queryPosition - 1);
            UriValidationResult queryResult = ValidateQueryOrFragment(result.Query, maximumSize, false);
            if (queryResult.Status != HttpOperationStatus.Done)
            {
                queryResult.ErrorPosition = queryPosition + 1 + queryResult.ErrorPosition;
                return queryResult;
            }

            hierarchyEnd = queryPosition;
        }

        int firstSlash = FindCharacter(value, 0, hierarchyEnd, '/');
        int colon = FindCharacter(value, 0, hierarchyEnd, ':');
        int hierarchyStart = 0;
        if (colon >= 0 && (firstSlash < 0 || colon < firstSlash))
        {
            string scheme = value.Substring(0, colon);
            UriValidationResult schemeResult = ValidateScheme(scheme, maximumSize);
            if (schemeResult.Status != HttpOperationStatus.Done)
            {
                return schemeResult;
            }

            result.IsAbsolute = true;
            result.Scheme = scheme;
            hierarchyStart = colon + 1;
        }

        if (hierarchyStart + 1 < hierarchyEnd && value[hierarchyStart] == '/' &&
            value[hierarchyStart + 1] == '/')
        {
            int authorityStart = hierarchyStart + 2;
            int authorityEnd = FindCharacter(value, authorityStart, hierarchyEnd, '/');
            if (authorityEnd < 0)
            {
                authorityEnd = hierarchyEnd;
            }

            string authority = value.Substring(authorityStart, authorityEnd - authorityStart);
            UriValidationResult authorityResult = ValidateAuthority(authority, maximumSize);
            if (authorityResult.Status != HttpOperationStatus.Done)
            {
                authorityResult.ErrorPosition = authorityStart + authorityResult.ErrorPosition;
                return authorityResult;
            }

            CopyAuthorityResult(result, authorityResult);
            result.Path = value.Substring(authorityEnd, hierarchyEnd - authorityEnd);
            UriValidationResult pathResult = ValidatePath(result.Path, maximumSize);
            if (pathResult.Status != HttpOperationStatus.Done)
            {
                pathResult.ErrorPosition = authorityEnd + pathResult.ErrorPosition;
                return pathResult;
            }
        }
        else
        {
            result.Path = value.Substring(hierarchyStart, hierarchyEnd - hierarchyStart);
            UriValidationResult pathResult = ValidatePath(result.Path, maximumSize);
            if (pathResult.Status != HttpOperationStatus.Done)
            {
                pathResult.ErrorPosition = hierarchyStart + pathResult.ErrorPosition;
                return pathResult;
            }

            if (!result.IsAbsolute && result.Path.Length > 0 && result.Path[0] != '/')
            {
                int segmentEnd = FindCharacter(result.Path, 0, result.Path.Length, '/');
                if (segmentEnd < 0)
                {
                    segmentEnd = result.Path.Length;
                }

                int relativeColon = FindCharacter(result.Path, 0, segmentEnd, ':');
                if (relativeColon >= 0)
                {
                    return CreateMalformed(hierarchyStart + relativeColon);
                }
            }
        }

        result.Consumed = value.Length;
        return result;
    }

    internal static UriValidationResult ValidateScheme(string value, int maximumSize)
    {
        UriValidationResult inputResult = ValidateInput(value, maximumSize);
        if (inputResult.Status != HttpOperationStatus.Done)
        {
            return inputResult;
        }

        if (value.Length == 0 || !IsAlpha(value[0]))
        {
            return CreateMalformed(0);
        }

        int index = 1;
        while (index < value.Length)
        {
            char character = value[index];
            if (!IsAlpha(character) && !IsDigit(character) && character != '+' &&
                character != '-' && character != '.')
            {
                return CreateMalformed(index);
            }

            index = index + 1;
        }

        UriValidationResult result = CreateDone(value.Length);
        result.Scheme = value;
        return result;
    }

    internal static UriValidationResult ValidateAuthority(string value, int maximumSize)
    {
        UriValidationResult inputResult = ValidateInput(value, maximumSize);
        if (inputResult.Status != HttpOperationStatus.Done)
        {
            return inputResult;
        }

        UriValidationResult result = CreateDone(value.Length);
        result.HasAuthority = true;
        result.Authority = value;
        int at = FindCharacter(value, 0, value.Length, '@');
        int hostStart = 0;
        if (at >= 0)
        {
            string userInfo = value.Substring(0, at);
            UriValidationResult userInfoResult = ValidateUserInfo(userInfo, maximumSize);
            if (userInfoResult.Status != HttpOperationStatus.Done)
            {
                return userInfoResult;
            }

            result.HasUserInfo = true;
            result.UserInfo = userInfo;
            hostStart = at + 1;
        }

        int extraAt = FindCharacter(value, hostStart, value.Length, '@');
        if (extraAt >= 0)
        {
            return CreateMalformed(extraAt);
        }

        int hostEnd = value.Length;
        if (hostStart < value.Length && value[hostStart] == '[')
        {
            int close = FindCharacter(value, hostStart + 1, value.Length, ']');
            if (close < 0)
            {
                return CreateMalformed(hostStart);
            }

            hostEnd = close + 1;
            if (hostEnd < value.Length)
            {
                if (value[hostEnd] != ':')
                {
                    return CreateMalformed(hostEnd);
                }

                result.HasPort = true;
                result.PortText = value.Substring(hostEnd + 1);
            }
        }
        else
        {
            int colon = FindCharacter(value, hostStart, value.Length, ':');
            if (colon >= 0)
            {
                hostEnd = colon;
                result.HasPort = true;
                result.PortText = value.Substring(colon + 1);
                int extraColon = FindCharacter(value, colon + 1, value.Length, ':');
                if (extraColon >= 0)
                {
                    return CreateMalformed(extraColon);
                }
            }
        }

        result.Host = value.Substring(hostStart, hostEnd - hostStart);
        UriValidationResult hostResult = ValidateHost(result.Host, maximumSize);
        if (hostResult.Status != HttpOperationStatus.Done)
        {
            hostResult.ErrorPosition = hostStart + hostResult.ErrorPosition;
            return hostResult;
        }

        result.IsIpLiteral = hostResult.IsIpLiteral;
        if (result.HasPort)
        {
            UriValidationResult portResult = ValidatePort(result.PortText, maximumSize);
            if (portResult.Status != HttpOperationStatus.Done)
            {
                portResult.ErrorPosition = hostEnd + 1 + portResult.ErrorPosition;
                return portResult;
            }

            result.HasPortValue = portResult.HasPortValue;
            result.IsPortInRange = portResult.IsPortInRange;
            result.Port = portResult.Port;
        }

        return result;
    }

    internal static UriValidationResult ValidateHost(string value, int maximumSize)
    {
        UriValidationResult inputResult = ValidateInput(value, maximumSize);
        if (inputResult.Status != HttpOperationStatus.Done)
        {
            return inputResult;
        }

        UriValidationResult result = CreateDone(value.Length);
        result.Host = value;
        if (value.Length == 0)
        {
            return result;
        }

        if (value[0] == '[')
        {
            if (value.Length < 2 || value[value.Length - 1] != ']')
            {
                return CreateMalformed(0);
            }

            string literal = value.Substring(1, value.Length - 2);
            bool validLiteral;
            if (literal.Length > 0 && (literal[0] == 'v' || literal[0] == 'V'))
            {
                validLiteral = IsValidIpvFuture(literal);
            }
            else
            {
                validLiteral = IsValidIpv6(literal);
            }

            if (!validLiteral)
            {
                return CreateMalformed(1);
            }

            result.IsIpLiteral = true;
            return result;
        }

        if (ContainsOnlyDigitsAndDots(value) && ContainsCharacter(value, '.') && IsValidIpv4(value))
        {
            return result;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '%')
            {
                if (!IsValidPercentAt(value, index))
                {
                    return CreateMalformed(index);
                }

                index = index + 3;
                continue;
            }

            if (!IsUnreserved(character) && !IsSubDelimiter(character))
            {
                return CreateMalformed(index);
            }

            index = index + 1;
        }

        return result;
    }

    internal static UriValidationResult ValidatePort(string value, int maximumSize)
    {
        UriValidationResult inputResult = ValidateInput(value, maximumSize);
        if (inputResult.Status != HttpOperationStatus.Done)
        {
            return inputResult;
        }

        UriValidationResult result = CreateDone(value.Length);
        result.HasPort = true;
        result.PortText = value;
        if (value.Length == 0)
        {
            return result;
        }

        int port = 0;
        bool inRange = true;
        int index = 0;
        while (index < value.Length)
        {
            if (!IsDigit(value[index]))
            {
                return CreateMalformed(index);
            }

            int digit = value[index] - '0';
            if (inRange)
            {
                if (port > (65535 - digit) / 10)
                {
                    inRange = false;
                }
                else
                {
                    port = port * 10 + digit;
                }
            }

            index = index + 1;
        }

        result.HasPortValue = true;
        result.IsPortInRange = inRange;
        if (inRange)
        {
            result.Port = port;
        }

        return result;
    }

    internal static UriValidationResult ValidatePath(string value, int maximumSize)
    {
        UriValidationResult result = ValidateComponent(value, maximumSize, false);
        if (result.Status == HttpOperationStatus.Done)
        {
            result.Path = value;
        }

        return result;
    }

    internal static UriValidationResult ValidateQuery(string value, int maximumSize)
    {
        UriValidationResult result = ValidateQueryOrFragment(value, maximumSize, false);
        if (result.Status == HttpOperationStatus.Done)
        {
            result.HasQuery = true;
            result.Query = value;
        }

        return result;
    }

    internal static UriValidationResult ValidateFragment(string value, int maximumSize)
    {
        UriValidationResult result = ValidateQueryOrFragment(value, maximumSize, true);
        if (result.Status == HttpOperationStatus.Done)
        {
            result.HasFragment = true;
            result.Fragment = value;
        }

        return result;
    }

    internal static UriValidationResult ValidatePercentEncoding(string value, int maximumSize)
    {
        UriValidationResult inputResult = ValidateInput(value, maximumSize);
        if (inputResult.Status != HttpOperationStatus.Done)
        {
            return inputResult;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '%')
            {
                if (!IsValidPercentAt(value, index))
                {
                    return CreateMalformed(index);
                }

                index = index + 3;
            }
            else
            {
                if (value[index] > 0x7f)
                {
                    return CreateMalformed(index);
                }

                index = index + 1;
            }
        }

        return CreateDone(value.Length);
    }

    static UriValidationResult ValidateUserInfo(string value, int maximumSize)
    {
        UriValidationResult inputResult = ValidateInput(value, maximumSize);
        if (inputResult.Status != HttpOperationStatus.Done)
        {
            return inputResult;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '%')
            {
                if (!IsValidPercentAt(value, index))
                {
                    return CreateMalformed(index);
                }

                index = index + 3;
                continue;
            }

            if (!IsUnreserved(character) && !IsSubDelimiter(character) && character != ':')
            {
                return CreateMalformed(index);
            }

            index = index + 1;
        }

        return CreateDone(value.Length);
    }

    static UriValidationResult ValidateComponent(string value, int maximumSize, bool queryCharacters)
    {
        UriValidationResult inputResult = ValidateInput(value, maximumSize);
        if (inputResult.Status != HttpOperationStatus.Done)
        {
            return inputResult;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '%')
            {
                if (!IsValidPercentAt(value, index))
                {
                    return CreateMalformed(index);
                }

                index = index + 3;
                continue;
            }

            bool valid = IsPchar(character) || character == '/';
            if (queryCharacters && character == '?')
            {
                valid = true;
            }

            if (!valid)
            {
                return CreateMalformed(index);
            }

            index = index + 1;
        }

        return CreateDone(value.Length);
    }

    static UriValidationResult ValidateQueryOrFragment(string value, int maximumSize, bool fragment)
    {
        UriValidationResult result = ValidateComponent(value, maximumSize, true);
        if (result.Status != HttpOperationStatus.Done)
        {
            return result;
        }

        if (ContainsCharacter(value, '#'))
        {
            return CreateMalformed(FindCharacter(value, 0, value.Length, '#'));
        }

        if (fragment)
        {
            result.HasFragment = true;
            result.Fragment = value;
        }
        else
        {
            result.HasQuery = true;
            result.Query = value;
        }

        return result;
    }

    static bool IsValidIpv4(string value)
    {
        int parts = 0;
        int start = 0;
        while (start <= value.Length)
        {
            int end = FindCharacter(value, start, value.Length, '.');
            if (end < 0)
            {
                end = value.Length;
            }

            int length = end - start;
            if (length < 1 || length > 3 || length > 1 && value[start] == '0')
            {
                return false;
            }

            int number = 0;
            int index = start;
            while (index < end)
            {
                if (!IsDigit(value[index]))
                {
                    return false;
                }

                number = number * 10 + value[index] - '0';
                index = index + 1;
            }

            if (number > 255)
            {
                return false;
            }

            parts = parts + 1;
            if (end == value.Length)
            {
                break;
            }

            start = end + 1;
        }

        return parts == 4;
    }

    static bool IsValidIpv6(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int groups = 0;
        int position = 0;
        bool compressed = false;
        if (value.Length >= 2 && value[0] == ':' && value[1] == ':')
        {
            compressed = true;
            position = 2;
            if (position == value.Length)
            {
                return true;
            }
        }
        else if (value[0] == ':')
        {
            return false;
        }

        while (position < value.Length)
        {
            int end = FindCharacter(value, position, value.Length, ':');
            if (end < 0)
            {
                end = value.Length;
            }

            if (end == position)
            {
                return false;
            }

            string component = value.Substring(position, end - position);
            if (ContainsCharacter(component, '.'))
            {
                if (end != value.Length || !IsValidIpv4(component))
                {
                    return false;
                }

                groups = groups + 2;
            }
            else
            {
                if (component.Length < 1 || component.Length > 4)
                {
                    return false;
                }

                int characterIndex = 0;
                while (characterIndex < component.Length)
                {
                    if (!IsHexDigit(component[characterIndex]))
                    {
                        return false;
                    }

                    characterIndex = characterIndex + 1;
                }

                groups = groups + 1;
            }

            if (groups > 8 || end == value.Length)
            {
                break;
            }

            if (end + 1 < value.Length && value[end + 1] == ':')
            {
                if (compressed)
                {
                    return false;
                }

                compressed = true;
                position = end + 2;
                if (position == value.Length)
                {
                    break;
                }
            }
            else
            {
                position = end + 1;
                if (position == value.Length)
                {
                    return false;
                }
            }
        }

        if (compressed)
        {
            return groups < 8;
        }

        return groups == 8;
    }

    static bool IsValidIpvFuture(string value)
    {
        int dot = FindCharacter(value, 1, value.Length, '.');
        if (dot <= 1 || dot == value.Length - 1)
        {
            return false;
        }

        int index = 1;
        while (index < dot)
        {
            if (!IsHexDigit(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        index = dot + 1;
        while (index < value.Length)
        {
            char character = value[index];
            if (!IsUnreserved(character) && !IsSubDelimiter(character) && character != ':')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsValidPercentAt(string value, int position)
    {
        return position >= 0 && position + 2 < value.Length &&
            IsHexDigit(value[position + 1]) && IsHexDigit(value[position + 2]);
    }

    static bool IsPchar(char value)
    {
        return IsUnreserved(value) || IsSubDelimiter(value) || value == ':' || value == '@';
    }

    static bool IsUnreserved(char value)
    {
        return IsAlpha(value) || IsDigit(value) || value == '-' || value == '.' ||
            value == '_' || value == '~';
    }

    static bool IsSubDelimiter(char value)
    {
        return value == '!' || value == '$' || value == '&' || value == '\'' ||
            value == '(' || value == ')' || value == '*' || value == '+' ||
            value == ',' || value == ';' || value == '=';
    }

    static bool IsAlpha(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    static bool IsDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    static bool IsHexDigit(char value)
    {
        return IsDigit(value) || value >= 'A' && value <= 'F' || value >= 'a' && value <= 'f';
    }

    static bool ContainsOnlyDigitsAndDots(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!IsDigit(value[index]) && value[index] != '.')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool ContainsCharacter(string value, char character)
    {
        return FindCharacter(value, 0, value.Length, character) >= 0;
    }

    static int FindCharacter(string value, int start, int end, char character)
    {
        int index = start;
        if (index < 0)
        {
            index = 0;
        }

        while (index < end)
        {
            if (value[index] == character)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static void CopyAuthorityResult(UriValidationResult target, UriValidationResult source)
    {
        target.HasAuthority = true;
        target.Authority = source.Authority;
        target.HasUserInfo = source.HasUserInfo;
        target.UserInfo = source.UserInfo;
        target.Host = source.Host;
        target.HasPort = source.HasPort;
        target.PortText = source.PortText;
        target.HasPortValue = source.HasPortValue;
        target.IsPortInRange = source.IsPortInRange;
        target.Port = source.Port;
        target.IsIpLiteral = source.IsIpLiteral;
    }

    static UriValidationResult ValidateInput(string value, int maximumSize)
    {
        if (value == null)
        {
            return CreateMalformed(0);
        }

        if (maximumSize < 0 || value.Length > maximumSize)
        {
            return CreateLimit();
        }

        return CreateDone(0);
    }

    static UriValidationResult CreateDone(int consumed)
    {
        UriValidationResult result = new UriValidationResult();
        result.Status = HttpOperationStatus.Done;
        result.Consumed = consumed;
        return result;
    }

    static UriValidationResult CreateMalformed(int errorPosition)
    {
        UriValidationResult result = new UriValidationResult();
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorPosition = errorPosition;
        result.IsMalformed = true;
        return result;
    }

    static UriValidationResult CreateLimit()
    {
        UriValidationResult result = new UriValidationResult();
        result.Status = HttpOperationStatus.Rejected;
        result.IsLimitExceeded = true;
        return result;
    }
}
