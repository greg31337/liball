namespace sip;

internal static class SipHeaderParser
{
    internal static SipHeaderParseResult ParseHeaderBytes(byte[]? source, SipParseOptions? options)
    {
        if (source == null)
        {
            return CreateError(0, "Header source bytes are required.");
        }

        if (options == null)
        {
            return CreateError(0, "Header parse options are required.");
        }

        if (options.MaxHeaderLineBytes < 0 || options.MaxHeaderBytes < 0
            || options.MaxListValueCount < 0 || options.MaxParameterCount < 0)
        {
            return CreateError(0, "Header size limits cannot be negative.");
        }

        int limit = options.MaxHeaderLineBytes < options.MaxHeaderBytes
            ? options.MaxHeaderLineBytes : options.MaxHeaderBytes;
        if (source.Length > limit)
        {
            return CreateError(limit, "The header field exceeds the configured size limit.");
        }

        if (source.Length == 0)
        {
            return CreateError(0, "A complete header field is required.");
        }

        int invalidUtf8Offset = SipUtf8Validator.GetInvalidUtf8Offset(source);
        if (invalidUtf8Offset >= 0)
        {
            return CreateError(invalidUtf8Offset, "The header field is not valid UTF-8.");
        }

        string text = System.Text.Encoding.UTF8.GetString(source);
        if (text.Length >= 2 && text[text.Length - 2] == '\r' && text[text.Length - 1] == '\n')
        {
            text = text.Substring(0, text.Length - 2);
        }

        SipHeaderPrimitiveResult unfolded = UnfoldHeaderText(text, options.AllowHeaderFolding);
        if (!unfolded.Success || unfolded.Text == null)
        {
            return CreateError(GetUtf8ByteOffset(text, unfolded.ErrorOffset), unfolded.ErrorMessage);
        }

        string field = unfolded.Text;
        int colon = field.IndexOf(':');
        if (colon < 0)
        {
            return CreateError(0, "The header field is missing its colon.");
        }

        int nameEnd = colon;
        while (nameEnd > 0 && (field[nameEnd - 1] == ' ' || field[nameEnd - 1] == '\t'))
        {
            nameEnd = nameEnd - 1;
        }

        string name = field.Substring(0, nameEnd);
        if (!SipAscii.IsToken(name))
        {
            return CreateError(0, "The header field name must be a token.");
        }

        int valueStart = colon + 1;
        while (valueStart < field.Length && (field[valueStart] == ' ' || field[valueStart] == '\t'))
        {
            valueStart = valueStart + 1;
        }

        int valueEnd = field.Length;
        while (valueEnd > valueStart && (field[valueEnd - 1] == ' ' || field[valueEnd - 1] == '\t'))
        {
            valueEnd = valueEnd - 1;
        }

        SipHeader header = new SipHeader();
        header.Name = name;
        header.Value = field.Substring(valueStart, valueEnd - valueStart);
        if (options.PreserveOriginal)
        {
            header.OriginalBytes = new byte[source.Length];
            System.Array.Copy(source, header.OriginalBytes, source.Length);
        }

        SipValidationIssue[] issues = SipHeaderValidator.ValidateHeaderModel(header);
        if (issues.Length > 0)
        {
            SipHeaderParseResult invalid = CreateError(0,
                issues[0].Message == null ? "The header field is invalid." : issues[0].Message);
            invalid.ValidationIssues = issues;
            return invalid;
        }

        SipHeaderParseResult result = new SipHeaderParseResult();
        result.Status = SipParseStatus.Complete;
        result.Header = header;
        result.Consumed = source.Length;
        result.RequiredBytes = source.Length;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Via)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactVia))
        {
            SipViaParseResult via = SipViaCodec.ParseViaHeaderValue(header.Value, options);
            if (!via.Success)
            {
                return CreateError(valueStart + via.ErrorOffset, via.ErrorMessage);
            }

            result.ViaValues = via.Values;
        }
        else if (SipStructuredHeaderCodec.IsStructuredHeaderName(name))
        {
            SipStructuredHeaderResult structured = SipStructuredHeaderCodec.ParseStructuredHeaderValue(
                name, header.Value, options);
            if (!structured.Success)
            {
                return CreateError(valueStart + structured.ErrorOffset, structured.ErrorMessage);
            }

            result.NameAddress = structured.NameAddress;
            result.RouteValues = structured.RouteValues;
            result.ContactValues = structured.ContactValues;
            result.CSeq = structured.CSeq;
            result.CallId = structured.CallId;
            result.MaxForwards = structured.MaxForwards;
        }
        else if (SipCoreHeaderCodec.IsCoreHeaderName(name))
        {
            SipCoreHeaderResult core = SipCoreHeaderCodec.ParseCoreHeaderValue(name, header.Value, options);
            if (!core.Success)
            {
                return CreateError(valueStart + core.ErrorOffset, core.ErrorMessage);
            }

            result.ContentLength = core.ContentLength;
            result.MediaType = core.MediaType;
            result.MediaTypes = core.MediaTypes;
            result.ContentDisposition = core.ContentDisposition;
            result.Methods = core.Methods;
            result.OptionTags = core.OptionTags;
            result.Date = core.Date;
            result.HasDate = core.HasDate;
            result.ExpiresSeconds = core.ExpiresSeconds;
            result.MinExpiresSeconds = core.MinExpiresSeconds;
            result.RetryAfter = core.RetryAfter;
            result.WarningValues = core.WarningValues;
            result.ProductValues = core.ProductValues;
        }
        else if (SipAuthenticationHeaderCodec.IsAuthenticationHeaderName(name))
        {
            SipAuthenticationHeaderResult authentication =
                SipAuthenticationHeaderCodec.ParseAuthenticationHeaderValue(name, header.Value, options);
            if (!authentication.Success)
            {
                return CreateError(valueStart + authentication.ErrorOffset, authentication.ErrorMessage);
            }

            result.DigestChallenge = authentication.Challenge;
            result.DigestAuthorization = authentication.Authorization;
            result.BearerToken = authentication.BearerToken;
        }
        else if (SipExtensionHeaderCodec.IsExtensionHeaderName(name))
        {
            SipExtensionHeaderResult extension = SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                name, header.Value, options);
            if (!extension.Success)
            {
                return CreateError(valueStart + extension.ErrorOffset, extension.ErrorMessage);
            }

            result.ExtensionValues = extension.Values;
        }

        return result;
    }

    static SipHeaderPrimitiveResult UnfoldHeaderText(string text, bool allowFolding)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        bool seenColon = false;
        while (index < text.Length)
        {
            int character = text[index];
            if (character == '\r')
            {
                if (!seenColon || !allowFolding || index + 2 >= text.Length || text[index + 1] != '\n'
                    || text[index + 2] != ' ' && text[index + 2] != '\t')
                {
                    return CreatePrimitiveError(index, "The header field contains an invalid line break.");
                }

                while (builder.Length > 0
                    && (builder[builder.Length - 1] == ' ' || builder[builder.Length - 1] == '\t'))
                {
                    builder.Remove(builder.Length - 1, 1);
                }

                builder.Append(' ');
                index = index + 3;
                while (index < text.Length && (text[index] == ' ' || text[index] == '\t'))
                {
                    index = index + 1;
                }
            }
            else
            {
                if (character == '\n'
                    || (character < 32 && character != '\t' || character == 127)
                    && !IsEscapedControlCharacter(text, index))
                {
                    return CreatePrimitiveError(index, "The header field contains an invalid control character.");
                }

                builder.Append((char)character);
                if (character == ':')
                {
                    seenColon = true;
                }

                index = index + 1;
            }
        }

        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
        result.Success = true;
        result.Text = builder.ToString();
        return result;
    }

    static bool IsEscapedControlCharacter(string text, int index)
    {
        bool quoted = false;
        bool escaped = false;
        int commentDepth = 0;
        int position = 0;
        while (position < index)
        {
            int character = text[position];
            if (escaped)
            {
                escaped = false;
            }
            else if ((quoted || commentDepth > 0) && character == '\\')
            {
                escaped = true;
            }
            else if (commentDepth == 0 && character == '"')
            {
                quoted = !quoted;
            }
            else if (!quoted && character == '(')
            {
                commentDepth = commentDepth + 1;
            }
            else if (!quoted && commentDepth > 0 && character == ')')
            {
                commentDepth = commentDepth - 1;
            }
            position = position + 1;
        }
        return escaped && (quoted || commentDepth > 0);
    }

    static int GetUtf8ByteOffset(string text, int characterOffset)
    {
        if (characterOffset <= 0)
        {
            return 0;
        }

        if (characterOffset >= text.Length)
        {
            return System.Text.Encoding.UTF8.GetByteCount(text);
        }

        return System.Text.Encoding.UTF8.GetByteCount(text.Substring(0, characterOffset));
    }

    static SipHeaderPrimitiveResult CreatePrimitiveError(int offset, string message)
    {
        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
        result.ErrorOffset = offset;
        result.ErrorMessage = message;
        return result;
    }

    static SipHeaderParseResult CreateError(int offset, string? message)
    {
        SipHeaderParseResult result = new SipHeaderParseResult();
        result.Status = SipParseStatus.Error;
        result.ErrorOffset = offset;
        result.ErrorMessage = message;
        return result;
    }
}
