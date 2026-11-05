using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Writes mbox in its wire or storage format.
/// </summary>
public class EmailMboxWriter : IDisposable, IEmailMboxWriterContract
{
    const string DefaultEnvelopeSender = "mailer-daemon@localhost";
    Stream? output;
    EmailMboxWriterOptions? options;
    long logicalPosition;
    int entryCount;
    bool closed;
    /// <summary>
    /// Creates an mbox writer using the supplied policy.
    /// </summary>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    public EmailMboxWriter(Stream output, EmailMboxWriterOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        Initialize(output, options.Clone());
    }

    internal static EmailMboxWriter CreateImplementationWithOutput(Stream output)
    {
        return new EmailMboxWriter(output, new EmailMboxWriterOptions());
    }

    internal static EmailMboxWriter CreateImplementationWithOutputAndOptions(Stream output, EmailMboxWriterOptions options)
    {
        return new EmailMboxWriter(output, options);
    }

    void Initialize(Stream suppliedOutput, EmailMboxWriterOptions suppliedOptions)
    {
        if (suppliedOutput == null)
            throw new ArgumentNullException("output");
        if (!suppliedOutput.CanWrite)
            throw new ArgumentException("The mbox output stream must be writable.", "output");
        output = suppliedOutput;
        options = suppliedOptions;
        logicalPosition = 0;
        if (output.CanSeek)
        {
            output.Position = output.Length;
            logicalPosition = output.Length;
        }

        entryCount = 0;
        closed = false;
    }

    /// <summary>
    /// Gets the options.
    /// </summary>
    /// <returns>The current options.</returns>
    public EmailMboxWriterOptions GetOptions()
    {
        return (options ?? throw new InvalidOperationException()).Clone();
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The mbox write result produced by the operation.</returns>
    public EmailMboxWriteResult Write(EmailMessage message)
    {
        return Append(message);
    }

    /// <summary>
    /// Appends the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The mbox write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailMboxWriteResult Append(EmailMessage message)
    {
        if (message == null)
            throw new ArgumentNullException("message");
        string? sender = (options ?? throw new InvalidOperationException()).HasEnvelopeSender() ? options.GetEnvelopeSender() : GetMessageEnvelopeSender(message);
        return AppendPrepared(message, sender, options.GetEnvelopeTimestamp());
    }

    /// <summary>
    /// Appends the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "envelopeSender">The envelope sender used by the operation.</param>
    /// <param name = "envelopeTimestamp">The envelope timestamp used by the operation.</param>
    /// <returns>The mbox write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailMboxWriteResult AppendWithEnvelope(EmailMessage message, string envelopeSender, DateTimeOffset envelopeTimestamp)
    {
        if (message == null)
            throw new ArgumentNullException("message");
        ValidateEnvelopeSenderImplementation(envelopeSender, "envelopeSender");
        return AppendPrepared(message, envelopeSender, envelopeTimestamp);
    }

    EmailMboxWriteResult AppendPrepared(EmailMessage message, string? envelopeSender, DateTimeOffset envelopeTimestamp)
    {
        EnsureOpen();
        EmailArchiveLimits limits = (options ?? throw new InvalidOperationException()).GetLimits();
        if (entryCount >= limits.GetMaxEntryCount())
            throw new EmailLimitException("MaxMboxEntryCount", limits.GetMaxEntryCount(), entryCount + 1);
        EmailWriteResult messageResult = new EmailWriter().WriteMessageWithMessageAndOptions(message, options.GetWriteOptions());
        if (!messageResult.IsSuccess())
            return new EmailMboxWriteResult(entryCount, options.GetFormat(), null, null, null, null, messageResult, 0, 0, false);
        byte[] envelopeLine = CreateEnvelopeLine(envelopeSender, envelopeTimestamp);
        EmailMboxEscapedMessage escaped = EscapeMessage(messageResult.GetBytes(), options.GetFormat());
        byte[] storedMessage = TerminateMessage(escaped.GetBytes());
        long entryBytes = (long)envelopeLine.Length + storedMessage.Length;
        if (entryBytes > limits.GetMaxEntryBytes())
            throw new EmailLimitException("MaxMboxEntryBytes", limits.GetMaxEntryBytes(), entryBytes);
        byte[] padding = GetAppendPadding();
        long resultingArchiveBytes = logicalPosition + padding.Length + entryBytes;
        if (resultingArchiveBytes > limits.GetMaxArchiveBytes())
            throw new EmailLimitException("MaxMboxArchiveBytes", limits.GetMaxArchiveBytes(), resultingArchiveBytes);
        if ((output ?? throw new InvalidOperationException()).CanSeek)
            output.Position = output.Length;
        WriteBytes(padding);
        long archiveStart = logicalPosition;
        WriteBytes(envelopeLine);
        long messageStart = logicalPosition;
        WriteBytes(storedMessage);
        EmailRange envelopeRange = new EmailRange(archiveStart, envelopeLine.Length);
        EmailRange messageRange = new EmailRange(messageStart, storedMessage.Length);
        EmailRange archiveRange = new EmailRange(archiveStart, entryBytes);
        EmailMboxWriteResult result = new EmailMboxWriteResult(entryCount, options.GetFormat(), archiveRange, envelopeRange, messageRange, envelopeLine, messageResult, padding.Length, escaped.GetEscapedLineCount(), true);
        entryCount++;
        if (options.GetAutoFlush())
            output.Flush();
        return result;
    }

    /// <summary>
    /// Flushes the supplied value.
    /// </summary>
    public void Flush()
    {
        EnsureOpen();
        (output ?? throw new InvalidOperationException()).Flush();
    }

    /// <summary>
    /// Closes the supplied value.
    /// </summary>
    public void Close()
    {
        if (closed)
            return;
        (output ?? throw new InvalidOperationException()).Flush();
        closed = true;
        if (!(options ?? throw new InvalidOperationException()).GetLeaveOpen())
            output.Close();
    }

    /// <summary>
    /// Releases streams, temporary storage, and other resources retained by this instance.
    /// </summary>
    public void Dispose()
    {
        Close();
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
    /// Gets the written archive byte count.
    /// </summary>
    /// <returns>The current written archive byte count.</returns>
    public long GetWrittenArchiveByteCount()
    {
        return logicalPosition;
    }

    /// <summary>
    /// Determines whether the current value or state is closed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is closed; otherwise, <c>false</c>.</returns>
    public bool IsClosed()
    {
        return closed;
    }

    byte[] GetAppendPadding()
    {
        if (logicalPosition == 0)
            return new byte[0];
        if (entryCount > 0)
            return new byte[0];
        if (!(output ?? throw new InvalidOperationException()).CanSeek || !output.CanRead)
            return new byte[]
            {
                10,
                10
            };
        long original = output.Position;
        long length = output.Length;
        int take = length < 4 ? (int)length : 4;
        byte[] tail = new byte[take];
        output.Position = length - take;
        int read = 0;
        while (read < take)
        {
            int count = output.Read(tail, read, take - read);
            if (count == 0)
                break;
            read += count;
        }

        output.Position = original;
        int breaks = CountTrailingLineBreaks(tail, read);
        if (breaks >= 2)
            return new byte[0];
        if (breaks == 1)
            return new byte[]
            {
                10
            };
        return new byte[]
        {
            10,
            10
        };
    }

    static int CountTrailingLineBreaks(byte[] value, int length)
    {
        int index = length;
        int count = 0;
        while (index > 0)
        {
            if (value[index - 1] == 10)
            {
                index--;
                if (index > 0 && value[index - 1] == 13)
                    index--;
                count++;
                continue;
            }

            if (value[index - 1] == 13)
            {
                index--;
                count++;
                continue;
            }

            break;
        }

        return count;
    }

    static EmailMboxEscapedMessage EscapeMessage(byte[] value, EmailMboxFormat format)
    {
        using (MemoryStream escaped = new MemoryStream())
        {
            int offset = 0;
            bool inBody = false;
            int escapedCount = 0;
            while (offset < value.Length)
            {
                int lineEnd = FindLineEnd(value, offset);
                int physicalEnd = IncludeLineEnding(value, lineEnd);
                if (inBody && ShouldEscape(value, offset, lineEnd, format))
                {
                    escaped.WriteByte(62);
                    escapedCount++;
                }

                escaped.Write(value, offset, physicalEnd - offset);
                if (!inBody && lineEnd == offset)
                    inBody = true;
                offset = physicalEnd;
            }

            return new EmailMboxEscapedMessage(escaped.ToArray(), escapedCount);
        }
    }

    static int FindLineEnd(byte[] value, int offset)
    {
        int index = offset;
        while (index < value.Length && value[index] != 13 && value[index] != 10)
            index++;
        return index;
    }

    static int IncludeLineEnding(byte[] value, int lineEnd)
    {
        if (lineEnd >= value.Length)
            return lineEnd;
        if (value[lineEnd] == 13 && lineEnd + 1 < value.Length && value[lineEnd + 1] == 10)
            return lineEnd + 2;
        return lineEnd + 1;
    }

    static bool ShouldEscape(byte[] value, int start, int end, EmailMboxFormat format)
    {
        int index = start;
        if (format == EmailMboxFormat.Mboxrd)
        {
            while (index < end && value[index] == 62)
                index++;
        }

        return end - index >= 5 && value[index] == 70 && value[index + 1] == 114 && value[index + 2] == 111 && value[index + 3] == 109 && value[index + 4] == 32;
    }

    static byte[] TerminateMessage(byte[] value)
    {
        int lineBreaks = CountTrailingLineBreaks(value, value.Length);
        int suffixBytes = lineBreaks >= 2 ? 0 : lineBreaks == 1 ? 1 : 2;
        if (suffixBytes == 0)
            return value;
        byte[] terminated = new byte[value.Length + suffixBytes];
        value.CopyTo(terminated, 0);
        int index = value.Length;
        while (index < terminated.Length)
        {
            terminated[index] = 10;
            index++;
        }

        return terminated;
    }

    static byte[] CreateEnvelopeLine(string? sender, DateTimeOffset timestamp)
    {
        DateTimeOffset utc = timestamp.ToUniversalTime();
        string day = utc.Day < 10 ? " " + utc.Day.ToString(CultureInfo.InvariantCulture) : utc.Day.ToString(CultureInfo.InvariantCulture);
        string date = utc.ToString("ddd MMM ", CultureInfo.InvariantCulture) + day + utc.ToString(" HH:mm:ss yyyy", CultureInfo.InvariantCulture);
        return Encoding.ASCII.GetBytes("From " + sender + " " + date + "\n");
    }

    static string GetMessageEnvelopeSender(EmailMessage message)
    {
        EmailEnvelope? envelope = message.GetEnvelope();
        if (!(envelope ?? throw new InvalidOperationException()).HasSender())
            return DefaultEnvelopeSender;
        EmailAddress? address = envelope.GetSender();
        string sender = (address ?? throw new InvalidOperationException()).GetLocalPart() + "@" + address.GetAsciiDomain();
        return IsValidEnvelopeSender(sender) ? sender : DefaultEnvelopeSender;
    }

    internal static void ValidateEnvelopeSenderImplementation(string value, string parameterName)
    {
        if (value == null)
            throw new ArgumentNullException(parameterName);
        if (!IsValidEnvelopeSender(value))
            throw new ArgumentException("An mbox envelope sender must be a non-empty token without whitespace, controls, or NUL bytes.", parameterName);
    }

    static bool IsValidEnvelopeSender(string value)
    {
        if (value == null || value.Length == 0)
            return false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character <= 32 || character == 127 || character > 127)
                return false;
            index++;
        }

        return true;
    }

    void WriteBytes(byte[] value)
    {
        (output ?? throw new InvalidOperationException()).Write(value, 0, value.Length);
        logicalPosition += value.Length;
    }

    void EnsureOpen()
    {
        if (closed)
            throw new ObjectDisposedException("EmailMboxWriter");
    }
}
