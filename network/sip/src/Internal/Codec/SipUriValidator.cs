namespace sip;

internal static class SipUriValidator
{
    internal static SipValidationIssue[] ValidateUriModel(SipUri? uri)
    {
        System.Collections.Generic.List<SipValidationIssue> issues = new System.Collections.Generic.List<SipValidationIssue>();
        if (uri == null)
        {
            issues.Add(CreateIssue("uri-required", "A URI value is required.", "uri"));
            return issues.ToArray();
        }

        if (uri.Scheme == SipUriScheme.Sip || uri.Scheme == SipUriScheme.Sips)
        {
            ValidateSipUri(uri, issues);
        }
        else if (uri.Scheme == SipUriScheme.Tel)
        {
            ValidateTelephoneUri(uri, issues);
        }
        else if (uri.Scheme == SipUriScheme.Absolute)
        {
            ValidateAbsoluteUri(uri, issues);
        }
        else
        {
            issues.Add(CreateIssue("uri-scheme", "The URI scheme is not recognized.", "uri.scheme"));
        }

        return issues.ToArray();
    }

    static void ValidateSipUri(SipUri uri, System.Collections.Generic.List<SipValidationIssue> issues)
    {
        string expectedScheme = uri.Scheme == SipUriScheme.Sips ? "sips" : "sip";
        if (uri.SchemeToken != null && !SipAscii.EqualsIgnoreCase(uri.SchemeToken, expectedScheme))
        {
            issues.Add(CreateIssue("uri-scheme", "The URI scheme token does not match the recognized scheme.", "uri.schemeToken"));
        }

        if (uri.Host == null || uri.Host.Length == 0)
        {
            issues.Add(CreateIssue("uri-host", "A SIP URI requires a host.", "uri.host"));
        }
        else if (uri.Host.IndexOf(':') >= 0)
        {
            if (!SipUriSyntax.IsIpv6Address(uri.Host))
            {
                issues.Add(CreateIssue("uri-host", "The SIP URI IPv6 host is invalid.", "uri.host"));
            }
        }
        else if (!SipUriSyntax.IsHostname(uri.Host))
        {
            issues.Add(CreateIssue("uri-host", "The SIP URI host is invalid.", "uri.host"));
        }

        if (uri.Port < -1 || uri.Port > 65535)
        {
            issues.Add(CreateIssue("uri-port", "The SIP URI port must be between 0 and 65535 or absent.", "uri.port"));
        }

        if (uri.Password != null && uri.User == null)
        {
            issues.Add(CreateIssue("uri-userinfo", "A SIP URI password requires a user.", "uri.password"));
        }
        else if (uri.User != null && uri.User.Length == 0)
        {
            issues.Add(CreateIssue("uri-userinfo", "A SIP URI user cannot be empty.", "uri.user"));
        }

        if (uri.OpaquePart != null)
        {
            issues.Add(CreateIssue("uri-components", "A SIP URI cannot contain an opaque scheme-specific part.", "uri.opaquePart"));
        }

        ValidateParameterModels(uri.Parameters, issues);
        ValidateSipParameterModels(uri, issues);
        ValidateUniqueParameters(uri.Parameters, issues);
        ValidateHeaderModels(uri.Headers, issues);
    }

    static void ValidateTelephoneUri(SipUri uri, System.Collections.Generic.List<SipValidationIssue> issues)
    {
        if (uri.SchemeToken != null && !SipAscii.EqualsIgnoreCase(uri.SchemeToken, "tel"))
        {
            issues.Add(CreateIssue("uri-scheme", "The URI scheme token does not match the TEL scheme.", "uri.schemeToken"));
        }

        if (!SipUriSyntax.IsTelephoneSubscriber(uri.User))
        {
            issues.Add(CreateIssue("uri-subscriber", "The TEL URI subscriber is invalid.", "uri.user"));
        }
        else if (uri.User != null && uri.User[0] != '+' && !ContainsParameter(uri.Parameters, "phone-context"))
        {
            issues.Add(CreateIssue("uri-phone-context", "A local TEL URI requires a phone-context parameter.", "uri.parameters"));
        }

        if (uri.Host != null || uri.Password != null || uri.Port != -1 || uri.OpaquePart != null
            || uri.Headers == null || uri.Headers.Length != 0)
        {
            issues.Add(CreateIssue("uri-tel-components", "A TEL URI cannot contain SIP host, password, port, or header components.", "uri"));
        }

        ValidateParameterModels(uri.Parameters, issues);
        ValidateTelephoneParameterModels(uri.Parameters, issues);
        ValidateUniqueParameters(uri.Parameters, issues);
        if (uri.User != null && uri.User.Length > 0 && uri.User[0] == '+'
            && ContainsParameter(uri.Parameters, "phone-context"))
            issues.Add(CreateIssue("uri-phone-context",
                "A global TEL URI cannot contain a phone-context parameter.", "uri.parameters"));
        if (ContainsParameter(uri.Parameters, "ext") && ContainsParameter(uri.Parameters, "isub"))
            issues.Add(CreateIssue("uri-tel-subaddress",
                "A TEL URI cannot contain both ext and isub parameters.", "uri.parameters"));
    }

