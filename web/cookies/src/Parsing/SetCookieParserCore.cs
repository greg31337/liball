namespace cookies;

internal static class SetCookieParserCore
{
    internal static SetCookieParseResult Parse(
        global::System.ReadOnlySpan<char> input,
        CookieParseOptions options)
    {
        SetCookieParseResult result = new SetCookieParseResult();
        global::System.Collections.Generic.List<CookieIssue> issues =
            new global::System.Collections.Generic.List<CookieIssue>();
        bool useModernRules = UsesModernRules(options.ComplianceMode);
        bool useCompatibleRules = options.ComplianceMode == CookieComplianceMode.Compatible;
        int characterIndex = 0;
        while (characterIndex < input.Length)
        {
            if (CookieCharacterRules.IsHeaderInjection(input[characterIndex]))
            {
                issues.Add(CookieIssueFactory.CreateIssue(
                    CookieErrorCode.HeaderInjection,
                    CookieParseSeverity.Error,
                    characterIndex,
                    1,
                    input.Slice(characterIndex, 1)));
                result.ErrorCode = CookieErrorCode.HeaderInjection;
                result.Issues = issues.ToArray();
                return result;
            }

            if (useModernRules && IsRejectedModernControl(input[characterIndex]))
            {
                issues.Add(CookieIssueFactory.CreateIssue(
                    CookieErrorCode.MalformedSyntax,
                    CookieParseSeverity.Error,
                    characterIndex,
                    1,
                    input.Slice(characterIndex, 1)));
                result.ErrorCode = CookieErrorCode.MalformedSyntax;
                result.Issues = issues.ToArray();
                return result;
            }

            characterIndex = characterIndex + 1;
        }

        if (useModernRules && GetUtf8OctetCount(input) > options.Limits.MaximumInputLineOctets)
        {
            issues.Add(CookieIssueFactory.CreateIssue(
                CookieErrorCode.InputLimitExceeded,
                CookieParseSeverity.Error,
                0,
                input.Length,
                input));
            result.ErrorCode = CookieErrorCode.InputLimitExceeded;
            result.Issues = issues.ToArray();
            return result;
        }

        CookieScanResult pair = CookieSpanScanner.ScanCookiePairSegment(input, 0, input.Length);
        if (!pair.HasValue && !useModernRules)
        {
            issues.Add(CookieIssueFactory.CreateIssue(
                CookieErrorCode.MalformedSyntax,
                CookieParseSeverity.Error,
                pair.SegmentStart,
                pair.SegmentLength,
                input.Slice(pair.SegmentStart, pair.SegmentLength)));
            result.ErrorCode = CookieErrorCode.MalformedSyntax;
            result.Issues = issues.ToArray();
            return result;
        }

        global::System.ReadOnlySpan<char> cookieName = input.Slice(pair.NameStart, pair.NameLength);
        global::System.ReadOnlySpan<char> cookieValue = input.Slice(pair.ValueStart, pair.ValueLength);
        int cookieValueStart = pair.ValueStart;
        bool isValueQuoted = false;
        if (!pair.HasValue)
        {
            cookieName = input.Slice(pair.NameStart, 0);
            cookieValue = input.Slice(pair.NameStart, pair.NameLength);
            cookieValueStart = pair.NameStart;
        }

        if (useCompatibleRules && CookieCompatibleValueRules.HasQuoteBoundary(cookieValue))
        {
            if (cookieValue.Length < 2 || cookieValue[0] != '"' ||
                cookieValue[cookieValue.Length - 1] != '"' ||
                !CookieCompatibleValueRules.IsSafeQuotedValue(cookieValue.Slice(1, cookieValue.Length - 2)))
            {
                issues.Add(CookieIssueFactory.CreateIssue(
                    CookieErrorCode.InvalidValue,
                    CookieParseSeverity.Error,
                    cookieValueStart,
                    cookieValue.Length,
                    cookieValue));
                result.ErrorCode = CookieErrorCode.InvalidValue;
                result.Issues = issues.ToArray();
                return result;
            }

            if (options.ReportCompatibilityRecoveries)
            {
                issues.Add(CreateCompatibilityIssue(cookieValue, cookieValueStart));
            }

            cookieValue = cookieValue.Slice(1, cookieValue.Length - 2);
            cookieValueStart = cookieValueStart + 1;
            isValueQuoted = true;
        }

        if (cookieName.Length == 0 && (!useModernRules || cookieValue.Length == 0))
        {
            issues.Add(CookieIssueFactory.CreateIssue(
                CookieErrorCode.InvalidName,
                CookieParseSeverity.Error,
                pair.NameStart,
                0,
                cookieName));
            result.ErrorCode = CookieErrorCode.InvalidName;
            result.Issues = issues.ToArray();
            return result;
        }

        if (useModernRules &&
            (long)GetUtf8OctetCount(cookieName) + GetUtf8OctetCount(cookieValue) >
                options.Limits.MaximumNameValueOctets)
        {
            issues.Add(CookieIssueFactory.CreateIssue(
                CookieErrorCode.NameValueLimitExceeded,
                CookieParseSeverity.Error,
                pair.SegmentStart,
                pair.SegmentLength,
                input.Slice(pair.SegmentStart, pair.SegmentLength)));
            result.ErrorCode = CookieErrorCode.NameValueLimitExceeded;
            result.Issues = issues.ToArray();
            return result;
        }

        if (useModernRules && cookieName.Length == 0 &&
            (StartsWithAsciiIgnoreCase(cookieValue, CookieAttributeNames.SecurePrefix) ||
                StartsWithAsciiIgnoreCase(cookieValue, CookieAttributeNames.HostPrefix)))
        {
            issues.Add(CookieIssueFactory.CreateIssue(
                CookieErrorCode.PrefixViolation,
                CookieParseSeverity.Error,
                cookieValueStart,
                cookieValue.Length,
                cookieValue));
            result.ErrorCode = CookieErrorCode.PrefixViolation;
            result.Issues = issues.ToArray();
            return result;
        }

        SetCookie cookie = new SetCookie();
        cookie.Name = cookieName.ToString();
        cookie.Value = cookieValue.ToString();
        cookie.IsValueQuoted = isValueQuoted;
        global::System.Collections.Generic.List<CookieAttribute> extensions =
            new global::System.Collections.Generic.List<CookieAttribute>();
        bool seenExpires = false;
        bool seenMaxAge = false;
        bool seenDomain = false;
        bool seenPath = false;
        bool seenSecure = false;
        bool seenHttpOnly = false;
        bool seenSameSite = false;
        bool seenPartitioned = false;
        int offset = pair.NextOffset;
        while (offset < input.Length)
        {
            CookieScanResult attribute = CookieSpanScanner.ScanAttributeSegment(
                input,
                offset,
                input.Length - offset);
            global::System.ReadOnlySpan<char> attributeName = input.Slice(attribute.NameStart, attribute.NameLength);
            global::System.ReadOnlySpan<char> attributeValue = input.Slice(attribute.ValueStart, attribute.ValueLength);
            if (attribute.SegmentLength == 0)
            {
                issues.Add(CookieIssueFactory.CreateIssue(
                    CookieErrorCode.IgnoredSegment,
                    CookieParseSeverity.Information,
                    attribute.SegmentStart,
                    attribute.SegmentLength,
                    input.Slice(attribute.SegmentStart, attribute.SegmentLength)));
                offset = attribute.NextOffset;
                continue;
            }
            if (useModernRules &&
                GetUtf8OctetCount(attributeValue) > options.Limits.MaximumAttributeValueOctets)
            {
                issues.Add(CookieIssueFactory.CreateIssue(
                    CookieErrorCode.AttributeLimitExceeded,
                    CookieParseSeverity.Warning,
                    attribute.ValueStart,
                    attribute.ValueLength,
                    attributeValue));
                offset = attribute.NextOffset;
                continue;
            }

            if (attribute.NameLength == 0)
            {
                issues.Add(CookieIssueFactory.CreateIssue(
                    CookieErrorCode.InvalidAttribute,
                    CookieParseSeverity.Warning,
                    attribute.SegmentStart,
                    attribute.SegmentLength,
                    input.Slice(attribute.SegmentStart, attribute.SegmentLength)));
                offset = attribute.NextOffset;
                continue;
            }

            if (EqualsAsciiIgnoreCase(attributeName, "Expires"))
            {
                if (seenExpires && options.ReportDuplicateAttributes)
                {
                    issues.Add(CreateDuplicateIssue(attributeName, attribute.NameStart));
                }

                seenExpires = true;
                if (attribute.HasValue)
                {
                    CookieDateParseResult dateResult = CookieDateParser.ParseCookieDate(attributeValue);
                    bool recoveredDate = false;
                    if (useCompatibleRules && dateResult.Success &&
                        !EqualsOrdinal(attributeValue, CookieDateFormatter.FormatExpires(dateResult.Value)))
                    {
                        recoveredDate = true;
                    }

                    if (useCompatibleRules && !dateResult.Success)
                    {
                        dateResult = CookieCompatibleDateParser.ParseDateWithMissingSeconds(attributeValue);
                        recoveredDate = dateResult.Success;
                    }

                    if (dateResult.Success)
                    {
                        cookie.HasExpires = true;
                        cookie.Expires = dateResult.Value;
                        if (recoveredDate && options.ReportCompatibilityRecoveries)
                        {
                            issues.Add(CreateCompatibilityIssue(attributeValue, attribute.ValueStart));
                        }
                    }
                    else
                    {
                        issues.Add(CookieIssueFactory.CreateIssue(
                            CookieErrorCode.InvalidDate,
                            CookieParseSeverity.Warning,
                            attribute.ValueStart,
                            attribute.ValueLength,
                            attributeValue));
                    }
                }
                else
                {
                    issues.Add(CreateInvalidAttributeIssue(attributeName, attribute.NameStart));
                }
            }
            else if (EqualsAsciiIgnoreCase(attributeName, "Max-Age"))
            {
                if (seenMaxAge && options.ReportDuplicateAttributes)
                {
                    issues.Add(CreateDuplicateIssue(attributeName, attribute.NameStart));
                }

                seenMaxAge = true;
                DecimalSecondsParseResult secondsResult = DecimalSecondsParser.ParseMaxAgeSeconds(attributeValue);
                if (attribute.HasValue && secondsResult.Success)
                {
                    cookie.HasMaxAge = true;
                    cookie.MaxAgeSeconds = secondsResult.Value;
                    if (useModernRules)
                    {
                        long maximumLifetimeSeconds = GetMaximumLifetimeSeconds(options.Limits.MaximumLifetime);
                        if (cookie.MaxAgeSeconds > maximumLifetimeSeconds)
                        {
                            cookie.MaxAgeSeconds = maximumLifetimeSeconds;
                            issues.Add(CookieIssueFactory.CreateIssue(
                                CookieErrorCode.LifetimeLimitExceeded,
                                CookieParseSeverity.Warning,
                                attribute.ValueStart,
                                attribute.ValueLength,
                                attributeValue));
                        }
                    }
                }
                else
                {
                    issues.Add(CreateInvalidAttributeIssue(attributeName, attribute.NameStart));
                }
            }
            else if (EqualsAsciiIgnoreCase(attributeName, "Domain"))
            {
                if (seenDomain && options.ReportDuplicateAttributes)
                {
                    issues.Add(CreateDuplicateIssue(attributeName, attribute.NameStart));
                }

                seenDomain = true;
                int domainStart = 0;
                if (attributeValue.Length > 0 && attributeValue[0] == '.')
                {
                    domainStart = 1;
                }

                if (attribute.HasValue && domainStart < attributeValue.Length &&
                    attributeValue[domainStart] != '.')
                {
                    cookie.HasDomain = true;
                    cookie.Domain = AsciiLowerString(attributeValue.Slice(domainStart));
                }
                else
                {
                    issues.Add(CreateInvalidAttributeIssue(attributeName, attribute.NameStart));
                }
            }
            else if (EqualsAsciiIgnoreCase(attributeName, "Path"))
            {
                if (seenPath && options.ReportDuplicateAttributes)
                {
                    issues.Add(CreateDuplicateIssue(attributeName, attribute.NameStart));
                }

                seenPath = true;
                if (attribute.HasValue && attributeValue.Length > 0 && attributeValue[0] == '/')
                {
                    cookie.HasPath = true;
                    cookie.Path = attributeValue.ToString();
                }
                else
                {
                    issues.Add(CreateInvalidAttributeIssue(attributeName, attribute.NameStart));
                }
            }
            else if (EqualsAsciiIgnoreCase(attributeName, "Secure"))
            {
                if (seenSecure && options.ReportDuplicateAttributes)
                {
                    issues.Add(CreateDuplicateIssue(attributeName, attribute.NameStart));
                }

                seenSecure = true;
                cookie.Secure = true;
            }
            else if (EqualsAsciiIgnoreCase(attributeName, "HttpOnly"))
            {
                if (seenHttpOnly && options.ReportDuplicateAttributes)
                {
                    issues.Add(CreateDuplicateIssue(attributeName, attribute.NameStart));
                }

                seenHttpOnly = true;
                cookie.HttpOnly = true;
            }
            else if (EqualsAsciiIgnoreCase(attributeName, "SameSite"))
            {
                if (seenSameSite && options.ReportDuplicateAttributes)
                {
                    issues.Add(CreateDuplicateIssue(attributeName, attribute.NameStart));
                }

                seenSameSite = true;
                cookie.HasSameSite = true;
                if (attribute.HasValue && EqualsAsciiIgnoreCase(attributeValue, "Strict"))
                {
                    cookie.SameSiteMode = CookieSameSiteMode.Strict;
                }
                else if (attribute.HasValue && EqualsAsciiIgnoreCase(attributeValue, "Lax"))
                {
                    cookie.SameSiteMode = CookieSameSiteMode.Lax;
                }
                else if (attribute.HasValue && EqualsAsciiIgnoreCase(attributeValue, "None"))
                {
                    cookie.SameSiteMode = CookieSameSiteMode.None;
                }
                else
                {
                    cookie.SameSiteMode = CookieSameSiteMode.Default;
                    issues.Add(CreateInvalidAttributeIssue(attributeName, attribute.NameStart));
                }
            }
            else if (EqualsAsciiIgnoreCase(attributeName, "Partitioned"))
            {
                if (seenPartitioned && options.ReportDuplicateAttributes)
                {
                    issues.Add(CreateDuplicateIssue(attributeName, attribute.NameStart));
                }

                seenPartitioned = true;
                cookie.Partitioned = true;
            }
            else if (IsSafeExtension(attributeName, attributeValue, attribute.HasValue))
            {
                if (options.PreserveUnknownAttributes)
                {
                    CookieAttribute extension = new CookieAttribute();
                    extension.Name = attributeName.ToString();
                    extension.HasValue = attribute.HasValue;
                    extension.Value = attributeValue.ToString();
                    extensions.Add(extension);
                }
                if (options.ReportUnknownAttributes)
                {
                    issues.Add(CookieIssueFactory.CreateIssue(
                        CookieErrorCode.UnknownAttribute,
                        CookieParseSeverity.Information,
                        attribute.NameStart,
                        attribute.NameLength,
                        attributeName));
                }
            }
            else
            {
                issues.Add(CreateInvalidAttributeIssue(attributeName, attribute.NameStart));
            }

            offset = attribute.NextOffset;
        }

        cookie.Extensions = extensions.ToArray();

        result.Success = true;
        result.Cookie = cookie;
        result.ErrorCode = CookieErrorCode.None;
        result.Issues = issues.ToArray();
        return result;
    }

