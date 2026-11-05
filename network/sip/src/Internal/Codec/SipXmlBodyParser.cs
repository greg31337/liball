namespace sip;

internal static class SipXmlBodyParser
{
    internal static SipParseResult ParseBodyBytes(byte[]? source, SipMediaType? mediaType,
        SipParseOptions? options)
    {
        if (source == null) return CreateError("The XML event body source is required.", 0);
        if (mediaType == null) return CreateError("The XML event body media type is required.", 0);
        if (options == null) return CreateError("XML event body parse options are required.", 0);
        if (options.MaxMessageBytes < 0 || options.MaxBodyBytes < 0
            || options.MaxHeaderLineBytes < 0 || options.MaxXmlDepth < 0)
            return CreateError("XML event body parse limits cannot be negative.", 0);
        int maximum = options.MaxMessageBytes < options.MaxBodyBytes
            ? options.MaxMessageBytes : options.MaxBodyBytes;
        if (source.Length > maximum)
        {
            SipParseResult limited = CreateError(
                "The XML event body exceeds the configured maximum size.", maximum);
            limited.RequiredBytes = source.Length;
            return limited;
        }
        if (source.Length == 0) return CreateError("The XML event body is empty.", 0);
        int invalidUtf8 = SipUtf8Validator.GetInvalidUtf8Offset(source);
        if (invalidUtf8 >= 0) return CreateError("The event body is not valid UTF-8.", invalidUtf8);
        SipXmlMediaProfile? profile = SipXmlMediaProfiles.FindProfile(mediaType);
        if (profile == null) return CreateError("The event body media type is not supported.", 0);
        int byteStart = HasUtf8Bom(source) ? 3 : 0;
        string text = System.Text.Encoding.UTF8.GetString(source, byteStart, source.Length - byteStart);
        if (profile.MessageSummary)
            return ParseMessageSummary(source, text, mediaType, profile, options);
        SipXmlScanResult scan = new SipXmlScanner(text, options.MaxXmlDepth).ScanDocument();
        if (!scan.Success)
        {
            int characterOffset = scan.ErrorOffset < 0 ? 0 : scan.ErrorOffset;
            int byteOffset = byteStart + System.Text.Encoding.UTF8.GetByteCount(
                text.Substring(0, characterOffset));
            return CreateError(scan.ErrorMessage == null
                ? "The XML event body is malformed." : scan.ErrorMessage, byteOffset);
        }
        SipValidationIssue[] profileIssues = SipXmlBodyValidator.ValidateScanForProfile(
            scan, profile, options.Strict);
        SipValidationIssue[] mediaIssues = SipXmlBodyValidator.ValidateMediaTypeModel(
            mediaType, options.Strict);
        SipValidationIssue[] issues = CombineIssues(profileIssues, mediaIssues);
        if (options.Strict && HasErrorIssues(issues))
            return CreateValidationError(issues, source.Length);
        SipXmlBody body = new SipXmlBody();
        body.MediaType = mediaType;
        body.RootName = scan.RootName;
        body.NamespaceUri = scan.NamespaceUri;
        body.XmlText = text;
        if (options.PreserveOriginal) body.OriginalBytes = CopyBytes(source);
        return CreateComplete(body, issues, source.Length);
    }

    static SipParseResult ParseMessageSummary(byte[] source, string text, SipMediaType mediaType,
        SipXmlMediaProfile profile, SipParseOptions options)
    {
        bool compatible = !options.Strict;
        string? error = SipMessageSummarySyntax.ValidateSummaryText(
            text, compatible, options.MaxHeaderLineBytes);
        if (error != null) return CreateError(error, 0);
        System.Collections.Generic.List<SipValidationIssue> issues =
            new System.Collections.Generic.List<SipValidationIssue>();
        string normalized = SipMessageSummarySyntax.NormalizeSummaryText(text);
        if (normalized != text)
        {
            SipValidationIssue issue = new SipValidationIssue();
            issue.Severity = SipValidationSeverity.Warning;
            issue.Code = "message-summary-line-ending";
            issue.Message = "Non-canonical message-summary line endings were accepted.";
            issue.Path = "xmlBody.xmlText";
            issues.Add(issue);
        }
        SipValidationIssue[] mediaIssues = SipXmlBodyValidator.ValidateMediaTypeModel(
            mediaType, options.Strict);
        issues.AddRange(mediaIssues);
        if (options.Strict && HasErrorIssues(issues.ToArray()))
            return CreateValidationError(issues.ToArray(), source.Length);
        SipXmlBody body = new SipXmlBody();
        body.MediaType = mediaType;
        body.RootName = profile.RootName;
        body.XmlText = normalized;
        if (options.PreserveOriginal) body.OriginalBytes = CopyBytes(source);
        return CreateComplete(body, issues.ToArray(), source.Length);
    }

    static bool HasUtf8Bom(byte[] source)
    {
        return source.Length >= 3 && source[0] == 239 && source[1] == 187 && source[2] == 191;
    }

    static SipValidationIssue[] CombineIssues(SipValidationIssue[] first,
        SipValidationIssue[] second)
    {
        SipValidationIssue[] result = new SipValidationIssue[first.Length + second.Length];
        System.Array.Copy(first, result, first.Length);
        System.Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }

    static bool HasErrorIssues(SipValidationIssue[] issues)
    {
        int index = 0;
        while (index < issues.Length)
        {
            if (issues[index].Severity == SipValidationSeverity.Error) return true;
            index = index + 1;
        }
        return false;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        System.Array.Copy(source, result, source.Length);
        return result;
    }

    static SipParseResult CreateComplete(SipXmlBody body, SipValidationIssue[] issues, int length)
    {
        SipParseResult result = new SipParseResult();
        result.Status = SipParseStatus.Complete;
        result.XmlBody = body;
        result.Consumed = length;
        result.RequiredBytes = length;
        result.ValidationIssues = issues;
        return result;
    }

    static SipParseResult CreateValidationError(SipValidationIssue[] issues, int length)
    {
        string? message = issues[0].Message;
        SipParseResult result = CreateError(
            message == null ? "The XML event body is invalid." : message, 0);
        result.RequiredBytes = length;
        result.ValidationIssues = issues;
        return result;
    }

    static SipParseResult CreateError(string message, int offset)
    {
        SipParseResult result = new SipParseResult();
        result.Status = SipParseStatus.Error;
        result.Consumed = offset;
        result.ErrorOffset = offset;
        result.ErrorMessage = message;
        SipValidationIssue issue = new SipValidationIssue();
        issue.Severity = SipValidationSeverity.Error;
        issue.Code = "invalid-xml-body";
        issue.Message = message;
        issue.ByteOffset = offset;
        issue.Path = "xmlBody";
        result.ValidationIssues = new SipValidationIssue[] { issue };
        return result;
    }
}
