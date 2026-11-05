namespace cookies;

internal static class CookieHostCanonicalizer
{
    internal static CookieCanonicalHostResult Canonicalize(CookieUriHostResult extracted)
    {
        if (extracted == null || !extracted.Success)
        {
            if (extracted != null && extracted.ErrorCode != CookieErrorCode.None)
            {
                return CreateFailure(extracted.ErrorCode);
            }

            return CreateFailure(CookieErrorCode.InvalidHost);
        }

        int typeCount = 0;
        if (extracted.IsDnsName)
        {
            typeCount = typeCount + 1;
        }

        if (extracted.IsIpv4)
        {
            typeCount = typeCount + 1;
        }

        if (extracted.IsIpv6)
        {
            typeCount = typeCount + 1;
        }

        if (typeCount != 1 || extracted.Host == null || extracted.Host.Length == 0)
        {
            return CreateFailure(CookieErrorCode.InvalidHost);
        }

        if (extracted.IsIpv4 || extracted.IsIpv6)
        {
            return CanonicalizeIp(extracted);
        }

        if (!HasValidDnsInputStructure(extracted.Host))
        {
            return CreateFailure(CookieErrorCode.InvalidHost);
        }

        string asciiHost;
        if (ContainsNonAscii(extracted.Host))
        {
            global::System.Globalization.IdnMapping mapping = CreateMapping();
            CookieIdnConversionResult conversion = ConvertToAscii(mapping, extracted.Host);
            if (!conversion.Success)
            {
                return CreateFailure(CookieErrorCode.InvalidIdna);
            }

            asciiHost = LowerAscii(conversion.Value);
        }
        else
        {
            asciiHost = LowerAscii(extracted.Host);
        }

        if (!HasValidAsciiDomain(asciiHost))
        {
            return CreateFailure(CookieErrorCode.InvalidHost);
        }

        global::System.Globalization.IdnMapping roundTripMapping = CreateMapping();
        if (!HasValidALabels(asciiHost, roundTripMapping))
        {
            return CreateFailure(CookieErrorCode.InvalidIdna);
        }

        CookieCanonicalHostResult result = new CookieCanonicalHostResult();
        result.Success = true;
        result.ErrorCode = CookieErrorCode.None;
        result.Host = asciiHost;
        result.IsDnsName = true;
        return result;
    }

    static CookieCanonicalHostResult CanonicalizeIp(CookieUriHostResult extracted)
    {
        global::System.UriHostNameType expected = extracted.IsIpv4 ?
            global::System.UriHostNameType.IPv4 : global::System.UriHostNameType.IPv6;
        global::System.UriHostNameType actual = global::System.Uri.CheckHostName(extracted.Host);
        if (actual != expected)
        {
            return CreateFailure(CookieErrorCode.InvalidHost);
        }

        global::System.Threading.Tasks.Task<string> normalization =
            new global::System.Threading.Tasks.Task<string>(
                () => global::System.Net.IPAddress.Parse(extracted.Host).ToString());
        normalization.RunSynchronously(global::System.Threading.Tasks.TaskScheduler.Default);
        if (!normalization.IsCompletedSuccessfully)
        {
            normalization.Exception?.Handle(exception => true);
            return CreateFailure(CookieErrorCode.InvalidHost);
        }

        CookieCanonicalHostResult result = new CookieCanonicalHostResult();
        result.Success = true;
        result.ErrorCode = CookieErrorCode.None;
        result.Host = LowerAscii(normalization.Result);
        result.IsIpv4 = extracted.IsIpv4;
        result.IsIpv6 = extracted.IsIpv6;
        return result;
    }

    static bool HasValidDnsInputStructure(string host)
    {
        int labelLength = 0;
        int index = 0;
        while (index < host.Length)
        {
            char character = host[index];
            if (character == '.')
            {
                if (labelLength == 0 || host[index - 1] == '-')
                {
                    return false;
                }

                labelLength = 0;
            }
            else
            {
                if (labelLength == 0 && character == '-')
                {
                    return false;
                }

                if (character <= (char)0x7f && !IsAsciiLetter(character) &&
                    !IsAsciiDigit(character) && character != '-')
                {
                    return false;
                }

                labelLength = labelLength + 1;
            }

            index = index + 1;
        }

        return labelLength > 0 && host[host.Length - 1] != '-';
    }