    static void ValidateAbsoluteUri(SipUri uri, System.Collections.Generic.List<SipValidationIssue> issues)
    {
        if (!SipUriSyntax.IsScheme(uri.SchemeToken))
        {
            issues.Add(CreateIssue("uri-scheme", "The absolute URI scheme is invalid.", "uri.schemeToken"));
        }

        if (uri.OpaquePart == null || uri.OpaquePart.Length == 0 || !IsAbsoluteUriText(uri.OpaquePart))
        {
            issues.Add(CreateIssue("uri-opaque", "The absolute URI scheme-specific part is invalid.", "uri.opaquePart"));
        }


        if (uri.User != null || uri.Password != null || uri.Host != null || uri.Port != -1
            || uri.Parameters == null || uri.Parameters.Length != 0 || uri.Headers == null || uri.Headers.Length != 0)
        {
            issues.Add(CreateIssue("uri-components", "An absolute extension URI cannot contain SIP or TEL components.", "uri"));
        }
    }

    static void ValidateParameterModels(SipUriParameter[]? parameters, System.Collections.Generic.List<SipValidationIssue> issues)
    {
        if (parameters == null)
        {
            issues.Add(CreateIssue("uri-parameters", "The URI parameter collection cannot be null.", "uri.parameters"));
            return;
        }

        int index = 0;
        while (index < parameters.Length)
        {
            SipUriParameter? parameter = parameters[index];
            if (parameter == null || parameter.Name == null || parameter.Name.Length == 0)
            {
                issues.Add(CreateIssue("uri-parameter-name", "A URI parameter requires a name.", $"uri.parameters[{index}]"));
            }
            else if (parameter.HasValue && (parameter.Value == null || parameter.Value.Length == 0))
            {
                issues.Add(CreateIssue("uri-parameter-value", "A valued URI parameter requires a value.", $"uri.parameters[{index}]"));
            }

            index = index + 1;
        }
    }

    static void ValidateHeaderModels(SipUriHeader[]? headers, System.Collections.Generic.List<SipValidationIssue> issues)
    {
        if (headers == null)
        {
            issues.Add(CreateIssue("uri-headers", "The URI header collection cannot be null.", "uri.headers"));
            return;
        }

        int index = 0;
        while (index < headers.Length)
        {
            SipUriHeader? header = headers[index];
            if (header == null || header.Name == null || header.Name.Length == 0)
            {
                issues.Add(CreateIssue("uri-header-name", "A URI header requires a name.", $"uri.headers[{index}]"));
            }
            else if (header.Value == null)
            {
                issues.Add(CreateIssue("uri-header-value", "A URI header requires a value.", $"uri.headers[{index}]"));
            }

            index = index + 1;
        }
    }

    static void ValidateUniqueParameters(SipUriParameter[]? parameters, System.Collections.Generic.List<SipValidationIssue> issues)
    {
        if (parameters == null)
        {
            return;
        }

        int left = 0;
        while (left < parameters.Length)
        {
            if (parameters[left] != null && parameters[left].Name != null)
            {
                int right = left + 1;
                while (right < parameters.Length)
                {
                    if (parameters[right] != null && SipAscii.EqualsIgnoreCase(parameters[left].Name, parameters[right].Name))
                    {
                        issues.Add(CreateIssue("uri-parameter-duplicate", "A URI parameter cannot be repeated.", $"uri.parameters[{right}]"));
                    }

                    right = right + 1;
                }
            }

            left = left + 1;
        }
    }

    static void ValidateSipParameterModels(SipUri uri,
        System.Collections.Generic.List<SipValidationIssue> issues)
    {
        if (uri.Parameters == null) return;
        string? transport = null;
        int index = 0;
        while (index < uri.Parameters.Length)
        {
            SipUriParameter? parameter = uri.Parameters[index];
            if (parameter != null && parameter.Name != null)
            {
                string path = $"uri.parameters[{index}]";
                if (SipAscii.EqualsIgnoreCase(parameter.Name, "transport"))
                {
                    if (!parameter.HasValue || !SipAscii.IsToken(parameter.Value))
                        issues.Add(CreateIssue("uri-transport",
                            "The SIP URI transport parameter is invalid.", path));
                    else transport = parameter.Value;
                }
                else if (SipAscii.EqualsIgnoreCase(parameter.Name, "user"))
                {
                    if (!parameter.HasValue || !SipAscii.IsToken(parameter.Value))
                        issues.Add(CreateIssue("uri-user-parameter",
                            "The SIP URI user parameter is invalid.", path));
                }
                else if (SipAscii.EqualsIgnoreCase(parameter.Name, "method"))
                {
                    if (!parameter.HasValue || !SipAscii.IsToken(parameter.Value))
                        issues.Add(CreateIssue("uri-method",
                            "The SIP URI method parameter is invalid.", path));
                }
                else if (SipAscii.EqualsIgnoreCase(parameter.Name, "ttl"))
                {
                    if (!parameter.HasValue || !IsDecimalAtMost(parameter.Value, 255))
                        issues.Add(CreateIssue("uri-ttl", "The SIP URI ttl parameter is invalid.", path));
                }
                else if (SipAscii.EqualsIgnoreCase(parameter.Name, "maddr"))
                {
                    if (!parameter.HasValue || !IsSipParameterHost(parameter.Value))
                        issues.Add(CreateIssue("uri-maddr", "The SIP URI maddr parameter is invalid.", path));
                }
            }
            index = index + 1;
        }
        if (uri.Scheme == SipUriScheme.Sips && SipAscii.EqualsIgnoreCase(transport, "udp"))
            issues.Add(CreateIssue("uri-sips-transport", "A SIPS URI cannot select UDP transport.",
                "uri.parameters"));
    }

