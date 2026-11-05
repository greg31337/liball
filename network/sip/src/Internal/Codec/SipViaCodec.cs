namespace sip;

internal static class SipViaCodec
{
    internal static SipViaParseResult ParseViaHeaderValue(string? text, SipParseOptions? options)
    {
        if (text == null || options == null)
        {
            return CreateParseError(0, "A Via value and parse options are required.");
        }

        SipHeaderPrimitiveResult split = SipHeaderPrimitives.SplitCommaListText(text, options.MaxListValueCount);
        if (!split.Success)
        {
            return CreateParseError(split.ErrorOffset, split.ErrorMessage);
        }

        SipViaValue[] values = new SipViaValue[split.TextValues.Length];
        int index = 0;
        int searchStart = 0;
        while (index < split.TextValues.Length)
        {
            string valueText = split.TextValues[index];
            int valueOffset = text.IndexOf(valueText, searchStart, System.StringComparison.Ordinal);
            if (valueOffset < 0)
            {
                valueOffset = searchStart;
            }

            SipViaParseResult parsed = ParseSingleViaValue(valueText, options);
            if (!parsed.Success || parsed.Values.Length == 0)
            {
                return CreateParseError(valueOffset + parsed.ErrorOffset, parsed.ErrorMessage);
            }

            values[index] = parsed.Values[0];
            searchStart = valueOffset + valueText.Length;
            index = index + 1;
        }

        SipViaParseResult result = new SipViaParseResult();
        result.Success = true;
        result.Values = values;
        return result;
    }

