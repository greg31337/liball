namespace cookies;

/// <summary>
/// Parses Set-Cookie response fields and Cookie request fields without retaining mutable parser state.
/// </summary>
public class CookieParser : ICookieParser
{
    /// <summary>
    /// Creates a cookie parser.
    /// </summary>
    public CookieParser()
    {
    }

    /// <summary>
    /// Parses one Set-Cookie field value or throws for malformed input.
    /// </summary>
    /// <param name="fieldValue">The Set-Cookie field value.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsed cookie model.</returns>
    /// <exception cref="CookieParseException">The field value or parser configuration is invalid.</exception>
    public SetCookie ParseSetCookie(string fieldValue, CookieParseOptions options)
    {
        SetCookieParseResult result = TryParseSetCookie(fieldValue, options);
        if (!result.Success)
        {
            throw new CookieParseException(CreateFailureMessage(result.ErrorCode));
        }

        SetCookie? cookie = result.Cookie;
        if (cookie == null)
        {
            throw new CookieParseException(CreateFailureMessage(CookieErrorCode.InvalidConfiguration));
        }

        return cookie;
    }

    /// <summary>
    /// Parses one Set-Cookie field value without throwing for malformed input.
    /// </summary>
    /// <param name="fieldValue">The Set-Cookie field value.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parse result.</returns>
    public SetCookieParseResult TryParseSetCookie(string fieldValue, CookieParseOptions options)
    {
        if (!IsValidOptions(options))
        {
            return CreateSetCookieFailure(CookieErrorCode.InvalidConfiguration, false);
        }

        if (fieldValue == null)
        {
            return CreateSetCookieFailure(CookieErrorCode.MalformedSyntax, true);
        }

        global::System.ReadOnlySpan<char> field = fieldValue;
        return SetCookieParserCore.Parse(field, options);
    }

    /// <summary>
    /// Parses Cookie request field values or throws for malformed input.
    /// </summary>
    /// <param name="fieldValues">The Cookie field values in field order.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The ordered cookie pairs.</returns>
    /// <exception cref="CookieParseException">A field value or parser configuration is invalid.</exception>
    public CookiePair[] ParseCookieFields(string[] fieldValues, CookieParseOptions options)
    {
        CookieHeaderParseResult result = TryParseCookieFields(fieldValues, options);
        if (!result.Success)
        {
            throw new CookieParseException(CreateFailureMessage(result.ErrorCode));
        }

        return result.Pairs;
    }

    /// <summary>
    /// Parses Cookie request field values without throwing for malformed input.
    /// </summary>
    /// <param name="fieldValues">The Cookie field values in field order.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parse result.</returns>
    public CookieHeaderParseResult TryParseCookieFields(string[] fieldValues, CookieParseOptions options)
    {
        if (!IsValidOptions(options))
        {
            return CreateCookieHeaderFailure(CookieErrorCode.InvalidConfiguration, false);
        }

        return CookieHeaderParserCore.Parse(fieldValues, options);
    }

    internal static bool IsValidOptions(CookieParseOptions options)
    {
        if (options == null || options.Limits == null)
        {
            return false;
        }

        if (options.ComplianceMode != CookieComplianceMode.Rfc6265 &&
            options.ComplianceMode != CookieComplianceMode.Rfc6265Bis &&
            options.ComplianceMode != CookieComplianceMode.Compatible)
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

    internal static SetCookieParseResult CreateSetCookieFailure(CookieErrorCode errorCode, bool hasOffset)
    {
        SetCookieParseResult result = new SetCookieParseResult();
        result.ErrorCode = errorCode;
        result.Issues = CreateFailureIssues(errorCode, hasOffset);
        return result;
    }

    internal static CookieHeaderParseResult CreateCookieHeaderFailure(CookieErrorCode errorCode, bool hasOffset)
    {
        CookieHeaderParseResult result = new CookieHeaderParseResult();
        result.ErrorCode = errorCode;
        result.Issues = CreateFailureIssues(errorCode, hasOffset);
        return result;
    }

    internal static CookieIssue[] CreateFailureIssues(CookieErrorCode errorCode, bool hasOffset)
    {
        CookieIssue issue = new CookieIssue();
        issue.ErrorCode = errorCode;
        issue.Severity = CookieParseSeverity.Error;
        issue.HasOffset = hasOffset;
        issue.Offset = 0;
        issue.Length = 0;
        issue.SafeFragment = "";
        return [issue];
    }

    internal static string CreateFailureMessage(CookieErrorCode errorCode)
    {
        return $"Cookie parsing failed with error code {(int)errorCode}.";
    }
}
