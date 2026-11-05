namespace sip;

internal class SipUriParseState
{
    const int UserComponent = 1;
    const int PasswordComponent = 2;
    const int ParameterComponent = 3;
    const int HeaderComponent = 4;
    const int TelephoneComponent = 5;

    string text;
    SipParseOptions options;
    int failureOffset;
    string? failureMessage;

    internal SipUriParseState(string text, SipParseOptions options)
    {
        this.text = text;
        this.options = options;
        failureOffset = -1;
    }

    internal SipParseResult ParseUriText()
    {
        int colon = text.IndexOf(':');
        if (colon <= 0)
        {
            return CreateErrorResult(0, "The URI does not contain a valid scheme separator.");
        }

        string scheme = text.Substring(0, colon);
        if (!SipUriSyntax.IsScheme(scheme))
        {
            return CreateErrorResult(0, "The URI scheme is invalid.");
        }

        SipUri uri = new SipUri();
        uri.SchemeToken = scheme;
        if (options.PreserveOriginal)
        {
            uri.OriginalText = text;
        }

        int contentStart = colon + 1;
        bool success;
        if (SipAscii.EqualsIgnoreCase(scheme, "sip"))
        {
            uri.Scheme = SipUriScheme.Sip;
            success = ParseSipUri(uri, contentStart);
        }
        else if (SipAscii.EqualsIgnoreCase(scheme, "sips"))
        {
            uri.Scheme = SipUriScheme.Sips;
            success = ParseSipUri(uri, contentStart);
        }
        else if (SipAscii.EqualsIgnoreCase(scheme, "tel"))
        {
            uri.Scheme = SipUriScheme.Tel;
            success = ParseTelephoneUri(uri, contentStart);
        }
        else
        {
            uri.Scheme = SipUriScheme.Absolute;
            success = ParseAbsoluteUri(uri, contentStart);
        }

        if (!success)
        {
            return CreateErrorResult(failureOffset, failureMessage == null ? "The URI is invalid." : failureMessage);
        }

        SipValidationIssue[] issues = SipUriValidator.ValidateUriModel(uri);
        if (issues.Length > 0)
        {
            int issueOffset = issues[0].ByteOffset < 0 ? contentStart : issues[0].ByteOffset;
            string? issueMessage = issues[0].Message;
            return CreateErrorResult(issueOffset, issueMessage == null ? "The URI is invalid." : issueMessage);
        }

        SipParseResult result = new SipParseResult();
        result.Status = SipParseStatus.Complete;
        result.Uri = uri;
        result.Consumed = text.Length;
        result.RequiredBytes = text.Length;
        return result;
    }

    bool ParseSipUri(SipUri uri, int contentStart)
    {
        if (contentStart == text.Length)
        {
            return Fail(contentStart, "A SIP URI requires a host.");
        }

        int at = text.IndexOf('@', contentStart);
        int hostStart = contentStart;
        if (at >= 0)
        {
            if (!ParseUserInfo(uri, contentStart, at))
            {
                return false;
            }

            hostStart = at + 1;
        }

        int queryStart = text.IndexOf('?', hostStart);
        int componentEnd = queryStart < 0 ? text.Length : queryStart;
        int parameterStart = text.IndexOf(';', hostStart);
        if (parameterStart < 0 || parameterStart > componentEnd)
        {
            parameterStart = componentEnd;
        }

        if (!ParseHostPort(uri, hostStart, parameterStart))
        {
            return false;
        }

        if (!ParseParameters(uri, parameterStart, componentEnd, false))
        {
            return false;
        }

        if (queryStart >= 0 && !ParseHeaders(uri, queryStart + 1))
        {
            return false;
        }

        return true;
    }

