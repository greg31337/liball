using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailStandardHeaderParserWorker
{
    string fieldName;
    EmailStandardHeaderValueKind kind;
    byte[] input;
    EmailStructuredValueParseMode mode;
    EmailSourcePosition position;
    EmailIssueList issues;
    bool malformed;

    internal EmailStandardHeaderParserWorker(string fieldName, EmailStandardHeaderValueKind kind, byte[] input, EmailStructuredValueParseMode mode, EmailSourcePosition position)
    {
        this.fieldName = fieldName;
        this.kind = kind;
        this.input = input;
        this.mode = mode;
        this.position = position;
        issues = new EmailIssueList();
        malformed = false;
    }

    internal EmailStandardHeaderParseResult Parse()
    {
        if (kind == EmailStandardHeaderValueKind.ReturnPath) { return ParseReturnPath(); }
        if (kind == EmailStandardHeaderValueKind.MimeVersion) { return ParseMimeVersion(); }
        if (kind == EmailStandardHeaderValueKind.ContentTransferEncoding) { return ParseTransferEncoding(); }
        if (kind == EmailStandardHeaderValueKind.ContentLocation) { return ParseContentLocation(); }
        if (kind == EmailStandardHeaderValueKind.Newsgroups || kind == EmailStandardHeaderValueKind.FollowupTo) { return ParseNewsgroups(); }
        if (kind == EmailStandardHeaderValueKind.ListId) { return ParseListId(); }
        return ParseTrace();
    }

    EmailStandardHeaderParseResult ParseReturnPath()
    {
        string text = GetTokenText();
        bool nullPath = string.Equals(text, "<>", StringComparison.Ordinal);
        EmailAddress? address = null;
        string addressText = text;
        if (!nullPath)
        {
            bool bracketed = text.Length >= 2 && text[0] == '<' && text[text.Length - 1] == '>';
            if (bracketed) { addressText = text.Substring(1, text.Length - 2).Trim(); }
            else { AddMalformed("Return-Path requires angle brackets around the reverse path.", "The unbracketed mailbox was parsed in tolerant mode."); }
            EmailAddressParseMode addressMode = mode == EmailStructuredValueParseMode.Strict ? EmailAddressParseMode.Strict : EmailAddressParseMode.Tolerant;
            EmailAddressParseResult parsed = new EmailAddressParser().ParseMailbox(Encoding.UTF8.GetBytes(addressText), addressMode);
            issues.AddRange(parsed.GetIssues());
            address = parsed.GetMailbox();
            if (address == null) { AddMalformed("Return-Path does not contain one valid mailbox or the null path.", "The original bytes were retained."); }
        }

        EmailStandardHeaderValue value = Create(null, null, new List<string>(), address, nullPath, 0, 0, false, EmailTransferEncodingKind.SevenBit);
        return Result(value);
    }

    EmailStandardHeaderParseResult ParseMimeVersion()
    {
        string text = GetTokenText();
        int dot = text.IndexOf('.');
        int major = dot > 0 ? ParseNumber(text.Substring(0, dot)) : -1;
        int minor = dot > 0 && dot + 1 < text.Length && text.IndexOf('.', dot + 1) < 0 ? ParseNumber(text.Substring(dot + 1)) : -1;
        if (major < 0 || minor < 0)
        {
            AddMalformed("MIME-Version must contain two decimal integers separated by one dot.", "The original version text was retained.");
            major = major < 0 ? 0 : major;
            minor = minor < 0 ? 0 : minor;
        }
        else if (major != 1 || minor != 0)
        {
            AddMalformed("MIME-Version is not the Internet MIME version 1.0.", "The numeric version was retained.");
        }

        EmailStandardHeaderValue value = Create(text, null, new List<string>(), null, false, major, minor, false, EmailTransferEncodingKind.SevenBit);
        return Result(value);
    }

    EmailStandardHeaderParseResult ParseTransferEncoding()
    {
        string text = GetTokenText();
        string token = text.ToLowerInvariant();
        bool safe = IsToken(token);
        if (!safe) { AddMalformed("Content-Transfer-Encoding must contain one MIME token.", "The original encoding name was retained."); }
        bool known = true;
        EmailTransferEncodingKind encoding = EmailTransferEncodingKind.SevenBit;
        if (token == "7bit") { encoding = EmailTransferEncodingKind.SevenBit; }
        else if (token == "8bit") { encoding = EmailTransferEncodingKind.EightBit; }
        else if (token == "binary") { encoding = EmailTransferEncodingKind.Binary; }
        else if (token == "quoted-printable") { encoding = EmailTransferEncodingKind.QuotedPrintable; }
        else if (token == "base64") { encoding = EmailTransferEncodingKind.Base64; }
        else { known = false; }
        EmailStandardHeaderValue value = Create(token, null, new List<string>(), null, false, 0, 0, known, encoding);
        return Result(value);
    }

    EmailStandardHeaderParseResult ParseContentLocation()
    {
        string text = GetText().Trim();
        if (text.Length == 0 || HasWhitespaceOrControl(text)) { AddMalformed("Content-Location must contain one URI reference without whitespace or controls.", "The trimmed location was retained."); }
        EmailStandardHeaderValue value = Create(text, null, new List<string>(), null, false, 0, 0, false, EmailTransferEncodingKind.SevenBit);
        return Result(value);
    }

    EmailStandardHeaderParseResult ParseNewsgroups()
    {
        string text = GetText();
        string[] pieces = text.Split(',');
        List<string> values = new List<string>();
        int index = 0;
        while (index < pieces.Length)
        {
            string item = pieces[index].Trim();
            if (item.Length == 0) { AddMalformed("A newsgroup list contains an empty item.", "The empty item was skipped."); }
            else
            {
                if (!IsNewsgroup(item)) { AddMalformed("A newsgroup name contains invalid characters.", "The item was retained in tolerant mode."); }
                values.Add(item);
            }
            index++;
        }
        if (values.Count == 0) { AddMalformed("A newsgroup list requires at least one item.", "The original bytes were retained."); }
        EmailStandardHeaderValue value = Create(null, null, values, null, false, 0, 0, false, EmailTransferEncodingKind.SevenBit);
        return Result(value);
    }

    EmailStandardHeaderParseResult ParseListId()
    {
        string text = GetText().Trim();
        int open = text.LastIndexOf('<');
        int close = text.LastIndexOf('>');
        string? display = null;
        string? identifier = null;
        if (open >= 0 && close == text.Length - 1 && open < close)
        {
            display = text.Substring(0, open).Trim();
            identifier = text.Substring(open + 1, close - open - 1).Trim();
            if (display.Length == 0) { display = null; }
            if (!IsListIdentifier(identifier)) { AddMalformed("List-ID contains an invalid list identifier.", "The identifier text was retained."); }
        }
        else
        {
            identifier = text.Trim('<', '>').Trim();
            AddMalformed("List-ID requires one identifier enclosed in angle brackets.", "The apparent identifier was retained.");
        }
        EmailStandardHeaderValue value = Create(display, identifier, new List<string>(), null, false, 0, 0, false, EmailTransferEncodingKind.SevenBit);
        return Result(value);
    }

    EmailStandardHeaderParseResult ParseTrace()
    {
        string text = GetText().Trim();
        if (text.Length == 0 || HasUnsafeText(text)) { AddMalformed("The trace field contains no safe printable value.", "The trimmed trace text was retained."); }
        EmailStandardHeaderValue value = Create(text, null, new List<string>(), null, false, 0, 0, false, EmailTransferEncodingKind.SevenBit);
        return Result(value);
    }

    EmailStandardHeaderValue Create(string? primary, string? secondary, List<string> values, EmailAddress? address, bool nullPath, int major, int minor, bool known, EmailTransferEncodingKind encoding)
    {
        return new EmailStandardHeaderValue(kind, fieldName, primary, secondary, values, address, nullPath, major, minor, known, encoding, input, position, malformed);
    }

    EmailStandardHeaderParseResult Result(EmailStandardHeaderValue value)
    {
        return new EmailStandardHeaderParseResult(value, issues, input.Length, input.Length);
    }

    void AddMalformed(string description, string recovery)
    {
        malformed = true;
        EmailIssueSeverity severity = mode == EmailStructuredValueParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        issues.Add(EmailStandardHeaderParser.CreateIssue(severity, EmailIssueCode.MetadataValueMalformed, description, position, fieldName, recovery));
    }

    string GetText()
    {
        StringBuilder builder = new StringBuilder();
        string text = Encoding.UTF8.GetString(input);
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '\r' && index + 2 < text.Length && text[index + 1] == '\n' && (text[index + 2] == ' ' || text[index + 2] == '\t'))
            {
                builder.Append(' ');
                index += 3;
                while (index < text.Length && (text[index] == ' ' || text[index] == '\t')) { index++; }
                continue;
            }
            builder.Append(text[index]);
            index++;
        }
        return builder.ToString();
    }

    string GetTokenText()
    {
        string text = GetText();
        StringBuilder builder = new StringBuilder();
        int depth = 0;
        bool escaped = false;
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (depth > 0)
            {
                if (escaped) { escaped = false; }
                else if (current == '\\') { escaped = true; }
                else if (current == '(') { depth++; }
                else if (current == ')') { depth--; }
            }
            else if (current == '(')
            {
                depth = 1;
                if (builder.Length > 0 && builder[builder.Length - 1] != ' ') { builder.Append(' '); }
            }
            else
            {
                builder.Append(current);
            }
            index++;
        }
        if (depth != 0) { AddMalformed("The field contains an unterminated comment.", "The text outside the comment was retained."); }
        return builder.ToString().Trim();
    }

    static int ParseNumber(string value)
    {
        if (value.Length == 0) { return -1; }
        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            int digit = value[index] - '0';
            if (digit < 0 || digit > 9 || result > 214748364 || result == 214748364 && digit > 7) { return -1; }
            result = result * 10 + digit;
            index++;
        }
        return result;
    }

    static bool IsToken(string value)
    {
        if (value.Length == 0) { return false; }
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < 33 || current > 126 || "()<>@,;:\\\"/[]?=".IndexOf(current) >= 0) { return false; }
            index++;
        }
        return true;
    }

    static bool IsNewsgroup(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current <= 32 || current >= 127 || current == ',' || current == '(' || current == ')' || current == '<' || current == '>') { return false; }
            index++;
        }
        return value.Length > 0;
    }

    static bool IsListIdentifier(string value)
    {
        if (value.Length == 0 || value[0] == '.' || value[value.Length - 1] == '.') { return false; }
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            bool valid = current >= 'A' && current <= 'Z' || current >= 'a' && current <= 'z' || current >= '0' && current <= '9' || current == '.' || current == '-';
            if (!valid) { return false; }
            index++;
        }
        return true;
    }

    static bool HasWhitespaceOrControl(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] <= 32 || value[index] == 127) { return true; }
            index++;
        }
        return false;
    }

    static bool HasUnsafeText(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\r' || value[index] == '\n' || value[index] == 0) { return true; }
            index++;
        }
        return false;
    }
}
