namespace sip;

internal static class SipExtensionHeaderCodec
{
    internal static bool IsExtensionHeaderName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.RSeq)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.RAck)
            || IsSessionExpiresName(name) || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.MinSE)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Reason)
            || IsEventName(name) || IsAllowEventsName(name)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.SubscriptionState)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.SipETag)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.SipIfMatch)
            || IsReferToName(name) || IsReferredByName(name)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ReferSub)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Replaces)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Join)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.TargetDialog)
            || IsAcceptContactName(name) || IsRejectContactName(name)
            || IsRequestDispositionName(name)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.FeatureCaps)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Privacy)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.PAssertedIdentity)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.PPreferredIdentity)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.HistoryInfo)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.SessionId)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Geolocation)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ResourcePriority);
    }

    internal static SipExtensionHeaderResult ParseExtensionHeaderValue(string? name, string? text,
        SipParseOptions? options)
    {
        if (name == null || text == null || options == null)
        {
            return CreateError(0, "An extension header name, value, and parse options are required.");
        }

        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.RSeq)) return ParseRSeq(text);
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.RAck)) return ParseRAck(text);
        if (IsSessionExpiresName(name) || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.MinSE))
            return ParseNumericParameters(text, options);
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Reason)) return ParseReason(text, options);
        if (IsEventName(name)) return ParseEvent(text, options);
        if (IsAllowEventsName(name) || IsRequestDispositionName(name))
            return ParseTokenList(text, options);
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ResourcePriority))
            return ParseResourcePriority(text, options);
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.SubscriptionState))
            return ParseSubscriptionState(text, options);
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.SipETag)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.SipIfMatch)) return ParseBareSingleToken(text, options);
        if (IsReferToName(name) || IsReferredByName(name)) return ParseAddressList(text, options, 1, false);
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ReferSub)) return ParseBoolean(text, options);
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Replaces)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Join)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.TargetDialog))
            return ParseDialogReference(name, text, options);
        if (IsAcceptContactName(name) || IsRejectContactName(name)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.FeatureCaps))
            return ParseTokenParameterList(text, options);
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Privacy)) return ParsePrivacy(text, options);
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.PAssertedIdentity))
            return ParseAddressList(text, options, options.MaxListValueCount, false);
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.PPreferredIdentity))
            return ParseAddressList(text, options, 2, false);
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.HistoryInfo)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Geolocation))
            return ParseAddressList(text, options, options.MaxListValueCount, true);
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.SessionId)) return ParseSingleToken(text, options);
        return CreateError(0, "The header field is not a supported extension header.");
    }

    internal static SipWriteResult FormatExtensionHeaderValue(string? name, SipExtensionHeaderResult? parsed,
        SipWriteOptions? options)
    {
        if (name == null || parsed == null || options == null || !parsed.Success)
        {
            return CreateWriteError("A valid extension header value and write options are required.");
        }

        string? value = null;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.RSeq)) value = FormatInteger(parsed.Values[0].Number);
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.RAck)) value = FormatRAck(parsed.Values[0]);
        else if (IsSessionExpiresName(name) || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.MinSE))
            value = FormatNumericParameters(parsed.Values[0]);
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Privacy)) value = FormatPrivacy(parsed.Values);
        else if (IsReferToName(name) || IsReferredByName(name)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.PAssertedIdentity)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.PPreferredIdentity)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.HistoryInfo)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Geolocation)) value = FormatAddresses(parsed.Values);
        else value = FormatTokenValues(parsed.Values);

        if (value == null)
        {
            return CreateWriteError("The extension header model is incomplete.");
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

    static SipExtensionHeaderResult ParseRSeq(string text)
    {
        SipNumericParseResult number = SipNumericParser.ParseNonNegativeInt32Text(text, int.MaxValue);
        if (!number.Success || number.Value == 0) return CreateError(number.ErrorOffset,
            "RSeq must be an integer from one through 2147483647.");
        SipExtensionHeaderValue value = new SipExtensionHeaderValue();
        value.Number = number.Value;
        return CreateValueSuccess(value);
    }

    static SipExtensionHeaderResult ParseRAck(string text)
    {
        string[] parts = SplitWhitespace(text);
        if (parts.Length != 3) return CreateError(0, "RAck requires RSeq, CSeq, and method values.");
        SipNumericParseResult response = SipNumericParser.ParseNonNegativeInt32Text(parts[0], int.MaxValue);
        SipNumericParseResult sequence = SipNumericParser.ParseNonNegativeInt32Text(parts[1], int.MaxValue);
        if (!response.Success || response.Value == 0 || !sequence.Success || !SipAscii.IsToken(parts[2]))
            return CreateError(0, "The RAck sequence numbers or method are invalid.");
        SipExtensionHeaderValue value = new SipExtensionHeaderValue();
        value.Number = response.Value;
        value.SecondNumber = sequence.Value;
        value.MethodToken = parts[2];
        value.Method = GetMethod(parts[2]);
        return CreateValueSuccess(value);
    }

    static SipExtensionHeaderResult ParseNumericParameters(string text, SipParseOptions options)
    {
        int semicolon = text.IndexOf(';');
        string numberText = TrimWhitespace(semicolon < 0 ? text : text.Substring(0, semicolon));
        SipNumericParseResult number = SipNumericParser.ParseNonNegativeInt32Text(numberText, int.MaxValue);
        if (!number.Success) return CreateError(0,
            "The extension interval must be an integer from zero through 2147483647.");
        SipExtensionHeaderValue value = new SipExtensionHeaderValue();
        value.Number = number.Value;
        SipExtensionHeaderResult parameters = ApplyParameters(text, semicolon, value, options);
        if (!parameters.Success) return parameters;
        SipUriParameter? refresher = FindParameter(value.Parameters, "refresher");
        if (refresher != null)
        {
            if (!refresher.HasValue || refresher.Value == null
                || !SipAscii.EqualsIgnoreCase(refresher.Value, "uac")
                    && !SipAscii.EqualsIgnoreCase(refresher.Value, "uas"))
                return CreateError(semicolon, "The refresher parameter must be uac or uas.");
            value.SecondaryToken = refresher.Value;
        }
        return parameters;
    }

    static SipExtensionHeaderResult ParseReason(string text, SipParseOptions options)
    {
        SipExtensionHeaderResult result = ParseTokenParameterList(text, options);
        if (!result.Success) return result;
        int index = 0;
        while (index < result.Values.Length)
        {
            SipExtensionHeaderValue value = result.Values[index];
            SipUriParameter? cause = FindParameter(value.Parameters, "cause");
            SipUriParameter? reasonText = FindParameter(value.Parameters, "text");
            if (cause != null)
            {
                SipNumericParseResult parsed = SipNumericParser.ParseNonNegativeInt32Text(cause.Value, int.MaxValue);
                if (!cause.HasValue || !parsed.Success) return CreateError(0, "A Reason cause must be an integer.");
                value.Number = parsed.Value;
            }
            if (reasonText != null)
            {
                if (!reasonText.HasValue || !IsQuotedParameter(reasonText))
                    return CreateError(0, "A Reason text parameter must be quoted.");
                value.SecondaryToken = reasonText.Value;
            }
            index = index + 1;
        }
        return result;
    }

    static SipExtensionHeaderResult ParseEvent(string text, SipParseOptions options)
    {
        SipExtensionHeaderResult result = ParseTokenParameterListWithLimit(text, options, 1);
        if (!result.Success) return result;
        SipUriParameter? id = FindParameter(result.Values[0].Parameters, "id");
        if (id != null)
        {
            if (!id.HasValue || !SipAscii.IsToken(id.Value)) return CreateError(0, "The Event id must be a token.");
            result.Values[0].SecondaryToken = id.Value;
        }
        return result;
    }

    static SipExtensionHeaderResult ParseSubscriptionState(string text, SipParseOptions options)
    {
        SipExtensionHeaderResult result = ParseTokenParameterListWithLimit(text, options, 1);
        if (!result.Success) return result;
        SipExtensionHeaderValue value = result.Values[0];
        SipUriParameter? reason = FindParameter(value.Parameters, "reason");
        SipUriParameter? expires = FindParameter(value.Parameters, "expires");
        SipUriParameter? retry = FindParameter(value.Parameters, "retry-after");
        if (reason != null)
        {
            if (!reason.HasValue || !SipAscii.IsToken(reason.Value)) return CreateError(0,
                "The Subscription-State reason must be a token.");
            value.SecondaryToken = reason.Value;
        }
        if (expires != null)
        {
            SipNumericParseResult parsed = SipNumericParser.ParseNonNegativeInt32Text(expires.Value, int.MaxValue);
            if (!expires.HasValue || !parsed.Success) return CreateError(0,
                "The Subscription-State expires value is invalid.");
            value.Number = parsed.Value;
        }
        if (retry != null)
        {
            SipNumericParseResult parsed = SipNumericParser.ParseNonNegativeInt32Text(retry.Value, int.MaxValue);
            if (!retry.HasValue || !parsed.Success) return CreateError(0,
                "The Subscription-State retry-after value is invalid.");
            value.SecondNumber = parsed.Value;
        }
        return result;
    }

    static SipExtensionHeaderResult ParseSingleToken(string text, SipParseOptions options)
    {
        return ParseTokenParameterListWithLimit(text, options, 1);
    }

    static SipExtensionHeaderResult ParseBareSingleToken(string text, SipParseOptions options)
    {
        SipExtensionHeaderResult result = ParseTokenParameterListWithLimit(text, options, 1);
        if (result.Success && result.Values[0].Parameters.Length > 0)
        {
            return CreateError(0, "This extension header requires one token without parameters.");
        }

        return result;
    }

    static SipExtensionHeaderResult ParseBoolean(string text, SipParseOptions options)
    {
        SipExtensionHeaderResult result = ParseTokenParameterListWithLimit(text, options, 1);
        if (!result.Success) return result;
        if (!SipAscii.EqualsIgnoreCase(result.Values[0].Token, "true")
            && !SipAscii.EqualsIgnoreCase(result.Values[0].Token, "false"))
            return CreateError(0, "The Refer-Sub value must be true or false.");
        result.Values[0].HasFlag = true;
        result.Values[0].Flag = SipAscii.EqualsIgnoreCase(result.Values[0].Token, "true");
        return result;
    }

    static SipExtensionHeaderResult ParseDialogReference(string name, string text, SipParseOptions options)
    {
        int semicolon = text.IndexOf(';');
        string callId = TrimWhitespace(semicolon < 0 ? text : text.Substring(0, semicolon));
        if (!IsCallId(callId)) return CreateError(0, "A dialog reference requires a valid Call-ID.");
        SipExtensionHeaderValue value = new SipExtensionHeaderValue();
        value.Token = callId;
        SipExtensionHeaderResult result = ApplyParameters(text, semicolon, value, options);
        if (!result.Success) return result;
        SipUriParameter? first = FindParameter(value.Parameters, "to-tag");
        if (first == null) first = FindParameter(value.Parameters, "local-tag");
        SipUriParameter? second = FindParameter(value.Parameters, "from-tag");
        if (second == null) second = FindParameter(value.Parameters, "remote-tag");
        SipUriParameter? early = FindParameter(value.Parameters, "early-only");
        if (first != null)
        {
            if (!first.HasValue || !SipAscii.IsToken(first.Value)) return CreateError(0, "A dialog tag is invalid.");
            value.SecondaryToken = first.Value;
        }
        if (second != null)
        {
            if (!second.HasValue || !SipAscii.IsToken(second.Value)) return CreateError(0, "A dialog tag is invalid.");
            value.TertiaryToken = second.Value;
        }
        if (early != null)
        {
            if (early.HasValue) return CreateError(0, "The early-only parameter must be valueless.");
            value.HasFlag = true;
            value.Flag = true;
        }
        if (first == null || second == null)
            return CreateError(0, "A dialog reference requires both local and remote dialog tags.");
        if (!SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Replaces) && early != null)
            return CreateError(0, "The early-only parameter is valid only for Replaces.");
        return result;
    }

    static SipExtensionHeaderResult ParseResourcePriority(string text, SipParseOptions options)
    {
        SipExtensionHeaderResult result = ParseTokenList(text, options);
        if (!result.Success) return result;
        int index = 0;
        while (index < result.Values.Length)
        {
            string? token = result.Values[index].Token;
            if (token == null) return CreateError(0, "A Resource-Priority value is missing.");
            int dot = token.IndexOf('.');
            if (dot <= 0 || dot == token.Length - 1 || token.IndexOf('.', dot + 1) >= 0)
                return CreateError(0, "A Resource-Priority value requires namespace.priority form.");
            index = index + 1;
        }
        return result;
    }

    static SipExtensionHeaderResult ParseTokenList(string text, SipParseOptions options)
    {
        SipHeaderPrimitiveResult split = SipHeaderPrimitives.SplitCommaListText(text, options.MaxListValueCount);
        if (!split.Success) return CreateError(split.ErrorOffset, split.ErrorMessage);
        SipExtensionHeaderValue[] values = new SipExtensionHeaderValue[split.TextValues.Length];
        int index = 0;
        while (index < values.Length)
        {
            string token = split.TextValues[index];
            if (!SipAscii.IsToken(token)) return CreateError(0, "Every extension list value must be a token.");
            if (token.IndexOf('.') >= 0 && (token[0] == '.' || token[token.Length - 1] == '.'))
                return CreateError(0, "A resource-priority value is invalid.");
            SipExtensionHeaderValue value = new SipExtensionHeaderValue();
            value.Token = token;
            SetOriginal(value, token, options.PreserveOriginal);
            values[index] = value;
            index = index + 1;
        }
        return CreateValuesSuccess(values);
    }

    static SipExtensionHeaderResult ParseTokenParameterList(string text, SipParseOptions options)
    {
        return ParseTokenParameterListWithLimit(text, options, options.MaxListValueCount);
    }

    static SipExtensionHeaderResult ParseTokenParameterListWithLimit(string text, SipParseOptions options, int limit)
    {
        SipHeaderPrimitiveResult split = SipHeaderPrimitives.SplitCommaListText(text, limit);
        if (!split.Success) return CreateError(split.ErrorOffset, split.ErrorMessage);
        SipExtensionHeaderValue[] values = new SipExtensionHeaderValue[split.TextValues.Length];
        int index = 0;
        while (index < values.Length)
        {
            string item = split.TextValues[index];
            int semicolon = item.IndexOf(';');
            string token = TrimWhitespace(semicolon < 0 ? item : item.Substring(0, semicolon));
            if (!SipAscii.IsToken(token)) return CreateError(0, "The extension value must begin with a token.");
            SipExtensionHeaderValue value = new SipExtensionHeaderValue();
            value.Token = token;
            SipExtensionHeaderResult applied = ApplyParameters(item, semicolon, value, options);
            if (!applied.Success) return applied;
            SetOriginal(value, item, options.PreserveOriginal);
            values[index] = value;
            index = index + 1;
        }
        return CreateValuesSuccess(values);
    }

    static SipExtensionHeaderResult ParsePrivacy(string text, SipParseOptions options)
    {
        string[] tokens = text.Split(';');
        if (tokens.Length > options.MaxListValueCount) return CreateError(0,
            "The Privacy value exceeds the configured item count.");
        SipExtensionHeaderValue[] values = new SipExtensionHeaderValue[tokens.Length];
        int index = 0;
        while (index < tokens.Length)
        {
            string token = TrimWhitespace(tokens[index]);
            if (!SipAscii.IsToken(token)) return CreateError(0, "Every Privacy value must be a token.");
            SipExtensionHeaderValue value = new SipExtensionHeaderValue();
            value.Token = token;
            SetOriginal(value, token, options.PreserveOriginal);
            values[index] = value;
            index = index + 1;
        }
        return CreateValuesSuccess(values);
    }

    static SipExtensionHeaderResult ParseAddressList(string text, SipParseOptions options, int limit,
        bool requireAngles)
    {
        SipHeaderPrimitiveResult split = SipHeaderPrimitives.SplitCommaListText(text, limit);
        if (!split.Success) return CreateError(split.ErrorOffset, split.ErrorMessage);
        SipExtensionHeaderValue[] values = new SipExtensionHeaderValue[split.TextValues.Length];
        int index = 0;
        while (index < values.Length)
        {
            string item = split.TextValues[index];
            if (requireAngles && item.IndexOf('<') < 0) return CreateError(0,
                "This extension address requires angle brackets.");
            SipHeaderPrimitiveResult address = SipHeaderPrimitives.ParseNameAddressText(item, options);
            if (!address.Success || address.NameAddress == null) return CreateError(address.ErrorOffset,
                address.ErrorMessage);
            ClearParameterOriginals(address.NameAddress.Parameters, options.PreserveOriginal);
            SipExtensionHeaderValue value = new SipExtensionHeaderValue();
            value.Address = address.NameAddress;
            value.Uri = address.NameAddress.Uri;
            value.Parameters = address.NameAddress.Parameters;
            SetOriginal(value, item, options.PreserveOriginal);
            values[index] = value;
            index = index + 1;
        }
        return CreateValuesSuccess(values);
    }

    static SipExtensionHeaderResult ApplyParameters(string text, int semicolon, SipExtensionHeaderValue value,
        SipParseOptions options)
    {
        if (semicolon >= 0)
        {
            SipHeaderPrimitiveResult parameters = SipHeaderPrimitives.ParseParameterSequenceText(
                text, semicolon, options.MaxParameterCount);
            if (!parameters.Success) return CreateError(parameters.ErrorOffset, parameters.ErrorMessage);
            if (HasDuplicateParameter(parameters.Parameters)) return CreateError(semicolon,
                "An extension parameter cannot be repeated.");
            value.Parameters = parameters.Parameters;
            ClearParameterOriginals(value.Parameters, options.PreserveOriginal);
        }
        SetOriginal(value, text, options.PreserveOriginal);
        return CreateValueSuccess(value);
    }

    static string FormatRAck(SipExtensionHeaderValue value)
    {
        return FormatInteger(value.Number) + " " + FormatInteger(value.SecondNumber) + " " + value.MethodToken;
    }

    static string FormatNumericParameters(SipExtensionHeaderValue value)
    {
        return FormatInteger(value.Number) + FormatParameters(value.Parameters);
    }

    static string FormatTokenValues(SipExtensionHeaderValue[] values)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0) builder.Append(", ");
            builder.Append(values[index].Token);
            builder.Append(FormatParameters(values[index].Parameters));
            index = index + 1;
        }
        return builder.ToString();
    }

    static string FormatPrivacy(SipExtensionHeaderValue[] values)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0) builder.Append(';');
            builder.Append(values[index].Token);
            index = index + 1;
        }
        return builder.ToString();
    }

    static string FormatAddresses(SipExtensionHeaderValue[] values)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0) builder.Append(", ");
            builder.Append(SipHeaderPrimitives.FormatNameAddressValue(values[index].Address, new SipWriteOptions()));
            index = index + 1;
        }
        return builder.ToString();
    }

    static string FormatParameters(SipUriParameter[] parameters)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < parameters.Length)
        {
            SipUriParameter parameter = CopyParameter(parameters[index]);
            if (parameter.Quoted)
            {
                builder.Append(';');
                builder.Append(parameter.Name);
                builder.Append('=');
                builder.Append(SipHeaderPrimitives.FormatQuotedString(parameter.Value));
            }
            else
            {
                builder.Append(SipHeaderPrimitives.FormatParameterValue(parameter));
            }
            index = index + 1;
        }
        return builder.ToString();
    }

    static SipUriParameter CopyParameter(SipUriParameter source)
    {
        SipUriParameter result = new SipUriParameter();
        result.Name = source.Name == null ? null : source.Name.ToLowerInvariant();
        result.Value = source.Value;
        result.HasValue = source.HasValue;
        result.Quoted = source.Quoted;
        return result;
    }

    static SipUriParameter? FindParameter(SipUriParameter[] parameters, string name)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            if (SipAscii.EqualsIgnoreCase(parameters[index].Name, name)) return parameters[index];
            index = index + 1;
        }
        return null;
    }

    static bool HasDuplicateParameter(SipUriParameter[] parameters)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            int other = index + 1;
            while (other < parameters.Length)
            {
                if (SipAscii.EqualsIgnoreCase(parameters[index].Name, parameters[other].Name)) return true;
                other = other + 1;
            }
            index = index + 1;
        }
        return false;
    }

    static void ClearParameterOriginals(SipUriParameter[] parameters, bool preserve)
    {
        if (preserve) return;
        int index = 0;
        while (index < parameters.Length)
        {
            parameters[index].OriginalText = null;
            index = index + 1;
        }
    }

    static bool IsQuotedParameter(SipUriParameter parameter)
    {
        if (parameter.Quoted) return true;
        if (parameter.OriginalText == null) return false;
        int equals = parameter.OriginalText.IndexOf('=');
        int position = equals + 1;
        while (position < parameter.OriginalText.Length
            && (parameter.OriginalText[position] == ' ' || parameter.OriginalText[position] == '\t'))
            position = position + 1;
        return position < parameter.OriginalText.Length && parameter.OriginalText[position] == '"';
    }

    static bool IsCallId(string text)
    {
        if (text.Length == 0) return false;
        int at = text.IndexOf('@');
        if (at == 0 || at == text.Length - 1 || at >= 0 && text.IndexOf('@', at + 1) >= 0) return false;
        int index = 0;
        while (index < text.Length)
        {
            int character = text[index];
            if (character != '@' && !SipAscii.IsTokenCharacter(character) && character != ':' && character != '/'
                && character != '?' && character != '[' && character != ']') return false;
            index = index + 1;
        }
        return true;
    }

    static string[] SplitWhitespace(string text)
    {
        System.Collections.Generic.List<string> values = new System.Collections.Generic.List<string>();
        int position = 0;
        while (position < text.Length)
        {
            while (position < text.Length && (text[position] == ' ' || text[position] == '\t')) position = position + 1;
            if (position == text.Length) break;
            int end = position;
            while (end < text.Length && text[end] != ' ' && text[end] != '\t') end = end + 1;
            values.Add(text.Substring(position, end - position));
            position = end;
        }
        return values.ToArray();
    }

    static SipMethod GetMethod(string token)
    {
        switch (token)
        {
            case SipMethods.Ack: return SipMethod.Ack;
            case SipMethods.Bye: return SipMethod.Bye;
            case SipMethods.Cancel: return SipMethod.Cancel;
            case SipMethods.Info: return SipMethod.Info;
            case SipMethods.Invite: return SipMethod.Invite;
            case SipMethods.Message: return SipMethod.Message;
            case SipMethods.Notify: return SipMethod.Notify;
            case SipMethods.Options: return SipMethod.Options;
            case SipMethods.Prack: return SipMethod.Prack;
            case SipMethods.Publish: return SipMethod.Publish;
            case SipMethods.Refer: return SipMethod.Refer;
            case SipMethods.Register: return SipMethod.Register;
            case SipMethods.Subscribe: return SipMethod.Subscribe;
            case SipMethods.Update: return SipMethod.Update;
            default: return SipMethod.Unknown;
        }
    }

    static void SetOriginal(SipExtensionHeaderValue value, string text, bool preserve)
    {
        if (preserve) value.OriginalText = text;
    }

    static string GetFormattedHeaderName(string name, bool compact)
    {
        if (IsSessionExpiresName(name)) return compact ? SipHeaderNames.CompactSessionExpires : SipHeaderNames.SessionExpires;
        if (IsEventName(name)) return compact ? SipHeaderNames.CompactEvent : SipHeaderNames.Event;
        if (IsAllowEventsName(name)) return compact ? SipHeaderNames.CompactAllowEvents : SipHeaderNames.AllowEvents;
        if (IsReferToName(name)) return compact ? SipHeaderNames.CompactReferTo : SipHeaderNames.ReferTo;
        if (IsReferredByName(name)) return compact ? SipHeaderNames.CompactReferredBy : SipHeaderNames.ReferredBy;
        if (IsAcceptContactName(name)) return compact ? SipHeaderNames.CompactAcceptContact : SipHeaderNames.AcceptContact;
        if (IsRejectContactName(name)) return compact ? SipHeaderNames.CompactRejectContact : SipHeaderNames.RejectContact;
        if (IsRequestDispositionName(name)) return compact ? SipHeaderNames.CompactRequestDisposition : SipHeaderNames.RequestDisposition;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.RSeq)) return SipHeaderNames.RSeq;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.RAck)) return SipHeaderNames.RAck;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.MinSE)) return SipHeaderNames.MinSE;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Reason)) return SipHeaderNames.Reason;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.SubscriptionState)) return SipHeaderNames.SubscriptionState;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.SipETag)) return SipHeaderNames.SipETag;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.SipIfMatch)) return SipHeaderNames.SipIfMatch;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ReferSub)) return SipHeaderNames.ReferSub;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Replaces)) return SipHeaderNames.Replaces;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Join)) return SipHeaderNames.Join;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.TargetDialog)) return SipHeaderNames.TargetDialog;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.FeatureCaps)) return SipHeaderNames.FeatureCaps;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Privacy)) return SipHeaderNames.Privacy;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.PAssertedIdentity)) return SipHeaderNames.PAssertedIdentity;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.PPreferredIdentity)) return SipHeaderNames.PPreferredIdentity;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.HistoryInfo)) return SipHeaderNames.HistoryInfo;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.SessionId)) return SipHeaderNames.SessionId;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Geolocation)) return SipHeaderNames.Geolocation;
        return SipHeaderNames.ResourcePriority;
    }

    static bool IsSessionExpiresName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.SessionExpires)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactSessionExpires);
    }
    static bool IsEventName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Event)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactEvent);
    }
    static bool IsAllowEventsName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.AllowEvents)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactAllowEvents);
    }
    static bool IsReferToName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ReferTo)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactReferTo);
    }
    static bool IsReferredByName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ReferredBy)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactReferredBy);
    }
    static bool IsAcceptContactName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.AcceptContact)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactAcceptContact);
    }
    static bool IsRejectContactName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.RejectContact)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactRejectContact);
    }
    static bool IsRequestDispositionName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.RequestDisposition)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactRequestDisposition);
    }

    static string TrimWhitespace(string text)
    {
        return text.Trim(' ', '\t');
    }
    static string FormatInteger(int value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
    static SipExtensionHeaderResult CreateValueSuccess(SipExtensionHeaderValue value)
    {
        return CreateValuesSuccess(new SipExtensionHeaderValue[] { value });
    }
    static SipExtensionHeaderResult CreateValuesSuccess(SipExtensionHeaderValue[] values)
    {
        SipExtensionHeaderResult result = new SipExtensionHeaderResult();
        result.Success = true;
        result.Values = values;
        return result;
    }
    static SipExtensionHeaderResult CreateError(int offset, string? message)
    {
        SipExtensionHeaderResult result = new SipExtensionHeaderResult();
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
