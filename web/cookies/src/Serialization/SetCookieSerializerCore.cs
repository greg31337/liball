namespace cookies;

internal static class SetCookieSerializerCore
{
    internal static CookieStringSerializationResult Serialize(
        SetCookie cookie,
        CookieSerializationOptions options)
    {
        CookieValidationResult validation = CookieModelValidator.ValidateSetCookie(cookie, options);
        if (!validation.Success)
        {
            return CreateFailure(validation);
        }

        global::System.Text.StringBuilder output = new global::System.Text.StringBuilder();
        output.Append(cookie.Name);
        output.Append('=');
        bool quoteValue = cookie.IsValueQuoted && options.PreserveQuotedValues &&
            options.ComplianceMode == CookieComplianceMode.Compatible;
        if (quoteValue)
        {
            output.Append('"');
        }

        output.Append(cookie.Value);
        if (quoteValue)
        {
            output.Append('"');
        }

        if (cookie.HasExpires)
        {
            AppendValueAttribute(
                output,
                CookieAttributeNames.Expires,
                CookieDateFormatter.FormatExpires(cookie.Expires));
        }

        if (cookie.HasMaxAge)
        {
            AppendValueAttribute(
                output,
                CookieAttributeNames.MaxAge,
                cookie.MaxAgeSeconds.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        if (cookie.HasDomain)
        {
            AppendValueAttribute(output, CookieAttributeNames.Domain, cookie.Domain);
        }

        if (cookie.HasPath)
        {
            AppendValueAttribute(output, CookieAttributeNames.Path, cookie.Path);
        }

        if (cookie.Secure)
        {
            AppendFlagAttribute(output, CookieAttributeNames.Secure);
        }

        if (cookie.HttpOnly)
        {
            AppendFlagAttribute(output, CookieAttributeNames.HttpOnly);
        }

        if (cookie.HasSameSite)
        {
            AppendValueAttribute(output, CookieAttributeNames.SameSite, FormatSameSite(cookie.SameSiteMode));
        }

        if (cookie.Partitioned)
        {
            AppendFlagAttribute(output, CookieAttributeNames.Partitioned);
        }

        if (options.IncludeExtensions)
        {
            int extensionIndex = 0;
            while (extensionIndex < cookie.Extensions.Length)
            {
                CookieAttribute extension = cookie.Extensions[extensionIndex];
                output.Append("; ");
                output.Append(extension.Name);
                if (extension.HasValue)
                {
                    output.Append('=');
                    output.Append(extension.Value);
                }

                extensionIndex = extensionIndex + 1;
            }
        }

        CookieStringSerializationResult result = new CookieStringSerializationResult();
        result.Success = true;
        result.ErrorCode = CookieErrorCode.None;
        result.Value = output.ToString();
        return result;
    }

    static void AppendValueAttribute(
        global::System.Text.StringBuilder output,
        string name,
        string value)
    {
        output.Append("; ");
        output.Append(name);
        output.Append('=');
        output.Append(value);
    }

    static void AppendFlagAttribute(global::System.Text.StringBuilder output, string name)
    {
        output.Append("; ");
        output.Append(name);
    }

    static string FormatSameSite(CookieSameSiteMode mode)
    {
        if (mode == CookieSameSiteMode.Lax)
        {
            return "Lax";
        }

        if (mode == CookieSameSiteMode.Strict)
        {
            return "Strict";
        }

        return "None";
    }

    static CookieStringSerializationResult CreateFailure(CookieValidationResult validation)
    {
        CookieStringSerializationResult result = new CookieStringSerializationResult();
        result.ErrorCode = validation.ErrorCode;
        result.Issues = validation.Issues;
        return result;
    }
}
