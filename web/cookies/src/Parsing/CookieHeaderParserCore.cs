namespace cookies;

internal static class CookieHeaderParserCore
{
    internal static CookieHeaderParseResult Parse(string[] fieldValues, CookieParseOptions options)
    {
        CookieHeaderParseResult result = new CookieHeaderParseResult();
        global::System.Collections.Generic.List<CookieIssue> issues =
            new global::System.Collections.Generic.List<CookieIssue>();
        global::System.Collections.Generic.List<CookiePair> pairs =
            new global::System.Collections.Generic.List<CookiePair>();
        if (fieldValues == null)
        {
            issues.Add(CookieIssueFactory.CreateIssue(
                CookieErrorCode.MalformedSyntax,
                CookieParseSeverity.Error,
                0,
                0,
                global::System.ReadOnlySpan<char>.Empty));
            return CreateFatalResult(CookieErrorCode.MalformedSyntax, issues);
        }

        bool useModernRules = options.ComplianceMode == CookieComplianceMode.Rfc6265Bis ||
            options.ComplianceMode == CookieComplianceMode.Compatible;
        int fieldIndex = 0;
        while (fieldIndex < fieldValues.Length)
        {
            string fieldValue = fieldValues[fieldIndex];
            if (fieldValue == null)
            {
                issues.Add(CookieIssueFactory.CreateIssue(
                    CookieErrorCode.MalformedSyntax,
                    CookieParseSeverity.Error,
                    0,
                    0,
                    global::System.ReadOnlySpan<char>.Empty));
                return CreateFatalResult(CookieErrorCode.MalformedSyntax, issues);
            }

            global::System.ReadOnlySpan<char> field = fieldValue;
            int characterIndex = 0;
            while (characterIndex < field.Length)
            {
                char character = field[characterIndex];
                if (CookieCharacterRules.IsHeaderInjection(character))
                {
                    issues.Add(CookieIssueFactory.CreateIssue(
                        CookieErrorCode.HeaderInjection,
                        CookieParseSeverity.Error,
                        characterIndex,
                        1,
                        field.Slice(characterIndex, 1)));
                    return CreateFatalResult(CookieErrorCode.HeaderInjection, issues);
                }

                if (useModernRules && IsRejectedModernControl(character))
                {
                    issues.Add(CookieIssueFactory.CreateIssue(
                        CookieErrorCode.MalformedSyntax,
                        CookieParseSeverity.Error,
                        characterIndex,
                        1,
                        field.Slice(characterIndex, 1)));
                    return CreateFatalResult(CookieErrorCode.MalformedSyntax, issues);
                }

                characterIndex = characterIndex + 1;
            }

            if (useModernRules && GetUtf8OctetCount(field) > options.Limits.MaximumInputLineOctets)
            {
                issues.Add(CookieIssueFactory.CreateIssue(
                    CookieErrorCode.InputLimitExceeded,
                    CookieParseSeverity.Error,
                    0,
                    field.Length,
                    field));
                return CreateFatalResult(CookieErrorCode.InputLimitExceeded, issues);
            }

            fieldIndex = fieldIndex + 1;
        }

        fieldIndex = 0;
        while (fieldIndex < fieldValues.Length)
        {
            global::System.ReadOnlySpan<char> field = fieldValues[fieldIndex];
            int offset = 0;
            while (offset < field.Length)
            {
                CookieScanResult segment = CookieSpanScanner.ScanCookiePairSegment(
                    field,
                    offset,
                    field.Length - offset);
                ProcessSegment(field, segment, options, useModernRules, pairs, issues);
                offset = segment.NextOffset;
            }

            if (field.Length == 0 || field[field.Length - 1] == ';')
            {
                issues.Add(CookieIssueFactory.CreateIssue(
                    CookieErrorCode.IgnoredSegment,
                    CookieParseSeverity.Information,
                    field.Length,
                    0,
                    field.Slice(field.Length, 0)));
            }

            fieldIndex = fieldIndex + 1;
        }

        result.Success = true;
        result.Pairs = pairs.ToArray();
        result.ErrorCode = CookieErrorCode.None;
        result.Issues = issues.ToArray();
        return result;
    }