    internal static SipWriteResult FormatViaHeaderValues(SipViaValue[]? values, SipWriteOptions? options)
    {
        if (values == null || values.Length == 0)
        {
            return CreateWriteError("At least one Via value is required.");
        }

        if (options == null)
        {
            return CreateWriteError("Via write options are required.");
        }

        if (options.MaxHeaderLineBytes < 0)
        {
            return CreateWriteError("The maximum header line size cannot be negative.");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append(options.CompactHeaderNames ? SipHeaderNames.CompactVia : SipHeaderNames.Via);
        builder.Append(": ");
        int index = 0;
        while (index < values.Length)
        {
            SipViaValue? value = values[index];
            string? issue = ValidateViaModel(value);
            if (issue != null || value == null)
            {
                return CreateWriteError(issue == null ? "A Via value is required." : issue);
            }

            if (index > 0)
            {
                builder.Append(", ");
            }

            AppendViaValue(builder, value, options.Canonical);
            index = index + 1;
        }

        builder.Append("\r\n");
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(builder.ToString());
        if (bytes.Length > options.MaxHeaderLineBytes)
        {
            SipWriteResult limited = CreateWriteError("The formatted Via header exceeds the configured output limit.");
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

    static SipViaParseResult ParseSingleViaValue(string text, SipParseOptions options)
    {
        int position = 0;
        int protocolEnd = ReadTokenEnd(text, position);
        if (protocolEnd == position)
        {
            return CreateParseError(position, "The Via protocol name is invalid.");
        }

        string protocol = text.Substring(position, protocolEnd - position);
        position = SkipWhitespace(text, protocolEnd);
        if (position >= text.Length || text[position] != '/')
        {
            return CreateParseError(position, "The Via sent-protocol is missing its first slash.");
        }

        position = SkipWhitespace(text, position + 1);
        int versionEnd = ReadTokenEnd(text, position);
        if (versionEnd == position)
        {
            return CreateParseError(position, "The Via protocol version is invalid.");
        }

        string version = text.Substring(position, versionEnd - position);
        position = SkipWhitespace(text, versionEnd);
        if (position >= text.Length || text[position] != '/')
        {
            return CreateParseError(position, "The Via sent-protocol is missing its second slash.");
        }

        position = SkipWhitespace(text, position + 1);
        int transportEnd = ReadTokenEnd(text, position);
        if (transportEnd == position)
        {
            return CreateParseError(position, "The Via transport is invalid.");
        }

        string transport = text.Substring(position, transportEnd - position);
        if (transportEnd >= text.Length || text[transportEnd] != ' ' && text[transportEnd] != '\t')
        {
            return CreateParseError(transportEnd, "The Via sent-protocol must be followed by linear whitespace.");
        }

        position = SkipWhitespace(text, transportEnd);
        int sentByEnd = position;
        while (sentByEnd < text.Length && text[sentByEnd] != ';'
            && text[sentByEnd] != ' ' && text[sentByEnd] != '\t')
        {
            sentByEnd = sentByEnd + 1;
        }

        if (sentByEnd == position)
        {
            return CreateParseError(position, "The Via sent-by host is required.");
        }

        SipHeaderPrimitiveResult sentBy = SipHeaderPrimitives.ParseHostPortText(
            text.Substring(position, sentByEnd - position));
        if (!sentBy.Success)
        {
            return CreateParseError(position + sentBy.ErrorOffset, sentBy.ErrorMessage);
        }

        SipViaValue via = new SipViaValue();
        via.ProtocolName = protocol;
        via.ProtocolVersion = version;
        via.Transport = transport;
        via.Host = sentBy.Host;
        via.Port = sentBy.Port;
        position = SkipWhitespace(text, sentByEnd);
        System.Collections.Generic.List<SipUriParameter> parameters = new System.Collections.Generic.List<SipUriParameter>();
        SipViaKnownParameterState known = new SipViaKnownParameterState();
        while (position < text.Length)
        {
            if (text[position] != ';')
            {
                return CreateParseError(position, "A Via parameter must begin with a semicolon.");
            }

            int parameterStart = position + 1;
            int parameterEnd = FindParameterEnd(text, parameterStart);
            if (parameterEnd < 0)
            {
                return CreateParseError(text.Length, "A Via parameter contains an unterminated quoted string.");
            }

            if (parameters.Count >= options.MaxParameterCount)
            {
                return CreateParseError(parameterStart, "The Via value exceeds the configured parameter count.");
            }

            string parameterText = text.Substring(parameterStart, parameterEnd - parameterStart);
            SipHeaderPrimitiveResult parameterResult = ParseViaParameter(parameterText);
            if (!parameterResult.Success || parameterResult.Parameter == null)
            {
                return CreateParseError(parameterStart + parameterResult.ErrorOffset, parameterResult.ErrorMessage);
            }

            string? issue = ApplyKnownParameter(via, parameterResult.Parameter, known);
            if (issue != null)
            {
                return CreateParseError(parameterStart, issue);
            }

            if (!options.PreserveOriginal)
            {
                parameterResult.Parameter.OriginalText = null;
            }

            parameters.Add(parameterResult.Parameter);
            position = SkipWhitespace(text, parameterEnd);
        }

        via.Parameters = parameters.ToArray();
        SipViaParseResult result = new SipViaParseResult();
        result.Success = true;
        result.Values = new SipViaValue[] { via };
        return result;
    }

    static SipHeaderPrimitiveResult ParseViaParameter(string text)
    {
        SipHeaderPrimitiveResult generic = SipHeaderPrimitives.ParseParameterText(text);
        if (generic.Success)
        {
            SipUriParameter? parsed = generic.Parameter;
            if (parsed != null && SipAscii.EqualsIgnoreCase(parsed.Name, "received")
                && parsed.Value != null && parsed.Value.Length > 2 && parsed.Value[0] == '['
                && parsed.Value[parsed.Value.Length - 1] == ']')
            {
                string address = parsed.Value.Substring(1, parsed.Value.Length - 2);
                if (SipUriSyntax.IsIpv6Address(address)) parsed.Value = address;
            }
            return generic;
        }

        int equals = text.IndexOf('=');
        if (equals < 0)
        {
            return generic;
        }

        string name = TrimWhitespace(text.Substring(0, equals));
        string value = TrimWhitespace(text.Substring(equals + 1));
        if (!SipAscii.EqualsIgnoreCase(name, "received") || !SipUriSyntax.IsIpv6Address(value))
        {
            return generic;
        }

        SipUriParameter parameter = new SipUriParameter();
        parameter.Name = name;
        parameter.Value = value;
        parameter.HasValue = true;
        parameter.OriginalText = TrimWhitespace(text);
        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
        result.Success = true;
        result.Parameter = parameter;
        return result;
    }

    static string? ApplyKnownParameter(SipViaValue via, SipUriParameter parameter,
        SipViaKnownParameterState known)
    {
        if (SipAscii.EqualsIgnoreCase(parameter.Name, "branch"))
        {
            if (known.Branch)
            {
                return "The Via branch parameter cannot be repeated.";
            }

            if (!parameter.HasValue || IsParameterValueQuoted(parameter) || !SipAscii.IsToken(parameter.Value))
            {
                return "The Via branch parameter requires a token value.";
            }

            known.Branch = true;
            via.Branch = parameter.Value;
        }
        else if (SipAscii.EqualsIgnoreCase(parameter.Name, "received"))
        {
            if (known.Received)
            {
                return "The Via received parameter cannot be repeated.";
            }

            if (!parameter.HasValue || IsParameterValueQuoted(parameter) || parameter.Value == null
                || !SipUriSyntax.IsIpv4Address(parameter.Value) && !SipUriSyntax.IsIpv6Address(parameter.Value))
            {
                return "The Via received parameter requires an IPv4 or IPv6 address.";
            }

            known.Received = true;
            via.Received = parameter.Value;
        }
        else if (SipAscii.EqualsIgnoreCase(parameter.Name, "rport"))
        {
            if (known.RPort)
            {
                return "The Via rport parameter cannot be repeated.";
            }

            known.RPort = true;
            via.HasRPort = true;
            if (parameter.HasValue)
            {
                if (IsParameterValueQuoted(parameter))
                {
                    return "The Via rport parameter requires an unquoted port number.";
                }

                SipNumericParseResult port = SipNumericParser.ParseNonNegativeInt32Text(parameter.Value, 65535);
                if (!port.Success)
                {
                    return "The Via rport parameter requires a valid port number.";
                }

                via.RPort = port.Value;
            }
        }
        else if (SipAscii.EqualsIgnoreCase(parameter.Name, "maddr"))
        {
            if (known.MAddr)
            {
                return "The Via maddr parameter cannot be repeated.";
            }

            if (!parameter.HasValue || IsParameterValueQuoted(parameter) || !IsHostParameterValue(parameter.Value))
            {
                return "The Via maddr parameter requires a host value.";
            }

            known.MAddr = true;
        }
        else if (SipAscii.EqualsIgnoreCase(parameter.Name, "ttl"))
        {
            if (known.Ttl)
            {
                return "The Via ttl parameter cannot be repeated.";
            }

            SipNumericParseResult ttl = SipNumericParser.ParseNonNegativeInt32Text(parameter.Value, 255);
            if (!parameter.HasValue || IsParameterValueQuoted(parameter) || !ttl.Success
                || parameter.Value == null || parameter.Value.Length > 3)
            {
                return "The Via ttl parameter requires a value from zero through 255.";
            }

            known.Ttl = true;
        }
        else if (SipAscii.EqualsIgnoreCase(parameter.Name, "alias"))
        {
            if (known.Alias)
            {
                return "The Via alias parameter cannot be repeated.";
            }

            if (parameter.HasValue)
            {
                return "The Via alias parameter must not have a value.";
            }

            known.Alias = true;
        }

        return null;
    }

    static string? ValidateViaModel(SipViaValue? via)
    {
        if (via == null)
        {
            return "A Via value is required.";
        }

        if (!SipAscii.IsToken(via.ProtocolName) || !SipAscii.IsToken(via.ProtocolVersion)
            || !SipAscii.IsToken(via.Transport))
        {
            return "The Via sent-protocol components must be tokens.";
        }

        if (via.Host == null || via.Host.Length == 0 || via.Port < -1 || via.Port > 65535)
        {
            return "The Via sent-by value is invalid.";
        }

        bool ipv6Host = via.Host.IndexOf(':') >= 0;
        if (ipv6Host && !SipUriSyntax.IsIpv6Address(via.Host)
            || !ipv6Host && !SipUriSyntax.IsHostname(via.Host))
        {
            return "The Via sent-by value is invalid.";
        }

        if (via.Parameters == null)
        {
            return "The Via parameter collection cannot be null.";
        }

        SipViaValue parsed = new SipViaValue();
        SipViaKnownParameterState known = new SipViaKnownParameterState();
        int index = 0;
        while (index < via.Parameters.Length)
        {
            SipUriParameter? parameter = via.Parameters[index];
            if (parameter == null)
            {
                return "A Via parameter cannot be null.";
            }

            string? issue = ApplyKnownParameter(parsed, parameter, known);
            if (issue != null)
            {
                return issue;
            }

            if (!IsValidGenericParameter(parameter))
            {
                return "A Via extension parameter is invalid.";
            }

            index = index + 1;
        }

        if (via.Branch != null && !SipAscii.IsToken(via.Branch))
        {
            return "The Via branch field must be a token.";
        }

        if (via.Received != null && !SipUriSyntax.IsIpv4Address(via.Received)
            && !SipUriSyntax.IsIpv6Address(via.Received))
        {
            return "The Via received field must be an IPv4 or IPv6 address.";
        }

        if (via.RPort < -1 || via.RPort > 65535 || via.RPort >= 0 && !via.HasRPort)
        {
            return "The Via rport fields are inconsistent.";
        }

        return null;
    }

    static bool IsValidGenericParameter(SipUriParameter parameter)
    {
        if (!SipAscii.IsToken(parameter.Name))
        {
            return false;
        }

        if (!parameter.HasValue)
        {
            return true;
        }

        if (parameter.Value == null || parameter.Value.Length == 0)
        {
            return false;
        }

        if (SipAscii.IsToken(parameter.Value) || IsHostParameterValue(parameter.Value))
        {
            return true;
        }

        int index = 0;
        while (index < parameter.Value.Length)
        {
            int character = parameter.Value[index];
            if (character == '\r' || character == '\n')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsHostParameterValue(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        if (value[0] == '[' && value[value.Length - 1] == ']')
        {
            return SipUriSyntax.IsIpv6Address(value.Substring(1, value.Length - 2));
        }

        return SipUriSyntax.IsHostname(value);
    }

    static bool IsParameterValueQuoted(SipUriParameter parameter)
    {
        if (parameter.OriginalText == null)
        {
            return false;
        }

        int equals = parameter.OriginalText.IndexOf('=');
        if (equals < 0)
        {
            return false;
        }

        int index = SkipWhitespace(parameter.OriginalText, equals + 1);
        return index < parameter.OriginalText.Length && parameter.OriginalText[index] == '"';
    }

    static void AppendViaValue(System.Text.StringBuilder builder, SipViaValue via, bool canonical)
    {
        string protocol = via.ProtocolName == null ? "" : via.ProtocolName;
        string version = via.ProtocolVersion == null ? "" : via.ProtocolVersion;
        string transport = via.Transport == null ? "" : via.Transport;
        string host = via.Host == null ? "" : via.Host;
        if (canonical)
        {
            protocol = protocol.ToUpperInvariant();
            version = version.ToUpperInvariant();
            transport = transport.ToUpperInvariant();
            host = host.ToLowerInvariant();
        }

        builder.Append(protocol);
        builder.Append('/');
        builder.Append(version);
        builder.Append('/');
        builder.Append(transport);
        builder.Append(' ');
        builder.Append(SipHeaderPrimitives.FormatHostPortText(host, via.Port));

        bool branchWritten = false;
        bool receivedWritten = false;
        bool rportWritten = false;
        int index = 0;
        while (index < via.Parameters.Length)
        {
            SipUriParameter parameter = via.Parameters[index];
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "branch"))
            {
                string branch = via.Branch == null ? parameter.Value == null ? "" : parameter.Value : via.Branch;
                AppendKnownParameter(builder, parameter.Name == null ? "branch" : parameter.Name,
                    branch, true, canonical);
                branchWritten = true;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "received"))
            {
                string received = via.Received == null
                    ? parameter.Value == null ? "" : parameter.Value : via.Received;
                if (canonical)
                {
                    received = received.ToLowerInvariant();
                }

                AppendKnownParameter(builder, parameter.Name == null ? "received" : parameter.Name,
                    received, true, canonical);
                receivedWritten = true;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "rport"))
            {
                string? port = parameter.Value;
                bool hasValue = parameter.HasValue;
                if (via.HasRPort)
                {
                    port = via.RPort < 0 ? null
                        : via.RPort.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    hasValue = via.RPort >= 0;
                }

                AppendKnownParameter(builder, parameter.Name == null ? "rport" : parameter.Name,
                    port, hasValue, canonical);
                rportWritten = true;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "ttl"))
            {
                SipNumericParseResult ttl = SipNumericParser.ParseNonNegativeInt32Text(parameter.Value, 255);
                string ttlText = ttl.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                AppendKnownParameter(builder, parameter.Name == null ? "ttl" : parameter.Name,
                    ttlText, true, canonical);
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "maddr"))
            {
                string maddr = parameter.Value == null ? "" : parameter.Value;
                if (canonical)
                {
                    maddr = maddr.ToLowerInvariant();
                }

                AppendKnownParameter(builder, parameter.Name == null ? "maddr" : parameter.Name,
                    maddr, true, canonical);
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "alias"))
            {
                AppendKnownParameter(builder, parameter.Name == null ? "alias" : parameter.Name,
                    null, false, canonical);
            }
            else
            {
                AppendExtensionParameter(builder, parameter, canonical);
            }

