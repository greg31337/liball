namespace sip;

internal static class SipReferCore
{
    internal static SipReferHeaderParse ParseReferHeaders(SipMessage message)
    {
        SipReferHeaderParse result = new SipReferHeaderParse();
        int referToCount = 0;
        int referSubCount = 0;
        int index = 0;
        while (message.Headers != null && index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, SipHeaderNames.ReferTo))
            {
                referToCount = referToCount + 1;
                SipExtensionHeaderResult parsed =
                    SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                        SipHeaderNames.ReferTo, header.Value,
                        new SipParseOptions());
                if (!parsed.Success || parsed.Values.Length != 1
                    || parsed.Values[0].Address == null)
                {
                    result.ErrorMessage = "The Refer-To value is malformed.";
                    return result;
                }
                result.ReferTo = SipSnapshotCopy.CopyNameAddressModel(
                    parsed.Values[0].Address);
            }
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, SipHeaderNames.ReferSub))
            {
                referSubCount = referSubCount + 1;
                SipExtensionHeaderResult parsed =
                    SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                        SipHeaderNames.ReferSub, header.Value,
                        new SipParseOptions());
                if (!parsed.Success || parsed.Values.Length != 1)
                {
                    result.ErrorMessage = "The Refer-Sub value is malformed.";
                    return result;
                }
                result.ReferSubPresent = true;
                result.ReferSub = parsed.Values[0].Flag;
            }
            index = index + 1;
        }
        if (referToCount != 1)
        {
            result.ErrorMessage =
                "A REFER request requires exactly one Refer-To value.";
            return result;
        }
        if (referSubCount > 1)
        {
            result.ErrorMessage =
                "A REFER request can contain only one Refer-Sub value.";
            return result;
        }
        result.Success = true;
        return result;
    }

    internal static bool ReadReferSubFalse(SipMessage message)
    {
        int index = 0;
        while (message.Headers != null && index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, SipHeaderNames.ReferSub))
            {
                SipExtensionHeaderResult parsed =
                    SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                        SipHeaderNames.ReferSub, header.Value,
                        new SipParseOptions());
                return parsed.Success && parsed.Values.Length == 1
                    && !parsed.Values[0].Flag;
            }
            index = index + 1;
        }
        return false;
    }

    internal static SipSipfragParse ParseSipfrag(SipMessage message)
    {
        SipSipfragParse result = new SipSipfragParse();
        if (message.ContentType == null
            || !SipAscii.EqualsIgnoreCase(message.ContentType.Type, "message")
            || !SipAscii.EqualsIgnoreCase(
                message.ContentType.Subtype, "sipfrag")
            || message.Body == null || message.Body.Length == 0)
        {
            result.ErrorMessage =
                "A refer-event NOTIFY requires a message/sipfrag body.";
            return result;
        }
        string text = System.Text.Encoding.UTF8.GetString(message.Body);
        int lineEnd = text.IndexOf("\r\n",
            System.StringComparison.Ordinal);
        string line = lineEnd < 0 ? text : text.Substring(0, lineEnd);
        if (!line.StartsWith("SIP/2.0 ",
                System.StringComparison.Ordinal) || line.Length < 12)
        {
            result.ErrorMessage =
                "The message/sipfrag body must begin with a SIP status line.";
            return result;
        }
        SipNumericParseResult status = SipNumericParser.ParseNonNegativeInt32Text(
            line.Substring(8, 3), 699);
        if (!status.Success || status.Value < 100
            || line.Length > 11 && line[11] != ' ')
        {
            result.ErrorMessage =
                "The message/sipfrag status line is malformed.";
            return result;
        }
        result.Success = true;
        result.StatusCode = status.Value;
        result.ReasonPhrase = line.Length <= 12
            ? "" : line.Substring(12);
        return result;
    }

    internal static SipExtensionHeaderValue? ParseEventHeader(
        SipMessage message)
    {
        return ParseSingleExtensionHeader(
            message, SipHeaderNames.Event);
    }

    internal static SipExtensionHeaderValue? ParseSubscriptionStateHeader(
        SipMessage message)
    {
        return ParseSingleExtensionHeader(
            message, SipHeaderNames.SubscriptionState);
    }

    internal static string? ReadCallId(SipMessage message)
    {
        int index = 0;
        while (message.Headers != null && index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, SipHeaderNames.CallId))
            {
                return header.Value;
            }
            index = index + 1;
        }
        return null;
    }

    internal static string? ReadTag(SipNameAddress? address)
    {
        int index = 0;
        while (address != null && address.Parameters != null
            && index < address.Parameters.Length)
        {
            SipUriParameter? parameter = address.Parameters[index];
            if (parameter != null && SipAscii.EqualsIgnoreCase(
                    parameter.Name, "tag"))
            {
                return parameter.Value;
            }
            index = index + 1;
        }
        return null;
    }

    internal static bool HasOptionTag(
        SipMessage message, string headerName, string optionTag)
    {
        int index = 0;
        while (message.Headers != null && index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, headerName))
            {
                SipHeaderPrimitiveResult split =
                    SipHeaderPrimitives.SplitCommaListText(
                        header.Value, SipDefaults.MaxListValueCount);
                int valueIndex = 0;
                while (split.Success
                    && valueIndex < split.TextValues.Length)
                {
                    if (SipAscii.EqualsIgnoreCase(
                            split.TextValues[valueIndex].Trim(), optionTag))
                    {
                        return true;
                    }
                    valueIndex = valueIndex + 1;
                }
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool MessageMatchesInboundDialog(
        SipMessage message, SipDialogId dialogId)
    {
        return ReadCallId(message) == dialogId.CallId
            && ReadTag(message.To) == dialogId.LocalTag
            && ReadTag(message.From) == dialogId.RemoteTag;
    }

    internal static bool IsValidSipOrAbsoluteUri(SipUri? uri)
    {
        return uri != null
            && SipUriValidator.ValidateUriModel(uri).Length == 0;
    }

    internal static bool IsValidSipUri(SipUri? uri)
    {
        return uri != null
            && (uri.Scheme == SipUriScheme.Sip
                || uri.Scheme == SipUriScheme.Sips)
            && SipUriValidator.ValidateUriModel(uri).Length == 0;
    }

    internal static bool IsValidMediaType(SipMediaType? mediaType)
    {
        return mediaType != null && SipAscii.IsToken(mediaType.Type)
            && SipAscii.IsToken(mediaType.Subtype)
            && mediaType.Parameters != null;
    }

    internal static SipDialogId CopyDialogId(SipDialogId source)
    {
        SipDialogId copy = new SipDialogId();
        copy.CallId = source.CallId;
        copy.LocalTag = source.LocalTag;
        copy.RemoteTag = source.RemoteTag;
        return copy;
    }

    internal static SipNameAddress? CopyNameAddress(SipNameAddress? source)
    {
        return SipSnapshotCopy.CopyNameAddressModel(source);
    }

    internal static SipMediaType? CopyMediaType(SipMediaType? source)
    {
        if (source == null)
        {
            return null;
        }
        SipOfferAnswer holder = new SipOfferAnswer();
        holder.ContentType = source;
        SipOfferAnswer? copied = SipSnapshotCopy.CopyOfferAnswer(holder);
        return copied == null ? null : copied.ContentType;
    }

    internal static void AppendHeader(
        SipMessage message, string name, string value)
    {
        SipUasResponseCore.AppendResponseHeader(message, name, value);
    }

    internal static void ReplaceBodyHeaders(SipMessage message)
    {
        RemoveHeader(message, SipHeaderNames.ContentLength);
        RemoveHeader(message, SipHeaderNames.ContentType);
        if (message.ContentType != null)
        {
            AppendHeader(message, SipHeaderNames.ContentType,
                FormatMediaType(message.ContentType));
        }
        AppendHeader(message, SipHeaderNames.ContentLength,
            message.Body.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        message.OriginalBytes = new byte[0];
    }

    internal static SipMessage CopyMessage(SipMessage source)
    {
        SipMessage? copy = SipSnapshotCopy.CopyMessageModel(source);
        if (copy == null)
        {
            throw new System.InvalidOperationException(
                "The REFER message could not be copied.");
        }
        return copy;
    }

    internal static string ReasonPhrase(int statusCode)
    {
        switch (statusCode)
        {
            case SipStatusCodes.Trying:
                return SipReasonPhrases.Trying;
            case SipStatusCodes.Ok:
                return SipReasonPhrases.Ok;
            case SipStatusCodes.Accepted:
                return SipReasonPhrases.Accepted;
            case SipStatusCodes.BadRequest:
                return SipReasonPhrases.BadRequest;
            case SipStatusCodes.Unauthorized:
                return SipReasonPhrases.Unauthorized;
            case SipStatusCodes.Forbidden:
                return SipReasonPhrases.Forbidden;
            case SipStatusCodes.MethodNotAllowed:
                return SipReasonPhrases.MethodNotAllowed;
            case SipStatusCodes.ProxyAuthenticationRequired:
                return SipReasonPhrases.ProxyAuthenticationRequired;
            case SipStatusCodes.RequestEntityTooLarge:
                return SipReasonPhrases.RequestEntityTooLarge;
            case SipStatusCodes.UnsupportedMediaType:
                return SipReasonPhrases.UnsupportedMediaType;
            case SipStatusCodes.BadExtension:
                return SipReasonPhrases.BadExtension;
            case SipStatusCodes.CallOrTransactionDoesNotExist:
                return SipReasonPhrases.CallOrTransactionDoesNotExist;
            default:
                return statusCode >= 200 && statusCode < 300
                    ? SipReasonPhrases.Ok
                    : SipReasonPhrases.ServerInternalError;
        }
    }

    static SipExtensionHeaderValue? ParseSingleExtensionHeader(
        SipMessage message, string name)
    {
        SipExtensionHeaderValue? result = null;
        int count = 0;
        int index = 0;
        while (message.Headers != null && index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, name))
            {
                count = count + 1;
                SipExtensionHeaderResult parsed =
                    SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                        name, header.Value, new SipParseOptions());
                if (!parsed.Success || parsed.Values.Length != 1)
                {
                    return null;
                }
                result = parsed.Values[0];
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    static void RemoveHeader(SipMessage message, string name)
    {
        System.Collections.Generic.List<SipHeader> kept =
            new System.Collections.Generic.List<SipHeader>();
        int index = 0;
        while (message.Headers != null && index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && !SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, name))
            {
                kept.Add(header);
            }
            index = index + 1;
        }
        message.Headers = kept.ToArray();
    }

    static string FormatMediaType(SipMediaType mediaType)
    {
        SipCoreHeaderResult parsed = new SipCoreHeaderResult();
        parsed.Success = true;
        parsed.MediaType = mediaType;
        SipWriteResult formatted = SipCoreHeaderCodec.FormatCoreHeaderValue(
            SipHeaderNames.ContentType, parsed, new SipWriteOptions());
        if (!formatted.Success)
        {
            throw new System.ArgumentException(
                "The REFER Content-Type is invalid.", "request");
        }
        string line = System.Text.Encoding.UTF8.GetString(formatted.Bytes);
        int colon = line.IndexOf(':');
        return colon < 0 ? line.Trim()
            : line.Substring(colon + 1).Trim();
    }
}
