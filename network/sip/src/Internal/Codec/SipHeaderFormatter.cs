namespace sip;

internal static class SipHeaderFormatter
{
    internal static SipWriteResult FormatHeaderModel(SipHeader? header, SipWriteOptions? options)
    {
        if (header == null)
        {
            return CreateError("A header field is required.");
        }

        if (options == null)
        {
            return CreateError("Header write options are required.");
        }

        if (options.MaxHeaderLineBytes < 0)
        {
            return CreateError("The maximum header line size cannot be negative.");
        }

        SipValidationIssue[] issues = SipHeaderValidator.ValidateHeaderModel(header);
        if (issues.Length > 0)
        {
            string? issueMessage = issues[0].Message;
            return CreateError(issueMessage == null ? "The header field is invalid." : issueMessage);
        }

        byte[] bytes;
        if (options.PreserveOriginal && header.OriginalBytes.Length > 0 && OriginalMatchesHeader(header))
        {
            bytes = new byte[header.OriginalBytes.Length];
            System.Array.Copy(header.OriginalBytes, bytes, bytes.Length);
        }
        else if (SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.Via)
            || SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.CompactVia))
        {
            SipParseOptions parseOptions = new SipParseOptions();
            parseOptions.PreserveOriginal = false;
            SipViaParseResult via = SipViaCodec.ParseViaHeaderValue(header.Value, parseOptions);
            if (!via.Success)
            {
                return CreateError(via.ErrorMessage == null ? "The Via header field is invalid." : via.ErrorMessage);
            }

            return SipViaCodec.FormatViaHeaderValues(via.Values, options);
        }
        else if (SipStructuredHeaderCodec.IsStructuredHeaderName(header.Name))
        {
            SipParseOptions parseOptions = new SipParseOptions();
            parseOptions.PreserveOriginal = false;
            parseOptions.AllowCompatibleWhitespace = true;
            SipStructuredHeaderResult structured = SipStructuredHeaderCodec.ParseStructuredHeaderValue(
                header.Name, header.Value, parseOptions);
            if (!structured.Success)
            {
                return CreateError(structured.ErrorMessage == null
                    ? "The structured header field is invalid." : structured.ErrorMessage);
            }

            return SipStructuredHeaderCodec.FormatStructuredHeaderValue(header.Name, structured, options);
        }
        else if (SipCoreHeaderCodec.IsCoreHeaderName(header.Name))
        {
            SipParseOptions parseOptions = new SipParseOptions();
            parseOptions.PreserveOriginal = false;
            SipCoreHeaderResult core = SipCoreHeaderCodec.ParseCoreHeaderValue(
                header.Name, header.Value, parseOptions);
            if (!core.Success)
            {
                return CreateError(core.ErrorMessage == null ? "The core header field is invalid."
                    : core.ErrorMessage);
            }

            return SipCoreHeaderCodec.FormatCoreHeaderValue(header.Name, core, options);
        }
        else if (SipAuthenticationHeaderCodec.IsAuthenticationHeaderName(header.Name))
        {
            SipParseOptions parseOptions = new SipParseOptions();
            parseOptions.PreserveOriginal = false;
            SipAuthenticationHeaderResult authentication =
                SipAuthenticationHeaderCodec.ParseAuthenticationHeaderValue(header.Name, header.Value, parseOptions);
            if (!authentication.Success)
            {
                return CreateError(authentication.ErrorMessage == null
                    ? "The authentication header field is invalid." : authentication.ErrorMessage);
            }

            return SipAuthenticationHeaderCodec.FormatAuthenticationHeaderValue(
                header.Name, authentication, options);
        }
        else if (SipExtensionHeaderCodec.IsExtensionHeaderName(header.Name))
        {
            SipParseOptions parseOptions = new SipParseOptions();
            parseOptions.PreserveOriginal = false;
            SipExtensionHeaderResult extension = SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                header.Name, header.Value, parseOptions);
            if (!extension.Success)
            {
                return CreateError(extension.ErrorMessage == null
                    ? "The extension header field is invalid." : extension.ErrorMessage);
            }

            return SipExtensionHeaderCodec.FormatExtensionHeaderValue(header.Name, extension, options);
        }
        else
        {
            string name = GetGenericHeaderName(header.Name, options.CompactHeaderNames);
            string value = header.Value == null ? "" : header.Value;
            bytes = System.Text.Encoding.UTF8.GetBytes(name + ": " + value + "\r\n");
        }

        if (bytes.Length > options.MaxHeaderLineBytes)
        {
            SipWriteResult limited = CreateError("The formatted header field exceeds the configured output limit.");
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

    static string GetGenericHeaderName(string? name, bool compact)
    {
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ContentEncoding)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactContentEncoding))
            return compact ? SipHeaderNames.CompactContentEncoding : SipHeaderNames.ContentEncoding;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Identity)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactIdentity))
            return compact ? SipHeaderNames.CompactIdentity : SipHeaderNames.Identity;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Subject)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactSubject))
            return compact ? SipHeaderNames.CompactSubject : SipHeaderNames.Subject;
        return name == null ? "" : name;
    }

    static bool OriginalMatchesHeader(SipHeader header)
    {
        SipParseOptions options = new SipParseOptions();
        options.Strict = false;
        options.PreserveOriginal = false;
        options.AllowHeaderFolding = true;
        options.AllowCompatibleWhitespace = true;
        options.MaxHeaderBytes = int.MaxValue;
        options.MaxHeaderLineBytes = int.MaxValue;
        options.MaxListValueCount = int.MaxValue;
        options.MaxParameterCount = int.MaxValue;
        SipHeaderParseResult parsed = SipHeaderParser.ParseHeaderBytes(header.OriginalBytes, options);
        return parsed.Status == SipParseStatus.Complete && parsed.Header != null
            && parsed.Consumed == header.OriginalBytes.Length
            && parsed.Header.Name == header.Name && parsed.Header.Value == header.Value;
    }

    static SipWriteResult CreateError(string message)
    {
        SipWriteResult result = new SipWriteResult();
        result.ErrorMessage = message;
        return result;
    }
}
