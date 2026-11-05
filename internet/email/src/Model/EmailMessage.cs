using System;
using System.Collections.Generic;

namespace email;
internal class EmailMessageHeaderSnapshot
{
    EmailHeader header;
    byte[] rawFieldBytes;
    byte[] nameBytes;
    byte[] unfoldedValue;
    EmailHeaderValueKind valueKind;
    EmailStructuredValueParseMode parseMode;
    EmailRange? sourceRange;

    internal EmailMessageHeaderSnapshot(EmailHeader header)
    {
        this.header = header;
        rawFieldBytes = header.GetRawFieldBytes();
        nameBytes = header.GetNameBytes();
        unfoldedValue = header.GetUnfoldedValue();
        valueKind = header.GetValueKind();
        parseMode = header.GetParseMode();
        sourceRange = header.GetSourceRange();
    }

    internal EmailHeader GetHeader()
    {
        return header;
    }

    internal byte[] GetRawFieldBytes()
    {
        return rawFieldBytes;
    }

    internal byte[] GetNameBytes()
    {
        return nameBytes;
    }

    internal byte[] GetUnfoldedValue()
    {
        return unfoldedValue;
    }

    internal EmailHeaderValueKind GetValueKind()
    {
        return valueKind;
    }

    internal EmailStructuredValueParseMode GetParseMode()
    {
        return parseMode;
    }

    internal EmailRange? GetSourceRange()
    {
        return sourceRange;
    }

    internal bool IsExactMatch(EmailHeader value)
    {
        return Equal(rawFieldBytes, value.GetRawFieldBytes());
    }

    internal bool IsSemanticMatch(EmailHeader value)
    {
        return AsciiNameEquals(nameBytes, value.GetNameBytes()) && Equal(unfoldedValue, value.GetUnfoldedValue()) && valueKind == value.GetValueKind() && parseMode == value.GetParseMode();
    }

    static bool AsciiNameEquals(byte[] first, byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }

        int index = 0;
        while (index < first.Length)
        {
            int left = first[index];
            int right = second[index];
            if (left >= 65 && left <= 90)
            {
                left += 32;
            }

            if (right >= 65 && right <= 90)
            {
                right += 32;
            }

            if (left != right)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool Equal(byte[] first, byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }

        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }
}
internal class EmailMessageEnvelopeSnapshot
{
    EmailAddress? sender;
    List<EmailAddress> recipients;
    byte[]? mboxFromLine;
    EmailRange? mboxFromLineRange;

    internal EmailMessageEnvelopeSnapshot(EmailEnvelope? envelope)
    {
        sender = (envelope ?? throw new InvalidOperationException()).GetSender();
        recipients = envelope.GetRecipients();
        mboxFromLine = envelope.GetMboxFromLine();
        mboxFromLineRange = envelope.GetMboxFromLineRange();
    }

    internal EmailAddress? GetSender()
    {
        return sender;
    }

    internal List<EmailAddress> GetRecipients()
    {
        return recipients;
    }

    internal byte[]? GetMboxFromLine()
    {
        return mboxFromLine;
    }

    internal EmailRange? GetMboxFromLineRange()
    {
        return mboxFromLineRange;
    }
}