    bool ParseUserInfo(SipUri uri, int start, int end)
    {
        if (start == end)
        {
            return Fail(start, "The SIP URI user cannot be empty when an at sign is present.");
        }

        int colon = text.IndexOf(':', start, end - start);
        int userEnd = colon < 0 ? end : colon;
        SipDecodedUriComponent user = DecodeComponent(start, userEnd, UserComponent);
        if (!user.Success)
        {
            return Fail(user.ErrorOffset, "The SIP URI user contains an invalid character or escape.");
        }

        uri.User = user.Value;
        uri.ComparisonUserSource = user.Value;
        uri.ComparisonUserValue = user.ComparisonValue;
        if (colon >= 0)
        {
            SipDecodedUriComponent password = DecodeComponent(colon + 1, end, PasswordComponent);
            if (!password.Success)
            {
                return Fail(password.ErrorOffset, "The SIP URI password contains an invalid character or escape.");
            }

            uri.Password = password.Value;
            uri.ComparisonPasswordSource = password.Value;
            uri.ComparisonPasswordValue = password.ComparisonValue;
        }

        return true;
    }

    bool ParseHostPort(SipUri uri, int start, int end)
    {
        if (start == end)
        {
            return Fail(start, "A SIP URI requires a host.");
        }

        int componentEnd = end;
        int portStart = -1;
        if (text[start] == '[')
        {
            int bracket = text.IndexOf(']', start + 1);
            if (bracket < 0 || bracket >= end)
            {
                return Fail(start, "The IPv6 host is missing its closing bracket.");
            }

            string ipv6 = text.Substring(start + 1, bracket - start - 1);
            if (!SipUriSyntax.IsIpv6Address(ipv6))
            {
                int obsoleteColon = ipv6.IndexOf(":::", System.StringComparison.Ordinal);
                if (obsoleteColon < 0)
                {
                    return Fail(start + 1, "The IPv6 host is invalid.");
                }
                string compatible = ipv6.Substring(0, obsoleteColon) + "::"
                    + ipv6.Substring(obsoleteColon + 3);
                if (!SipUriSyntax.IsIpv6Address(compatible))
                {
                    return Fail(start + 1, "The IPv6 host is invalid.");
                }
                ipv6 = compatible;
            }

            uri.Host = ipv6;
            if (bracket + 1 < end)
            {
                if (text[bracket + 1] != ':')
                {
                    return Fail(bracket + 1, "Unexpected text follows the IPv6 host.");
                }

                portStart = bracket + 2;
            }
        }
        else
        {
            int colon = text.IndexOf(':', start, end - start);
            if (colon >= 0)
            {
                if (text.IndexOf(':', colon + 1, end - colon - 1) >= 0)
                {
                    return Fail(colon, "An IPv6 host must be enclosed in brackets.");
                }

                portStart = colon + 1;
                end = colon;
            }

            string host = text.Substring(start, end - start);
            if (!SipUriSyntax.IsHostname(host))
            {
                return Fail(start, "The SIP URI host is invalid.");
            }

            uri.Host = host;
        }

        if (portStart >= 0)
        {
            int portEnd = componentEnd;

            if (portStart == portEnd)
            {
                return Fail(portStart, "The SIP URI port is empty.");
            }

            string portText = text.Substring(portStart, portEnd - portStart);
            SipNumericParseResult port = SipNumericParser.ParseNonNegativeInt32Text(portText, 65535);
            if (!port.Success)
            {
                return Fail(portStart + port.ErrorOffset, "The SIP URI port is invalid.");
            }

            uri.Port = port.Value;
        }

        return true;
    }

