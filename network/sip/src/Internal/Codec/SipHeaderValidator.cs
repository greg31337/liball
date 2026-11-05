namespace sip;

internal static class SipHeaderValidator
{
    internal static SipValidationIssue[] ValidateHeaderModel(SipHeader? header)
    {
        System.Collections.Generic.List<SipValidationIssue> issues = new System.Collections.Generic.List<SipValidationIssue>();
        if (header == null)
        {
            issues.Add(CreateIssue("header-required", "A header field is required.", "header"));
            return issues.ToArray();
        }

        if (!SipAscii.IsToken(header.Name))
        {
            issues.Add(CreateIssue("header-name", "The header field name must be a token.", "header.name"));
        }

        if (header.Value == null)
        {
            issues.Add(CreateIssue("header-value", "The header field value cannot be null.", "header.value"));
        }
        else
        {
            int index = 0;
            while (index < header.Value.Length)
            {
                int character = header.Value[index];
                if (character == '\r' || character == '\n'
                    || (character < 32 && character != '\t' || character == 127)
                    && !IsEscapedControlCharacter(header.Value, index))
                {
                    issues.Add(CreateIssue("header-value", "The header field value contains a control character.",
                        "header.value"));
                    break;
                }

                index = index + 1;
            }
        }

        if (header.OriginalBytes == null)
        {
            issues.Add(CreateIssue("header-original", "The original header byte collection cannot be null.",
                "header.originalBytes"));
        }

        return issues.ToArray();
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

    static SipValidationIssue CreateIssue(string code, string message, string path)
    {
        SipValidationIssue issue = new SipValidationIssue();
        issue.Severity = SipValidationSeverity.Error;
        issue.Code = code;
        issue.Message = message;
        issue.Path = path;
        return issue;
    }
}
