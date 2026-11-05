namespace sip;

internal static class SipUriFormatter
{
    internal static SipWriteResult FormatUriModel(SipUri? uri, SipWriteOptions? options)
    {
        if (uri == null)
        {
            return CreateError("A URI value is required.");
        }

        if (options == null)
        {
            return CreateError("URI write options are required.");
        }

        if (options.MaxHeaderLineBytes < 0)
        {
            return CreateError("The maximum output size cannot be negative.");
        }

        SipValidationIssue[] issues = SipUriValidator.ValidateUriModel(uri);
        if (issues.Length > 0)
        {
            string? issueMessage = issues[0].Message;
            return CreateError(issueMessage == null ? "The URI is invalid." : issueMessage);
        }

        string text;
        if (options.PreserveOriginal && uri.OriginalText != null)
        {
            text = uri.OriginalText;
        }
        else if (uri.Scheme == SipUriScheme.Sip || uri.Scheme == SipUriScheme.Sips)
        {
            text = FormatSipUri(uri, options.Canonical);
        }
        else if (uri.Scheme == SipUriScheme.Tel)
        {
            text = FormatTelephoneUri(uri, options.Canonical);
        }
        else
        {
            text = FormatAbsoluteUri(uri, options.Canonical);
        }

        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(text);
        if (bytes.Length > options.MaxHeaderLineBytes)
        {
            SipWriteResult limited = CreateError("The formatted URI exceeds the configured output limit.");
            limited.RequiredBytes = bytes.Length;
            return limited;
        }

        SipWriteResult result = new SipWriteResult();
        result.Success = true;
        result.Bytes = bytes;
        result.Written = bytes.Length;
        result.RequiredBytes = bytes.Length;
        return result;
    }

    static string FormatSipUri(SipUri uri, bool canonical)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        string scheme = uri.Scheme == SipUriScheme.Sips ? "sips" : "sip";
        if (!canonical && uri.SchemeToken != null)
        {
            scheme = uri.SchemeToken;
        }

        builder.Append(scheme);
        builder.Append(':');
        if (uri.User != null)
        {
            if (uri.ComparisonUserSource == uri.User && uri.ComparisonUserValue != null)
            {
                builder.Append(uri.ComparisonUserValue);
            }
            else
            {
                builder.Append(SipUriEncoding.EncodeUser(uri.User));
            }

            if (uri.Password != null)
            {
                builder.Append(':');
                if (uri.ComparisonPasswordSource == uri.Password && uri.ComparisonPasswordValue != null)
                {
                    builder.Append(uri.ComparisonPasswordValue);
                }
                else
                {
                    builder.Append(SipUriEncoding.EncodePassword(uri.Password));
                }
            }

            builder.Append('@');
        }

        string host = uri.Host == null ? "" : uri.Host;
        if (canonical)
        {
            host = host.ToLowerInvariant();
        }

        if (host.IndexOf(':') >= 0)
        {
            builder.Append('[');
            builder.Append(host);
            builder.Append(']');
        }
        else
        {
            builder.Append(host);
        }

        if (uri.Port >= 0)
        {
            builder.Append(':');
            builder.Append(uri.Port);
        }

        AppendParameters(builder, uri.Parameters, canonical);
        if (uri.Headers.Length > 0)
        {
            builder.Append('?');
            int index = 0;
            while (index < uri.Headers.Length)
            {
                if (index > 0)
                {
                    builder.Append('&');
                }

                SipUriHeader header = uri.Headers[index];
                if (header.ComparisonNameSource == header.Name && header.ComparisonNameValue != null)
                {
                    builder.Append(header.ComparisonNameValue);
                }
                else
                {
                    builder.Append(SipUriEncoding.EncodeHeader(header.Name == null ? "" : header.Name));
                }

                builder.Append('=');
                if (header.ComparisonValueSource == header.Value && header.ComparisonValueValue != null)
                {
                    builder.Append(header.ComparisonValueValue);
                }
                else
                {
                    builder.Append(SipUriEncoding.EncodeHeader(header.Value == null ? "" : header.Value));
                }
                index = index + 1;
            }
        }

        return builder.ToString();
    }

    static string FormatTelephoneUri(SipUri uri, bool canonical)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        string scheme = canonical || uri.SchemeToken == null ? "tel" : uri.SchemeToken;
        builder.Append(scheme);
        builder.Append(':');
        builder.Append(SipUriEncoding.EncodeTelephone(uri.User == null ? "" : uri.User));
        AppendParameters(builder, uri.Parameters, canonical);
        return builder.ToString();
    }

    static string FormatAbsoluteUri(SipUri uri, bool canonical)
    {
        string scheme = uri.SchemeToken == null ? "" : uri.SchemeToken;
        if (canonical)
        {
            scheme = scheme.ToLowerInvariant();
        }

        return scheme + ":" + uri.OpaquePart;
    }

    static void AppendParameters(System.Text.StringBuilder builder, SipUriParameter[] parameters, bool canonical)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            SipUriParameter parameter = parameters[index];
            builder.Append(';');
            string name = parameter.Name == null ? "" : parameter.Name;
            if (canonical)
            {
                name = name.ToLowerInvariant();
            }

            if (parameter.ComparisonNameSource == parameter.Name && parameter.ComparisonNameValue != null)
            {
                string comparisonName = canonical
                    ? parameter.ComparisonNameValue.ToLowerInvariant() : parameter.ComparisonNameValue;
                builder.Append(comparisonName);
            }
            else
            {
                builder.Append(SipUriEncoding.EncodeParameter(name));
            }

            if (parameter.HasValue)
            {
                builder.Append('=');
                if (parameter.ComparisonValueSource == parameter.Value && parameter.ComparisonValueValue != null)
                {
                    builder.Append(parameter.ComparisonValueValue);
                }
                else
                {
                    builder.Append(SipUriEncoding.EncodeParameter(parameter.Value == null ? "" : parameter.Value));
                }
            }

            index = index + 1;
        }
    }

    static SipWriteResult CreateError(string message)
    {
        SipWriteResult result = new SipWriteResult();
        result.ErrorMessage = message;
        return result;
    }
}