    bool ParseParameters(SipUri uri, int start, int end, bool telephone)
    {
        System.Collections.Generic.List<SipUriParameter> parameters = new System.Collections.Generic.List<SipUriParameter>();
        int position = start;
        while (position < end)
        {
            if (text[position] != ';')
            {
                return Fail(position, "A URI parameter must begin with a semicolon.");
            }

            int segmentStart = position + 1;
            int segmentEnd = segmentStart;
            while (segmentEnd < end && text[segmentEnd] != ';')
            {
                segmentEnd = segmentEnd + 1;
            }

            if (segmentStart == segmentEnd)
            {
                return Fail(segmentStart, "A URI parameter name cannot be empty.");
            }

            if (parameters.Count >= options.MaxParameterCount)
            {
                return Fail(segmentStart, "The URI exceeds the configured parameter count.");
            }

            int equals = text.IndexOf('=', segmentStart, segmentEnd - segmentStart);
            int nameEnd = equals < 0 ? segmentEnd : equals;
            if (telephone && text.IndexOf('%', segmentStart, nameEnd - segmentStart) >= 0)
            {
                return Fail(segmentStart, "A TEL URI parameter name cannot contain a percent escape.");
            }

            SipDecodedUriComponent name = DecodeComponent(segmentStart, nameEnd, ParameterComponent);
            if (!name.Success || name.Value == null || name.Value.Length == 0)
            {
                return Fail(name.ErrorOffset < 0 ? segmentStart : name.ErrorOffset, "The URI parameter name is invalid.");
            }

            if (telephone && !SipUriSyntax.IsTelephoneParameterName(name.Value))
            {
                return Fail(segmentStart, "The TEL URI parameter name is invalid.");
            }

            if (telephone && HasParameterName(parameters, name.Value))
            {
                return Fail(segmentStart, "A TEL URI parameter name cannot be repeated.");
            }

            SipUriParameter parameter = new SipUriParameter();
            parameter.Name = name.Value;
            parameter.ComparisonNameSource = name.Value;
            parameter.ComparisonNameValue = name.ComparisonValue;
            parameter.HasValue = equals >= 0;
            if (equals >= 0)
            {
                if (equals + 1 == segmentEnd)
                {
                    return Fail(equals + 1, "A URI parameter value cannot be empty.");
                }

                SipDecodedUriComponent value = DecodeComponent(equals + 1, segmentEnd, ParameterComponent);
                if (!value.Success)
                {
                    return Fail(value.ErrorOffset, "The URI parameter value is invalid.");
                }

                parameter.Value = value.Value;
                parameter.ComparisonValueSource = value.Value;
                parameter.ComparisonValueValue = value.ComparisonValue;
            }

            if (telephone && !ValidateTelephoneParameter(parameter))
            {
                return Fail(segmentStart, "The TEL URI parameter value is invalid.");
            }

            if (options.PreserveOriginal)
            {
                parameter.OriginalText = text.Substring(segmentStart, segmentEnd - segmentStart);
            }

            parameters.Add(parameter);
            position = segmentEnd;
        }

        uri.Parameters = parameters.ToArray();
        return true;
    }

    bool ParseHeaders(SipUri uri, int start)
    {
        if (start == text.Length)
        {
            return Fail(start, "A URI header name is required after the question mark.");
        }

        System.Collections.Generic.List<SipUriHeader> headers = new System.Collections.Generic.List<SipUriHeader>();
        int position = start;
        while (position < text.Length)
        {
            int segmentEnd = text.IndexOf('&', position);
            if (segmentEnd < 0)
            {
                segmentEnd = text.Length;
            }

            if (headers.Count >= options.MaxListValueCount)
            {
                return Fail(position, "The URI exceeds the configured header count.");
            }

            int equals = text.IndexOf('=', position, segmentEnd - position);
            if (equals < 0 || equals == position)
            {
                return Fail(position, "A URI header requires a non-empty name and an equals sign.");
            }

            SipDecodedUriComponent name = DecodeComponent(position, equals, HeaderComponent);
            SipDecodedUriComponent value = DecodeComponent(equals + 1, segmentEnd, HeaderComponent);
            if (!name.Success || name.Value == null || name.Value.Length == 0)
            {
                return Fail(name.ErrorOffset < 0 ? position : name.ErrorOffset, "The URI header name is invalid.");
            }

            if (!value.Success)
            {
                return Fail(value.ErrorOffset, "The URI header value is invalid.");
            }

            SipUriHeader header = new SipUriHeader();
            header.Name = name.Value;
            header.Value = value.Value;
            header.ComparisonNameSource = name.Value;
            header.ComparisonNameValue = name.ComparisonValue;
            header.ComparisonValueSource = value.Value;
            header.ComparisonValueValue = value.ComparisonValue;
            if (options.PreserveOriginal)
            {
                header.OriginalText = text.Substring(position, segmentEnd - position);
            }

            headers.Add(header);
            position = segmentEnd + 1;
            if (position == text.Length && segmentEnd < text.Length)
            {
                return Fail(position, "A URI header cannot be empty.");
            }
        }

        uri.Headers = headers.ToArray();
        return true;
    }

