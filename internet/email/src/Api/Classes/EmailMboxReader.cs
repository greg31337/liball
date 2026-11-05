using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Reads and interprets mbox.
/// </summary>
public class EmailMboxReader : IDisposable, IEmailMboxReaderContract
{
    Stream? input;
    EmailMboxReaderOptions? options;
    EmailMboxLineReader? lines;
    EmailMboxLine? pendingEnvelopeLine;
    EmailMboxEntry? current;
    EmailIssueList? issues;
    int entryCount;
    bool complete;
    bool closed;
    /// <summary>
    /// Creates an mbox reader using the supplied policy.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    public EmailMboxReader(Stream input, EmailMboxReaderOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        Initialize(input, options.Clone());
    }

    internal static EmailMboxReader CreateImplementationWithInput(Stream input)
    {
        return new EmailMboxReader(input, new EmailMboxReaderOptions());
    }

    internal static EmailMboxReader CreateImplementationWithInputAndOptions(Stream input, EmailMboxReaderOptions options)
    {
        return new EmailMboxReader(input, options);
    }

    void Initialize(Stream suppliedInput, EmailMboxReaderOptions suppliedOptions)
    {
        if (suppliedInput == null)
            throw new ArgumentNullException("input");
        if (!suppliedInput.CanRead)
            throw new ArgumentException("The mbox input stream must be readable.", "input");
        input = suppliedInput;
        options = suppliedOptions;
        lines = new EmailMboxLineReader(input, options.GetReadBufferBytes(), options.GetLimits().GetMaxArchiveBytes(), options.GetLimits().GetMaxEntryBytes());
        pendingEnvelopeLine = null;
        current = null;
        issues = new EmailIssueList();
        entryCount = 0;
        complete = false;
        closed = false;
    }

    /// <summary>
    /// Gets the options.
    /// </summary>
    /// <returns>The current options.</returns>
    public EmailMboxReaderOptions GetOptions()
    {
        return (options ?? throw new InvalidOperationException()).Clone();
    }

    /// <summary>
    /// Reads the next from the input.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public bool ReadNext()
    {
        EnsureOpen();
        current = null;
        if (complete && pendingEnvelopeLine == null)
            return false;
        EmailMboxLine? envelopeLine = pendingEnvelopeLine;
        pendingEnvelopeLine = null;
        EmailMboxLine? firstMessageLine = null;
        bool recovered = false;
        EmailIssueList entryIssues = new EmailIssueList();
        if (envelopeLine == null)
        {
            EmailMboxLine? first = (lines ?? throw new InvalidOperationException()).ReadLine();
            if (first == null)
            {
                complete = true;
                return false;
            }

            if (IsEnvelopeLine(first.GetBytes()))
                envelopeLine = first;
            else
            {
                firstMessageLine = first;
                recovered = true;
                entryIssues.Add(Issue(EmailIssueSeverity.Warning, EmailIssueCode.MboxMessageRecovered, "Message data appears before the first mbox envelope line.", first, "The leading data was recovered as an entry without envelope metadata."));
            }
        }

        EmailArchiveLimits limits = (options ?? throw new InvalidOperationException()).GetLimits();
        if (entryCount >= limits.GetMaxEntryCount())
            throw new EmailLimitException("MaxMboxEntryCount", limits.GetMaxEntryCount(), entryCount + 1);
        long archiveStart = envelopeLine == null ? (firstMessageLine ?? throw new InvalidOperationException()).GetOffset() : envelopeLine.GetOffset();
        long messageStart = envelopeLine == null ? archiveStart : envelopeLine.GetEndOffset();
        EmailMboxEnvelopeData? envelope = envelopeLine == null ? null : ParseEnvelope(envelopeLine, entryIssues);
        using (MemoryStream messageBytes = new MemoryStream())
        {
            bool inBody = false;
            int unescapedCount = 0;
            EmailMboxLine? line = firstMessageLine;
            while (true)
            {
                if (line == null)
                    line = (lines ?? throw new InvalidOperationException()).ReadLine();
                if (line == null)
                {
                    complete = true;
                    break;
                }

                if (IsEnvelopeLine(line.GetBytes()))
                {
                    pendingEnvelopeLine = line;
                    break;
                }

                long rawEntryBytes = line.GetEndOffset() - archiveStart;
                if (rawEntryBytes > limits.GetMaxEntryBytes())
                    throw new EmailLimitException("MaxMboxEntryBytes", limits.GetMaxEntryBytes(), rawEntryBytes);
                byte[] physical = line.GetBytes();
                byte[] logical = physical;
                if (inBody && ShouldUnescape(physical, options.GetFormat()))
                {
                    logical = RemoveFirstByte(physical);
                    unescapedCount++;
                }

                messageBytes.Write(logical, 0, logical.Length);
                if (!inBody && IsBlankLine(physical))
                    inBody = true;
                line = null;
            }

            long archiveEnd = pendingEnvelopeLine == null ? (lines ?? throw new InvalidOperationException()).GetLogicalByteCount() : pendingEnvelopeLine.GetOffset();
            long entryBytes = archiveEnd - archiveStart;
            if (entryBytes > limits.GetMaxEntryBytes())
                throw new EmailLimitException("MaxMboxEntryBytes", limits.GetMaxEntryBytes(), entryBytes);
            byte[] logicalMessage = messageBytes.ToArray();
            EmailParseResult parsed = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(logicalMessage, options.GetParseOptions());
            entryIssues.AddRange(parsed.GetIssues());
            if (!parsed.IsComplete())
                entryIssues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.MboxMessageMalformed, "The recovered mbox entry is not a complete Internet message.", new EmailSourcePosition(messageStart, envelopeLine == null ? (firstMessageLine ?? throw new InvalidOperationException()).GetLineNumber() : envelopeLine.GetLineNumber() + 1, 1), null, null, "The parser's partial message and diagnostics were retained."));
            EmailMessage? message = BuildMessage(parsed, envelope, entryIssues);
            EmailRange archiveRange = new EmailRange(archiveStart, archiveEnd - archiveStart);
            EmailRange? envelopeRange = envelopeLine == null ? null : new EmailRange(envelopeLine.GetOffset(), envelopeLine.GetLength());
            EmailRange rawMessageRange = new EmailRange(messageStart, archiveEnd - messageStart);
            current = new EmailMboxEntry(entryCount, options.GetFormat(), archiveRange, envelopeRange, rawMessageRange, envelopeLine == null ? null : envelopeLine.GetBytes(), envelope == null ? null : envelope.GetSenderText(), envelope == null ? null : envelope.GetDateText(), message, logicalMessage.LongLength, unescapedCount, recovered, entryIssues);
            entryCount++;
            (issues ?? throw new InvalidOperationException()).AddRange(entryIssues);
            return true;
        }
    }

    /// <summary>
    /// Determines whether the current is present or available.
    /// </summary>
    /// <returns><c>true</c> when the current is available; otherwise, <c>false</c>.</returns>
    public bool HasCurrent()
    {
        return current != null;
    }

    /// <summary>
    /// Gets the current.
    /// </summary>
    /// <returns>The current current.</returns>
    public EmailMboxEntry? GetCurrent()
    {
        return current;
    }

    /// <summary>
    /// Gets the entry count.
    /// </summary>
    /// <returns>The current entry count.</returns>
    public int GetEntryCount()
    {
        return entryCount;
    }

    /// <summary>
    /// Gets the consumed byte count.
    /// </summary>
    /// <returns>The current consumed byte count.</returns>
    public long GetConsumedByteCount()
    {
        return (lines ?? throw new InvalidOperationException()).GetLogicalByteCount();
    }

    /// <summary>
    /// Gets the read byte count.
    /// </summary>
    /// <returns>The current read byte count.</returns>
    public long GetReadByteCount()
    {
        return (lines ?? throw new InvalidOperationException()).GetPhysicalByteCount();
    }

    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return complete && pendingEnvelopeLine == null;
    }

    /// <summary>
    /// Determines whether the current value or state is closed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is closed; otherwise, <c>false</c>.</returns>
    public bool IsClosed()
    {
        return closed;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    /// <summary>
    /// Closes the supplied value.
    /// </summary>
    public void Close()
    {
        if (closed)
            return;
        closed = true;
        current = null;
        pendingEnvelopeLine = null;
        if (!(options ?? throw new InvalidOperationException()).GetLeaveOpen())
            (input ?? throw new InvalidOperationException()).Close();
    }

    /// <summary>
    /// Releases streams, temporary storage, and other resources retained by this instance.
    /// </summary>
    public void Dispose()
    {
        Close();
    }

    EmailMessage? BuildMessage(EmailParseResult parsed, EmailMboxEnvelopeData? envelopeData, EmailIssueList entryIssues)
    {
        if (!parsed.HasMessage())
            return null;
        EmailMessage? parsedMessage = parsed.GetMessage();
        EmailEnvelope envelope = new EmailEnvelopeOperations().CreateWith();
        if (envelopeData != null)
        {
            if (envelopeData.CanAttachRawLine())
                envelope.SetMboxFromLine(envelopeData.GetRawLine());
            if (envelopeData.HasSender())
                envelope.SetSender(envelopeData.GetSender());
        }

        MimeEntity? body = (parsedMessage ?? throw new InvalidOperationException()).HasBody() ? parsedMessage.GetBody() : null;
        return new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(parsedMessage.GetHeaders(), body, envelope, parsedMessage.GetSourceInfo(), entryIssues);
    }

    EmailMboxEnvelopeData ParseEnvelope(EmailMboxLine line, EmailIssueList entryIssues)
    {
        byte[] value = line.GetBytes();
        int contentEnd = ContentEnd(value);
        int senderStart = 5;
        int separator = senderStart;
        while (separator < contentEnd && value[separator] != 32)
            separator++;
        int dateStart = separator;
        while (dateStart < contentEnd && value[dateStart] == 32)
            dateStart++;
        bool valid = separator > senderStart && dateStart < contentEnd && !ContainsNul(value);
        string? senderText = separator > senderStart ? Encoding.UTF8.GetString(value, senderStart, separator - senderStart) : null;
        string? dateText = dateStart < contentEnd ? Encoding.UTF8.GetString(value, dateStart, contentEnd - dateStart) : null;
        EmailAddress? sender = null;
        if (senderText != null)
        {
            EmailAddressParseResult parsed = new EmailAddressParser().ParseMailbox(Encoding.UTF8.GetBytes(senderText), EmailAddressParseMode.Tolerant);
            if (parsed.IsSuccess() && parsed.HasMailbox())
                sender = parsed.GetMailbox();
        }

        if (!valid)
        {
            entryIssues.Add(Issue(EmailIssueSeverity.Warning, EmailIssueCode.MboxEnvelopeMalformed, "An mbox envelope line lacks a usable sender, timestamp, or NUL-free representation.", line, "The line was still used as an entry boundary and retained on the entry result."));
        }
        else if (sender == null)
        {
            entryIssues.Add(Issue(EmailIssueSeverity.Warning, EmailIssueCode.MboxEnvelopeMalformed, "The sender token in an mbox envelope line is not a parseable mailbox.", line, "The raw sender token and envelope line were retained without semantic sender metadata."));
        }

        return new EmailMboxEnvelopeData(value, senderText, dateText, sender, !ContainsNul(value));
    }

    static bool IsEnvelopeLine(byte[] value)
    {
        return value.Length >= 5 && value[0] == 70 && value[1] == 114 && value[2] == 111 && value[3] == 109 && value[4] == 32;
    }

    static bool IsBlankLine(byte[] value)
    {
        return value.Length == 1 && (value[0] == 10 || value[0] == 13) || value.Length == 2 && value[0] == 13 && value[1] == 10;
    }

    static bool ShouldUnescape(byte[] value, EmailMboxFormat format)
    {
        if (value.Length < 6 || value[0] != 62)
            return false;
        if (format == EmailMboxFormat.Mboxo)
            return value[1] == 70 && value[2] == 114 && value[3] == 111 && value[4] == 109 && value[5] == 32;
        int index = 1;
        while (index < value.Length && value[index] == 62)
            index++;
        return index + 4 < value.Length && value[index] == 70 && value[index + 1] == 114 && value[index + 2] == 111 && value[index + 3] == 109 && value[index + 4] == 32;
    }

    static byte[] RemoveFirstByte(byte[] value)
    {
        byte[] result = new byte[value.Length - 1];
        Array.Copy(value, 1, result, 0, result.Length);
        return result;
    }

    static int ContentEnd(byte[] value)
    {
        if (value.Length >= 2 && value[value.Length - 2] == 13 && value[value.Length - 1] == 10)
            return value.Length - 2;
        if (value.Length >= 1 && (value[value.Length - 1] == 13 || value[value.Length - 1] == 10))
            return value.Length - 1;
        return value.Length;
    }

    static bool ContainsNul(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == 0)
                return true;
            index++;
        }

        return false;
    }

    static EmailIssue Issue(EmailIssueSeverity severity, EmailIssueCode code, string description, EmailMboxLine line, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, new EmailSourcePosition(line.GetOffset(), line.GetLineNumber(), 1), null, null, recovery);
    }

    void EnsureOpen()
    {
        if (closed)
            throw new ObjectDisposedException("EmailMboxReader");
    }
}