            index = index + 1;
        }

        if (!branchWritten && via.Branch != null)
        {
            AppendKnownParameter(builder, "branch", via.Branch, true, canonical);
        }

        if (!receivedWritten && via.Received != null)
        {
            string received = canonical ? via.Received.ToLowerInvariant() : via.Received;
            AppendKnownParameter(builder, "received", received, true, canonical);
        }

        if (!rportWritten && via.HasRPort)
        {
            string? port = via.RPort < 0 ? null
                : via.RPort.ToString(System.Globalization.CultureInfo.InvariantCulture);
            AppendKnownParameter(builder, "rport", port, via.RPort >= 0, canonical);
        }
    }

    static void AppendKnownParameter(System.Text.StringBuilder builder, string name, string? value,
        bool hasValue, bool canonical)
    {
        builder.Append(';');
        builder.Append(canonical ? name.ToLowerInvariant() : name);
        if (hasValue)
        {
            builder.Append('=');
            builder.Append(value);
        }
    }

    static void AppendExtensionParameter(System.Text.StringBuilder builder, SipUriParameter parameter, bool canonical)
    {
        SipUriParameter output = new SipUriParameter();
        output.Name = canonical && parameter.Name != null ? parameter.Name.ToLowerInvariant() : parameter.Name;
        output.Value = parameter.Value;
        output.HasValue = parameter.HasValue;
        builder.Append(SipHeaderPrimitives.FormatParameterValue(output));
    }

    static int ReadTokenEnd(string text, int start)
    {
        int index = start;
        while (index < text.Length && SipAscii.IsTokenCharacter(text[index]))
        {
            index = index + 1;
        }

        return index;
    }

    static int SkipWhitespace(string text, int start)
    {
        int index = start;
        while (index < text.Length && (text[index] == ' ' || text[index] == '\t'))
        {
            index = index + 1;
        }

        return index;
    }

    static string TrimWhitespace(string text)
    {
        int start = SkipWhitespace(text, 0);
        int end = text.Length;
        while (end > start && (text[end - 1] == ' ' || text[end - 1] == '\t'))
        {
            end = end - 1;
        }

        return text.Substring(start, end - start);
    }

    static int FindParameterEnd(string text, int start)
    {
        bool quoted = false;
        bool escaped = false;
        int index = start;
        while (index < text.Length)
        {
            int character = text[index];
            if (escaped)
            {
                escaped = false;
            }
            else if (quoted && character == '\\')
            {
                escaped = true;
            }
            else if (character == '"')
            {
                quoted = !quoted;
            }
            else if (!quoted && character == ';')
            {
                return index;
            }

            index = index + 1;
        }

        return quoted || escaped ? -1 : text.Length;
    }

    static SipViaParseResult CreateParseError(int offset, string? message)
    {
        SipViaParseResult result = new SipViaParseResult();
        result.ErrorOffset = offset;
        result.ErrorMessage = message;
        return result;
    }

    static SipWriteResult CreateWriteError(string message)
    {
        SipWriteResult result = new SipWriteResult();
        result.ErrorMessage = message;
        return result;
    }
}