    bool ParseTelephoneUri(SipUri uri, int contentStart)
    {
        int query = text.IndexOf('?', contentStart);
        if (query >= 0)
        {
            return Fail(query, "A TEL URI cannot contain URI headers.");
        }

        int parameterStart = text.IndexOf(';', contentStart);
        if (parameterStart < 0)
        {
            parameterStart = text.Length;
        }

        if (text.IndexOf('%', contentStart, parameterStart - contentStart) >= 0)
        {
            return Fail(contentStart, "A TEL URI subscriber cannot contain a percent escape.");
        }

        SipDecodedUriComponent subscriber = DecodeComponent(contentStart, parameterStart, TelephoneComponent);
        if (!subscriber.Success || !SipUriSyntax.IsTelephoneSubscriber(subscriber.Value))
        {
            return Fail(subscriber.ErrorOffset < 0 ? contentStart : subscriber.ErrorOffset, "The TEL URI subscriber is invalid.");
        }

        uri.User = subscriber.Value;
        if (!ParseParameters(uri, parameterStart, text.Length, true))
        {
            return false;
        }

        if (uri.User != null && uri.User[0] != '+' && !ContainsParameterName(uri.Parameters, "phone-context"))
        {
            return Fail(contentStart, "A local TEL URI requires a phone-context parameter.");
        }

        return true;
    }

    bool ParseAbsoluteUri(SipUri uri, int contentStart)
    {
        string opaque = text.Substring(contentStart);
        if (!SipUriSyntax.IsVisibleAsciiString(opaque))
        {
            return Fail(contentStart, "The absolute URI scheme-specific part is invalid.");
        }

        int index = contentStart;
        while (index < text.Length)
        {
            if (text[index] == '%')
            {
                if (index + 2 >= text.Length || !SipUriSyntax.IsHexDigit(text[index + 1])
                    || !SipUriSyntax.IsHexDigit(text[index + 2]))
                {
                    return Fail(index, "The absolute URI contains an invalid percent escape.");
                }

                index = index + 3;
            }
            else
            {
                if (!SipUriSyntax.IsAbsoluteUriCharacter(text[index]))
                {
                    return Fail(index, "The absolute URI contains an invalid character.");
                }

                index = index + 1;
            }
        }

        uri.OpaquePart = opaque;
        return true;
    }

    SipDecodedUriComponent DecodeComponent(int start, int end, int component)
    {
        System.Collections.Generic.List<byte> bytes = new System.Collections.Generic.List<byte>();
        System.Text.StringBuilder comparison = new System.Text.StringBuilder();
        int index = start;
        while (index < end)
        {
            int character = text[index];
            if (character == '%')
            {
                if (index + 2 >= end || !SipUriSyntax.IsHexDigit(text[index + 1])
                    || !SipUriSyntax.IsHexDigit(text[index + 2]))
                {
                    return new SipDecodedUriComponent(false, null, null, index);
                }

                int high = SipUriSyntax.GetHexValue(text[index + 1]);
                int low = SipUriSyntax.GetHexValue(text[index + 2]);
                int decodedByte = high * 16 + low;
                bytes.Add((byte)decodedByte);
                if (decodedByte < 128 && SipUriSyntax.IsUnreserved(decodedByte))
                {
                    comparison.Append((char)decodedByte);
                }
                else
                {
                    comparison.Append('%');
                    comparison.Append(GetHexCharacter(high));
                    comparison.Append(GetHexCharacter(low));
                }

                index = index + 3;
            }
            else
            {
                if (!IsAllowedRawCharacter(character, component))
                {
                    return new SipDecodedUriComponent(false, null, null, index);
                }

                bytes.Add((byte)character);
                comparison.Append((char)character);
                index = index + 1;
            }
        }

        byte[] decodedBytes = bytes.ToArray();
        if (!SipUtf8Validator.IsValidUtf8(decodedBytes))
        {
            return new SipDecodedUriComponent(false, null, null, start);
        }

        string decoded = System.Text.Encoding.UTF8.GetString(decodedBytes);
        return new SipDecodedUriComponent(true, decoded, comparison.ToString(), -1);
    }