    static bool IsDecimalAtMost(string? value, int maximum)
    {
        if (value == null || value.Length == 0) return false;
        int number = 0;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9') return false;
            int digit = value[index] - '0';
            if (number > (maximum - digit) / 10) return false;
            number = number * 10 + digit;
            index = index + 1;
        }
        return true;
    }

    static bool IsSipParameterHost(string? value)
    {
        if (value == null || value.Length == 0) return false;
        if (value[0] == '[')
            return value.Length >= 3 && value[value.Length - 1] == ']'
                && SipUriSyntax.IsIpv6Address(value.Substring(1, value.Length - 2));
        return SipUriSyntax.IsHostname(value);
    }

    static void ValidateTelephoneParameterModels(SipUriParameter[]? parameters, System.Collections.Generic.List<SipValidationIssue> issues)
    {
        if (parameters == null)
        {
            return;
        }

        int index = 0;
        while (index < parameters.Length)
        {
            SipUriParameter? parameter = parameters[index];
            if (parameter != null && parameter.Name != null)
            {
                if (!SipUriSyntax.IsTelephoneParameterName(parameter.Name))
                {
                    issues.Add(CreateIssue("uri-parameter-name", "The TEL URI parameter name is invalid.", $"uri.parameters[{index}]"));
                }
                else if (SipAscii.EqualsIgnoreCase(parameter.Name, "ext")
                    && (!parameter.HasValue || !SipUriSyntax.IsTelephoneExtension(parameter.Value)))
                {
                    issues.Add(CreateIssue("uri-parameter-value", "The TEL URI extension parameter is invalid.", $"uri.parameters[{index}]"));
                }
                else if (SipAscii.EqualsIgnoreCase(parameter.Name, "isub")
                    && (!parameter.HasValue || parameter.Value == null || parameter.Value.Length == 0))
                {
                    issues.Add(CreateIssue("uri-parameter-value", "The TEL URI ISDN subaddress parameter is invalid.", $"uri.parameters[{index}]"));
                }
                else if (SipAscii.EqualsIgnoreCase(parameter.Name, "phone-context")
                    && !IsTelephoneContext(parameter))
                {
                    issues.Add(CreateIssue("uri-parameter-value", "The TEL URI phone context is invalid.", $"uri.parameters[{index}]"));
                }
            }

            index = index + 1;
        }
    }

    static bool IsTelephoneContext(SipUriParameter parameter)
    {
        if (!parameter.HasValue || parameter.Value == null || parameter.Value.Length == 0)
        {
            return false;
        }

        if (parameter.Value[0] == '+')
        {
            return SipUriSyntax.IsTelephoneSubscriber(parameter.Value);
        }

        return SipUriSyntax.IsHostname(parameter.Value);
    }

    static bool ContainsParameter(SipUriParameter[]? parameters, string name)
    {
        if (parameters == null)
        {
            return false;
        }

        foreach (SipUriParameter parameter in parameters)
        {
            if (parameter != null && SipAscii.EqualsIgnoreCase(parameter.Name, name))
            {
                return true;
            }
        }

        return false;
    }

    static SipValidationIssue CreateIssue(string code, string message, string path)
    {
        SipValidationIssue issue = new SipValidationIssue();
        issue.Severity = SipValidationSeverity.Error;
        issue.Code = code;
        issue.Message = message;
        issue.Path = path;
        return issue;
    }

    static bool IsAbsoluteUriText(string value)
    {
        if (!SipUriSyntax.IsVisibleAsciiString(value) || !SipUriSyntax.HasValidPercentEscapes(value))
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '%')
            {
                index = index + 3;
            }
            else
            {
                if (!SipUriSyntax.IsAbsoluteUriCharacter(value[index]))
                {
                    return false;
                }

                index = index + 1;
            }
        }

        return true;
    }
}