    static bool HasValidAsciiDomain(string host)
    {
        if (host.Length == 0 || host.Length > 253)
        {
            return false;
        }

        int labelLength = 0;
        int index = 0;
        while (index < host.Length)
        {
            char character = host[index];
            if (character == '.')
            {
                if (labelLength == 0 || labelLength > 63 || host[index - 1] == '-')
                {
                    return false;
                }

                labelLength = 0;
            }
            else
            {
                if (!IsAsciiLetter(character) && !IsAsciiDigit(character) && character != '-')
                {
                    return false;
                }

                if (labelLength == 0 && character == '-')
                {
                    return false;
                }

                labelLength = labelLength + 1;
            }

            index = index + 1;
        }

        return labelLength > 0 && labelLength <= 63 && host[host.Length - 1] != '-';
    }

    static bool HasValidALabels(
        string asciiHost,
        global::System.Globalization.IdnMapping mapping)
    {
        int labelStart = 0;
        int index = 0;
        while (index <= asciiHost.Length)
        {
            if (index == asciiHost.Length || asciiHost[index] == '.')
            {
                int labelLength = index - labelStart;
                string label = asciiHost.Substring(labelStart, labelLength);
                bool hasReservedHyphens = label.Length >= 4 && label[2] == '-' && label[3] == '-';
                if (hasReservedHyphens && !StartsWithAcePrefix(label))
                {
                    return false;
                }

                if (StartsWithAcePrefix(label))
                {
                    CookieIdnConversionResult unicode = ConvertToUnicode(mapping, label);
                    if (!unicode.Success || !ContainsNonAscii(unicode.Value))
                    {
                        return false;
                    }

                    CookieIdnConversionResult ascii = ConvertToAscii(mapping, unicode.Value);
                    if (!ascii.Success || LowerAscii(ascii.Value) != label)
                    {
                        return false;
                    }
                }

                labelStart = index + 1;
            }

            index = index + 1;
        }

        return true;
    }

    static CookieIdnConversionResult ConvertToAscii(
        global::System.Globalization.IdnMapping mapping,
        string value)
    {
        global::System.Threading.Tasks.Task<string> conversion =
            new global::System.Threading.Tasks.Task<string>(() => mapping.GetAscii(value));
        conversion.RunSynchronously(global::System.Threading.Tasks.TaskScheduler.Default);

        if (!conversion.IsCompletedSuccessfully)
        {
            conversion.Exception?.Handle(exception => true);
            return new CookieIdnConversionResult();
        }

        CookieIdnConversionResult result = new CookieIdnConversionResult();
        result.Success = true;
        result.Value = conversion.Result;
        return result;
    }

    static CookieIdnConversionResult ConvertToUnicode(
        global::System.Globalization.IdnMapping mapping,
        string value)
    {
        global::System.Threading.Tasks.Task<string> conversion =
            new global::System.Threading.Tasks.Task<string>(() => mapping.GetUnicode(value));
        conversion.RunSynchronously(global::System.Threading.Tasks.TaskScheduler.Default);

        if (!conversion.IsCompletedSuccessfully)
        {
            conversion.Exception?.Handle(exception => true);
            return new CookieIdnConversionResult();
        }

        CookieIdnConversionResult result = new CookieIdnConversionResult();
        result.Success = true;
        result.Value = conversion.Result;
        return result;
    }

    static global::System.Globalization.IdnMapping CreateMapping()
    {
        global::System.Globalization.IdnMapping mapping = new global::System.Globalization.IdnMapping();
        mapping.AllowUnassigned = false;
        mapping.UseStd3AsciiRules = true;
        return mapping;
    }

    static bool StartsWithAcePrefix(string label)
    {
        return label.Length >= 4 && label[0] == 'x' && label[1] == 'n' &&
            label[2] == '-' && label[3] == '-';
    }

    static bool ContainsNonAscii(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > (char)0x7f)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static string LowerAscii(string value)
    {
        char[] characters = value.ToCharArray();
        int index = 0;
        while (index < characters.Length)
        {
            char character = characters[index];
            if (character >= 'A' && character <= 'Z')
            {
                characters[index] = (char)(character + ('a' - 'A'));
            }

            index = index + 1;
        }

        return new string(characters);
    }

    static bool IsAsciiLetter(char character)
    {
        return character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z';
    }

    static bool IsAsciiDigit(char character)
    {
        return character >= '0' && character <= '9';
    }

    static CookieCanonicalHostResult CreateFailure(CookieErrorCode errorCode)
    {
        CookieCanonicalHostResult result = new CookieCanonicalHostResult();
        result.ErrorCode = errorCode;
        return result;
    }
}