    char GetHexCharacter(int value)
    {
        if (value < 10)
        {
            return (char)('0' + value);
        }

        return (char)('A' + value - 10);
    }

    bool IsAllowedRawCharacter(int character, int component)
    {
        if (SipUriSyntax.IsUnreserved(character))
        {
            return true;
        }

        switch (component)
        {
            case UserComponent:
                return character == '&' || character == '=' || character == '+' || character == '$'
                    || character == ',' || character == ';' || character == '?' || character == '/';
            case PasswordComponent:
                return character == '&' || character == '=' || character == '+' || character == '$' || character == ',';
            case ParameterComponent:
                return character == '[' || character == ']' || character == '/' || character == ':'
                    || character == '&' || character == '+' || character == '$';
            case HeaderComponent:
                return character == '[' || character == ']' || character == '/' || character == '?'
                    || character == ':' || character == '+' || character == '$';
            case TelephoneComponent:
                return SipUriSyntax.IsAsciiDigit(character) || character >= 'A' && character <= 'F'
                    || character >= 'a' && character <= 'f' || character == '+' || character == '*'
                    || character == '#' || character == '-' || character == '.' || character == '(' || character == ')';
            default:
                return false;
        }
    }

    bool HasParameterName(System.Collections.Generic.List<SipUriParameter> parameters, string name)
    {
        foreach (SipUriParameter parameter in parameters)
        {
            if (SipAscii.EqualsIgnoreCase(parameter.Name, name))
            {
                return true;
            }
        }

        return false;
    }

    bool ValidateTelephoneParameter(SipUriParameter parameter)
    {
        if (SipAscii.EqualsIgnoreCase(parameter.Name, "ext"))
        {
            return parameter.HasValue && SipUriSyntax.IsTelephoneExtension(parameter.Value);
        }

        if (SipAscii.EqualsIgnoreCase(parameter.Name, "isub"))
        {
            return parameter.HasValue && parameter.Value != null && parameter.Value.Length > 0;
        }

        if (SipAscii.EqualsIgnoreCase(parameter.Name, "phone-context"))
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

        return true;
    }

    bool ContainsParameterName(SipUriParameter[] parameters, string name)
    {
        foreach (SipUriParameter parameter in parameters)
        {
            if (SipAscii.EqualsIgnoreCase(parameter.Name, name))
            {
                return true;
            }
        }

        return false;
    }

    bool Fail(int offset, string message)
    {
        failureOffset = offset;
        failureMessage = message;
        return false;
    }

    SipParseResult CreateErrorResult(int offset, string message)
    {
        SipParseResult result = new SipParseResult();
        result.Status = SipParseStatus.Error;
        result.Consumed = offset < 0 ? 0 : offset;
        result.RequiredBytes = text.Length;
        result.ErrorOffset = offset < 0 ? 0 : offset;
        result.ErrorMessage = message;
        SipValidationIssue issue = new SipValidationIssue();
        issue.Severity = SipValidationSeverity.Error;
        issue.Code = "invalid-uri";
        issue.Message = message;
        issue.ByteOffset = result.ErrorOffset;
        issue.Path = "uri";
        result.ValidationIssues = new SipValidationIssue[] { issue };
        return result;
    }
}
