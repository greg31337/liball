using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailHeaderWriterWorker
{
    EmailHeaders? headers;
    EmailHeaderWriteMode mode;
    EmailHeaderWriteOptions options;
    EmailLineEnding preservedSeparator;
    bool hasPreservedSeparator;
    List<byte> output;
    EmailIssueList issues;
    int copiedHeaderCount;
    int regeneratedHeaderCount;
    int foldCount;
    bool stopped;

    internal EmailHeaderWriterWorker(EmailHeaders? headers, EmailHeaderWriteMode mode, EmailHeaderWriteOptions options, EmailLineEnding preservedSeparator, bool hasPreservedSeparator)
    {
        this.headers = headers;
        this.mode = mode;
        this.options = options;
        this.preservedSeparator = preservedSeparator;
        this.hasPreservedSeparator = hasPreservedSeparator;
        output = new List<byte>();
        issues = new EmailIssueList();
        copiedHeaderCount = 0;
        regeneratedHeaderCount = 0;
        foldCount = 0;
        stopped = false;
    }

    internal EmailHeaderWriteResult Write()
    {
        CheckCardinality();
        int index = 0;
        while (index < (headers ?? throw new InvalidOperationException()).GetCount() && !stopped)
        {
            EmailHeader header = headers.GetAt(index);
            if (mode == EmailHeaderWriteMode.Raw)
            {
                AppendCopied(header.GetRawFieldBytes(), header, header.GetFoldCount());
            }
            else if (mode == EmailHeaderWriteMode.Preserving && !header.IsDirty() && header.HasSourceRange())
            {
                AppendCopied(header.GetOriginalRawFieldBytes(), header, header.GetOriginalFoldCount());
            }
            else
            {
                AppendCanonical(header);
            }

            index++;
        }

        if (!stopped && options.GetIncludeSeparator())
        {
            EmailLineEnding separator = EmailLineEnding.CrLf;
            if (mode != EmailHeaderWriteMode.Canonical)
            {
                separator = hasPreservedSeparator ? preservedSeparator : EmailLineEnding.None;
            }

            if (separator != EmailLineEnding.None)
            {
                AppendLineEnding(separator, null);
            }
        }

        byte[] bytes = stopped ? new byte[0] : output.ToArray();
        return new EmailHeaderWriteResult(mode, bytes, issues, headers.GetCount(), copiedHeaderCount, regeneratedHeaderCount, stopped ? 0 : foldCount);
    }

    void CheckCardinality()
    {
        if (!options.GetCheckCardinality() || mode == EmailHeaderWriteMode.Raw)
        {
            return;
        }

        string[] singletonNames = GetSingletonNames();
        int index = 0;
        while (index < singletonNames.Length)
        {
            int count = (headers ?? throw new InvalidOperationException()).GetCountNamed(singletonNames[index]);
            if (count > 1)
            {
                EmailIssueSeverity severity = mode == EmailHeaderWriteMode.Canonical ? EmailIssueSeverity.Fatal : EmailIssueSeverity.Warning;
                issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.HeaderWriteCardinalityViolation, "The singleton header field '" + singletonNames[index] + "' occurs " + count + " times.", null, null, singletonNames[index], mode == EmailHeaderWriteMode.Canonical ? "Canonical output was rejected." : "Every original occurrence was preserved."));
                if (severity == EmailIssueSeverity.Fatal)
                {
                    stopped = true;
                }
            }

            index++;
        }
    }

    void AppendCopied(byte[] bytes, EmailHeader header, int copiedFoldCount)
    {
        if (!Append(bytes, header.GetValueSourcePosition()))
        {
            return;
        }

        copiedHeaderCount++;
        foldCount += copiedFoldCount;
    }

    void AppendCanonical(EmailHeader header)
    {
        if (!ValidateSafeHeader(header))
        {
            return;
        }

        string fieldName = GetCanonicalFieldName(header.GetName());
        int firstLineColumn = fieldName.Length + 2;
        byte[]? value = FormatValue(header, firstLineColumn);
        if (value == null || stopped)
        {
            return;
        }

        List<byte> field = new List<byte>();
        AddAscii(field, fieldName);
        field.Add(58);
        if (value.Length > 0 && !StartsWithFold(value))
        {
            field.Add(32);
        }

        AddBytes(field, value);
        field.Add(13);
        field.Add(10);
        byte[] bytes = field.ToArray();
        if (!ValidateLineLengths(bytes, header))
        {
            return;
        }

        if (!Append(bytes, header.GetValueSourcePosition()))
        {
            return;
        }

        regeneratedHeaderCount++;
        foldCount += CountFolds(value);
    }

    byte[]? FormatValue(EmailHeader header, int firstLineColumn)
    {
        EmailHeaderValueKind kind = GetEffectiveKind(header);
        if (kind == EmailHeaderValueKind.Unstructured || kind == EmailHeaderValueKind.Keywords)
        {
            return FormatUnstructured(header, kind, firstLineColumn);
        }

        if (HasDedicatedFormatter(kind))
        {
            return FormatStructured(header, kind, firstLineColumn);
        }

        byte[] value = Trim(header.GetUnfoldedValue());
        if (!options.GetAllowInternationalized() && HasNonAscii(value))
        {
            AddFatal(EmailIssueCode.HeaderWriteInvalid, "A field without an encoded-word grammar contains non-ASCII bytes.", header, "Enable internationalized output or replace the field value.");
            return null;
        }

        return FoldGeneric(value, firstLineColumn, header);
    }

    byte[]? FormatUnstructured(EmailHeader header, EmailHeaderValueKind kind, int firstLineColumn)
    {
        EmailHeaderDecodeResult decoded = EmailHeaderDecoder.Decode(header.GetUnfoldedValue(), kind, header.GetParseMode(), header.GetValueSourcePosition(), header.GetParseMode() != EmailStructuredValueParseMode.Strict);
        EmailIssueList decodeIssues = decoded.GetIssues();
        issues.AddRange(decodeIssues);
        if (decodeIssues.HasSeverityAtLeast(EmailIssueSeverity.Error))
        {
            stopped = true;
            return null;
        }

        string text = decoded.GetValue();
        byte[] value = Encoding.UTF8.GetBytes(text);
        if (HasUnsafeTextByte(value))
        {
            AddFatal(EmailIssueCode.HeaderWriteInjectionRejected, "A decoded unstructured value contains an unsafe control character.", header, "Replace the encoded or literal control character before writing.");
            return null;
        }

        if (HasNonAscii(value) && !options.GetAllowInternationalized())
        {
            EmailEncodedWordGenerationOptions wordOptions = new EmailEncodedWordGenerationOptions("utf-8");
            wordOptions.SetContext(EmailEncodedWordContext.UnstructuredText);
            wordOptions.SetEncodingChoice(options.GetEncodedWordEncoding());
            wordOptions.SetFirstLineColumn(firstLineColumn);
            int lineLength = options.GetPreferredLineLength();
            if (lineLength > EmailEncodedWordGenerationOptionsConstants.DefaultMaximumLineLength)
            {
                lineLength = EmailEncodedWordGenerationOptionsConstants.DefaultMaximumLineLength;
            }

            wordOptions.SetMaximumLineLength(lineLength);
            wordOptions.SetMaximumEncodedWords(options.GetMaximumEncodedWords());
            EmailEncodedWordEncodeResult encoded = new EmailEncodedWordGenerator().EncodeText(text, Encoding.UTF8, wordOptions);
            return encoded.GetBytes();
        }

        return FoldGeneric(value, firstLineColumn, header);
    }

    byte[]? FormatStructured(EmailHeader header, EmailHeaderValueKind kind, int firstLineColumn)
    {
        EmailHeaderStructuredResult parsed = EmailHeaderStructuredParser.Parse(kind, header.GetUnfoldedValue(), header.GetParseMode(), header.GetValueSourcePosition(), header.GetName(), header.GetParseMode() != EmailStructuredValueParseMode.Strict);
        EmailIssueList parseIssues = parsed.GetIssues();
        issues.AddRange(parseIssues);
        if (parseIssues.HasSeverityAtLeast(EmailIssueSeverity.Error))
        {
            stopped = true;
            return null;
        }

        object? value = parsed.GetValue();
        if (value == null)
        {
            AddFatal(EmailIssueCode.HeaderWriteInvalid, "The structured header value could not be parsed for canonical output.", header, "Repair or replace the field value before canonical writing.");
            return null;
        }

        EmailValueFormatResult result;
        if (kind == EmailHeaderValueKind.Mailbox)
        {
            result = new EmailValueWriter().WriteAddressWithValueAndOptions((EmailAddress)value, CreateAddressOptions(firstLineColumn));
        }
        else if (kind == EmailHeaderValueKind.AddressGroup)
        {
            result = new EmailValueWriter().WriteAddressGroupWithValueAndOptions((EmailAddressGroup)value, CreateAddressOptions(firstLineColumn));
        }
        else if (kind == EmailHeaderValueKind.AddressList)
        {
            result = new EmailValueWriter().WriteAddressListWithValueAndOptions((EmailAddressList)value, CreateAddressOptions(firstLineColumn));
        }
        else if (kind == EmailHeaderValueKind.Date)
        {
            EmailDateFormatOptions dateOptions = new EmailDateFormatOptions();
            dateOptions.SetMaximumOutputBytes(options.GetMaximumOutputBytes());
            result = new EmailValueWriter().WriteDateWithValueAndOptions((EmailDateTime)value, dateOptions);
        }
        else if (kind == EmailHeaderValueKind.MessageId || kind == EmailHeaderValueKind.ContentId)
        {
            EmailMessageIdFormatOptions identifierOptions = CreateIdentifierOptions(firstLineColumn);
            result = kind == EmailHeaderValueKind.ContentId ? new EmailValueWriter().WriteContentIdWithValueAndOptions((EmailMessageId)value, identifierOptions) : new EmailValueWriter().WriteMessageIdWithValueAndOptions((EmailMessageId)value, identifierOptions);
        }
        else if (kind == EmailHeaderValueKind.References || kind == EmailHeaderValueKind.InReplyTo)
        {
            result = new EmailValueWriter().WriteMessageIdListWithValueAndOptions((EmailMessageIdList)value, CreateIdentifierOptions(firstLineColumn));
        }
        else if (kind == EmailHeaderValueKind.Parameters)
        {
            result = new EmailValueWriter().WriteParametersWithValueAndOptions((EmailParameterList)value, CreateValueOptions(firstLineColumn));
        }
        else if (kind == EmailHeaderValueKind.MediaType)
        {
            result = new EmailValueWriter().WriteMediaTypeWithValueAndOptions((EmailMediaType)value, CreateValueOptions(firstLineColumn));
        }
        else if (kind == EmailHeaderValueKind.Disposition)
        {
            result = new EmailValueWriter().WriteDispositionWithValueAndOptions((EmailDisposition)value, CreateValueOptions(firstLineColumn));
        }
        else if (kind == EmailHeaderValueKind.ContentLanguage)
        {
            result = new EmailValueWriter().WriteContentLanguageWithValueAndOptions((EmailContentLanguage)value, CreateValueOptions(firstLineColumn));
        }
        else if (kind == EmailHeaderValueKind.Received)
        {
            result = new EmailValueWriter().WriteReceivedWithValueAndOptions((EmailReceived)value, CreateValueOptions(firstLineColumn));
        }
        else if (kind == EmailHeaderValueKind.AuthenticationResults)
        {
            result = new EmailValueWriter().WriteAuthenticationResultsWithValueAndOptions((EmailAuthenticationResults)value, CreateAuthenticationOptions(firstLineColumn));
        }
        else if (kind == EmailHeaderValueKind.ReceivedSpf)
        {
            result = new EmailValueWriter().WriteReceivedSpfWithValueAndOptions((EmailReceivedSpf)value, CreateAuthenticationOptions(firstLineColumn));
        }
        else if (kind == EmailHeaderValueKind.DkimSignature || kind == EmailHeaderValueKind.ArcMessageSignature)
        {
            result = kind == EmailHeaderValueKind.DkimSignature ? new EmailValueWriter().WriteDkimSignatureWithValueAndOptions((EmailDkimSignature)value, CreateSignatureOptions(firstLineColumn)) : new EmailValueWriter().WriteArcMessageSignatureWithValueAndOptions((EmailDkimSignature)value, CreateSignatureOptions(firstLineColumn));
        }
        else if (kind == EmailHeaderValueKind.ArcSeal)
        {
            result = new EmailValueWriter().WriteArcSealWithValueAndOptions((EmailArcSeal)value, CreateSignatureOptions(firstLineColumn));
        }
        else
        {
            result = new EmailValueWriter().WriteArcAuthenticationResultsWithValueAndOptions((EmailArcAuthenticationResults)value, CreateSignatureOptions(firstLineColumn));
        }

        issues.AddRange(result.GetIssues());
        if (!result.IsSuccess())
        {
            stopped = true;
            return null;
        }

        return result.GetBytes();
    }

    EmailAddressFormatOptions CreateAddressOptions(int firstLineColumn)
    {
        EmailAddressFormatOptions format = new EmailAddressFormatOptions();
        format.SetFirstLineColumn(firstLineColumn);
        int lineLength = options.GetPreferredLineLength();
        if (lineLength > EmailAddressFormatOptionsConstants.DefaultMaximumLineLength)
        {
            lineLength = EmailAddressFormatOptionsConstants.DefaultMaximumLineLength;
        }

        format.SetMaximumLineLength(lineLength);
        format.SetMaximumEncodedWords(options.GetMaximumEncodedWords());
        format.SetMaximumOutputBytes(options.GetMaximumOutputBytes());
        format.SetEncodedWordEncoding(options.GetEncodedWordEncoding());
        format.SetAllowInternationalized(options.GetAllowInternationalized());
        format.SetDisplayNameFormat(options.GetAllowInternationalized() ? EmailAddressDisplayNameFormat.Unicode : EmailAddressDisplayNameFormat.EncodedWord);
        format.SetDomainFormat(options.GetAllowInternationalized() ? EmailAddressDomainFormat.Unicode : EmailAddressDomainFormat.Ascii);
        return format;
    }

    EmailMessageIdFormatOptions CreateIdentifierOptions(int firstLineColumn)
    {
        EmailMessageIdFormatOptions format = new EmailMessageIdFormatOptions();
        format.SetFirstLineColumn(firstLineColumn);
        format.SetMaximumLineLength(options.GetPreferredLineLength());
        format.SetMaximumOutputBytes(options.GetMaximumOutputBytes());
        return format;
    }

    EmailValueFormatOptions CreateValueOptions(int firstLineColumn)
    {
        EmailValueFormatOptions format = new EmailValueFormatOptions();
        format.SetFirstLineColumn(firstLineColumn);
        format.SetMaximumLineLength(options.GetPreferredLineLength());
        format.SetMaximumOutputBytes(options.GetMaximumOutputBytes());
        return format;
    }

    EmailAuthenticationFormatOptions CreateAuthenticationOptions(int firstLineColumn)
    {
        EmailAuthenticationFormatOptions format = new EmailAuthenticationFormatOptions();
        format.SetFirstLineColumn(firstLineColumn);
        format.SetMaximumLineLength(options.GetPreferredLineLength());
        format.SetMaximumOutputBytes(options.GetMaximumOutputBytes());
        return format;
    }

    EmailSignatureFormatOptions CreateSignatureOptions(int firstLineColumn)
    {
        EmailSignatureFormatOptions format = new EmailSignatureFormatOptions();
        format.SetFirstLineColumn(firstLineColumn);
        format.SetMaximumLineLength(options.GetPreferredLineLength());
        format.SetMaximumOutputBytes(options.GetMaximumOutputBytes());
        return format;
    }

    byte[]? FoldGeneric(byte[] input, int firstLineColumn, EmailHeader header)
    {
        List<byte[]> words = SplitWords(input);
        List<byte> result = new List<byte>();
        int column = firstLineColumn;
        int index = 0;
        while (index < words.Count)
        {
            byte[] word = words[index];
            int separatorLength = result.Count == 0 ? 0 : 1;
            if (result.Count == 0 && column + word.Length > options.GetMaximumLineLength())
            {
                AddFold(result);
                column = 1;
            }
            else if (result.Count > 0 && column + separatorLength + word.Length > options.GetPreferredLineLength())
            {
                AddFold(result);
                column = 1;
                separatorLength = 0;
            }

            if (column + separatorLength + word.Length > options.GetMaximumLineLength())
            {
                AddFatal(EmailIssueCode.HeaderWriteLineLengthExceeded, "A header value contains a token that exceeds the configured hard line length.", header, "Shorten or replace the unbreakable token.");
                return null;
            }

            if (separatorLength > 0)
            {
                result.Add(32);
                column++;
            }

            AddBytes(result, word);
            column += word.Length;
            index++;
        }

        return result.ToArray();
    }

    bool ValidateSafeHeader(EmailHeader header)
    {
        byte[] name = header.GetNameBytes();
        int index = 0;
        if (name.Length == 0)
        {
            AddFatal(EmailIssueCode.HeaderWriteInjectionRejected, "A generated header requires a non-empty field name.", header, "Replace the malformed field name before writing.");
            return false;
        }

        while (index < name.Length)
        {
            if (name[index] < 33 || name[index] > 126 || name[index] == 58)
            {
                AddFatal(EmailIssueCode.HeaderWriteInjectionRejected, "A generated header field name contains an unsafe byte.", header, "Replace the malformed field name before writing.");
                return false;
            }

            index++;
        }

        byte[] value = header.GetRawValue();
        index = 0;
        while (index < value.Length)
        {
            if (value[index] == 0)
            {
                AddFatal(EmailIssueCode.HeaderWriteInjectionRejected, "A generated header value contains a NUL byte.", header, "Remove the NUL byte before writing.");
                return false;
            }

            if (value[index] < 32 && value[index] != 9 && value[index] != 13 && value[index] != 10 || value[index] == 127)
            {
                AddFatal(EmailIssueCode.HeaderWriteInjectionRejected, "A generated header value contains an unsafe control byte.", header, "Remove the control byte before writing.");
                return false;
            }

            if (value[index] == 13)
            {
                if (index + 2 < value.Length && value[index + 1] == 10 && (value[index + 2] == 32 || value[index + 2] == 9))
                {
                    index += 2;
                }
                else if (header.HasSourceRange() && index + 1 < value.Length && (value[index + 1] == 32 || value[index + 1] == 9))
                {
                }
                else
                {
                    AddFatal(EmailIssueCode.HeaderWriteInjectionRejected, "A generated header value contains an unsafe carriage return.", header, "Use only CRLF folding followed by whitespace.");
                    return false;
                }
            }
            else if (value[index] == 10)
            {
                if (!header.HasSourceRange() || index + 1 >= value.Length || value[index + 1] != 32 && value[index + 1] != 9)
                {
                    AddFatal(EmailIssueCode.HeaderWriteInjectionRejected, "A generated header value contains an unsafe line feed.", header, "Use only source folds or CRLF folding followed by whitespace.");
                    return false;
                }
            }

            index++;
        }

        return true;
    }

    bool ValidateLineLengths(byte[] field, EmailHeader header)
    {
        int column = 0;
        int index = 0;
        while (index < field.Length)
        {
            if (field[index] == 13 && index + 1 < field.Length && field[index + 1] == 10)
            {
                column = 0;
                index += 2;
                continue;
            }

            column++;
            if (column > options.GetMaximumLineLength())
            {
                AddFatal(EmailIssueCode.HeaderWriteLineLengthExceeded, "A generated header line exceeds the configured hard line length.", header, "Use a shorter field value or a larger permitted line length.");
                return false;
            }

            index++;
        }

        return true;
    }

    bool Append(byte[] bytes, EmailSourcePosition? position)
    {
        if ((long)output.Count + bytes.Length > options.GetMaximumOutputBytes())
        {
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.HeaderWriteOutputLimitExceeded, "The header output exceeds the configured byte limit.", position, null, null, "No partial output was returned."));
            stopped = true;
            return false;
        }

        AddBytes(output, bytes);
        return true;
    }

    void AppendLineEnding(EmailLineEnding ending, EmailSourcePosition? position)
    {
        List<byte> bytes = new List<byte>();
        if (ending == EmailLineEnding.CrLf || ending == EmailLineEnding.Cr)
        {
            bytes.Add(13);
        }

        if (ending == EmailLineEnding.CrLf || ending == EmailLineEnding.Lf)
        {
            bytes.Add(10);
        }

        Append(bytes.ToArray(), position);
    }

    void AddFatal(EmailIssueCode code, string description, EmailHeader header, string recovery)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, code, description, header.GetValueSourcePosition(), null, SafeFieldName(header), recovery));
        stopped = true;
    }

    string GetCanonicalFieldName(string name)
    {
        if (!options.GetCanonicalizeFieldNames())
        {
            return name;
        }

        int index = 0;
        while (index < new EmailHeaderRegistry().GetCount())
        {
            string registered = new EmailHeaderRegistry().GetNameAt(index);
            if (string.Equals(registered, name, StringComparison.OrdinalIgnoreCase))
            {
                return registered;
            }

            index++;
        }

        return name;
    }

    static EmailHeaderValueKind GetEffectiveKind(EmailHeader header)
    {
        EmailHeaderValueKind registered = new EmailHeaderRegistry().GetValueKind(header.GetName());
        return registered == EmailHeaderValueKind.Unknown ? header.GetValueKind() : registered;
    }

    static bool HasDedicatedFormatter(EmailHeaderValueKind kind)
    {
        return kind >= EmailHeaderValueKind.Mailbox && kind <= EmailHeaderValueKind.Received || kind >= EmailHeaderValueKind.AuthenticationResults && kind <= EmailHeaderValueKind.ArcAuthenticationResults;
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

    static bool StartsWithFold(byte[] value)
    {
        return value.Length >= 2 && value[0] == 13 && value[1] == 10;
    }

    static byte[] Trim(byte[] value)
    {
        int start = 0;
        while (start < value.Length && (value[start] == 32 || value[start] == 9))
        {
            start++;
        }

        int end = value.Length;
        while (end > start && (value[end - 1] == 32 || value[end - 1] == 9))
        {
            end--;
        }

        byte[] result = new byte[end - start];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = value[start + index];
            index++;
        }

        return result;
    }

    static List<byte[]> SplitWords(byte[] value)
    {
        List<byte[]> words = new List<byte[]>();
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length && (value[index] == 32 || value[index] == 9))
            {
                index++;
            }

            int start = index;
            while (index < value.Length && value[index] != 32 && value[index] != 9)
            {
                index++;
            }

            if (index > start)
            {
                byte[] word = new byte[index - start];
                int wordIndex = 0;
                while (wordIndex < word.Length)
                {
                    word[wordIndex] = value[start + wordIndex];
                    wordIndex++;
                }

                words.Add(word);
            }
        }

        return words;
    }

    static void AddFold(List<byte> output)
    {
        output.Add(13);
        output.Add(10);
        output.Add(9);
    }

    static bool HasNonAscii(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static bool HasUnsafeTextByte(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < 32 && value[index] != 9 || value[index] == 127)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static int CountFolds(byte[] value)
    {
        int count = 0;
        int index = 0;
        while (index + 1 < value.Length)
        {
            if (value[index] == 13 && value[index + 1] == 10)
            {
                count++;
                index++;
            }

            index++;
        }

        return count;
    }

    static void AddAscii(List<byte> target, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        AddBytes(target, bytes);
    }

    static void AddBytes(List<byte> target, byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            target.Add(value[index]);
            index++;
        }
    }

    static string[] GetSingletonNames()
    {
        return new string[]
        {
            "Date", "From", "Sender", "Reply-To", "To", "Cc", "Bcc", "Message-ID", "In-Reply-To", "References", "Subject", "Keywords",
            "MIME-Version", "Content-Type", "Content-Transfer-Encoding", "Content-ID", "Content-Description", "Content-Disposition", "Content-Language",
            "Priority", "X-Priority", "Importance", "Sensitivity", "Auto-Submitted", "List-Help", "List-Unsubscribe", "List-Subscribe", "List-Post", "List-Owner", "List-Archive", "List-Unsubscribe-Post"
        };
    }
}
