namespace sip;

internal static class SipSdpParser
{
    internal static SipParseResult ParseDescriptionBytes(byte[]? source, SipParseOptions? options)
    {
        if (source == null) return CreateInputError("The SDP source is required.", 0);
        if (options == null) return CreateInputError("The SDP parse options are required.", 0);
        if (options.MaxMessageBytes < 0 || options.MaxBodyBytes < 0
            || options.MaxHeaderLineBytes < 0 || options.MaxListValueCount < 0)
            return CreateInputError("SDP parse limits cannot be negative.", 0);
        int maximum = options.MaxMessageBytes < options.MaxBodyBytes
            ? options.MaxMessageBytes : options.MaxBodyBytes;
        if (source.Length > maximum)
        {
            SipParseResult limited = CreateInputError("The SDP body exceeds the configured maximum size.", maximum);
            limited.RequiredBytes = source.Length;
            return limited;
        }
        if (source.Length == 0) return CreateInputError("The SDP body is empty.", 0);
        int utf8Offset = SipUtf8Validator.GetInvalidUtf8Offset(source);
        if (utf8Offset >= 0) return CreateInputError("The SDP body contains invalid UTF-8.", utf8Offset);

        System.Collections.Generic.List<SipSdpLine> lines = new System.Collections.Generic.List<SipSdpLine>();
        System.Collections.Generic.List<SipValidationIssue> framingIssues =
            new System.Collections.Generic.List<SipValidationIssue>();
        int start = 0;
        while (start < source.Length)
        {
            int end = start;
            while (end < source.Length && source[end] != (byte)'\n') end = end + 1;
            bool hasLf = end < source.Length;
            bool hasCr = end > start && source[end - 1] == (byte)'\r';
            int contentEnd = hasCr ? end - 1 : end;
            int bareCr = FindBareCarriageReturn(source, start, contentEnd);
            if (bareCr >= 0) return CreateInputError("An SDP line contains a bare carriage return.", bareCr);
            if (contentEnd - start > options.MaxHeaderLineBytes)
                return CreateInputError("An SDP line exceeds the configured maximum size.",
                    start + options.MaxHeaderLineBytes);
            if (!hasLf || !hasCr)
            {
                if (options.Strict)
                    return CreateInputError("Every SDP line must end with CRLF.", contentEnd);
                framingIssues.Add(CreateCompatibilityIssue("sdp-line-ending",
                    "A non-canonical SDP line ending was accepted.", start));
            }
            if (contentEnd == start) return CreateInputError("An SDP line cannot be empty.", start);
            string text = System.Text.Encoding.UTF8.GetString(source, start, contentEnd - start);
            lines.Add(new SipSdpLine(text, start));
            if (!hasLf) start = source.Length;
            else start = end + 1;
        }

        SipSdpParseState state = new SipSdpParseState(options);
        int lineIndex = 0;
        while (lineIndex < lines.Count)
        {
            string? error = state.ProcessDescriptionLine(lines[lineIndex]);
            if (error != null) return CreateInputError(error, state.ErrorOffset);
            lineIndex = lineIndex + 1;
        }
        SipSessionDescription description = state.BuildDescription();
        SipValidationIssue[] validation = SipSdpValidator.ValidateDescriptionModel(description, options.Strict);
        System.Collections.Generic.List<SipValidationIssue> allIssues =
            new System.Collections.Generic.List<SipValidationIssue>();
        allIssues.AddRange(framingIssues);
        allIssues.AddRange(state.Issues);
        allIssues.AddRange(validation);
        if (options.Strict && HasErrorIssue(allIssues))
        {
            SipValidationIssue issue = GetFirstErrorIssue(allIssues);
            string message = issue.Message == null ? "The SDP session description is invalid." : issue.Message;
            int offset = issue.ByteOffset < 0 ? source.Length : issue.ByteOffset;
            SipParseResult invalid = CreateInputError(message, offset);
            invalid.ValidationIssues = allIssues.ToArray();
            return invalid;
        }
        if (options.PreserveOriginal) description.OriginalBytes = CopyBytes(source);
        SipParseResult result = new SipParseResult();
        result.Status = SipParseStatus.Complete;
        result.SessionDescription = description;
        result.Consumed = source.Length;
        result.RequiredBytes = source.Length;
        result.ValidationIssues = allIssues.ToArray();
        return result;
    }

    static int FindBareCarriageReturn(byte[] source, int start, int end)
    {
        int index = start;
        while (index < end)
        {
            if (source[index] == (byte)'\r') return index;
            if (source[index] == 0) return index;
            index = index + 1;
        }
        return -1;
    }

    static bool HasErrorIssue(System.Collections.Generic.List<SipValidationIssue> issues)
    {
        int index = 0;
        while (index < issues.Count)
        {
            if (issues[index].Severity == SipValidationSeverity.Error) return true;
            index = index + 1;
        }
        return false;
    }

    static SipValidationIssue GetFirstErrorIssue(
        System.Collections.Generic.List<SipValidationIssue> issues)
    {
        int index = 0;
        while (index < issues.Count)
        {
            if (issues[index].Severity == SipValidationSeverity.Error) return issues[index];
            index = index + 1;
        }
        return issues[0];
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        System.Array.Copy(source, result, source.Length);
        return result;
    }

    static SipValidationIssue CreateCompatibilityIssue(string code, string message, int offset)
    {
        SipValidationIssue issue = new SipValidationIssue();
        issue.Severity = SipValidationSeverity.Warning;
        issue.Code = code;
        issue.Message = message;
        issue.Path = "sdp";
        issue.ByteOffset = offset;
        return issue;
    }

    static SipParseResult CreateInputError(string message, int offset)
    {
        SipParseResult result = new SipParseResult();
        result.Status = SipParseStatus.Error;
        result.Consumed = offset;
        result.ErrorOffset = offset;
        result.ErrorMessage = message;
        SipValidationIssue issue = new SipValidationIssue();
        issue.Severity = SipValidationSeverity.Error;
        issue.Code = "invalid-sdp";
        issue.Message = message;
        issue.ByteOffset = offset;
        issue.Path = "sdp";
        result.ValidationIssues = new SipValidationIssue[] { issue };
        return result;
    }
}
