namespace sip;

internal static class SipCoreHeaderCodec
{
    internal static bool IsCoreHeaderName(string? name)
    {
        return IsContentLengthName(name) || IsContentTypeName(name)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ContentDisposition)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Accept)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Allow)
            || IsSupportedName(name) || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Require)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ProxyRequire)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Unsupported)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Date)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Expires)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.MinExpires)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.RetryAfter)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Warning)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Server)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.UserAgent);
    }

    internal static SipCoreHeaderResult ParseCoreHeaderValue(string? name, string? text, SipParseOptions? options)
    {
        if (name == null || text == null || options == null)
        {
            return CreateError(0, "A core header name, value, and parse options are required.");
        }

        if (IsContentLengthName(name))
        {
            return ParseContentLength(text);
        }

        if (IsContentTypeName(name))
        {
            return ParseContentType(text, options);
        }

        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ContentDisposition))
        {
            return ParseContentDisposition(text, options);
        }

        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Accept))
        {
            return ParseAccept(text, options);
        }

        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Allow))
        {
            return ParseAllow(text, options);
        }

        if (IsSupportedName(name) || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Require)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ProxyRequire)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Unsupported))
        {
            bool allowEmpty = IsSupportedName(name);
            return ParseOptionTags(text, options, allowEmpty);
        }

        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Date))
        {
            return ParseDate(text);
        }

        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Expires))
        {
            return ParseExpires(text, false);
        }

        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.MinExpires))
        {
            return ParseExpires(text, true);
        }

        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.RetryAfter))
        {
            return ParseRetryAfter(text, options);
        }

        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Warning))
        {
            return ParseWarnings(text, options);
        }

        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Server)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.UserAgent))
        {
            return ParseProducts(text);
        }

        return CreateError(0, "The header field is not a supported core header.");
    }

    internal static SipWriteResult FormatCoreHeaderValue(string? name, SipCoreHeaderResult? parsed,
        SipWriteOptions? options)
    {
        if (name == null || parsed == null || options == null)
        {
            return CreateWriteError("A core header value and write options are required.");
        }

        if (!parsed.Success)
        {
            return CreateWriteError(parsed.ErrorMessage == null ? "The core header value is invalid."
                : parsed.ErrorMessage);
        }

        string? value = null;
        if (IsContentLengthName(name) && parsed.ContentLength >= 0)
        {
            value = FormatInteger(parsed.ContentLength);
        }
        else if (IsContentTypeName(name) && parsed.MediaType != null)
        {
            value = FormatMediaType(parsed.MediaType, false);
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ContentDisposition)
            && parsed.ContentDisposition != null)
        {
            value = FormatContentDisposition(parsed.ContentDisposition);
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Accept))
        {
            value = FormatMediaTypes(parsed.MediaTypes);
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Allow))
        {
            value = FormatTokens(parsed.Methods);
        }
        else if (IsSupportedName(name) || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Require)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ProxyRequire)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Unsupported))
        {
            value = FormatTokens(parsed.OptionTags);
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Date) && parsed.HasDate)
        {
            value = SipHeaderPrimitives.FormatDateValue(parsed.Date);
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Expires) && parsed.ExpiresSeconds >= 0)
        {
            value = FormatInteger(parsed.ExpiresSeconds);
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.MinExpires) && parsed.MinExpiresSeconds >= 0)
        {
            value = FormatInteger(parsed.MinExpiresSeconds);
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.RetryAfter) && parsed.RetryAfter != null)
        {
            value = FormatRetryAfter(parsed.RetryAfter);
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Warning))
        {
            value = FormatWarnings(parsed.WarningValues);
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Server)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.UserAgent))
        {
            value = FormatProducts(parsed.ProductValues);
        }

        if (value == null)
        {
            return CreateWriteError("The core header model is incomplete.");
        }

        string headerName = GetFormattedHeaderName(name, options.CompactHeaderNames);
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(headerName + ": " + value + "\r\n");
        if (bytes.Length > options.MaxHeaderLineBytes)
        {
            SipWriteResult limited = CreateWriteError("The formatted header field exceeds the configured output limit.");
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

    static SipCoreHeaderResult ParseContentLength(string text)
    {
        SipNumericParseResult number = SipNumericParser.ParseNonNegativeInt32Text(text, int.MaxValue);
        if (!number.Success)
        {
            return CreateError(number.ErrorOffset, "The Content-Length value is invalid or too large.");
        }

        SipCoreHeaderResult result = CreateSuccess();
        result.ContentLength = number.Value;
        return result;
    }

    static SipCoreHeaderResult ParseContentType(string text, SipParseOptions options)
    {
        SipHeaderPrimitiveResult media = ParseMediaTypeValue(text, options, false);
        if (!media.Success || media.MediaType == null)
        {
            return CreateError(media.ErrorOffset, media.ErrorMessage);
        }

        SipCoreHeaderResult result = CreateSuccess();
        result.MediaType = media.MediaType;
        return result;
    }

    static SipCoreHeaderResult ParseAccept(string text, SipParseOptions options)
    {
        SipCoreHeaderResult result = CreateSuccess();
        if (text.Length == 0)
        {
            return result;
        }

        SipHeaderPrimitiveResult split = SipHeaderPrimitives.SplitCommaListText(text, options.MaxListValueCount);
        if (!split.Success)
        {
            return CreateError(split.ErrorOffset, split.ErrorMessage);
        }

        SipMediaType[] values = new SipMediaType[split.TextValues.Length];
        int index = 0;
        int offset = 0;
        while (index < split.TextValues.Length)
        {
            string item = split.TextValues[index];
            int itemOffset = FindItemOffset(text, item, offset);
            SipHeaderPrimitiveResult parsed = ParseMediaTypeValue(item, options, true);
            if (!parsed.Success || parsed.MediaType == null)
            {
                return CreateError(itemOffset + parsed.ErrorOffset, parsed.ErrorMessage);
            }

            values[index] = parsed.MediaType;
            offset = itemOffset + item.Length;
            index = index + 1;
        }

        result.MediaTypes = values;
        return result;
    }

    static SipHeaderPrimitiveResult ParseMediaTypeValue(string text, SipParseOptions options, bool allowWildcard)
    {
        int semicolon = text.IndexOf(';');
        int baseEnd = semicolon < 0 ? text.Length : semicolon;
        string media = TrimWhitespace(text.Substring(0, baseEnd));
        int slash = media.IndexOf('/');
        if (slash <= 0 || slash + 1 >= media.Length || media.IndexOf('/', slash + 1) >= 0)
        {
            return CreatePrimitiveError(0, "A media type requires one type/subtype pair.");
        }

        string type = TrimWhitespace(media.Substring(0, slash));
        string subtype = TrimWhitespace(media.Substring(slash + 1));
        if (!IsMediaToken(type, allowWildcard) || !IsMediaToken(subtype, allowWildcard)
            || type == "*" && subtype != "*")
        {
            return CreatePrimitiveError(0, "The media type or subtype is invalid.");
        }

        SipMediaType value = new SipMediaType();
        value.Type = type;
        value.Subtype = subtype;
        if (semicolon >= 0)
        {
            SipHeaderPrimitiveResult parameters = SipHeaderPrimitives.ParseParameterSequenceText(
                text, semicolon, options.MaxParameterCount);
            if (!parameters.Success)
            {
                return parameters;
            }

            value.Parameters = parameters.Parameters;
            if (HasDuplicateParameter(value.Parameters))
            {
                return CreatePrimitiveError(semicolon, "A media type parameter cannot be repeated.");
            }
        }

        bool quality = false;
        int index = 0;
        while (index < value.Parameters.Length)
        {
            SipUriParameter parameter = value.Parameters[index];
            if (!parameter.HasValue)
            {
                return CreatePrimitiveError(semicolon, "A media type parameter requires a value.");
            }

            if (allowWildcard && SipAscii.EqualsIgnoreCase(parameter.Name, "q"))
            {
                if (quality || IsQuotedParameter(parameter))
                {
                    return CreatePrimitiveError(semicolon, "The Accept q parameter is repeated or quoted.");
                }

                SipHeaderPrimitiveResult parsedQuality = SipHeaderPrimitives.ParseQualityText(parameter.Value);
                if (!parsedQuality.Success)
                {
                    return CreatePrimitiveError(semicolon, "The Accept q parameter is invalid.");
                }

                quality = true;
                value.QualityThousandths = parsedQuality.Number;
            }

            if (!options.PreserveOriginal)
            {
                parameter.OriginalText = null;
            }

            index = index + 1;
        }

        if (options.PreserveOriginal)
        {
            value.OriginalText = text;
        }

        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
        result.Success = true;
        result.MediaType = value;
        return result;
    }

    static SipCoreHeaderResult ParseContentDisposition(string text, SipParseOptions options)
    {
        int semicolon = text.IndexOf(';');
        string token = TrimWhitespace(semicolon < 0 ? text : text.Substring(0, semicolon));
        if (!SipAscii.IsToken(token))
        {
            return CreateError(0, "The Content-Disposition type must be a token.");
        }

        SipContentDisposition disposition = new SipContentDisposition();
        disposition.DispositionToken = token;
        disposition.Disposition = GetDisposition(token);
        if (semicolon >= 0)
        {
            SipHeaderPrimitiveResult parameters = SipHeaderPrimitives.ParseParameterSequenceText(
                text, semicolon, options.MaxParameterCount);
            if (!parameters.Success)
            {
                return CreateError(parameters.ErrorOffset, parameters.ErrorMessage);
            }

            disposition.Parameters = parameters.Parameters;
            if (HasDuplicateParameter(disposition.Parameters))
            {
                return CreateError(semicolon, "A Content-Disposition parameter cannot be repeated.");
            }
        }

        bool handling = false;
        int index = 0;
        while (index < disposition.Parameters.Length)
        {
            SipUriParameter parameter = disposition.Parameters[index];
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "handling"))
            {
                if (handling || !parameter.HasValue || IsQuotedParameter(parameter)
                    || !SipAscii.IsToken(parameter.Value))
                {
                    return CreateError(semicolon, "The Content-Disposition handling parameter is invalid.");
                }

                handling = true;
            }

            if (!options.PreserveOriginal)
            {
                parameter.OriginalText = null;
            }

            index = index + 1;
        }

        SipCoreHeaderResult result = CreateSuccess();
        result.ContentDisposition = disposition;
        return result;
    }

    static SipCoreHeaderResult ParseAllow(string text, SipParseOptions options)
    {
        SipHeaderPrimitiveResult tokens = ParseTokenList(text, options.MaxListValueCount, true);
        if (!tokens.Success)
        {
            return CreateError(tokens.ErrorOffset, tokens.ErrorMessage);
        }

        SipCoreHeaderResult result = CreateSuccess();
        result.Methods = tokens.TextValues;
        return result;
    }

    static SipCoreHeaderResult ParseOptionTags(string text, SipParseOptions options, bool allowEmpty)
    {
        SipHeaderPrimitiveResult tokens = ParseTokenList(text, options.MaxListValueCount, allowEmpty);
        if (!tokens.Success)
        {
            return CreateError(tokens.ErrorOffset, tokens.ErrorMessage);
        }

        SipCoreHeaderResult result = CreateSuccess();
        result.OptionTags = tokens.TextValues;
        return result;
    }

    static SipHeaderPrimitiveResult ParseTokenList(string text, int maximumCount, bool allowEmpty)
    {
        if (text.Length == 0 && allowEmpty)
        {
            SipHeaderPrimitiveResult empty = new SipHeaderPrimitiveResult();
            empty.Success = true;
            return empty;
        }

        SipHeaderPrimitiveResult split = SipHeaderPrimitives.SplitCommaListText(text, maximumCount);
        if (!split.Success)
        {
            return split;
        }

        int index = 0;
        while (index < split.TextValues.Length)
        {
            if (!SipAscii.IsToken(split.TextValues[index]))
            {
                return CreatePrimitiveError(FindItemOffset(text, split.TextValues[index], 0),
                    "Each list value must be a token.");
            }

            index = index + 1;
        }

        return split;
    }

    static SipCoreHeaderResult ParseDate(string text)
    {
        SipHeaderPrimitiveResult date = SipHeaderPrimitives.ParseDateText(text);
        if (!date.Success)
        {
            return CreateError(date.ErrorOffset, date.ErrorMessage);
        }

        SipCoreHeaderResult result = CreateSuccess();
        result.Date = date.Date;
        result.HasDate = true;
        return result;
    }

    static SipCoreHeaderResult ParseExpires(string text, bool minimum)
    {
        SipHeaderPrimitiveResult delta = SipHeaderPrimitives.ParseDeltaSecondsText(text);
        if (!delta.Success)
        {
            return CreateError(delta.ErrorOffset, delta.ErrorMessage);
        }

        SipCoreHeaderResult result = CreateSuccess();
        if (minimum)
        {
            result.MinExpiresSeconds = delta.Number;
        }
        else
        {
            result.ExpiresSeconds = delta.Number;
        }

        return result;
    }

    static SipCoreHeaderResult ParseRetryAfter(string text, SipParseOptions options)
    {
        int numberEnd = 0;
        while (numberEnd < text.Length && text[numberEnd] >= '0' && text[numberEnd] <= '9')
        {
            numberEnd = numberEnd + 1;
        }

        SipHeaderPrimitiveResult delta = SipHeaderPrimitives.ParseDeltaSecondsText(text.Substring(0, numberEnd));
        if (!delta.Success)
        {
            return CreateError(delta.ErrorOffset, "The Retry-After delay is invalid or too large.");
        }

        SipRetryAfterValue value = new SipRetryAfterValue();
        value.Seconds = delta.Number;
        int position = SkipWhitespace(text, numberEnd);
        if (position < text.Length && text[position] == '(')
        {
            SipHeaderPrimitiveResult comment = ParseCommentPrefix(text, position);
            if (!comment.Success)
            {
                return CreateError(comment.ErrorOffset, comment.ErrorMessage);
            }

            value.Comment = comment.Text;
            position = SkipWhitespace(text, comment.Number);
        }

        if (position < text.Length)
        {
            SipHeaderPrimitiveResult parameters = SipHeaderPrimitives.ParseParameterSequenceText(
                text, position, options.MaxParameterCount);
            if (!parameters.Success)
            {
                return CreateError(parameters.ErrorOffset, parameters.ErrorMessage);
            }

            value.Parameters = parameters.Parameters;
            if (HasDuplicateParameter(value.Parameters))
            {
                return CreateError(position, "A Retry-After parameter cannot be repeated.");
            }
        }

        bool duration = false;
        int index = 0;
        while (index < value.Parameters.Length)
        {
            SipUriParameter parameter = value.Parameters[index];
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "duration"))
            {
                if (duration || !parameter.HasValue || IsQuotedParameter(parameter))
                {
                    return CreateError(position, "The Retry-After duration parameter is invalid.");
                }

                SipHeaderPrimitiveResult parsedDuration = SipHeaderPrimitives.ParseDeltaSecondsText(parameter.Value);
                if (!parsedDuration.Success)
                {
                    return CreateError(position, "The Retry-After duration parameter is invalid.");
                }

                duration = true;
                value.DurationSeconds = parsedDuration.Number;
            }

            if (!options.PreserveOriginal)
            {
                parameter.OriginalText = null;
            }

            index = index + 1;
        }

        SipCoreHeaderResult result = CreateSuccess();
        result.RetryAfter = value;
        return result;
    }

    static SipHeaderPrimitiveResult ParseCommentPrefix(string text, int start)
    {
        int depth = 0;
        bool escaped = false;
        int index = start;
        while (index < text.Length)
        {
            int character = text[index];
            if (escaped)
            {
                escaped = false;
            }
            else if (character == '\\')
            {
                escaped = true;
            }
            else if (character == '(')
            {
                depth = depth + 1;
            }
            else if (character == ')')
            {
                depth = depth - 1;
                if (depth == 0)
                {
                    string source = text.Substring(start, index - start + 1);
                    SipHeaderPrimitiveResult parsed = SipHeaderPrimitives.ParseCommentText(source);
                    if (parsed.Success)
                    {
                        parsed.Number = index + 1;
                    }

                    return parsed;
                }
            }

            index = index + 1;
        }

        return CreatePrimitiveError(start, "The comment is missing a closing parenthesis.");
    }

    static SipCoreHeaderResult ParseWarnings(string text, SipParseOptions options)
    {
        SipHeaderPrimitiveResult split = SipHeaderPrimitives.SplitCommaListText(text, options.MaxListValueCount);
        if (!split.Success)
        {
            return CreateError(split.ErrorOffset, split.ErrorMessage);
        }

        SipWarningValue[] warnings = new SipWarningValue[split.TextValues.Length];
        int index = 0;
        int offset = 0;
        while (index < split.TextValues.Length)
        {
            string item = split.TextValues[index];
            int itemOffset = FindItemOffset(text, item, offset);
            SipHeaderPrimitiveResult warning = ParseWarningValue(item);
            if (!warning.Success || warning.WarningValue == null)
            {
                return CreateError(itemOffset + warning.ErrorOffset, warning.ErrorMessage);
            }

            warnings[index] = warning.WarningValue;
            offset = itemOffset + item.Length;
            index = index + 1;
        }

        SipCoreHeaderResult result = CreateSuccess();
        result.WarningValues = warnings;
        return result;
    }

    static SipHeaderPrimitiveResult ParseWarningValue(string text)
    {
        if (text.Length < 7 || !IsDigit(text[0]) || !IsDigit(text[1]) || !IsDigit(text[2])
            || !IsWhitespace(text[3]))
        {
            return CreatePrimitiveError(0, "A Warning value requires a three-digit code, agent, and quoted text.");
        }

        int agentStart = SkipWhitespace(text, 3);
        int agentEnd = agentStart;
        while (agentEnd < text.Length && !IsWhitespace(text[agentEnd]))
        {
            agentEnd = agentEnd + 1;
        }

        string agent = text.Substring(agentStart, agentEnd - agentStart);
        SipHeaderPrimitiveResult host = SipHeaderPrimitives.ParseHostPortText(agent);
        if (agent.Length == 0 || !host.Success && !SipAscii.IsToken(agent))
        {
            return CreatePrimitiveError(agentStart, "The Warning agent must be a host-port or token.");
        }

        if (agentEnd == text.Length || !IsWhitespace(text[agentEnd]))
        {
            return CreatePrimitiveError(agentEnd, "The Warning text is missing.");
        }

        int textStart = SkipWhitespace(text, agentEnd);
        SipHeaderPrimitiveResult quoted = SipHeaderPrimitives.ParseQuotedString(text.Substring(textStart));
        if (!quoted.Success)
        {
            return CreatePrimitiveError(textStart + quoted.ErrorOffset, quoted.ErrorMessage);
        }

        SipWarningValue value = new SipWarningValue();
        value.Code = (text[0] - '0') * 100 + (text[1] - '0') * 10 + text[2] - '0';
        value.Agent = agent;
        value.Text = quoted.Text;
        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
        result.Success = true;
        result.WarningValue = value;
        return result;
    }

    static SipCoreHeaderResult ParseProducts(string text)
    {
        if (text.Length == 0)
        {
            return CreateError(0, "A Server or User-Agent value is required.");
        }

        System.Collections.Generic.List<SipProductValue> values = new System.Collections.Generic.List<SipProductValue>();
        int position = 0;
        while (position < text.Length)
        {
            position = SkipWhitespace(text, position);
            if (position == text.Length)
            {
                break;
            }

            if (text[position] == '(')
            {
                SipHeaderPrimitiveResult comment = ParseCommentPrefix(text, position);
                if (!comment.Success)
                {
                    return CreateError(comment.ErrorOffset, comment.ErrorMessage);
                }

                SipProductValue value = new SipProductValue();
                value.IsComment = true;
                value.Comment = comment.Text;
                values.Add(value);
                position = comment.Number;
                if (position < text.Length && !IsWhitespace(text[position]))
                {
                    return CreateError(position, "Server and User-Agent values must be separated by whitespace.");
                }
            }
            else
            {
                int end = position;
                while (end < text.Length && !IsWhitespace(text[end]))
                {
                    end = end + 1;
                }

                string productText = text.Substring(position, end - position);
                int slash = productText.IndexOf('/');
                string product = slash < 0 ? productText : productText.Substring(0, slash);
                string? version = slash < 0 ? null : productText.Substring(slash + 1);
                if (!SipAscii.IsToken(product) || version != null && !SipAscii.IsToken(version)
                    || slash >= 0 && productText.IndexOf('/', slash + 1) >= 0)
                {
                    return CreateError(position, "A product must contain a token and optional token version.");
                }

                SipProductValue value = new SipProductValue();
                value.Name = product;
                value.Version = version;
                values.Add(value);
                position = end;
            }
        }

        if (values.Count == 0)
        {
            return CreateError(0, "A Server or User-Agent value is required.");
        }

        SipCoreHeaderResult result = CreateSuccess();
        result.ProductValues = values.ToArray();
        return result;
    }

    static string FormatMediaTypes(SipMediaType[] values)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(FormatMediaType(values[index], true));
            index = index + 1;
        }

        return builder.ToString();
    }

    static string FormatMediaType(SipMediaType value, bool accept)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append(value.Type == null ? "" : value.Type.ToLowerInvariant());
        builder.Append('/');
        builder.Append(value.Subtype == null ? "" : value.Subtype.ToLowerInvariant());
        int index = 0;
        bool quality = false;
        while (index < value.Parameters.Length)
        {
            SipUriParameter source = value.Parameters[index];
            SipUriParameter parameter = CopyParameter(source);
            if (accept && SipAscii.EqualsIgnoreCase(parameter.Name, "q") && value.QualityThousandths >= 0)
            {
                parameter.Name = "q";
                parameter.Value = SipHeaderPrimitives.FormatQualityValue(value.QualityThousandths);
                parameter.HasValue = true;
                quality = true;
            }

            builder.Append(SipHeaderPrimitives.FormatParameterValue(parameter));
            index = index + 1;
        }

        if (accept && !quality && value.QualityThousandths >= 0)
        {
            builder.Append(";q=");
            builder.Append(SipHeaderPrimitives.FormatQualityValue(value.QualityThousandths));
        }

        return builder.ToString();
    }

    static string FormatContentDisposition(SipContentDisposition value)
    {
        string token = GetDispositionToken(value);
        System.Text.StringBuilder builder = new System.Text.StringBuilder(token);
        int index = 0;
        while (index < value.Parameters.Length)
        {
            SipUriParameter parameter = CopyParameter(value.Parameters[index]);
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "handling"))
            {
                parameter.Name = "handling";
            }

            builder.Append(SipHeaderPrimitives.FormatParameterValue(parameter));
            index = index + 1;
        }

        return builder.ToString();
    }

    static string FormatTokens(string[] values)
    {
        if (values.Length == 0)
        {
            return "";
        }

        return SipHeaderPrimitives.JoinCommaListValues(values);
    }

    static string FormatRetryAfter(SipRetryAfterValue value)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder(FormatInteger(value.Seconds));
        if (value.Comment != null)
        {
            builder.Append(' ');
            builder.Append(SipHeaderPrimitives.FormatCommentText(value.Comment));
        }

        bool duration = false;
        int index = 0;
        while (index < value.Parameters.Length)
        {
            SipUriParameter parameter = CopyParameter(value.Parameters[index]);
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "duration") && value.DurationSeconds >= 0)
            {
                parameter.Name = "duration";
                parameter.Value = FormatInteger(value.DurationSeconds);
                parameter.HasValue = true;
                duration = true;
            }

            builder.Append(SipHeaderPrimitives.FormatParameterValue(parameter));
            index = index + 1;
        }

        if (!duration && value.DurationSeconds >= 0)
        {
            builder.Append(";duration=");
            builder.Append(FormatInteger(value.DurationSeconds));
        }

        return builder.ToString();
    }

    static string FormatWarnings(SipWarningValue[] values)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(values[index].Code.ToString("D3", System.Globalization.CultureInfo.InvariantCulture));
            builder.Append(' ');
            builder.Append(values[index].Agent);
            builder.Append(' ');
            builder.Append(SipHeaderPrimitives.FormatQuotedString(values[index].Text));
            index = index + 1;
        }

        return builder.ToString();
    }

    static string FormatProducts(SipProductValue[] values)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                builder.Append(' ');
            }

            SipProductValue value = values[index];
            if (value.IsComment)
            {
                builder.Append(SipHeaderPrimitives.FormatCommentText(value.Comment));
            }
            else
            {
                builder.Append(value.Name);
                if (value.Version != null)
                {
                    builder.Append('/');
                    builder.Append(value.Version);
                }
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    static SipBodyDisposition GetDisposition(string token)
    {
        if (SipAscii.EqualsIgnoreCase(token, "session")) return SipBodyDisposition.Session;
        if (SipAscii.EqualsIgnoreCase(token, "render")) return SipBodyDisposition.Render;
        if (SipAscii.EqualsIgnoreCase(token, "icon")) return SipBodyDisposition.Icon;
        if (SipAscii.EqualsIgnoreCase(token, "alert")) return SipBodyDisposition.Alert;
        if (SipAscii.EqualsIgnoreCase(token, "signal")) return SipBodyDisposition.Signal;
        if (SipAscii.EqualsIgnoreCase(token, "by-reference")) return SipBodyDisposition.ByReference;
        if (SipAscii.EqualsIgnoreCase(token, "recording-session")) return SipBodyDisposition.RecordingSession;
        return SipBodyDisposition.Unknown;
    }

    static string GetDispositionToken(SipContentDisposition value)
    {
        switch (value.Disposition)
        {
            case SipBodyDisposition.Session: return "session";
            case SipBodyDisposition.Render: return "render";
            case SipBodyDisposition.Icon: return "icon";
            case SipBodyDisposition.Alert: return "alert";
            case SipBodyDisposition.Signal: return "signal";
            case SipBodyDisposition.ByReference: return "by-reference";
            case SipBodyDisposition.RecordingSession: return "recording-session";
            default: return value.DispositionToken == null ? "" : value.DispositionToken;
        }
    }

    static SipUriParameter CopyParameter(SipUriParameter source)
    {
        SipUriParameter result = new SipUriParameter();
        result.Name = source.Name == null ? null : source.Name.ToLowerInvariant();
        result.Value = source.Value;
        result.HasValue = source.HasValue;
        return result;
    }

    static bool HasDuplicateParameter(SipUriParameter[] parameters)
    {
        int left = 0;
        while (left < parameters.Length)
        {
            int right = left + 1;
            while (right < parameters.Length)
            {
                if (SipAscii.EqualsIgnoreCase(parameters[left].Name, parameters[right].Name)) return true;
                right = right + 1;
            }
            left = left + 1;
        }
        return false;
    }

    static bool IsQuotedParameter(SipUriParameter parameter)
    {
        if (parameter.Quoted)
        {
            return true;
        }

        if (parameter.OriginalText == null)
        {
            return false;
        }

        int equals = parameter.OriginalText.IndexOf('=');
        int position = equals < 0 ? parameter.OriginalText.Length : SkipWhitespace(parameter.OriginalText, equals + 1);
        return position < parameter.OriginalText.Length && parameter.OriginalText[position] == '"';
    }

    static bool IsMediaToken(string value, bool allowWildcard)
    {
        return SipAscii.IsToken(value) && (allowWildcard || value != "*");
    }

    static bool IsContentLengthName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ContentLength)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactContentLength);
    }

    static bool IsContentTypeName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ContentType)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactContentType);
    }

    static bool IsSupportedName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Supported)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactSupported);
    }

    static string GetFormattedHeaderName(string name, bool compact)
    {
        if (IsContentLengthName(name)) return compact ? SipHeaderNames.CompactContentLength : SipHeaderNames.ContentLength;
        if (IsContentTypeName(name)) return compact ? SipHeaderNames.CompactContentType : SipHeaderNames.ContentType;
        if (IsSupportedName(name)) return compact ? SipHeaderNames.CompactSupported : SipHeaderNames.Supported;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ContentDisposition)) return SipHeaderNames.ContentDisposition;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Accept)) return SipHeaderNames.Accept;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Allow)) return SipHeaderNames.Allow;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Require)) return SipHeaderNames.Require;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ProxyRequire)) return SipHeaderNames.ProxyRequire;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Unsupported)) return SipHeaderNames.Unsupported;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Date)) return SipHeaderNames.Date;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Expires)) return SipHeaderNames.Expires;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.MinExpires)) return SipHeaderNames.MinExpires;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.RetryAfter)) return SipHeaderNames.RetryAfter;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Warning)) return SipHeaderNames.Warning;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Server)) return SipHeaderNames.Server;
        return SipHeaderNames.UserAgent;
    }

    static string FormatInteger(int value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    static int FindItemOffset(string text, string item, int start)
    {
        int offset = text.IndexOf(item, start, System.StringComparison.Ordinal);
        return offset < 0 ? start : offset;
    }

    static int SkipWhitespace(string text, int start)
    {
        int index = start;
        while (index < text.Length && IsWhitespace(text[index]))
        {
            index = index + 1;
        }

        return index;
    }

    static string TrimWhitespace(string text)
    {
        int start = SkipWhitespace(text, 0);
        int end = text.Length;
        while (end > start && IsWhitespace(text[end - 1]))
        {
            end = end - 1;
        }

        return text.Substring(start, end - start);
    }

    static bool IsWhitespace(int character)
    {
        return character == ' ' || character == '\t';
    }

    static bool IsDigit(int character)
    {
        return character >= '0' && character <= '9';
    }

    static SipCoreHeaderResult CreateSuccess()
    {
        SipCoreHeaderResult result = new SipCoreHeaderResult();
        result.Success = true;
        return result;
    }

    static SipCoreHeaderResult CreateError(int offset, string? message)
    {
        SipCoreHeaderResult result = new SipCoreHeaderResult();
        result.ErrorOffset = offset;
        result.ErrorMessage = message;
        return result;
    }

    static SipHeaderPrimitiveResult CreatePrimitiveError(int offset, string? message)
    {
        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
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
