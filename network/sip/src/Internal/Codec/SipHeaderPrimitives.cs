namespace sip;

internal static class SipHeaderPrimitives
{
    internal static bool IsTokenText(string? text)
    {
        return SipAscii.IsToken(text);
    }

    internal static SipHeaderPrimitiveResult ParseQuotedString(string? text)
    {
        if (text == null)
        {
            return CreateError(0, "A quoted string must begin with a quotation mark.");
        }

        int start = 0;
        while (start < text.Length && (text[start] == ' ' || text[start] == '\t'))
        {
            start = start + 1;
        }

        int end = text.Length;
        while (end > start && (text[end - 1] == ' ' || text[end - 1] == '\t'))
        {
            end = end - 1;
        }

        if (end - start < 2 || text[start] != '"')
        {
            return CreateError(start, "A quoted string must begin with a quotation mark.");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = start + 1;
        while (index < end)
        {
            int character = text[index];
            if (character == '"')
            {
                if (index + 1 != end)
                {
                    return CreateError(index + 1, "Unexpected text follows the quoted string.");
                }

                return CreateTextSuccess(builder.ToString());
            }

            if (character == '\\')
            {
                if (index + 1 >= end)
                {
                    return CreateError(index, "The quoted string ends with an incomplete escape.");
                }

                int escaped = text[index + 1];
                if (!IsQuotedPairCharacter(escaped))
                {
                    return CreateError(index + 1, "The quoted string escape is invalid.");
                }

                builder.Append((char)escaped);
                index = index + 2;
            }
            else
            {
                if (!IsQuotedTextCharacter(character))
                {
                    return CreateError(index, "The quoted string contains an invalid character.");
                }

                builder.Append((char)character);
                index = index + 1;
            }
        }

        return CreateError(end, "The quoted string is missing its closing quotation mark.");
    }

    internal static string FormatQuotedString(string? text)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append('"');
        int index = 0;
        while (index < text.Length)
        {
            int character = text[index];
            if (IsQuotedTextCharacter(character))
            {
                builder.Append((char)character);
            }
            else if (IsQuotedPairCharacter(character))
            {
                builder.Append('\\');
                builder.Append((char)character);
            }
            else
            {
                throw new System.ArgumentException("The quoted string contains an invalid character.", "text");
            }

            index = index + 1;
        }

        builder.Append('"');
        return builder.ToString();
    }

    internal static SipHeaderPrimitiveResult ParseCommentText(string? text)
    {
        if (text == null)
        {
            return CreateError(0, "A comment must begin with an opening parenthesis.");
        }

        int start = 0;
        while (start < text.Length && (text[start] == ' ' || text[start] == '\t'))
        {
            start = start + 1;
        }

        int end = text.Length;
        while (end > start && (text[end - 1] == ' ' || text[end - 1] == '\t'))
        {
            end = end - 1;
        }

        if (end - start < 2 || text[start] != '(')
        {
            return CreateError(start, "A comment must begin with an opening parenthesis.");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int depth = 1;
        int index = start + 1;
        while (index < end)
        {
            int character = text[index];
            if (character == '\\')
            {
                if (index + 1 >= end)
                {
                    return CreateError(index, "The comment ends with an incomplete escape.");
                }

                int escaped = text[index + 1];
                if (!IsQuotedPairCharacter(escaped))
                {
                    return CreateError(index + 1, "The comment escape is invalid.");
                }

                builder.Append((char)escaped);
                index = index + 2;
            }
            else if (character == '(')
            {
                depth = depth + 1;
                builder.Append('(');
                index = index + 1;
            }
            else if (character == ')')
            {
                depth = depth - 1;
                if (depth == 0)
                {
                    if (index + 1 != end)
                    {
                        return CreateError(index + 1, "Unexpected text follows the comment.");
                    }

                    return CreateTextSuccess(builder.ToString());
                }

                builder.Append(')');
                index = index + 1;
            }
            else
            {
                if (!IsCommentTextCharacter(character))
                {
                    return CreateError(index, "The comment contains an invalid character.");
                }

                builder.Append((char)character);
                index = index + 1;
            }
        }

        return CreateError(end, "The comment is missing a closing parenthesis.");
    }

    internal static string FormatCommentText(string? text)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append('(');
        int index = 0;
        while (index < text.Length)
        {
            int character = text[index];
            if (IsCommentTextCharacter(character))
            {
                builder.Append((char)character);
            }
            else if (IsQuotedPairCharacter(character))
            {
                builder.Append('\\');
                builder.Append((char)character);
            }
            else
            {
                throw new System.ArgumentException("The comment contains an invalid character.", "text");
            }

            index = index + 1;
        }

        builder.Append(')');
        return builder.ToString();
    }

