namespace uspto;

internal static class SecretRedactor
{
    internal const string Replacement = "[REDACTED]";

    internal static global::System.Collections.Generic.Dictionary<string, string[]> RedactHeaders(global::System.Net.Http.Headers.HttpHeaders headers, string[]? secrets)
    {
        if (headers == null)
        {
            throw new ValidationException("HTTP headers are required.", "headers", "REQUIRED", null);
        }

        global::System.Collections.Generic.Dictionary<string, string[]> redacted = new global::System.Collections.Generic.Dictionary<string, string[]>(global::System.StringComparer.OrdinalIgnoreCase);
        foreach (global::System.Collections.Generic.KeyValuePair<string, global::System.Collections.Generic.IEnumerable<string>> header in headers)
        {
            global::System.Collections.Generic.List<string> values = new global::System.Collections.Generic.List<string>();
            foreach (string value in header.Value)
            {
                if (IsSensitiveName(header.Key))
                {
                    values.Add(Replacement);
                }
                else
                {
                    string? safeValue = RedactText(value, secrets);
                    values.Add(safeValue ?? string.Empty);
                }
            }

            string[] copiedValues = new string[values.Count];
            values.CopyTo(copiedValues, 0);
            redacted.Add(header.Key, copiedValues);
        }

        return redacted;
    }

    internal static global::System.Uri RedactUri(global::System.Uri uri, string[]? secrets)
    {
        if (uri == null)
        {
            throw new ValidationException("A URI is required.", "uri", "REQUIRED", null);
        }

        string? safeOriginal = RedactText(uri.OriginalString, secrets);
        if (!uri.IsAbsoluteUri)
        {
            return new global::System.Uri(RedactRelativeUri(safeOriginal ?? string.Empty), global::System.UriKind.Relative);
        }

        global::System.Uri safeUri = new global::System.Uri(safeOriginal ?? string.Empty, global::System.UriKind.Absolute);
        global::System.UriBuilder builder = new global::System.UriBuilder(safeUri);
        builder.UserName = string.Empty;
        builder.Password = string.Empty;
        builder.Query = RedactQuery(safeUri.Query, secrets);
        if (safeUri.Fragment.Length > 1)
        {
            string? safeFragment = RedactText(safeUri.Fragment.Substring(1), secrets);
            builder.Fragment = safeFragment ?? string.Empty;
        }
        else
        {
            builder.Fragment = string.Empty;
        }

        return builder.Uri;
    }

    internal static global::System.Collections.Generic.Dictionary<string, string?> RedactFields(global::System.Collections.Generic.Dictionary<string, string?> fields, string[]? secrets)
    {
        if (fields == null)
        {
            throw new ValidationException("Structured fields are required.", "fields", "REQUIRED", null);
        }

        global::System.Collections.Generic.Dictionary<string, string?> redacted = new global::System.Collections.Generic.Dictionary<string, string?>(fields.Comparer);
        foreach (global::System.Collections.Generic.KeyValuePair<string, string?> field in fields)
        {
            if (field.Value == null)
            {
                redacted.Add(field.Key, null);
            }
            else if (IsSensitiveName(field.Key))
            {
                redacted.Add(field.Key, Replacement);
            }
            else
            {
                redacted.Add(field.Key, RedactText(field.Value, secrets));
            }
        }

        return redacted;
    }