    static void ProcessSegment(
        global::System.ReadOnlySpan<char> field,
        CookieScanResult segment,
        CookieParseOptions options,
        bool useModernRules,
        global::System.Collections.Generic.List<CookiePair> pairs,
        global::System.Collections.Generic.List<CookieIssue> issues)
    {
        if (segment.SegmentLength == 0)
        {
            issues.Add(CookieIssueFactory.CreateIssue(
                CookieErrorCode.IgnoredSegment,
                CookieParseSeverity.Information,
                segment.SegmentStart,
                segment.SegmentLength,
                field.Slice(segment.SegmentStart, segment.SegmentLength)));
            return;
        }

        if (!segment.HasValue)
        {
            issues.Add(CookieIssueFactory.CreateIssue(
                CookieErrorCode.MalformedSyntax,
                CookieParseSeverity.Warning,
                segment.SegmentStart,
                segment.SegmentLength,
                field.Slice(segment.SegmentStart, segment.SegmentLength)));
            return;
        }

        global::System.ReadOnlySpan<char> name = field.Slice(segment.NameStart, segment.NameLength);
        global::System.ReadOnlySpan<char> value = field.Slice(segment.ValueStart, segment.ValueLength);
        if (name.Length == 0 || !IsToken(name))
        {
            issues.Add(CookieIssueFactory.CreateIssue(
                CookieErrorCode.InvalidName,
                CookieParseSeverity.Warning,
                segment.NameStart,
                segment.NameLength,
                name));
            return;
        }

        bool isQuoted = false;
        bool useCompatibleRules = options.ComplianceMode == CookieComplianceMode.Compatible;
        if (useCompatibleRules && CookieCompatibleValueRules.HasQuoteBoundary(value))
        {
            if (value.Length < 2 || value[0] != '"' || value[value.Length - 1] != '"' ||
                !CookieCompatibleValueRules.IsSafeQuotedValue(value.Slice(1, value.Length - 2)))
            {
                issues.Add(CookieIssueFactory.CreateIssue(
                    CookieErrorCode.InvalidValue,
                    CookieParseSeverity.Warning,
                    segment.ValueStart,
                    segment.ValueLength,
                    value));
                return;
            }

            if (options.ReportCompatibilityRecoveries)
            {
                issues.Add(CookieIssueFactory.CreateIssue(
                    CookieErrorCode.CompatibilityRecovery,
                    CookieParseSeverity.Information,
                    segment.ValueStart,
                    segment.ValueLength,
                    value));
            }

            value = value.Slice(1, value.Length - 2);
            isQuoted = true;
        }
        else if (!IsCookieValue(value))
        {
            issues.Add(CookieIssueFactory.CreateIssue(
                CookieErrorCode.InvalidValue,
                CookieParseSeverity.Warning,
                segment.ValueStart,
                segment.ValueLength,
                value));
            return;
        }

        if (useModernRules &&
            (long)GetUtf8OctetCount(name) + GetUtf8OctetCount(value) >
                options.Limits.MaximumNameValueOctets)
        {
            issues.Add(CookieIssueFactory.CreateIssue(
                CookieErrorCode.NameValueLimitExceeded,
                CookieParseSeverity.Warning,
                segment.SegmentStart,
                segment.SegmentLength,
                field.Slice(segment.SegmentStart, segment.SegmentLength)));
            return;
        }

        CookiePair pair = new CookiePair();
        pair.Name = name.ToString();
        pair.Value = value.ToString();
        pair.IsQuoted = isQuoted;
        pairs.Add(pair);
    }

    static CookieHeaderParseResult CreateFatalResult(
        CookieErrorCode errorCode,
        global::System.Collections.Generic.List<CookieIssue> issues)
    {
        CookieHeaderParseResult result = new CookieHeaderParseResult();
        result.ErrorCode = errorCode;
        result.Issues = issues.ToArray();
        return result;
    }

    static bool IsToken(global::System.ReadOnlySpan<char> value)
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

    static bool IsCookieValue(global::System.ReadOnlySpan<char> value)
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

    static bool IsRejectedModernControl(char character)
    {
        return character != '\t' && CookieCharacterRules.IsControl(character);
    }

    static int GetUtf8OctetCount(global::System.ReadOnlySpan<char> value)
    {
        return global::System.Text.Encoding.UTF8.GetByteCount(value);
    }
}