    internal static SipHeaderPrimitiveResult ParseHostPortText(string? text)
    {
        if (text == null || text.Length == 0)
        {
            return CreateError(0, "A host is required.");
        }

        string host;
        int port = -1;
        if (text[0] == '[')
        {
            int close = text.IndexOf(']');
            if (close < 0)
            {
                return CreateError(0, "The IPv6 host is missing its closing bracket.");
            }

            host = text.Substring(1, close - 1);
            if (!SipUriSyntax.IsIpv6Address(host))
            {
                return CreateError(1, "The IPv6 host is invalid.");
            }

            if (close + 1 < text.Length)
            {
                if (text[close + 1] != ':')
                {
                    return CreateError(close + 1, "Unexpected text follows the host.");
                }

                SipNumericParseResult portResult = SipNumericParser.ParseNonNegativeInt32Text(
                    text.Substring(close + 2), 65535);
                if (!portResult.Success)
                {
                    return CreateError(close + 2 + portResult.ErrorOffset, "The port is invalid.");
                }

                port = portResult.Value;
            }
        }
        else
        {
            int colon = text.IndexOf(':');
            if (colon >= 0)
            {
                if (text.IndexOf(':', colon + 1) >= 0)
                {
                    return CreateError(colon, "An IPv6 host must be enclosed in brackets.");
                }

                host = text.Substring(0, colon);
                SipNumericParseResult portResult = SipNumericParser.ParseNonNegativeInt32Text(
                    text.Substring(colon + 1), 65535);
                if (!portResult.Success)
                {
                    return CreateError(colon + 1 + portResult.ErrorOffset, "The port is invalid.");
                }

                port = portResult.Value;
            }
            else
            {
                host = text;
            }

            if (!SipUriSyntax.IsHostname(host))
            {
                return CreateError(0, "The host is invalid.");
            }
        }

        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
        result.Success = true;
        result.Host = host;
        result.Port = port;
        return result;
    }

    internal static string FormatHostPortText(string? host, int port)
    {
        if (host == null || host.Length == 0)
        {
            throw new System.ArgumentNullException("host");
        }

        if (port < -1 || port > 65535)
        {
            throw new System.ArgumentOutOfRangeException("port");
        }

        bool ipv6 = host.IndexOf(':') >= 0;
        if (ipv6 && !SipUriSyntax.IsIpv6Address(host) || !ipv6 && !SipUriSyntax.IsHostname(host))
        {
            throw new System.ArgumentException("The host is invalid.", "host");
        }

        string result = ipv6 ? "[" + host + "]" : host;
        if (port >= 0)
        {
            result = result + ":" + port;
        }

        return result;
    }

