using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailValueFormatterWorker
{
    EmailValueFormatOptions options;
    List<byte> output;
    EmailIssueList issues;
    int column;
    int foldCount;
    bool stopped;

    internal EmailValueFormatterWorker(EmailValueFormatOptions options)
    {
        this.options = options;
        output = new List<byte>();
        issues = new EmailIssueList();
        column = options.GetFirstLineColumn();
        foldCount = 0;
        stopped = false;
    }

    internal void FormatParameters(EmailParameterList value)
    {
        AppendParameters(value, false);
    }

    internal void FormatMediaType(EmailMediaType? value)
    {
        if (!IsToken((value ?? throw new InvalidOperationException()).GetTopLevelType()) || !IsToken(value.GetSubtype()))
        {
            AddFatal("The media type contains a token that cannot be formatted safely.", value.GetSourcePosition());
            return;
        }

        Append(value.GetNormalizedTopLevelType() + "/" + value.GetNormalizedSubtype(), value.GetSourcePosition());
        AppendParameters(value.GetParameters(), true);
    }

    internal void FormatDisposition(EmailDisposition value)
    {
        if (!IsToken(value.GetDispositionType()))
        {
            AddFatal("The disposition type cannot be formatted safely.", value.GetSourcePosition());
            return;
        }

        Append(value.GetNormalizedDispositionType(), value.GetSourcePosition());
        AppendParameters(value.GetParameters(), true);
    }

    internal void FormatContentLanguage(EmailContentLanguage value)
    {
        if (value.GetCount() == 0)
        {
            AddFatal("Content-Language requires at least one language tag.", value.GetSourcePosition());
            return;
        }

        int index = 0;
        while (index < value.GetCount() && !stopped)
        {
            string language = value.GetNormalizedAt(index);
            if (!IsLanguage(language))
            {
                AddFatal("A Content-Language item cannot be formatted safely.", value.GetSourcePosition());
                break;
            }

            AppendPiece(index == 0 ? language : ", " + language, value.GetSourcePosition());
            index++;
        }
    }

    internal void FormatReceived(EmailReceived value)
    {
        if (value.GetClauseCount() == 0)
        {
            AddFatal("A canonical Received value requires at least one trace clause.", value.GetSourcePosition());
            return;
        }

        if (!value.HasDate())
        {
            AddFatal("A canonical Received value requires a date.", value.GetSourcePosition());
            return;
        }

        int index = 0;
        while (index < value.GetClauseCount() && !stopped)
        {
            EmailReceivedClause clause = value.GetClause(index);
            string name = clause.IsExtension() ? clause.GetOriginalName() : clause.GetNormalizedName();
            string clauseValue = clause.GetValue();
            if (clause.IsMalformed() || !IsToken(name) || !IsSafeText(clauseValue))
            {
                AddFatal("A Received clause cannot be formatted safely.", clause.GetSourcePosition());
                break;
            }

            string piece = name + " " + clauseValue;
            AppendPiece(index == 0 ? piece : " " + piece, clause.GetSourcePosition());
            index++;
        }

        if (stopped)
        {
            return;
        }

        EmailDateFormatResult date = new EmailDateFormatter().FormatWithValue(value.GetDate());
        if (!date.IsSuccess())
        {
            issues.AddRange(date.GetIssues());
            stopped = true;
            return;
        }

        byte[] dateBytes = date.GetBytes();
        string dateText = Encoding.ASCII.GetString(dateBytes);
        AppendPiece("; " + dateText, (value.GetDate() ?? throw new InvalidOperationException()).GetSourcePosition());
    }

    internal void FormatStandardHeader(EmailStandardHeaderValue value)
    {
        EmailStandardHeaderValueKind kind = value.GetKind();
        if (kind == EmailStandardHeaderValueKind.ReturnPath)
        {
            if (value.IsNullPath())
            {
                Append("<>", value.GetSourcePosition());
                return;
            }

            EmailAddress? address = value.GetAddress();
            if (address == null)
            {
                AddFatal("A canonical Return-Path requires a mailbox or the null path.", value.GetSourcePosition());
                return;
            }

            EmailAddress pathAddress = new EmailAddress(null, address.GetLocalPart(), address.GetDomain(), address.GetRawBytes(), address.GetRawLocalPartBytes(), address.GetRawDomainBytes(), address.GetSourcePosition(), address.IsDomainLiteral(), address.IsInternationalized(), false, new List<string>());
            EmailAddressFormatResult formattedAddress = new EmailAddressFormatter().FormatWithAddress(pathAddress);
            if (!formattedAddress.IsSuccess())
            {
                issues.AddRange(formattedAddress.GetIssues());
                stopped = true;
                return;
            }

            Append("<" + Encoding.UTF8.GetString(formattedAddress.GetBytes()) + ">", value.GetSourcePosition());
            return;
        }

        if (kind == EmailStandardHeaderValueKind.MimeVersion)
        {
            Append(value.GetMajorVersion().ToString() + "." + value.GetMinorVersion().ToString(), value.GetSourcePosition());
            return;
        }

        if (kind == EmailStandardHeaderValueKind.ContentTransferEncoding)
        {
            string? encoding = value.HasKnownTransferEncoding() ? GetTransferEncodingName(value.GetTransferEncodingKind()) : value.GetPrimaryValue();
            if (encoding == null || !IsToken(encoding))
            {
                AddFatal("The content transfer encoding cannot be formatted safely.", value.GetSourcePosition());
                return;
            }

            Append(encoding.ToLowerInvariant(), value.GetSourcePosition());
            return;
        }

        if (kind == EmailStandardHeaderValueKind.Newsgroups || kind == EmailStandardHeaderValueKind.FollowupTo)
        {
            FormatStringList(value);
            return;
        }

        if (kind == EmailStandardHeaderValueKind.ListId)
        {
            string? identifier = value.GetSecondaryValue();
            string? display = value.GetPrimaryValue();
            if (identifier == null || !IsListIdentifier(identifier) || display != null && !IsSafeText(display))
            {
                AddFatal("The List-ID value cannot be formatted safely.", value.GetSourcePosition());
                return;
            }

            AppendPiece((display == null || display.Length == 0 ? "" : display + " ") + "<" + identifier.ToLowerInvariant() + ">", value.GetSourcePosition());
            return;
        }

        string? text = value.GetPrimaryValue();
        if (text == null || !IsSafeText(text) || text.Length == 0)
        {
            AddFatal("The structured header value cannot be formatted safely.", value.GetSourcePosition());
            return;
        }

        AppendPiece(text, value.GetSourcePosition());
    }

    internal void FormatKeywords(EmailKeywordList value)
    {
        if (value.GetCount() == 0)
        {
            AddFatal("Keywords requires at least one keyword.", new EmailSourcePosition(0, 1, 1));
            return;
        }

        int index = 0;
        while (index < value.GetCount() && !stopped)
        {
            EmailKeyword keyword = value.GetAt(index);
            if (keyword.IsMalformed() || !IsSafeText(keyword.GetValue()) || keyword.GetValue().Length == 0)
            {
                AddFatal("A keyword cannot be formatted safely.", keyword.GetSourcePosition());
                break;
            }

            AppendPiece((index == 0 ? "" : ", ") + keyword.GetValue(), keyword.GetSourcePosition());
            index++;
        }
    }

    internal void FormatPriority(EmailPriority value)
    {
        string text;
        if (value.IsXPriority())
        {
            text = value.GetNumericValue().ToString();
        }
        else if (value.GetKind() == EmailPriorityKind.NonUrgent) { text = "non-urgent"; }
        else if (value.GetKind() == EmailPriorityKind.Normal) { text = "normal"; }
        else if (value.GetKind() == EmailPriorityKind.Urgent) { text = "urgent"; }
        else { text = value.GetOriginalValue().ToLowerInvariant(); }
        AppendSafeToken(text, value.GetSourcePosition(), "The priority value cannot be formatted safely.");
    }

    internal void FormatImportance(EmailImportance value)
    {
        string text = value.GetKind() == EmailImportanceKind.Low ? "low" : value.GetKind() == EmailImportanceKind.Normal ? "normal" : value.GetKind() == EmailImportanceKind.High ? "high" : value.GetOriginalValue().ToLowerInvariant();
        AppendSafeToken(text, value.GetSourcePosition(), "The importance value cannot be formatted safely.");
    }

    internal void FormatSensitivity(EmailSensitivity value)
    {
        string text = value.GetKind() == EmailSensitivityKind.Normal ? "normal" : value.GetKind() == EmailSensitivityKind.Personal ? "personal" : value.GetKind() == EmailSensitivityKind.Private ? "private" : value.GetKind() == EmailSensitivityKind.CompanyConfidential ? "company-confidential" : value.GetOriginalValue().ToLowerInvariant();
        AppendSafeToken(text, value.GetSourcePosition(), "The sensitivity value cannot be formatted safely.");
    }

    internal void FormatAutoSubmitted(EmailAutoSubmitted value)
    {
        string token = value.GetNormalizedToken();
        if (!IsToken(token))
        {
            AddFatal("The Auto-Submitted token cannot be formatted safely.", value.GetSourcePosition());
            return;
        }

        Append(token, value.GetSourcePosition());
        AppendParameters(value.GetParameters(), true);
    }

    internal void FormatListManagement(EmailListManagementValue value)
    {
        if (value.IsPostingProhibited())
        {
            Append("NO", value.GetSourcePosition());
            return;
        }

        if (value.GetCommandCount() == 0)
        {
            AddFatal("A list-management value requires at least one command or NO.", value.GetSourcePosition());
            return;
        }

        int index = 0;
        while (index < value.GetCommandCount() && !stopped)
        {
            EmailListCommand command = value.GetCommand(index);
            string uri = command.GetUri();
            if (!IsSafeText(uri) || uri.Length == 0 || uri.IndexOf('>') >= 0)
            {
                AddFatal("A list-management URI cannot be formatted safely.", command.GetSourcePosition());
                break;
            }

            AppendPiece((index == 0 ? "" : ", ") + "<" + uri + ">", command.GetSourcePosition());
            index++;
        }
    }

    internal void FormatListUnsubscribePost(EmailListUnsubscribePost value)
    {
        if (!value.IsOneClick())
        {
            AddFatal("List-Unsubscribe-Post is only canonical when it requests one-click processing.", value.GetSourcePosition());
            return;
        }

        Append("List-Unsubscribe=One-Click", value.GetSourcePosition());
    }

    internal void FormatResentField(EmailResentField value)
    {
        if (value.HasDate())
        {
            EmailDateFormatResult result = new EmailDateFormatter().FormatWithValue(value.GetDate());
            AppendExternal(result.GetBytes(), result.GetIssues(), value.GetSourcePosition());
            return;
        }

        if (value.HasAddresses())
        {
            EmailAddressFormatResult result = new EmailAddressFormatter().FormatListWithList(value.GetAddresses() ?? throw new InvalidOperationException());
            AppendExternal(result.GetBytes(), result.GetIssues(), value.GetSourcePosition());
            return;
        }

        if (value.HasMessageId())
        {
            EmailMessageIdFormatResult result = new EmailMessageIdFormatter().FormatWithIdentifier(value.GetMessageId());
            AppendExternal(result.GetBytes(), result.GetIssues(), value.GetSourcePosition());
            return;
        }

        string text = Encoding.UTF8.GetString(value.GetRawBytes()).Trim();
        if (!IsSafeText(text))
        {
            AddFatal("The resent extension value cannot be formatted safely.", value.GetSourcePosition());
            return;
        }

        AppendPiece(text, value.GetSourcePosition());
    }

    internal EmailValueFormatResult CreateResult()
    {
        if (stopped)
        {
            return new EmailValueFormatResult(new byte[0], issues, 0, options.GetFirstLineColumn());
        }

        return new EmailValueFormatResult(output.ToArray(), issues, foldCount, column);
    }

    void AppendParameters(EmailParameterList value, bool leadingSeparator)
    {
        int index = 0;
        while (index < value.GetCount() && !stopped)
        {
            EmailParameter parameter = value.GetAt(index);
            string name = parameter.GetName().ToLowerInvariant();
            string? parameterValue = FormatParameterValue(parameter.GetValueBytes());
            if (!IsParameterName(name) || parameterValue == null)
            {
                AddFatal("A MIME parameter cannot be formatted safely.", parameter.GetSourcePosition());
                break;
            }

            string assignment = name + "=" + parameterValue;
            if (leadingSeparator || index > 0)
            {
                Append(";", parameter.GetSourcePosition());
                AppendPiece(" " + assignment, parameter.GetSourcePosition());
            }
            else
            {
                AppendPiece(assignment, parameter.GetSourcePosition());
            }

            index++;
        }
    }

    void AppendPiece(string value, EmailSourcePosition position)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        if (column > 0 && column + bytes.Length > options.GetMaximumLineLength())
        {
            Append("\r\n\t", position);
            value = value.TrimStart();
        }

        Append(value, position);
    }

    void Append(string value, EmailSourcePosition position)
    {
        if (stopped)
        {
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(value);
        if ((long)output.Count + bytes.Length > options.GetMaximumOutputBytes())
        {
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.ValueOutputLimitExceeded, "The formatted value exceeds the configured output-byte limit.", position, null, null, "No partial formatted value was returned."));
            stopped = true;
            return;
        }

        int index = 0;
        while (index < bytes.Length)
        {
            byte current = bytes[index];
            output.Add(current);
            if (current == 13 && index + 1 < bytes.Length && bytes[index + 1] == 10)
            {
                output.Add(10);
                column = 0;
                foldCount++;
                index += 2;
                continue;
            }

            column = current == 10 ? 0 : column + 1;
            index++;
        }
    }

    void AddFatal(string description, EmailSourcePosition position)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.ValueFormattingInvalid, description, position, null, null, "No formatted value was returned."));
        stopped = true;
    }

    void FormatStringList(EmailStandardHeaderValue value)
    {
        if (value.GetCount() == 0)
        {
            AddFatal("The structured list requires at least one item.", value.GetSourcePosition());
            return;
        }

        int index = 0;
        while (index < value.GetCount() && !stopped)
        {
            string item = value.GetAt(index);
            if (!IsNewsgroup(item))
            {
                AddFatal("A newsgroup item cannot be formatted safely.", value.GetSourcePosition());
                break;
            }

            AppendPiece((index == 0 ? "" : ", ") + item, value.GetSourcePosition());
            index++;
        }
    }

    void AppendSafeToken(string text, EmailSourcePosition position, string description)
    {
        if (!IsToken(text))
        {
            AddFatal(description, position);
            return;
        }

        Append(text, position);
    }

    void AppendExternal(byte[] bytes, EmailIssueList externalIssues, EmailSourcePosition position)
    {
        issues.AddRange(externalIssues);
        if (externalIssues.HasSeverityAtLeast(EmailIssueSeverity.Error))
        {
            stopped = true;
            return;
        }

        AppendPiece(Encoding.UTF8.GetString(bytes), position);
    }

    static string GetTransferEncodingName(EmailTransferEncodingKind kind)
    {
        if (kind == EmailTransferEncodingKind.SevenBit) { return "7bit"; }
        if (kind == EmailTransferEncodingKind.EightBit) { return "8bit"; }
        if (kind == EmailTransferEncodingKind.Binary) { return "binary"; }
        if (kind == EmailTransferEncodingKind.QuotedPrintable) { return "quoted-printable"; }
        return "base64";
    }

    static bool IsNewsgroup(string value)
    {
        if (value.Length == 0) { return false; }
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current <= 32 || current >= 127 || current == ',' || current == '(' || current == ')' || current == '<' || current == '>') { return false; }
            index++;
        }
        return true;
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

    static string? FormatParameterValue(byte[] value)
    {
        if (!IsSafeBytes(value))
        {
            return null;
        }

        string text = Encoding.ASCII.GetString(value);
        if (text.Length > 0 && IsToken(text))
        {
            return text;
        }

        StringBuilder quoted = new StringBuilder();
        quoted.Append('"');
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '"' || text[index] == '\\')
            {
                quoted.Append('\\');
            }

            quoted.Append(text[index]);
            index++;
        }

        quoted.Append('"');
        return quoted.ToString();
    }

    static bool IsSafeBytes(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < 32 || value[index] > 126)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsParameterName(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (!IsTokenCharacter(current) && current != '*')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsToken(string value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!IsTokenCharacter(value[index]))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsTokenCharacter(char value)
    {
        if (value < 33 || value > 126)
        {
            return false;
        }

        return value != '(' && value != ')' && value != '<' && value != '>' && value != '@' && value != ',' && value != ';' && value != ':' && value != '\\' && value != '"' && value != '/' && value != '[' && value != ']' && value != '?' && value != '=';
    }

    static bool IsLanguage(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            bool alphaNumeric = current >= 'a' && current <= 'z' || current >= '0' && current <= '9';
            if (!alphaNumeric && current != '-')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsSafeText(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\r' || current == '\n' || current == 0)
            {
                return false;
            }

            index++;
        }

        return true;
    }
}
