namespace cookies;

internal static class NetscapeCookieFileReader
{
    internal static NetscapeCookieFileParseResult ReadFile(
        string text,
        NetscapeCookieFileOptions options,
        ICookieClock clock)
    {
        if (!IsValidOptions(options) || clock == null)
        {
            return CreateFailure(CookieErrorCode.InvalidConfiguration, false);
        }

        if (text == null)
        {
            return CreateFailure(CookieErrorCode.MalformedNetscapeLine, true);
        }

        CookieClockResult clockResult = CookieClockEvaluator.Evaluate(clock);
        if (!clockResult.Success)
        {
            return CreateFailure(CookieErrorCode.InvalidConfiguration, false);
        }

        NetscapeCookieFileOptions effective =
            CookieGraphCloner.CloneNetscapeOptions(options);
        global::System.Collections.Generic.List<StoredCookie> cookies = [];
        global::System.Collections.Generic.List<CookieIssue> issues = [];
        CookieErrorCode primaryError = CookieErrorCode.None;
        int offset = 0;
        while (offset < text.Length)
        {
            int lineEnd = FindLineEnd(text, offset);
            int contentEnd = lineEnd;
            if (contentEnd > offset && text[contentEnd - 1] == '\r')
            {
                contentEnd = contentEnd - 1;
            }

            string line = text.Substring(offset, contentEnd - offset);
            if (GetUtf8OctetCount(line) > effective.Limits.MaximumInputLineOctets)
            {
                AddIssue(
                    issues,
                    CookieErrorCode.MalformedNetscapeLine,
                    CookieParseSeverity.Error,
                    true,
                    offset,
                    contentEnd - offset);
                if (primaryError == CookieErrorCode.None)
                {
                    primaryError = CookieErrorCode.MalformedNetscapeLine;
                }
            }
            else if (!IsBlankOrComment(line))
            {
                NetscapeCookieLineResult lineResult = ParseLine(
                    line,
                    clockResult.Value,
                    effective);
                if (!lineResult.Success || lineResult.Cookie == null)
                {
                    AddIssue(
                        issues,
                        CookieErrorCode.MalformedNetscapeLine,
                        CookieParseSeverity.Error,
                        true,
                        offset,
                        contentEnd - offset);
                    if (primaryError == CookieErrorCode.None)
                    {
                        primaryError = CookieErrorCode.MalformedNetscapeLine;
                    }
                }
                else
                {
                    AddIssue(
                        issues,
                        CookieErrorCode.LossyImport,
                        CookieParseSeverity.Warning,
                        true,
                        offset,
                        contentEnd - offset);
                    if (lineResult.IncludeCookie)
                    {
                        if (cookies.Count >= effective.Limits.MaximumCookies)
                        {
                            AddIssue(
                                issues,
                                CookieErrorCode.TotalLimitExceeded,
                                CookieParseSeverity.Error,
                                true,
                                offset,
                                contentEnd - offset);
                            if (primaryError == CookieErrorCode.None)
                            {
                                primaryError = CookieErrorCode.TotalLimitExceeded;
                            }
                        }
                        else
                        {
                            lineResult.Cookie.SequenceNumber = cookies.Count;
                            cookies.Add(lineResult.Cookie);
                        }
                    }
                }
            }

            if (lineEnd == text.Length)
            {
                offset = text.Length;
            }
            else
            {
                offset = lineEnd + 1;
            }
        }

        NetscapeCookieFileParseResult result = new NetscapeCookieFileParseResult();
        result.Success = primaryError == CookieErrorCode.None;
        result.Cookies = cookies.ToArray();
        result.ErrorCode = primaryError;
        result.Issues = issues.ToArray();
        return result;
    }

    static NetscapeCookieLineResult ParseLine(
        string line,
        global::System.DateTimeOffset now,
        NetscapeCookieFileOptions options)
    {
        NetscapeCookieLineResult result = new NetscapeCookieLineResult();
        if (HasEmbeddedLineBreak(line))
        {
            return result;
        }

        bool httpOnly = false;
        string fieldsText = line;
        if (fieldsText.StartsWith(
            "#HttpOnly_",
            global::System.StringComparison.Ordinal))
        {
            httpOnly = true;
            fieldsText = fieldsText.Substring(10);
        }

        string[] fields = fieldsText.Split('\t');
        if (fields.Length != 7 || !HasValidFieldControls(fields))
        {
            return result;
        }

        bool includeSubdomains;
        if (fields[1] == "TRUE")
        {
            includeSubdomains = true;
        }
        else if (fields[1] == "FALSE")
        {
            includeSubdomains = false;
        }
        else
        {
            return result;
        }

        bool secure;
        if (fields[3] == "TRUE")
        {
            secure = true;
        }
        else if (fields[3] == "FALSE")
        {
            secure = false;
        }
        else
        {
            return result;
        }

        CookieCanonicalHostResult domain = CanonicalizeDomain(fields[0]);
        if (!domain.Success || domain.Host.Length == 0 ||
            includeSubdomains && (domain.IsIpv4 || domain.IsIpv6))
        {
            return result;
        }

        string path = fields[2];
        if (path.Length == 0 || path[0] != '/')
        {
            path = "/";
        }

        if (GetUtf8OctetCount(fields[0]) >
                options.Limits.MaximumAttributeValueOctets ||
            GetUtf8OctetCount(path) >
                options.Limits.MaximumAttributeValueOctets ||
            (long)GetUtf8OctetCount(fields[5]) + GetUtf8OctetCount(fields[6]) >
                options.Limits.MaximumNameValueOctets)
        {
            return result;
        }

        bool session = fields[4].Length == 0 || fields[4] == "0";
        global::System.DateTimeOffset expires = default(global::System.DateTimeOffset);
        if (!session)
        {
            DecimalSecondsParseResult expirySeconds =
                DecimalSecondsParser.ParseMaxAgeSeconds(fields[4]);
            if (!expirySeconds.Success || expirySeconds.IsNegative ||
                expirySeconds.Saturated || expirySeconds.Value <= 0 ||
                expirySeconds.Value > 253402300799L)
            {
                return result;
            }

            global::System.DateTimeOffset epoch = new global::System.DateTimeOffset(
                1970,
                1,
                1,
                0,
                0,
                0,
                global::System.TimeSpan.Zero);
            expires = DecimalSecondsParser.AddSecondsSaturated(
                epoch,
                expirySeconds.Value).Value;
        }

        StoredCookie cookie = new StoredCookie();
        cookie.Name = fields[5];
        cookie.Value = fields[6];
        cookie.Domain = domain.Host;
        cookie.Path = path;
        cookie.HasExpires = !session;
        cookie.Expires = expires;
        cookie.CreationTime = now;
        cookie.LastAccessTime = now;
        cookie.Persistent = !session;
        cookie.HostOnly = !includeSubdomains;
        cookie.Secure = secure;
        cookie.HttpOnly = httpOnly;

        result.Success = true;
        result.Cookie = cookie;
        result.IncludeCookie = session ? options.ImportSessionCookies :
            options.ImportExpiredCookies || expires > now;
        return result;
    }