    static CookieIssue CreateDuplicateIssue(global::System.ReadOnlySpan<char> name, int offset)
    {
        return CookieIssueFactory.CreateIssue(
            CookieErrorCode.DuplicateAttribute,
            CookieParseSeverity.Warning,
            offset,
            name.Length,
            name);
    }

    static CookieIssue CreateInvalidAttributeIssue(global::System.ReadOnlySpan<char> name, int offset)
    {
        return CookieIssueFactory.CreateIssue(
            CookieErrorCode.InvalidAttribute,
            CookieParseSeverity.Warning,
            offset,
            name.Length,
            name);
    }

    static CookieIssue CreateCompatibilityIssue(global::System.ReadOnlySpan<char> value, int offset)
    {
        return CookieIssueFactory.CreateIssue(
            CookieErrorCode.CompatibilityRecovery,
            CookieParseSeverity.Information,
            offset,
            value.Length,
            value);
    }

    static bool EqualsAsciiIgnoreCase(global::System.ReadOnlySpan<char> value, string expected)
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

    static bool StartsWithAsciiIgnoreCase(global::System.ReadOnlySpan<char> value, string expected)
    {
        if (value.Length < expected.Length)
        {
            return false;
        }

        int index = 0;
        while (index < expected.Length)
        {
            if (ToAsciiLower(value[index]) != ToAsciiLower(expected[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool EqualsOrdinal(global::System.ReadOnlySpan<char> value, string expected)
    {
        if (value.Length != expected.Length)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] != expected[index])
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

    static string AsciiLowerString(global::System.ReadOnlySpan<char> value)
    {
        char[] characters = new char[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            characters[index] = ToAsciiLower(value[index]);
            index = index + 1;
        }

        return new string(characters);
    }

    static bool UsesModernRules(CookieComplianceMode complianceMode)
    {
        return complianceMode == CookieComplianceMode.Rfc6265Bis ||
            complianceMode == CookieComplianceMode.Compatible;
    }

    static bool IsRejectedModernControl(char character)
    {
        return character != '\t' && CookieCharacterRules.IsControl(character);
    }

    static int GetUtf8OctetCount(global::System.ReadOnlySpan<char> value)
    {
        return global::System.Text.Encoding.UTF8.GetByteCount(value);
    }

    static long GetMaximumLifetimeSeconds(global::System.TimeSpan maximumLifetime)
    {
        return maximumLifetime.Ticks / global::System.TimeSpan.TicksPerSecond;
    }

    static bool IsSafeExtension(
        global::System.ReadOnlySpan<char> name,
        global::System.ReadOnlySpan<char> value,
        bool hasValue)
    {
        int index = 0;
        while (index < name.Length)
        {
            if (!CookieCharacterRules.IsTokenCharacter(name[index]))
            {
                return false;
            }

            index = index + 1;
        }

        if (!hasValue)
        {
            return true;
        }

        index = 0;
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
}
