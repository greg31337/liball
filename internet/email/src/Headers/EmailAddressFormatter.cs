using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailAddressFormatterWorker
{
    EmailAddressFormatOptions options;
    List<byte> output;
    EmailIssueList issues;
    int column;
    int foldCount;
    bool stopped;

    internal EmailAddressFormatterWorker(EmailAddressFormatOptions options)
    {
        this.options = options;
        output = new List<byte>();
        issues = new EmailIssueList();
        column = options.GetFirstLineColumn();
        foldCount = 0;
        stopped = false;
    }

    internal void FormatList(EmailAddressList list)
    {
        int index = 0;
        while (index < list.GetCount() && !stopped)
        {
            if (index > 0)
            {
                AppendAscii(", ", GetEntryPosition(list.GetAt(index)));
            }

            EmailAddressListEntry entry = list.GetAt(index);
            if (entry.IsMailbox())
            {
                FormatMailbox(entry.GetMailbox());
            }
            else
            {
                FormatGroup(entry.GetGroup());
            }

            index++;
        }
    }

    internal void FormatGroup(EmailAddressGroup? group)
    {
        if (stopped)
        {
            return;
        }

        EmailSourcePosition position = (group ?? throw new InvalidOperationException()).GetSourcePosition();
        FormatDisplayName(group.GetName(), position);
        if (stopped)
        {
            return;
        }

        AppendAscii(":", position);
        if (group.GetAddressCount() > 0)
        {
            AppendAscii(" ", position);
        }

        int index = 0;
        while (index < group.GetAddressCount() && !stopped)
        {
            if (index > 0)
            {
                AppendAscii(", ", group.GetAddress(index).GetSourcePosition());
            }

            FormatMailbox(group.GetAddress(index));
            index++;
        }

        if (!stopped)
        {
            AppendAscii(";", position);
        }
    }

    internal void FormatMailbox(EmailAddress? address)
    {
        if (stopped)
        {
            return;
        }

        EmailSourcePosition position = (address ?? throw new InvalidOperationException()).GetSourcePosition();
        if (address.HasDisplayName())
        {
            FormatDisplayName(address.GetDisplayName(), position);
            if (stopped)
            {
                return;
            }

            AppendAscii(" <", position);
            FormatAddrSpec(address);
            if (!stopped)
            {
                AppendAscii(">", position);
            }
        }
        else
        {
            FormatAddrSpec(address);
        }
    }

    internal EmailAddressFormatResult CreateResult()
    {
        if (stopped)
        {
            return new EmailAddressFormatResult(new byte[0], issues, 0, options.GetFirstLineColumn());
        }

        return new EmailAddressFormatResult(output.ToArray(), issues, foldCount, column);
    }

    void FormatAddrSpec(EmailAddress address)
    {
        FormatLocalPart(address.GetLocalPart(), address.GetSourcePosition());
        if (stopped)
        {
            return;
        }

        AppendAscii("@", address.GetSourcePosition());
        FormatDomain(address.GetDomain(), address.IsDomainLiteral(), address.GetSourcePosition());
    }

    void FormatDisplayName(string? value, EmailSourcePosition position)
    {
        if (!ValidateText(value, "A display name", position))
        {
            return;
        }

        if ((value ?? throw new InvalidOperationException()).Length == 0)
        {
            AddFatal(EmailIssueCode.AddressFormattingInvalid, "A display name cannot be empty.", position, "No address bytes were generated.");
            return;
        }

        bool internationalized = EmailAddressFormattingText.ContainsNonAscii(value);
        if (internationalized && options.GetDisplayNameFormat() == EmailAddressDisplayNameFormat.EncodedWord)
        {
            EmailEncodedWordGenerationOptions encodedOptions = new EmailEncodedWordGenerationOptions("utf-8");
            encodedOptions.SetContext(EmailEncodedWordContext.Phrase);
            encodedOptions.SetEncodingChoice(options.GetEncodedWordEncoding());
            encodedOptions.SetFirstLineColumn(column);
            encodedOptions.SetMaximumLineLength(options.GetMaximumLineLength());
            encodedOptions.SetMaximumEncodedWords(options.GetMaximumEncodedWords());
            EmailEncodedWordEncodeResult encoded = new EmailEncodedWord().EncodeText(value, Encoding.UTF8, encodedOptions);
            AppendBytes(encoded.GetBytes(), position);
            return;
        }

        if (internationalized && !options.GetAllowInternationalized())
        {
            AddFatal(EmailIssueCode.AddressInternationalizationRequired, "The selected display-name format requires an internationalized header.", position, "Enable internationalized formatting or use encoded words.");
            return;
        }

        string normalized = NormalizePhraseSpaces(value);
        if (IsPhrase(normalized, options.GetAllowInternationalized()))
        {
            AppendUtf8(normalized, position);
        }
        else
        {
            AppendQuotedString(value, position);
        }
    }

    void FormatLocalPart(string value, EmailSourcePosition position)
    {
        if (!ValidateText(value, "A local part", position))
        {
            return;
        }

        if (value.Length == 0)
        {
            AddFatal(EmailIssueCode.AddressFormattingInvalid, "A local part cannot be empty.", position, "No address bytes were generated.");
            return;
        }

        bool internationalized = EmailAddressFormattingText.ContainsNonAscii(value);
        if (internationalized && !options.GetAllowInternationalized())
        {
            AddFatal(EmailIssueCode.AddressInternationalizationRequired, "The local part requires internationalized address syntax.", position, "Enable internationalized formatting before generating this mailbox.");
            return;
        }

        if (IsDotAtom(value, options.GetAllowInternationalized()))
        {
            AppendUtf8(value, position);
        }
        else
        {
            AppendQuotedString(value, position);
        }
    }

    void FormatDomain(string value, bool domainLiteral, EmailSourcePosition position)
    {
        if (!ValidateText(value, "A domain", position))
        {
            return;
        }

        if (value.Length == 0)
        {
            AddFatal(EmailIssueCode.AddressFormattingInvalid, "A domain cannot be empty.", position, "No address bytes were generated.");
            return;
        }

        if (domainLiteral)
        {
            if (!IsDomainLiteralText(value))
            {
                AddFatal(EmailIssueCode.AddressFormattingInvalid, "A domain literal contains a character that cannot be generated canonically.", position, "No address bytes were generated.");
                return;
            }

            if (EmailAddressFormattingText.ContainsNonAscii(value) && !options.GetAllowInternationalized())
            {
                AddFatal(EmailIssueCode.AddressInternationalizationRequired, "The domain literal requires internationalized address syntax.", position, "Enable internationalized formatting before generating this mailbox.");
                return;
            }

            AppendAscii("[", position);
            AppendUtf8(value, position);
            AppendAscii("]", position);
            return;
        }

        if (!IsSafeDomain(value))
        {
            AddFatal(EmailIssueCode.AddressFormattingInvalid, "The domain contains an unsafe or invalid address character.", position, "No address bytes were generated.");
            return;
        }

        string formatted = options.GetDomainFormat() == EmailAddressDomainFormat.Ascii ? new EmailAddressIdn().ToAscii(value) : new EmailAddressIdn().ToUnicode(value);
        if (EmailAddressFormattingText.ContainsNonAscii(formatted) && !options.GetAllowInternationalized())
        {
            AddFatal(EmailIssueCode.AddressInternationalizationRequired, "The selected domain format requires internationalized address syntax.", position, "Select ASCII IDNA formatting or enable internationalized formatting.");
            return;
        }

        AppendUtf8(formatted, position);
    }

    bool ValidateText(string? value, string component, EmailSourcePosition position)
    {
        if (value == null || !EmailAddressFormattingText.IsValidUnicode(value))
        {
            AddFatal(EmailIssueCode.AddressFormattingInvalid, component + " must contain valid Unicode scalar values.", position, "No address bytes were generated.");
            return false;
        }

        if (EmailAddressFormattingText.ContainsInjectionOrControl(value))
        {
            AddFatal(EmailIssueCode.AddressInjectionRejected, component + " contains a control character or header-injection sequence.", position, "No address bytes were generated.");
            return false;
        }

        return true;
    }

    void AppendQuotedString(string value, EmailSourcePosition position)
    {
        AppendAscii("\"", position);
        int index = 0;
        while (index < value.Length && !stopped)
        {
            char character = value[index];
            if (character == '\\' || character == '"')
            {
                AppendAscii("\\", position);
            }

            int length = 1;
            if (char.IsHighSurrogate(character))
            {
                length = 2;
            }

            AppendUtf8(value.Substring(index, length), position);
            index += length;
        }

        if (!stopped)
        {
            AppendAscii("\"", position);
        }
    }

    void AppendAscii(string value, EmailSourcePosition position)
    {
        AppendBytes(Encoding.ASCII.GetBytes(value), position);
    }

    void AppendUtf8(string value, EmailSourcePosition position)
    {
        AppendBytes(Encoding.UTF8.GetBytes(value), position);
    }

    void AppendBytes(byte[] value, EmailSourcePosition position)
    {
        if (stopped)
        {
            return;
        }

        long nextLength = (long)output.Count + value.Length;
        if (nextLength > options.GetMaximumOutputBytes())
        {
            AddFatal(EmailIssueCode.AddressOutputLimitExceeded, "The formatted address value exceeds the configured output-byte limit.", position, "No partial address value was returned.");
            return;
        }

        int index = 0;
        while (index < value.Length)
        {
            byte current = value[index];
            output.Add(current);
            if (current == 13 && index + 1 < value.Length && value[index + 1] == 10)
            {
                output.Add(10);
                column = 0;
                foldCount++;
                index += 2;
                continue;
            }

            if (current == 10)
            {
                column = 0;
            }
            else
            {
                column++;
            }

            index++;
        }
    }

    void AddFatal(EmailIssueCode code, string description, EmailSourcePosition position, string recovery)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, code, description, position, null, null, recovery));
        stopped = true;
    }

    static EmailSourcePosition GetEntryPosition(EmailAddressListEntry entry)
    {
        if (entry.IsMailbox())
        {
            return (entry.GetMailbox() ?? throw new InvalidOperationException()).GetSourcePosition();
        }

        return (entry.GetGroup() ?? throw new InvalidOperationException()).GetSourcePosition();
    }

    static string NormalizePhraseSpaces(string value)
    {
        StringBuilder normalized = new StringBuilder();
        bool pendingSpace = false;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == ' ')
            {
                if (normalized.Length > 0)
                {
                    pendingSpace = true;
                }
            }
            else
            {
                if (pendingSpace)
                {
                    normalized.Append(' ');
                    pendingSpace = false;
                }

                normalized.Append(value[index]);
            }

            index++;
        }

        return normalized.ToString();
    }

    static bool IsPhrase(string value, bool allowInternationalized)
    {
        if (value.Length == 0 || value[0] == ' ' || value[value.Length - 1] == ' ')
        {
            return false;
        }

        int introducer = 0;
        while (introducer + 1 < value.Length)
        {
            if (value[introducer] == '=' && value[introducer + 1] == '?')
            {
                return false;
            }

            introducer++;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == ' ')
            {
                index++;
                continue;
            }

            if (character <= 127)
            {
                if (!new EmailByteScanner().IsAtext((byte)character))
                {
                    return false;
                }

                index++;
                continue;
            }

            if (!allowInternationalized)
            {
                return false;
            }

            if (char.IsHighSurrogate(character))
            {
                index += 2;
            }
            else
            {
                index++;
            }
        }

        return true;
    }

    static bool IsDotAtom(string value, bool allowInternationalized)
    {
        if (value.Length == 0 || value[0] == '.' || value[value.Length - 1] == '.')
        {
            return false;
        }

        bool previousDot = false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '.')
            {
                if (previousDot)
                {
                    return false;
                }

                previousDot = true;
                index++;
                continue;
            }

            previousDot = false;
            if (character <= 127)
            {
                if (!new EmailByteScanner().IsAtext((byte)character))
                {
                    return false;
                }

                index++;
                continue;
            }

            if (!allowInternationalized)
            {
                return false;
            }

            if (char.IsHighSurrogate(character))
            {
                index += 2;
            }
            else
            {
                index++;
            }
        }

        return true;
    }

    static bool IsDomainLiteralText(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '[' || character == ']' || character == '\\')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsSafeDomain(string value)
    {
        int labelLength = 0;
        bool lastHyphen = false;
        bool sawLabel = false;
        bool ascii = true;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '.')
            {
                if (labelLength == 0 || lastHyphen)
                {
                    return false;
                }

                sawLabel = true;
                labelLength = 0;
                lastHyphen = false;
                index++;
                continue;
            }

            if (character <= 127 && !new EmailByteScanner().IsAsciiLetterOrDigit((byte)character) && character != '-')
            {
                return false;
            }

            if (labelLength == 0 && character == '-')
            {
                return false;
            }

            labelLength++;
            lastHyphen = character == '-';
            if (character > 127)
            {
                ascii = false;
            }

            if (ascii && labelLength > 63)
            {
                return false;
            }

            if (char.IsHighSurrogate(character))
            {
                index += 2;
            }
            else
            {
                index++;
            }
        }

        if (lastHyphen)
        {
            return false;
        }

        if (ascii && value.Length > 255)
        {
            return false;
        }

        return labelLength > 0 || sawLabel && value[value.Length - 1] == '.';
    }
}