    internal static SipHeaderPrimitiveResult ParseParameterText(string? text)
    {
        if (text == null)
        {
            return CreateError(0, "A parameter is required.");
        }

        string trimmed = TrimLinearWhitespace(text);
        if (trimmed.Length > 0 && trimmed[0] == ';')
        {
            trimmed = TrimLinearWhitespace(trimmed.Substring(1));
        }

        int equals = FindUnquotedCharacter(trimmed, '=');
        string name = equals < 0 ? trimmed : TrimLinearWhitespace(trimmed.Substring(0, equals));
        if (!SipAscii.IsToken(name))
        {
            return CreateError(0, "The parameter name is not a token.");
        }

        SipUriParameter parameter = new SipUriParameter();
        parameter.Name = name;
        parameter.HasValue = equals >= 0;
        if (equals >= 0)
        {
            string valueText = TrimLinearWhitespace(trimmed.Substring(equals + 1));
            if (valueText.Length == 0)
            {
                return CreateError(equals + 1, "The parameter value is empty.");
            }

            if (valueText[0] == '"')
            {
                SipHeaderPrimitiveResult quoted = ParseQuotedString(valueText);
                if (!quoted.Success)
                {
                    return CreateError(equals + 1 + quoted.ErrorOffset, quoted.ErrorMessage);
                }

                parameter.Value = quoted.Text;
                parameter.Quoted = true;
            }
            else
            {
                SipHeaderPrimitiveResult host = ParseHostPortText(valueText);
                if (!SipAscii.IsToken(valueText) && (!host.Success || host.Port >= 0))
                {
                    return CreateError(equals + 1, "The parameter value is not a token, host, or quoted string.");
                }

                parameter.Value = valueText;
            }
        }

        parameter.OriginalText = trimmed;
        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
        result.Success = true;
        result.Parameter = parameter;
        return result;
    }

    internal static string FormatParameterValue(SipUriParameter? parameter)
    {
        if (parameter == null || !SipAscii.IsToken(parameter.Name))
        {
            throw new System.ArgumentException("A parameter with a token name is required.", "parameter");
        }

        string text = ";" + parameter.Name;
        if (parameter.HasValue)
        {
            if (parameter.Value == null || parameter.Value.Length == 0)
            {
                throw new System.ArgumentException("A valued parameter requires a value.", "parameter");
            }

            SipHeaderPrimitiveResult host = ParseHostPortText(parameter.Value);
            string value = SipAscii.IsToken(parameter.Value) || host.Success && host.Port < 0
                ? parameter.Value : FormatQuotedString(parameter.Value);
            text = text + "=" + value;
        }

        return text;
    }

    internal static SipHeaderPrimitiveResult SplitCommaListText(string? text, int maximumCount)
    {
        if (maximumCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumCount");
        }

        if (text == null || text.Length == 0)
        {
            return CreateError(0, "A comma-separated value is required.");
        }

        System.Collections.Generic.List<string> values = new System.Collections.Generic.List<string>();
        int start = 0;
        int index = 0;
        int commentDepth = 0;
        bool quoted = false;
        bool escaped = false;
        bool angled = false;
        while (index < text.Length)
        {
            int character = text[index];
            if (escaped)
            {
                escaped = false;
            }
            else if (character == '\\' && (quoted || commentDepth > 0))
            {
                escaped = true;
            }
            else if (character == '"' && commentDepth == 0)
            {
                quoted = !quoted;
            }
            else if (!quoted && character == '(')
            {
                commentDepth = commentDepth + 1;
            }
            else if (!quoted && character == ')')
            {
                if (commentDepth == 0)
                {
                    return CreateError(index, "The value contains an unmatched closing parenthesis.");
                }

                commentDepth = commentDepth - 1;
            }
            else if (!quoted && commentDepth == 0 && character == '<')
            {
                if (angled)
                {
                    return CreateError(index, "The value contains nested angle brackets.");
                }

                angled = true;
            }
            else if (!quoted && commentDepth == 0 && character == '>')
            {
                if (!angled)
                {
                    return CreateError(index, "The value contains an unmatched closing angle bracket.");
                }

                angled = false;
            }
            else if (!quoted && commentDepth == 0 && !angled && character == ',')
            {
                SipHeaderPrimitiveResult added = AddCommaValue(text, start, index, values, maximumCount);
                if (!added.Success)
                {
                    return added;
                }

                start = index + 1;
            }

            index = index + 1;
        }

        if (escaped || quoted || commentDepth != 0 || angled)
        {
            return CreateError(text.Length, "The comma-separated value contains an unterminated construct.");
        }

        SipHeaderPrimitiveResult finalValue = AddCommaValue(text, start, text.Length, values, maximumCount);
        if (!finalValue.Success)
        {
            return finalValue;
        }

        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
        result.Success = true;
        result.TextValues = values.ToArray();
        return result;
    }

