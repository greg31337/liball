namespace cookies;

internal static class CookieRequestSerializerCore
{
    internal static CookieStringSerializationResult SerializePairs(
        CookiePair[] pairs,
        CookieSerializationOptions options)
    {
        CookieValidationResult validation = CookieModelValidator.ValidateCookiePairs(pairs, options);
        if (!validation.Success)
        {
            return CreateStringFailure(validation);
        }

        string value = JoinPairs(pairs, options);
        if (options.Limits.MaximumRequestHeaderOctets > 0 &&
            GetUtf8OctetCount(value) > options.Limits.MaximumRequestHeaderOctets)
        {
            return CreateStringLimitFailure();
        }

        CookieStringSerializationResult result = new CookieStringSerializationResult();
        result.Success = true;
        result.ErrorCode = CookieErrorCode.None;
        result.Value = value;
        return result;
    }

    internal static CookieSelectionResult SerializeFields(
        CookiePair[] pairs,
        CookieSerializationOptions options)
    {
        CookieValidationResult validation = CookieModelValidator.ValidateCookiePairs(pairs, options);
        if (!validation.Success)
        {
            return CreateFieldFailure(validation);
        }

        CookieSelectionResult result = new CookieSelectionResult();
        result.Success = true;
        result.ErrorCode = CookieErrorCode.None;
        result.Pairs = CopyPairs(pairs);
        if (pairs.Length == 0)
        {
            return result;
        }

        if (!options.SplitCookieFields || options.Limits.MaximumRequestHeaderOctets == 0)
        {
            result.FieldValues = [JoinPairs(pairs, options)];
            return result;
        }

        global::System.Collections.Generic.List<string> fields =
            new global::System.Collections.Generic.List<string>();
        global::System.Text.StringBuilder field = new global::System.Text.StringBuilder();
        int fieldOctets = 0;
        int pairIndex = 0;
        while (pairIndex < pairs.Length)
        {
            string pairValue = SerializePair(pairs[pairIndex], options);
            int pairOctets = GetUtf8OctetCount(pairValue);
            int delimiterOctets = field.Length == 0 ? 0 : 2;
            if (field.Length > 0 &&
                (long)fieldOctets + delimiterOctets + pairOctets >
                    options.Limits.MaximumRequestHeaderOctets)
            {
                fields.Add(field.ToString());
                field.Clear();
                fieldOctets = 0;
                delimiterOctets = 0;
            }

            if (delimiterOctets != 0)
            {
                field.Append("; ");
                fieldOctets = fieldOctets + delimiterOctets;
            }

            field.Append(pairValue);
            fieldOctets = fieldOctets + pairOctets;
            pairIndex = pairIndex + 1;
        }

        fields.Add(field.ToString());
        result.FieldValues = fields.ToArray();
        return result;
    }

    static string JoinPairs(CookiePair[] pairs, CookieSerializationOptions options)
    {
        global::System.Text.StringBuilder output = new global::System.Text.StringBuilder();
        int pairIndex = 0;
        while (pairIndex < pairs.Length)
        {
            if (pairIndex > 0)
            {
                output.Append("; ");
            }

            output.Append(SerializePair(pairs[pairIndex], options));
            pairIndex = pairIndex + 1;
        }

        return output.ToString();
    }

    static string SerializePair(CookiePair pair, CookieSerializationOptions options)
    {
        global::System.Text.StringBuilder output = new global::System.Text.StringBuilder();
        output.Append(pair.Name);
        output.Append('=');
        bool quoteValue = pair.IsQuoted && options.PreserveQuotedValues &&
            options.ComplianceMode == CookieComplianceMode.Compatible;
        if (quoteValue)
        {
            output.Append('"');
        }

        output.Append(pair.Value);
        if (quoteValue)
        {
            output.Append('"');
        }

        return output.ToString();
    }

    static int GetUtf8OctetCount(string value)
    {
        return global::System.Text.Encoding.UTF8.GetByteCount(value);
    }

    static CookiePair[] CopyPairs(CookiePair[] pairs)
    {
        CookiePair[] copies = new CookiePair[pairs.Length];
        int pairIndex = 0;
        while (pairIndex < pairs.Length)
        {
            CookiePair source = pairs[pairIndex];
            CookiePair copy = new CookiePair();
            copy.Name = source.Name;
            copy.Value = source.Value;
            copy.IsQuoted = source.IsQuoted;
            copies[pairIndex] = copy;
            pairIndex = pairIndex + 1;
        }

        return copies;
    }

    static CookieStringSerializationResult CreateStringFailure(CookieValidationResult validation)
    {
        CookieStringSerializationResult result = new CookieStringSerializationResult();
        result.ErrorCode = validation.ErrorCode;
        result.Issues = validation.Issues;
        return result;
    }

    static CookieStringSerializationResult CreateStringLimitFailure()
    {
        CookieStringSerializationResult result = new CookieStringSerializationResult();
        CookieIssue issue = new CookieIssue();
        issue.ErrorCode = CookieErrorCode.HeaderLimitExceeded;
        issue.Severity = CookieParseSeverity.Error;
        issue.SafeFragment = "";
        result.ErrorCode = CookieErrorCode.HeaderLimitExceeded;
        result.Issues = [issue];
        return result;
    }

    static CookieSelectionResult CreateFieldFailure(CookieValidationResult validation)
    {
        CookieSelectionResult result = new CookieSelectionResult();
        result.ErrorCode = validation.ErrorCode;
        result.Issues = validation.Issues;
        return result;
    }
}
