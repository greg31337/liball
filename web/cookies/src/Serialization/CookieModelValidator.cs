namespace cookies;

internal static class CookieModelValidator
{
    internal static CookieValidationResult ValidateSetCookie(
        SetCookie cookie,
        CookieSerializationOptions options)
    {
        CookieErrorCode optionError = ValidateOptions(options);
        if (optionError != CookieErrorCode.None)
        {
            return CreateFailure(optionError);
        }

        if (cookie == null || cookie.Name == null || cookie.Value == null || cookie.Extensions == null)
        {
            return CreateFailure(CookieErrorCode.SerializationFailure);
        }

        if (ContainsHeaderInjection(cookie.Name) || ContainsHeaderInjection(cookie.Value))
        {
            return CreateFailure(CookieErrorCode.HeaderInjection);
        }

        if (cookie.Name.Length == 0 || !IsToken(cookie.Name))
        {
            return CreateFailure(CookieErrorCode.InvalidName);
        }

        if (!IsSerializableValue(cookie.Value, cookie.IsValueQuoted, options))
        {
            return CreateFailure(CookieErrorCode.InvalidValue);
        }

        if ((long)GetUtf8OctetCount(cookie.Name) + GetUtf8OctetCount(cookie.Value) >
            options.Limits.MaximumNameValueOctets)
        {
            return CreateFailure(CookieErrorCode.NameValueLimitExceeded);
        }

        if (cookie.HasExpires)
        {
            global::System.DateTimeOffset utcExpires = cookie.Expires.ToUniversalTime();
            if (utcExpires.Year < 1601)
            {
                return CreateFailure(CookieErrorCode.InvalidDate);
            }

            if (29 > options.Limits.MaximumAttributeValueOctets)
            {
                return CreateFailure(CookieErrorCode.AttributeLimitExceeded);
            }
        }

        if (cookie.HasMaxAge &&
            CountSignedDecimalOctets(cookie.MaxAgeSeconds) > options.Limits.MaximumAttributeValueOctets)
        {
            return CreateFailure(CookieErrorCode.AttributeLimitExceeded);
        }

        if (cookie.HasDomain)
        {
            if (cookie.Domain == null || cookie.Domain.Length == 0 ||
                ContainsHeaderInjection(cookie.Domain) || !IsDomainValue(cookie.Domain))
            {
                return CreateFailure(ContainsHeaderInjection(cookie.Domain) ?
                    CookieErrorCode.HeaderInjection : CookieErrorCode.InvalidAttribute);
            }

            if (GetUtf8OctetCount(cookie.Domain) > options.Limits.MaximumAttributeValueOctets)
            {
                return CreateFailure(CookieErrorCode.AttributeLimitExceeded);
            }
        }

        if (cookie.HasPath)
        {
            if (cookie.Path == null || cookie.Path.Length == 0 || cookie.Path[0] != '/' ||
                ContainsHeaderInjection(cookie.Path) || !IsAttributeValue(cookie.Path))
            {
                return CreateFailure(ContainsHeaderInjection(cookie.Path) ?
                    CookieErrorCode.HeaderInjection : CookieErrorCode.InvalidPath);
            }

            if (GetUtf8OctetCount(cookie.Path) > options.Limits.MaximumAttributeValueOctets)
            {
                return CreateFailure(CookieErrorCode.AttributeLimitExceeded);
            }
        }

        if (cookie.HasSameSite && cookie.SameSiteMode != CookieSameSiteMode.Lax &&
            cookie.SameSiteMode != CookieSameSiteMode.Strict &&
            cookie.SameSiteMode != CookieSameSiteMode.None)
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute);
        }

        if (cookie.HasSameSite &&
            CountSameSiteValueOctets(cookie.SameSiteMode) > options.Limits.MaximumAttributeValueOctets)
        {
            return CreateFailure(CookieErrorCode.AttributeLimitExceeded);
        }

        int extensionIndex = 0;
        while (extensionIndex < cookie.Extensions.Length)
        {
            CookieAttribute extension = cookie.Extensions[extensionIndex];
            CookieErrorCode extensionError = ValidateExtension(extension, options.Limits);
            if (extensionError != CookieErrorCode.None)
            {
                return CreateFailure(extensionError);
            }

            extensionIndex = extensionIndex + 1;
        }

        long outputOctets = CountSetCookieOutputOctets(cookie, options);
        if (outputOctets > options.Limits.MaximumInputLineOctets)
        {
            return CreateFailure(CookieErrorCode.InputLimitExceeded);
        }

        return CreateSuccess();
    }

    internal static CookieValidationResult ValidateCookiePairs(
        CookiePair[] pairs,
        CookieSerializationOptions options)
    {
        CookieErrorCode optionError = ValidateOptions(options);
        if (optionError != CookieErrorCode.None)
        {
            return CreateFailure(optionError);
        }

        if (pairs == null)
        {
            return CreateFailure(CookieErrorCode.SerializationFailure);
        }

        long fieldOctets = 0;
        int pairIndex = 0;
        while (pairIndex < pairs.Length)
        {
            CookiePair pair = pairs[pairIndex];
            if (pair == null || pair.Name == null || pair.Value == null)
            {
                return CreateFailure(CookieErrorCode.SerializationFailure);
            }

            if (ContainsHeaderInjection(pair.Name) || ContainsHeaderInjection(pair.Value))
            {
                return CreateFailure(CookieErrorCode.HeaderInjection);
            }

            if (pair.Name.Length == 0 || !IsToken(pair.Name))
            {
                return CreateFailure(CookieErrorCode.InvalidName);
            }

            if (!IsSerializableValue(pair.Value, pair.IsQuoted, options))
            {
                return CreateFailure(CookieErrorCode.InvalidValue);
            }

            int nameOctets = GetUtf8OctetCount(pair.Name);
            int valueOctets = GetUtf8OctetCount(pair.Value);
            if ((long)nameOctets + valueOctets > options.Limits.MaximumNameValueOctets)
            {
                return CreateFailure(CookieErrorCode.NameValueLimitExceeded);
            }

            long pairOctets = nameOctets + 1L + valueOctets;
            if (ShouldQuoteValue(pair.IsQuoted, options))
            {
                pairOctets = pairOctets + 2;
            }

            if (options.Limits.MaximumRequestHeaderOctets > 0 &&
                pairOctets > options.Limits.MaximumRequestHeaderOctets)
            {
                return CreateFailure(CookieErrorCode.HeaderLimitExceeded);
            }

            if (pairIndex > 0)
            {
                fieldOctets = fieldOctets + 2;
            }

            fieldOctets = fieldOctets + pairOctets;
            pairIndex = pairIndex + 1;
        }

        if (!options.SplitCookieFields && options.Limits.MaximumRequestHeaderOctets > 0 &&
            fieldOctets > options.Limits.MaximumRequestHeaderOctets)
        {
            return CreateFailure(CookieErrorCode.HeaderLimitExceeded);
        }

        return CreateSuccess();
    }

    static CookieErrorCode ValidateOptions(CookieSerializationOptions options)
    {
        if (options == null || options.Limits == null)
        {
            return CookieErrorCode.InvalidConfiguration;
        }

        if (options.ComplianceMode != CookieComplianceMode.Rfc6265 &&
            options.ComplianceMode != CookieComplianceMode.Rfc6265Bis &&
            options.ComplianceMode != CookieComplianceMode.Compatible)
        {
            return CookieErrorCode.InvalidConfiguration;
        }

        CookieLimits limits = options.Limits;
        if (limits.MaximumNameValueOctets < 0 || limits.MaximumAttributeValueOctets < 0 ||
            limits.MaximumInputLineOctets < 0 || limits.MaximumCookiesPerDomain < 0 ||
            limits.MaximumCookies < 0 || limits.MaximumRequestHeaderOctets < 0 ||
            limits.MaximumLifetime.Ticks < 0)
        {
            return CookieErrorCode.InvalidConfiguration;
        }

        return CookieErrorCode.None;
    }

    static CookieErrorCode ValidateExtension(CookieAttribute extension, CookieLimits limits)
    {
        if (extension == null || extension.Name == null || extension.Value == null)
        {
            return CookieErrorCode.SerializationFailure;
        }

        if (ContainsHeaderInjection(extension.Name) || ContainsHeaderInjection(extension.Value))
        {
            return CookieErrorCode.HeaderInjection;
        }

        if (extension.Name.Length == 0 || !IsToken(extension.Name) ||
            IsRecognizedAttributeName(extension.Name))
        {
            return CookieErrorCode.InvalidAttribute;
        }

        if (!extension.HasValue && extension.Value.Length != 0)
        {
            return CookieErrorCode.SerializationFailure;
        }

        if (extension.HasValue && !IsAttributeValue(extension.Value))
        {
            return CookieErrorCode.InvalidAttribute;
        }

        if (extension.HasValue && GetUtf8OctetCount(extension.Value) > limits.MaximumAttributeValueOctets)
        {
            return CookieErrorCode.AttributeLimitExceeded;
        }

        return CookieErrorCode.None;
    }

    static bool IsSerializableValue(string value, bool isQuoted, CookieSerializationOptions options)
    {
        if (ShouldQuoteValue(isQuoted, options))
        {
            return CookieCompatibleValueRules.IsSafeQuotedValue(value);
        }

        return IsCookieValue(value);
    }

    static bool ShouldQuoteValue(bool isQuoted, CookieSerializationOptions options)
    {
        return isQuoted && options.PreserveQuotedValues &&
            options.ComplianceMode == CookieComplianceMode.Compatible;
    }

    static bool IsToken(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!CookieCharacterRules.IsTokenCharacter(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsCookieValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!CookieCharacterRules.IsCookieOctet(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsAttributeValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!CookieCharacterRules.IsAttributeValueOctet(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsDomainValue(string value)
    {
        if (value.Length > 253)
        {
            return false;
        }

        int labelLength = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '.')
            {
                if (labelLength == 0 || labelLength > 63 || value[index - 1] == '-')
                {
                    return false;
                }

                labelLength = 0;
            }
            else
            {
                if (!CookieCharacterRules.IsAsciiAlpha(character) &&
                    !CookieCharacterRules.IsAsciiDigit(character) && character != '-')
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

        return labelLength > 0 && labelLength <= 63 && value[value.Length - 1] != '-';
    }

    static bool ContainsHeaderInjection(string? value)
    {
        if (value == null)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (CookieCharacterRules.IsHeaderInjection(value[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool IsRecognizedAttributeName(string value)
    {
        return EqualsAsciiIgnoreCase(value, CookieAttributeNames.Expires) ||
            EqualsAsciiIgnoreCase(value, CookieAttributeNames.MaxAge) ||
            EqualsAsciiIgnoreCase(value, CookieAttributeNames.Domain) ||
            EqualsAsciiIgnoreCase(value, CookieAttributeNames.Path) ||
            EqualsAsciiIgnoreCase(value, CookieAttributeNames.Secure) ||
            EqualsAsciiIgnoreCase(value, CookieAttributeNames.HttpOnly) ||
            EqualsAsciiIgnoreCase(value, CookieAttributeNames.SameSite) ||
            EqualsAsciiIgnoreCase(value, CookieAttributeNames.Partitioned);
    }

    static bool EqualsAsciiIgnoreCase(string value, string expected)
    {
        if (value.Length != expected.Length)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (ToAsciiLower(value[index]) != ToAsciiLower(expected[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static char ToAsciiLower(char character)
    {
        if (character >= 'A' && character <= 'Z')
        {
            return (char)(character + ('a' - 'A'));
        }

        return character;
    }

    static long CountSetCookieOutputOctets(SetCookie cookie, CookieSerializationOptions options)
    {
        long count = GetUtf8OctetCount(cookie.Name) + 1L + GetUtf8OctetCount(cookie.Value);
        if (ShouldQuoteValue(cookie.IsValueQuoted, options))
        {
            count = count + 2;
        }

        if (cookie.HasExpires)
        {
            count = count + 10 + 29;
        }

        if (cookie.HasMaxAge)
        {
            count = count + 10 + CountSignedDecimalOctets(cookie.MaxAgeSeconds);
        }

        if (cookie.HasDomain)
        {
            count = count + 9 + GetUtf8OctetCount(cookie.Domain);
        }

        if (cookie.HasPath)
        {
            count = count + 7 + GetUtf8OctetCount(cookie.Path);
        }

        if (cookie.Secure)
        {
            count = count + 8;
        }

        if (cookie.HttpOnly)
        {
            count = count + 10;
        }

        if (cookie.HasSameSite)
        {
            count = count + 11 + CountSameSiteValueOctets(cookie.SameSiteMode);
        }

        if (cookie.Partitioned)
        {
            count = count + 13;
        }

        if (options.IncludeExtensions)
        {
            int extensionIndex = 0;
            while (extensionIndex < cookie.Extensions.Length)
            {
                CookieAttribute extension = cookie.Extensions[extensionIndex];
                count = count + 2 + GetUtf8OctetCount(extension.Name);
                if (extension.HasValue)
                {
                    count = count + 1 + GetUtf8OctetCount(extension.Value);
                }

                extensionIndex = extensionIndex + 1;
            }
        }

        return count;
    }

    static int CountSameSiteValueOctets(CookieSameSiteMode mode)
    {
        if (mode == CookieSameSiteMode.Lax)
        {
            return 3;
        }

        if (mode == CookieSameSiteMode.Strict)
        {
            return 6;
        }

        return 4;
    }

    static int CountSignedDecimalOctets(long value)
    {
        if (value == 0)
        {
            return 1;
        }

        int count = 0;
        long remaining = value;
        if (remaining < 0)
        {
            count = 1;
        }
        else
        {
            remaining = -remaining;
        }

        while (remaining < 0)
        {
            remaining = remaining / 10;
            count = count + 1;
        }

        return count;
    }

    static int GetUtf8OctetCount(string value)
    {
        return global::System.Text.Encoding.UTF8.GetByteCount(value);
    }

    static CookieValidationResult CreateSuccess()
    {
        CookieValidationResult result = new CookieValidationResult();
        result.Success = true;
        result.ErrorCode = CookieErrorCode.None;
        return result;
    }

    static CookieValidationResult CreateFailure(CookieErrorCode errorCode)
    {
        CookieValidationResult result = new CookieValidationResult();
        CookieIssue issue = new CookieIssue();
        issue.ErrorCode = errorCode;
        issue.Severity = CookieParseSeverity.Error;
        issue.HasOffset = false;
        issue.SafeFragment = "";
        result.ErrorCode = errorCode;
        result.Issues = [issue];
        return result;
    }
}