    static CookieCanonicalHostResult CanonicalizeDomain(string value)
    {
        string candidate = value;
        while (candidate.Length > 0 && candidate[0] == '.')
        {
            candidate = candidate.Substring(1);
        }

        global::System.UriHostNameType hostType =
            global::System.Uri.CheckHostName(candidate);
        CookieUriHostResult extracted = new CookieUriHostResult();
        extracted.Success = true;
        extracted.Host = candidate;
        extracted.IsDnsName = hostType == global::System.UriHostNameType.Dns;
        extracted.IsIpv4 = hostType == global::System.UriHostNameType.IPv4;
        extracted.IsIpv6 = hostType == global::System.UriHostNameType.IPv6;
        return CookieHostCanonicalizer.Canonicalize(extracted);
    }

    static bool IsBlankOrComment(string line)
    {
        int index = 0;
        while (index < line.Length &&
            CookieCharacterRules.IsAsciiWhitespace(line[index]))
        {
            index = index + 1;
        }

        if (index == line.Length)
        {
            return true;
        }

        if (index == 0 && line.StartsWith(
            "#HttpOnly_",
            global::System.StringComparison.Ordinal))
        {
            return false;
        }

        return line[index] == '#';
    }

    static bool HasEmbeddedLineBreak(string line)
    {
        int index = 0;
        while (index < line.Length)
        {
            if (line[index] == '\r' || line[index] == '\n')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool HasValidFieldControls(string[] fields)
    {
        int fieldIndex = 0;
        while (fieldIndex < fields.Length)
        {
            int characterIndex = 0;
            while (characterIndex < fields[fieldIndex].Length)
            {
                char character = fields[fieldIndex][characterIndex];
                if (CookieCharacterRules.IsControl(character))
                {
                    return false;
                }

                characterIndex = characterIndex + 1;
            }

            fieldIndex = fieldIndex + 1;
        }

        return true;
    }

    static int FindLineEnd(string text, int start)
    {
        int index = start;
        while (index < text.Length && text[index] != '\n')
        {
            index = index + 1;
        }

        return index;
    }

    static int GetUtf8OctetCount(string value)
    {
        return global::System.Text.Encoding.UTF8.GetByteCount(value);
    }

    internal static bool IsValidOptions(NetscapeCookieFileOptions options)
    {
        if (options == null || options.Limits == null)
        {
            return false;
        }

        CookieLimits limits = options.Limits;
        return limits.MaximumNameValueOctets >= 0 &&
            limits.MaximumAttributeValueOctets >= 0 &&
            limits.MaximumInputLineOctets >= 0 &&
            limits.MaximumCookiesPerDomain >= 0 &&
            limits.MaximumCookies >= 0 &&
            limits.MaximumRequestHeaderOctets >= 0 &&
            limits.MaximumLifetime.Ticks >= 0;
    }

    static NetscapeCookieFileParseResult CreateFailure(
        CookieErrorCode errorCode,
        bool hasOffset)
    {
        NetscapeCookieFileParseResult result = new NetscapeCookieFileParseResult();
        result.ErrorCode = errorCode;
        CookieIssue issue = new CookieIssue();
        issue.ErrorCode = errorCode;
        issue.Severity = CookieParseSeverity.Error;
        issue.HasOffset = hasOffset;
        result.Issues = [issue];
        return result;
    }

    static void AddIssue(
        global::System.Collections.Generic.List<CookieIssue> issues,
        CookieErrorCode errorCode,
        CookieParseSeverity severity,
        bool hasOffset,
        int offset,
        int length)
    {
        CookieIssue issue = new CookieIssue();
        issue.ErrorCode = errorCode;
        issue.Severity = severity;
        issue.HasOffset = hasOffset;
        issue.Offset = offset;
        issue.Length = length;
        issue.SafeFragment = "";
        issues.Add(issue);
    }
}