    internal static string JoinCommaListValues(string[]? values)
    {
        if (values == null || values.Length == 0)
        {
            throw new System.ArgumentException("At least one comma-separated value is required.", "values");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null || TrimLinearWhitespace(values[index]).Length == 0)
            {
                throw new System.ArgumentException("Comma-separated values cannot be empty.", "values");
            }

            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(TrimLinearWhitespace(values[index]));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static SipHeaderPrimitiveResult ParseQualityText(string? text)
    {
        if (text == null || text.Length == 0)
        {
            return CreateError(0, "A quality value is required.");
        }

        int whole = text[0] - '0';
        if ((whole != 0 && whole != 1) || text.Length > 1 && text[1] != '.')
        {
            return CreateError(0, "The quality value must be between zero and one.");
        }

        int fraction = 0;
        int digits = 0;
        int index = text.Length == 1 ? 1 : 2;
        while (index < text.Length)
        {
            int digit = text[index] - '0';
            if (digit < 0 || digit > 9 || digits == 3 || whole == 1 && digit != 0)
            {
                return CreateError(index, "The quality value has an invalid fractional part.");
            }

            fraction = fraction * 10 + digit;
            digits = digits + 1;
            index = index + 1;
        }

        while (digits < 3)
        {
            fraction = fraction * 10;
            digits = digits + 1;
        }

        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
        result.Success = true;
        result.Number = whole * 1000 + fraction;
        return result;
    }

    internal static string FormatQualityValue(int thousandths)
    {
        if (thousandths < 0 || thousandths > 1000)
        {
            throw new System.ArgumentOutOfRangeException("thousandths");
        }

        if (thousandths == 1000)
        {
            return "1";
        }

        if (thousandths == 0)
        {
            return "0";
        }

        string digits = thousandths.ToString("D3", System.Globalization.CultureInfo.InvariantCulture);
        int end = digits.Length;
        while (end > 1 && digits[end - 1] == '0')
        {
            end = end - 1;
        }

        return "0." + digits.Substring(0, end);
    }

    internal static SipHeaderPrimitiveResult ParseDateText(string? text)
    {
        if (text == null || text.Length != 29 || text[3] != ',' || text[4] != ' ' || text[7] != ' '
            || text[11] != ' ' || text[16] != ' ' || text[19] != ':' || text[22] != ':'
            || text[25] != ' ' || text.Substring(26) != "GMT")
        {
            return CreateError(0, "The SIP date must use the RFC 1123 GMT form.");
        }

        int day = ParseFixedDigits(text, 5, 2);
        int month = GetMonthNumber(text.Substring(8, 3));
        int year = ParseFixedDigits(text, 12, 4);
        int hour = ParseFixedDigits(text, 17, 2);
        int minute = ParseFixedDigits(text, 20, 2);
        int second = ParseFixedDigits(text, 23, 2);
        if (day < 1 || month < 1 || year < 1 || hour < 0 || hour > 23 || minute < 0 || minute > 59
            || second < 0 || second > 59 || day > GetDaysInMonth(year, month))
        {
            return CreateError(5, "The SIP date contains an invalid calendar or time value.");
        }

        System.DateTime value = new System.DateTime(year, month, day, hour, minute, second, System.DateTimeKind.Utc);
        if (GetWeekdayName(value.DayOfWeek) != text.Substring(0, 3))
        {
            return CreateError(0, "The SIP date weekday does not match its calendar date.");
        }

        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
        result.Success = true;
        result.Date = value;
        return result;
    }

    internal static string FormatDateValue(System.DateTime value)
    {
        System.DateTime utc = value.Kind == System.DateTimeKind.Local ? value.ToUniversalTime() : value;
        string day = utc.Day.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
        string year = utc.Year.ToString("D4", System.Globalization.CultureInfo.InvariantCulture);
        string hour = utc.Hour.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
        string minute = utc.Minute.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
        string second = utc.Second.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
        return GetWeekdayName(utc.DayOfWeek) + ", " + day + " " + GetMonthName(utc.Month) + " " + year
            + " " + hour + ":" + minute + ":" + second + " GMT";
    }

    internal static SipHeaderPrimitiveResult ParseDeltaSecondsText(string? text)
    {
        SipNumericParseResult parsed = SipNumericParser.ParseNonNegativeInt32Text(text, int.MaxValue);
        if (!parsed.Success)
        {
            return CreateError(parsed.ErrorOffset, "The delta-seconds value is invalid or too large.");
        }

        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
        result.Success = true;
        result.Number = parsed.Value;
        return result;
    }

    internal static string FormatDeltaSecondsValue(int value)
    {
        if (value < 0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static SipHeaderPrimitiveResult ParseNameAddressText(string? text, SipParseOptions? options)
    {
        if (text == null || options == null)
        {
            return CreateError(0, "A name-address value and parse options are required.");
        }

        string trimmed = TrimLinearWhitespace(text);
        if (trimmed.Length == 0)
        {
            return CreateError(0, "A name-address value is required.");
        }

        SipNameAddress address = new SipNameAddress();
        int open = FindNameAddressOpenBracket(trimmed);
        string uriText;
        int parameterStart;
        if (open >= 0)
        {
            int close = trimmed.IndexOf('>', open + 1);
            if (close < 0)
            {
                return CreateError(open, "The name-address is missing its closing angle bracket.");
            }

            string display = TrimLinearWhitespace(trimmed.Substring(0, open));
            if (display.Length > 0)
            {
                if (!options.AllowCompatibleWhitespace && display[0] != '"' && open > 0
                    && trimmed[open - 1] != ' ' && trimmed[open - 1] != '\t')
                {
                    return CreateError(open, "An unquoted display name must be followed by linear whitespace.");
                }

                SipHeaderPrimitiveResult displayResult = ParseDisplayName(display);
                if (!displayResult.Success)
                {
                    return displayResult;
                }

                address.DisplayName = displayResult.Text;
            }

            uriText = trimmed.Substring(open + 1, close - open - 1);
            parameterStart = close + 1;
        }
        else
        {
            int semicolon = trimmed.IndexOf(';');
            if (semicolon < 0)
            {
                uriText = trimmed;
                parameterStart = trimmed.Length;
            }
            else
            {
                uriText = TrimLinearWhitespace(trimmed.Substring(0, semicolon));
                parameterStart = semicolon;
            }
        }

        if (uriText.Length == 0 || !IsAsciiText(uriText))
        {
            return CreateError(open < 0 ? 0 : open + 1, "The name-address URI is invalid.");
        }

        SipParseResult uriResult = SipUriParser.ParseUriBytes(System.Text.Encoding.ASCII.GetBytes(uriText), options);
        if (uriResult.Status != SipParseStatus.Complete || uriResult.Uri == null)
        {
            int offset = uriResult.ErrorOffset < 0 ? 0 : uriResult.ErrorOffset;
            return CreateError((open < 0 ? 0 : open + 1) + offset, "The name-address URI is invalid.");
        }
        if (open < 0 && uriResult.Uri.Headers.Length > 0 && !options.AllowCompatibleWhitespace)
        {
            return CreateError(0, "A URI containing headers must use the name-address form.");
        }

        address.Uri = uriResult.Uri;
        if (parameterStart < trimmed.Length)
        {
            SipHeaderPrimitiveResult parameters = ParseParameterSequenceText(
                trimmed, parameterStart, options.MaxParameterCount);
            if (!parameters.Success)
            {
                return parameters;
            }

            address.Parameters = parameters.Parameters;
        }

        if (options.PreserveOriginal)
        {
            address.OriginalText = trimmed;
        }

        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
        result.Success = true;
        result.NameAddress = address;
        return result;
    }

    internal static string FormatNameAddressValue(SipNameAddress? address, SipWriteOptions? options)
    {
        if (address == null || options == null || address.Uri == null || address.Parameters == null)
        {
            throw new System.ArgumentException("A complete name-address and write options are required.", "address");
        }

        if (options.PreserveOriginal && address.OriginalText != null)
        {
            return address.OriginalText;
        }

        SipWriteOptions uriOptions = new SipWriteOptions();
        uriOptions.Canonical = options.Canonical;
        uriOptions.PreserveOriginal = options.PreserveOriginal;
        uriOptions.MaxHeaderLineBytes = int.MaxValue;
        SipWriteResult uriResult = SipUriFormatter.FormatUriModel(address.Uri, uriOptions);
        if (!uriResult.Success)
        {
            throw new System.ArgumentException("The name-address URI is invalid.", "address");
        }

        string uriText = System.Text.Encoding.ASCII.GetString(uriResult.Bytes);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        if (address.DisplayName != null)
        {
            builder.Append(FormatDisplayName(address.DisplayName));
            builder.Append(' ');
        }

        builder.Append('<');
        builder.Append(uriText);
        builder.Append('>');
        int index = 0;
        while (index < address.Parameters.Length)
        {
            builder.Append(FormatParameterValue(address.Parameters[index]));
            index = index + 1;
        }

        return builder.ToString();
    }

    static SipHeaderPrimitiveResult ParseDisplayName(string text)
    {
        if (text[0] == '"')
        {
            return ParseQuotedString(text);
        }

        int index = 0;
        bool needToken = true;
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        while (index < text.Length)
        {
            int start = index;
            while (index < text.Length && text[index] != ' ' && text[index] != '\t')
            {
                index = index + 1;
            }

            if (index == start || !SipAscii.IsToken(text.Substring(start, index - start)))
            {
                return CreateError(start, "The display name must be quoted or contain only tokens.");
            }

            if (!needToken)
            {
                builder.Append(' ');
            }

            builder.Append(text.Substring(start, index - start));
            needToken = false;
            while (index < text.Length && (text[index] == ' ' || text[index] == '\t'))
            {
                index = index + 1;
            }
        }

        return CreateTextSuccess(builder.ToString());
    }

    static string FormatDisplayName(string text)
    {
        if (SipAscii.IsToken(text))
        {
            return text;
        }

        return FormatQuotedString(text);
    }

    internal static SipHeaderPrimitiveResult ParseParameterSequenceText(string text, int start, int maximumCount)
    {
        System.Collections.Generic.List<SipUriParameter> values = new System.Collections.Generic.List<SipUriParameter>();
        int position = start;
        while (position < text.Length)
        {
            while (position < text.Length && (text[position] == ' ' || text[position] == '\t'))
            {
                position = position + 1;
            }

            if (position == text.Length || text[position] != ';')
            {
                return CreateError(position, "A parameter must begin with a semicolon.");
            }

            int end = FindParameterEnd(text, position + 1);
            if (end < 0)
            {
                return CreateError(text.Length, "A parameter contains an unterminated quoted string.");
            }

            if (values.Count >= maximumCount)
            {
                return CreateError(position, "The value exceeds the configured parameter count.");
            }

            string parameterText = text.Substring(position + 1, end - position - 1);
            SipHeaderPrimitiveResult parsed = ParseParameterText(parameterText);
            if (!parsed.Success || parsed.Parameter == null)
            {
                return CreateError(position + 1 + parsed.ErrorOffset, parsed.ErrorMessage);
            }

            values.Add(parsed.Parameter);
            position = end;
        }

        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
        result.Success = true;
        result.Parameters = values.ToArray();
        return result;
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

    static int FindNameAddressOpenBracket(string text)
    {
        bool quoted = false;
        bool escaped = false;
        int index = 0;
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
            else if (!quoted && character == '<')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static int FindUnquotedCharacter(string text, int target)
    {
        bool quoted = false;
        bool escaped = false;
        int index = 0;
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
            else if (!quoted && character == target)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static SipHeaderPrimitiveResult AddCommaValue(string text, int start, int end,
        System.Collections.Generic.List<string> values, int maximumCount)
    {
        string value = TrimLinearWhitespace(text.Substring(start, end - start));
        if (value.Length == 0)
        {
            return CreateError(start, "A comma-separated list cannot contain an empty value.");
        }

        if (values.Count >= maximumCount)
        {
            return CreateError(start, "The comma-separated list exceeds the configured item count.");
        }

        values.Add(value);
        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
        result.Success = true;
        return result;
    }

    static string TrimLinearWhitespace(string text)
    {
        int start = 0;
        while (start < text.Length && (text[start] == ' ' || text[start] == '\t'))
        {
            start = start + 1;
        }

        int end = text.Length;
        while (end > start && (text[end - 1] == ' ' || text[end - 1] == '\t'))
        {
            end = end - 1;
        }

        return text.Substring(start, end - start);
    }

    static bool IsAsciiText(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] > 127)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsQuotedTextCharacter(int character)
    {
        return character == ' ' || character == '\t' || character == '!' || character >= 35 && character <= 91
            || character >= 93 && character <= 126 || character >= 128;
    }

    static bool IsCommentTextCharacter(int character)
    {
        return character == ' ' || character == '\t' || character >= 33 && character <= 39
            || character >= 42 && character <= 91 || character >= 93 && character <= 126 || character >= 128;
    }

    static bool IsQuotedPairCharacter(int character)
    {
        return character >= 0 && character <= 9 || character >= 11 && character <= 12
            || character >= 14 && character <= 127;
    }

    static int ParseFixedDigits(string text, int start, int count)
    {
        int value = 0;
        int index = 0;
        while (index < count)
        {
            int digit = text[start + index] - '0';
            if (digit < 0 || digit > 9)
            {
                return -1;
            }

            value = value * 10 + digit;
            index = index + 1;
        }

        return value;
    }

    static int GetMonthNumber(string text)
    {
        switch (text)
        {
            case "Jan": return 1;
            case "Feb": return 2;
            case "Mar": return 3;
            case "Apr": return 4;
            case "May": return 5;
            case "Jun": return 6;
            case "Jul": return 7;
            case "Aug": return 8;
            case "Sep": return 9;
            case "Oct": return 10;
            case "Nov": return 11;
            case "Dec": return 12;
            default: return -1;
        }
    }

    static string GetMonthName(int month)
    {
        switch (month)
        {
            case 1: return "Jan";
            case 2: return "Feb";
            case 3: return "Mar";
            case 4: return "Apr";
            case 5: return "May";
            case 6: return "Jun";
            case 7: return "Jul";
            case 8: return "Aug";
            case 9: return "Sep";
            case 10: return "Oct";
            case 11: return "Nov";
            case 12: return "Dec";
            default: return "";
        }
    }

    static string GetWeekdayName(System.DayOfWeek weekday)
    {
        switch (weekday)
        {
            case System.DayOfWeek.Monday: return "Mon";
            case System.DayOfWeek.Tuesday: return "Tue";
            case System.DayOfWeek.Wednesday: return "Wed";
            case System.DayOfWeek.Thursday: return "Thu";
            case System.DayOfWeek.Friday: return "Fri";
            case System.DayOfWeek.Saturday: return "Sat";
            default: return "Sun";
        }
    }

    static int GetDaysInMonth(int year, int month)
    {
        if (month == 2)
        {
            bool leap = year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
            return leap ? 29 : 28;
        }

        if (month == 4 || month == 6 || month == 9 || month == 11)
        {
            return 30;
        }

        return 31;
    }

    static SipHeaderPrimitiveResult CreateTextSuccess(string text)
    {
        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
        result.Success = true;
        result.Text = text;
        return result;
    }

    static SipHeaderPrimitiveResult CreateError(int offset, string? message)
    {
        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
        result.ErrorOffset = offset;
        result.ErrorMessage = message;
        return result;
    }
}
