using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailHeaderValidationWorker
{
    EmailHeaders headers;
    EmailHeaderValidationOptions options;
    EmailIssueList issues;
    bool requiresSmtpUtf8;
    int fromMailboxCount;
    int senderCount;
    int resentBlockCount;

    internal EmailHeaderValidationWorker(EmailHeaders headers, EmailHeaderValidationOptions options)
    {
        this.headers = headers;
        this.options = options;
        issues = new EmailIssueList();
        requiresSmtpUtf8 = false;
        fromMailboxCount = -1;
        senderCount = 0;
        resentBlockCount = 0;
    }

    internal EmailHeaderValidationResult Validate()
    {
        int index = 0;
        while (index < headers.GetCount())
        {
            ValidateHeader(headers.GetAt(index));
            index++;
        }

        ValidateCardinality();
        ValidateRequiredFields();
        ValidateFromSenderRelationship();
        ValidateResentBlocks();
        return new EmailHeaderValidationResult(issues, headers.GetCount(), resentBlockCount, requiresSmtpUtf8);
    }

    void ValidateHeader(EmailHeader header)
    {
        string name = header.GetName();
        ValidateName(header);
        ValidateRawValue(header);
        ValidateLineLengths(header);
        ValidateInternationalization(header);
        ValidateContext(header);
        ValidateBccPolicy(header);
        if (string.Equals(name, "MIME-Version", StringComparison.OrdinalIgnoreCase))
        {
            ValidateMimeVersion(header);
        }
        else if (string.Equals(name, "Content-Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
        {
            ValidateTransferEncoding(header);
        }

        object? value = null;
        if (options.GetValidateStructuredValues())
        {
            value = ValidateStructuredValue(header);
        }

        if (string.Equals(name, "From", StringComparison.OrdinalIgnoreCase))
        {
            if (value != null)
            {
                fromMailboxCount = ((EmailAddressList)value).GetMailboxCount();
            }
            else
            {
                fromMailboxCount = GetMailboxCount(header);
            }
        }

        if (string.Equals(name, "Sender", StringComparison.OrdinalIgnoreCase))
        {
            senderCount++;
        }
    }

    void ValidateName(EmailHeader header)
    {
        byte[] name = header.GetNameBytes();
        bool valid = name.Length > 0;
        int index = 0;
        while (index < name.Length)
        {
            if (name[index] < 33 || name[index] > 126 || name[index] == 58)
            {
                valid = false;
                break;
            }

            index++;
        }

        if (!valid)
        {
            AddError(EmailIssueCode.HeaderValidationInvalidName, "The header field name is empty or contains an invalid byte.", header, "Replace the field name with printable ASCII field-name characters.");
        }
    }

    void ValidateRawValue(EmailHeader header)
    {
        if (header.GetLineEnding() == EmailLineEnding.Cr || header.GetLineEnding() == EmailLineEnding.Lf)
        {
            AddError(EmailIssueCode.HeaderValidationInvalidValue, "The header field ends with a bare CR or LF line ending.", header, "Use CRLF for a conforming header field.");
        }

        byte[] value = header.GetRawValue();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == 0)
            {
                AddError(EmailIssueCode.HeaderValidationInvalidValue, "The header value contains a NUL byte.", header, "Remove the NUL byte.");
                return;
            }

            if (!options.GetAllowObsoleteSyntax() && (value[index] < 32 && value[index] != 9 && value[index] != 13 && value[index] != 10 || value[index] == 127))
            {
                AddError(EmailIssueCode.HeaderValidationInvalidValue, "The header value contains an obsolete control byte.", header, "Remove the control byte or explicitly allow obsolete syntax.");
                return;
            }

            if (value[index] == 13)
            {
                if (index + 2 >= value.Length || value[index + 1] != 10 || value[index + 2] != 32 && value[index + 2] != 9)
                {
                    AddError(EmailIssueCode.HeaderValidationInvalidValue, "The header value contains a carriage return outside legal folding white space.", header, "Use CRLF followed by space or tab for folding.");
                    return;
                }

                index += 2;
            }
            else if (value[index] == 10)
            {
                AddError(EmailIssueCode.HeaderValidationInvalidValue, "The header value contains a bare line feed.", header, "Use CRLF followed by space or tab for folding.");
                return;
            }

            index++;
        }
    }

    void ValidateLineLengths(EmailHeader header)
    {
        byte[] field = header.GetRawFieldBytes();
        int column = 0;
        int index = 0;
        while (index < field.Length)
        {
            if (field[index] == 13)
            {
                if (column > options.GetMaximumLineLength())
                {
                    AddLineLengthIssue(header);
                    return;
                }

                column = 0;
                if (index + 1 < field.Length && field[index + 1] == 10)
                {
                    index++;
                }
            }
            else if (field[index] == 10)
            {
                if (column > options.GetMaximumLineLength())
                {
                    AddLineLengthIssue(header);
                    return;
                }

                column = 0;
            }
            else
            {
                column++;
            }

            index++;
        }

        if (column > options.GetMaximumLineLength())
        {
            AddLineLengthIssue(header);
        }
    }

    void AddLineLengthIssue(EmailHeader header)
    {
        AddError(EmailIssueCode.HeaderValidationLineTooLong, "A physical header line exceeds " + options.GetMaximumLineLength() + " bytes.", header, "Refold or shorten the field value.");
    }

    void ValidateInternationalization(EmailHeader header)
    {
        byte[] value = header.GetRawValue();
        if (!EmailHeaderUtf8.ContainsNonAscii(value))
        {
            return;
        }

        requiresSmtpUtf8 = true;
        int invalid = EmailHeaderUtf8.GetInvalidIndex(value);
        if (invalid >= 0)
        {
            AddError(EmailIssueCode.HeaderValidationInvalidUtf8, "The internationalized header value contains invalid UTF-8.", header, "Replace the invalid byte sequence.");
        }
        else if (!options.GetAllowSmtpUtf8())
        {
            AddError(EmailIssueCode.HeaderValidationSmtpUtf8Required, "The header value requires SMTPUTF8 transport.", header, "Enable SMTPUTF8 policy or encode the value as permitted ASCII syntax.");
        }
    }

    void ValidateContext(EmailHeader header)
    {
        if (options.GetContext() == EmailHeaderValidationContext.MimeEntity && IsMessageOnlyField(header.GetName()))
        {
            AddError(EmailIssueCode.HeaderValidationContext, "The field is only valid in a message header block, not a MIME body-part header block.", header, "Move or remove the field from the MIME entity.");
        }
    }

    void ValidateBccPolicy(EmailHeader header)
    {
        if (!options.GetRequireBccStripped())
        {
            return;
        }

        string name = header.GetName();
        if (string.Equals(name, "Bcc", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Resent-Bcc", StringComparison.OrdinalIgnoreCase))
        {
            AddError(EmailIssueCode.HeaderValidationBccPolicy, "The header block retains a blind-recipient field that policy requires to be stripped.", header, "Remove the Bcc or Resent-Bcc field before transport.");
        }
    }

    object? ValidateStructuredValue(EmailHeader header)
    {
        EmailHeaderValueKind kind = new EmailHeaderRegistry().GetValueKind(header.GetName());
        if (kind == EmailHeaderValueKind.Unknown)
        {
            kind = header.GetValueKind();
        }

        if (kind == EmailHeaderValueKind.Unknown)
        {
            return null;
        }

        if (kind == EmailHeaderValueKind.Unstructured)
        {
            EmailHeaderDecodeResult decoded = EmailHeaderDecoder.Decode(header.GetUnfoldedValue(), kind, EmailStructuredValueParseMode.Strict, header.GetValueSourcePosition(), false);
            AddStructuredIssues(decoded.GetIssues());
            return decoded.GetValue();
        }

        EmailHeaderStructuredResult parsed = EmailHeaderStructuredParser.Parse(kind, header.GetUnfoldedValue(), EmailStructuredValueParseMode.Strict, header.GetValueSourcePosition(), header.GetName(), false);
        AddStructuredIssues(parsed.GetIssues());
        return parsed.GetValue();
    }

    void AddStructuredIssues(EmailIssueList additions)
    {
        int index = 0;
        while (index < additions.GetCount())
        {
            EmailIssue issue = additions.GetAt(index);
            if (!options.GetAllowObsoleteSyntax() || !IsObsoleteCode(issue.GetCode()))
            {
                issues.Add(issue);
            }

            index++;
        }
    }

    void ValidateMimeVersion(EmailHeader header)
    {
        byte[]? value = GetSingleLexicalToken(header);
        if (value == null || !string.Equals(Encoding.ASCII.GetString(value), "1.0", StringComparison.Ordinal))
        {
            AddError(EmailIssueCode.HeaderValidationMimeValue, "MIME-Version must have the value 1.0.", header, "Replace the value with '1.0'.");
        }
    }

    void ValidateTransferEncoding(EmailHeader header)
    {
        byte[]? value = GetSingleLexicalToken(header);
        if (value == null || !IsMimeToken(value))
        {
            AddError(EmailIssueCode.HeaderValidationMimeValue, "Content-Transfer-Encoding must contain one MIME token.", header, "Replace the value with a registered or extension transfer-encoding token.");
        }
    }

    byte[]? GetSingleLexicalToken(EmailHeader header)
    {
        EmailHeaderLexicalOptions lexicalOptions = new EmailHeaderLexicalOptions();
        lexicalOptions.SetMode(EmailHeaderLexicalMode.Strict);
        lexicalOptions.SetAllowInternationalized(false);
        EmailHeaderLexicalResult result = new EmailHeaderLexicalScanner().ScanWithInputAndOptionsAndInitialPosition(header.GetUnfoldedValue(), lexicalOptions, header.GetValueSourcePosition());
        AddStructuredIssues(result.GetIssues());
        EmailHeaderToken? selected = null;
        int significantCount = 0;
        int index = 0;
        while (index < result.GetTokenCount())
        {
            EmailHeaderToken token = result.GetToken(index);
            if (token.GetKind() != EmailHeaderTokenKind.Cfws)
            {
                selected = token;
                significantCount++;
            }

            index++;
        }

        if (significantCount != 1 || selected == null || selected.IsMalformed() || selected.GetKind() == EmailHeaderTokenKind.Invalid)
        {
            return null;
        }

        return selected.GetValueBytes();
    }

    void ValidateCardinality()
    {
        string[] singletons = GetSingletonNames();
        int index = 0;
        while (index < singletons.Length)
        {
            int count = headers.GetCountNamed(singletons[index]);
            if (count > 1)
            {
                EmailHeader? first = headers.GetFirst(singletons[index]);
                issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.HeaderValidationCardinality, "The singleton field '" + singletons[index] + "' occurs " + count + " times.", (first ?? throw new InvalidOperationException()).GetValueSourcePosition(), null, singletons[index], "Remove all but one occurrence."));
            }

            index++;
        }
    }

    void ValidateRequiredFields()
    {
        if (options.GetContext() != EmailHeaderValidationContext.Message)
        {
            return;
        }

        if (options.GetRequireDate() && !headers.Contains("Date"))
        {
            AddMissing("Date");
        }

        if (options.GetRequireFrom() && !headers.Contains("From"))
        {
            AddMissing("From");
        }

        if (options.GetRequireMessageId() && !headers.Contains("Message-ID"))
        {
            AddMissing("Message-ID");
        }
    }

    void AddMissing(string name)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.HeaderValidationRequiredFieldMissing, "The required field '" + name + "' is missing.", null, null, name, "Add exactly one valid field."));
    }

    void ValidateFromSenderRelationship()
    {
        if (fromMailboxCount > 1 && senderCount != 1)
        {
            AddRelationshipError("Sender", "A message with multiple From mailboxes requires exactly one Sender field.");
        }
        else if (fromMailboxCount <= 1 && senderCount > 0)
        {
            AddRelationshipError("Sender", "Sender must not be used when From contains exactly one mailbox.");
        }
    }

    void AddRelationshipError(string fieldName, string description)
    {
        EmailHeader? header = headers.GetFirst(fieldName);
        EmailSourcePosition? position = header == null ? null : header.GetValueSourcePosition();
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.HeaderValidationCardinality, description, position, null, fieldName, "Make the Sender and From fields consistent."));
    }

    void ValidateResentBlocks()
    {
        EmailResentValidationBlock? block = null;
        bool seenResent = false;
        bool leftResentRegion = false;
        int index = 0;
        while (index < headers.GetCount())
        {
            EmailHeader header = headers.GetAt(index);
            string name = header.GetName();
            if (name.StartsWith("Resent-", StringComparison.OrdinalIgnoreCase))
            {
                if (leftResentRegion)
                {
                    AddError(EmailIssueCode.HeaderValidationResentBlock, "Resent fields must form one contiguous region of complete resent blocks.", header, "Move every resent field into the contiguous resent region.");
                }

                if (string.Equals(name, "Resent-Date", StringComparison.OrdinalIgnoreCase) && block != null && block.GetFieldCount() > 0)
                {
                    block.Validate(issues);
                    resentBlockCount++;
                    block = null;
                }

                if (block == null)
                {
                    block = new EmailResentValidationBlock(options.GetAllowObsoleteSyntax());
                }

                block.Add(header);
                seenResent = true;
            }
            else if (seenResent)
            {
                leftResentRegion = true;
            }

            index++;
        }

        if (block != null)
        {
            block.Validate(issues);
            resentBlockCount++;
        }
    }

    void AddError(EmailIssueCode code, string description, EmailHeader header, string recovery)
    {
        string? fieldName = SafeFieldName(header);
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, code, description, header.GetValueSourcePosition(), null, fieldName, recovery));
    }

    static bool IsMessageOnlyField(string name)
    {
        if (name.StartsWith("Resent-", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string[] names = new string[]
        {
            "Return-Path", "Received", "Date", "From", "Sender", "Reply-To", "To", "Cc", "Bcc", "Message-ID", "In-Reply-To", "References", "Subject", "Comments", "Keywords", "MIME-Version",
            "Authentication-Results", "Original-Authentication-Results", "Received-SPF", "DKIM-Signature", "ARC-Message-Signature", "ARC-Seal", "ARC-Authentication-Results",
            "Priority", "X-Priority", "Importance", "Sensitivity", "Auto-Submitted", "List-Help", "List-Unsubscribe", "List-Subscribe", "List-Post", "List-Owner", "List-Archive", "List-Unsubscribe-Post"
        };
        int index = 0;
        while (index < names.Length)
        {
            if (string.Equals(name, names[index], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static bool IsObsoleteCode(EmailIssueCode code)
    {
        return code == EmailIssueCode.HeaderLexicalObsolete || code == EmailIssueCode.AddressObsoleteSyntax || code == EmailIssueCode.DateObsoleteSyntax || code == EmailIssueCode.MessageIdObsoleteSyntax || code == EmailIssueCode.ResentFieldObsolete;
    }

    static string? SafeFieldName(EmailHeader header)
    {
        byte[] name = header.GetNameBytes();
        int index = 0;
        while (index < name.Length)
        {
            if (name[index] < 33 || name[index] > 126 || name[index] == 58)
            {
                return null;
            }

            index++;
        }

        return name.Length == 0 ? null : Encoding.ASCII.GetString(name);
    }

    static int GetMailboxCount(EmailHeader header)
    {
        EmailHeaderStructuredResult parsed = EmailHeaderStructuredParser.Parse(EmailHeaderValueKind.AddressList, header.GetUnfoldedValue(), EmailStructuredValueParseMode.Strict, header.GetValueSourcePosition(), header.GetName(), false);
        EmailAddressList? value = (EmailAddressList?)parsed.GetValue();
        if (value == null)
        {
            return -1;
        }

        return value.GetMailboxCount();
    }

    static bool IsMimeToken(byte[] value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            byte current = value[index];
            if (current < 33 || current > 126 || current == 40 || current == 41 || current == 60 || current == 62 || current == 64 || current == 44 || current == 59 || current == 58 || current == 92 || current == 34 || current == 47 || current == 91 || current == 93 || current == 63 || current == 61)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static string[] GetSingletonNames()
    {
        return new string[]
        {
            "Return-Path", "Date", "From", "Sender", "Reply-To", "To", "Cc", "Bcc", "Message-ID", "In-Reply-To", "References", "Subject",
            "MIME-Version", "Content-Type", "Content-Transfer-Encoding", "Content-ID", "Content-Description", "Content-Disposition", "Content-Language",
            "Priority", "X-Priority", "Importance", "Sensitivity", "Auto-Submitted", "List-Help", "List-Unsubscribe", "List-Subscribe", "List-Post", "List-Owner", "List-Archive", "List-Unsubscribe-Post"
        };
    }
}
internal class EmailResentValidationBlock
{
    bool allowObsoleteSyntax;
    int fieldCount;
    int dateCount;
    int fromCount;
    int senderCount;
    int toCount;
    int ccCount;
    int bccCount;
    int messageIdCount;
    int replyToCount;
    int fromMailboxCount;
    int lastRank;
    EmailHeader? firstHeader;
    EmailHeader? senderHeader;
    bool orderInvalid;

    internal EmailResentValidationBlock(bool allowObsoleteSyntax)
    {
        this.allowObsoleteSyntax = allowObsoleteSyntax;
        fieldCount = 0;
        dateCount = 0;
        fromCount = 0;
        senderCount = 0;
        toCount = 0;
        ccCount = 0;
        bccCount = 0;
        messageIdCount = 0;
        replyToCount = 0;
        fromMailboxCount = -1;
        lastRank = -1;
        firstHeader = null;
        senderHeader = null;
        orderInvalid = false;
    }

    internal int GetFieldCount()
    {
        return fieldCount;
    }

    internal void Add(EmailHeader header)
    {
        if (firstHeader == null)
        {
            firstHeader = header;
        }

        fieldCount++;
        string name = header.GetName();
        int rank = GetRank(name);
        if (rank >= 0 && rank < lastRank)
        {
            orderInvalid = true;
        }

        if (rank >= 0)
        {
            lastRank = rank;
        }

        if (string.Equals(name, "Resent-Date", StringComparison.OrdinalIgnoreCase))
        {
            dateCount++;
        }
        else if (string.Equals(name, "Resent-From", StringComparison.OrdinalIgnoreCase))
        {
            fromCount++;
            fromMailboxCount = GetMailboxCount(header);
        }
        else if (string.Equals(name, "Resent-Sender", StringComparison.OrdinalIgnoreCase))
        {
            senderCount++;
            senderHeader = header;
        }
        else if (string.Equals(name, "Resent-To", StringComparison.OrdinalIgnoreCase))
        {
            toCount++;
        }
        else if (string.Equals(name, "Resent-Cc", StringComparison.OrdinalIgnoreCase))
        {
            ccCount++;
        }
        else if (string.Equals(name, "Resent-Bcc", StringComparison.OrdinalIgnoreCase))
        {
            bccCount++;
        }
        else if (string.Equals(name, "Resent-Message-ID", StringComparison.OrdinalIgnoreCase))
        {
            messageIdCount++;
        }
        else if (string.Equals(name, "Resent-Reply-To", StringComparison.OrdinalIgnoreCase))
        {
            replyToCount++;
        }
    }

    internal void Validate(EmailIssueList issues)
    {
        if (dateCount != 1 || fromCount != 1)
        {
            AddIssue(issues, "Each resent block requires exactly one Resent-Date and one Resent-From field.", firstHeader, "Add the missing fields or split merged resent blocks.");
        }

        if (senderCount > 1 || toCount > 1 || ccCount > 1 || bccCount > 1 || messageIdCount > 1 || replyToCount > 1 || fromCount > 1)
        {
            AddIssue(issues, "A resent block contains a duplicated singleton resent field.", firstHeader, "Remove duplicate fields from the block.");
        }

        if (orderInvalid)
        {
            AddIssue(issues, "The resent fields are not in RFC block order.", firstHeader, "Order the block as Date, From, Sender, recipients, and Message-ID.");
        }

        if (fromMailboxCount > 1 && senderCount != 1)
        {
            AddIssue(issues, "Multiple Resent-From mailboxes require exactly one Resent-Sender.", firstHeader, "Add one Resent-Sender mailbox.");
        }
        else if (fromMailboxCount == 1 && senderCount > 0)
        {
            AddIssue(issues, "Resent-Sender must not be used with exactly one Resent-From mailbox.", senderHeader, "Remove Resent-Sender.");
        }

        if (replyToCount > 0 && !allowObsoleteSyntax)
        {
            AddIssue(issues, "Resent-Reply-To is obsolete syntax.", firstHeader, "Remove the obsolete field or explicitly allow obsolete syntax.");
        }
    }

    static int GetMailboxCount(EmailHeader header)
    {
        EmailHeaderStructuredResult parsed = EmailHeaderStructuredParser.Parse(EmailHeaderValueKind.ResentField, header.GetUnfoldedValue(), EmailStructuredValueParseMode.Strict, header.GetValueSourcePosition(), header.GetName(), false);
        EmailResentField? value = (EmailResentField?)parsed.GetValue();
        if (value == null || !value.HasAddresses())
        {
            return -1;
        }

        return (value.GetAddresses() ?? throw new InvalidOperationException()).GetMailboxCount();
    }

    static int GetRank(string name)
    {
        if (string.Equals(name, "Resent-Date", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (string.Equals(name, "Resent-From", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Resent-Reply-To", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (string.Equals(name, "Resent-Sender", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (string.Equals(name, "Resent-To", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (string.Equals(name, "Resent-Cc", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (string.Equals(name, "Resent-Bcc", StringComparison.OrdinalIgnoreCase))
        {
            return 5;
        }

        if (string.Equals(name, "Resent-Message-ID", StringComparison.OrdinalIgnoreCase))
        {
            return 6;
        }

        return -1;
    }

    static void AddIssue(EmailIssueList issues, string description, EmailHeader? header, string recovery)
    {
        EmailSourcePosition? position = header == null ? null : header.GetValueSourcePosition();
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.HeaderValidationResentBlock, description, position, null, null, recovery));
    }
}