    internal static string? RedactText(string? text, string[]? secrets)
    {
        if (text == null)
        {
            return null;
        }

        string redacted = text;
        if (secrets != null)
        {
            int secretIndex;
            for (secretIndex = 0; secretIndex < secrets.Length; secretIndex++)
            {
                string secret = secrets[secretIndex];
                if (!global::System.String.IsNullOrEmpty(secret))
                {
                    redacted = redacted.Replace(secret, Replacement, global::System.StringComparison.Ordinal);
                    string encodedSecret = global::System.Uri.EscapeDataString(secret);
                    if (encodedSecret != secret)
                    {
                        redacted = redacted.Replace(encodedSecret, global::System.Uri.EscapeDataString(Replacement), global::System.StringComparison.OrdinalIgnoreCase);
                        string formEncodedSecret = encodedSecret.Replace("%20", "+", global::System.StringComparison.OrdinalIgnoreCase);
                        redacted = redacted.Replace(formEncodedSecret, global::System.Uri.EscapeDataString(Replacement), global::System.StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
        }

        string separator = "(?:[-_]|%2[dD]|%5[fF])?";
        string names = "(?:x" + separator + "api" + separator + "key|uspto" + separator + "api" + separator + "key|api" + separator + "key|authorization|proxy" + separator + "authorization|authentication" + separator + "info|proxy" + separator + "authentication" + separator + "info|cookie|set" + separator + "cookie|access" + separator + "token|refresh" + separator + "token|id" + separator + "token|security" + separator + "token|x" + separator + "amz" + separator + "security" + separator + "token|x" + separator + "amz" + separator + "credential|x" + separator + "amz" + separator + "signature|x" + separator + "goog" + separator + "credential|x" + separator + "goog" + separator + "signature|csrf" + separator + "token|xsrf" + separator + "token|client" + separator + "secret|subscription" + separator + "key|ocp" + separator + "apim" + separator + "subscription" + separator + "key|password|passwd|credential|credentials|signature|sig|secret|session|session" + separator + "id|token)";
        global::System.Text.RegularExpressions.RegexOptions options = global::System.Text.RegularExpressions.RegexOptions.IgnoreCase | global::System.Text.RegularExpressions.RegexOptions.CultureInvariant;
        redacted = global::System.Text.RegularExpressions.Regex.Replace(redacted, "(\"" + names + "\"\\s*:\\s*)\"(?:\\\\.|[^\"\\\\])*\"", "$1\"" + Replacement + "\"", options);
        redacted = global::System.Text.RegularExpressions.Regex.Replace(redacted, "(\"" + names + "\"\\s*:\\s*)(?!\")[^,}\\r\\n]*", "$1\"" + Replacement + "\"", options);
        redacted = global::System.Text.RegularExpressions.Regex.Replace(redacted, "(?m)(^\\s*" + names + "\\s*:\\s*)[^\\r\\n]*", "$1" + Replacement, options);
        redacted = global::System.Text.RegularExpressions.Regex.Replace(redacted, "(\\b" + names + "\\s*=\\s*)\"(?:\\\\.|[^\"\\\\])*\"", "$1\"" + Replacement + "\"", options);
        redacted = global::System.Text.RegularExpressions.Regex.Replace(redacted, "(\\b" + names + "\\s*=\\s*)'[^']*'", "$1'" + Replacement + "'", options);
        redacted = global::System.Text.RegularExpressions.Regex.Replace(redacted, "(\\b" + names + "\\s*=\\s*)[^,;&#\\s\"']+", "$1" + Replacement, options);
        redacted = global::System.Text.RegularExpressions.Regex.Replace(redacted, "(\\b" + names + "\\s*:\\s*)[^,;}\\r\\n]*", "$1" + Replacement, options);
        return redacted;
    }

    internal static bool IsSensitiveName(string name)
    {
        string normalized = NormalizeName(name);
        switch (normalized)
        {
            case "xapikey":
            case "usptoapikey":
            case "apikey":
            case "authorization":
            case "proxyauthorization":
            case "authenticationinfo":
            case "proxyauthenticationinfo":
            case "cookie":
            case "setcookie":
            case "accesstoken":
            case "refreshtoken":
            case "idtoken":
            case "securitytoken":
            case "xamzsecuritytoken":
            case "xamzcredential":
            case "xamzsignature":
            case "xgoogcredential":
            case "xgoogsignature":
            case "csrftoken":
            case "xsrftoken":
            case "clientsecret":
            case "subscriptionkey":
            case "ocpapimsubscriptionkey":
            case "password":
            case "passwd":
            case "credential":
            case "credentials":
            case "signature":
            case "sig":
            case "secret":
            case "session":
            case "sessionid":
            case "token":
                return true;
            default:
                return false;
        }
    }

    internal static string NormalizeName(string name)
    {
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        int index;
        for (index = 0; index < name.Length; index++)
        {
            char character = name[index];
            if (global::System.Char.IsLetterOrDigit(character))
            {
                builder.Append(global::System.Char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }

    internal static string RedactRelativeUri(string uri)
    {
        if (!uri.StartsWith("//", global::System.StringComparison.Ordinal))
        {
            return uri;
        }

        int authorityEnd = uri.Length;
        int slashIndex = uri.IndexOf('/', 2);
        int queryIndex = uri.IndexOf('?', 2);
        int fragmentIndex = uri.IndexOf('#', 2);
        if (slashIndex >= 0 && slashIndex < authorityEnd)
        {
            authorityEnd = slashIndex;
        }

        if (queryIndex >= 0 && queryIndex < authorityEnd)
        {
            authorityEnd = queryIndex;
        }

        if (fragmentIndex >= 0 && fragmentIndex < authorityEnd)
        {
            authorityEnd = fragmentIndex;
        }

        int userInfoEnd = uri.IndexOf('@', 2);
        if (userInfoEnd >= 0 && userInfoEnd < authorityEnd)
        {
            return "//" + uri.Substring(userInfoEnd + 1);
        }

        return uri;
    }

    internal static string RedactQuery(string query, string[]? secrets)
    {
        if (query.Length == 0)
        {
            return string.Empty;
        }

        string value = query;
        if (value[0] == '?')
        {
            value = value.Substring(1);
        }

        string[] parts = value.Split('&');
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        int partIndex;
        for (partIndex = 0; partIndex < parts.Length; partIndex++)
        {
            if (partIndex != 0)
            {
                builder.Append('&');
            }

            string part = parts[partIndex];
            int equalsIndex = part.IndexOf('=');
            string name = equalsIndex >= 0 ? part.Substring(0, equalsIndex) : part;
            string decodedName = global::System.Uri.UnescapeDataString(name.Replace("+", " ", global::System.StringComparison.Ordinal));
            if (IsSensitiveName(decodedName))
            {
                builder.Append(name);
                builder.Append('=');
                builder.Append(global::System.Uri.EscapeDataString(Replacement));
            }
            else
            {
                string? safePart = RedactText(part, secrets);
                builder.Append(safePart ?? string.Empty);
            }
        }

        return builder.ToString();
    }
}
