using System;
using System.Collections.Generic;
using System.IO;

namespace email;

internal class EmailStreamingWriter
{
    internal const int BufferBytes = 81920;

    internal static EmailStreamWriteResult WriteMessage(EmailMessage message, EmailWriteOptions options, EmailOutputSink output)
    {
        bool blindHeadersAllowed = options.GetIncludeBlindRecipientHeaders() || !EmailWriteHeaderPolicy.HasBlindRecipientHeaders(message.GetHeaders());
        if (!options.IsCanonical() && message.HasSourceInfo() && !message.IsExactDirty() && blindHeadersAllowed)
        {
            return WritePreservedMessage(message, options, output);
        }

        if (options.IsCanonical() && message.HasBody() && CanStreamDirect(message.GetBody()))
        {
            return WriteCanonicalDirect(message, message.GetBody(), options, output);
        }

        EmailWriteResult result = new EmailWriter().WriteMessageWithMessageAndOptions(message, options);
        return WriteFallback(result, output);
    }

    static EmailStreamWriteResult WritePreservedMessage(EmailMessage message, EmailWriteOptions options, EmailOutputSink output)
    {
        EmailSourceInfo? sourceInfo = message.GetSourceInfo();
        EmailRange? range = (sourceInfo ?? throw new InvalidOperationException()).GetMessageRange();
        EmailIssueList issues = new EmailIssueList();
        if ((range ?? throw new InvalidOperationException()).GetLength() > options.GetMaximumOutputBytes())
        {
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.WritePolicyViolation, "Preserved streaming output would exceed the configured maximum output size of " + options.GetMaximumOutputBytes() + " bytes after observing " + range.GetLength() + " bytes.", null, null, null, "No partial output was written."));
            return new EmailStreamWriteResult(options.GetMode(), issues, 0, range.GetLength(), 0, 0, 0, 0);
        }

        CopyRange(sourceInfo.GetContent(), range, output);
        return new EmailStreamWriteResult(options.GetMode(), issues, range.GetLength(), range.GetLength(), range.GetLength(), 0, range.GetLength() == 0 ? 0 : 1, 0);
    }

    static void CopyRange(EmailContent? content, EmailRange range, EmailOutputSink output)
    {
        Stream input = (content ?? throw new InvalidOperationException()).OpenReadRange(range.GetStartOffset(), range.GetLength());
        byte[] buffer = new byte[BufferBytes];
        long remaining = range.GetLength();
        while (remaining > 0)
        {
            int requested = remaining < buffer.Length ? (int)remaining : buffer.Length;
            int read = input.Read(buffer, 0, requested);
            if (read == 0)
            {
                throw new EndOfStreamException("The preserved message source ended before its declared range.");
            }

            output.WriteSegment(buffer, 0, read);
            remaining -= read;
        }

        if (!content.IsRangeReadStreamBorrowed())
        {
            input.Close();
        }
    }

    internal static EmailStreamWriteResult WriteEntity(MimeEntity entity, EmailWriteOptions options, EmailOutputSink output)
    {
        if (options.IsCanonical() && CanStreamDirect(entity))
        {
            return WriteCanonicalDirect(null, entity, options, output);
        }

        EmailWriteResult result = new EmailWriter().WriteEntityWithEntityAndOptions(entity, options);
        return WriteFallback(result, output);
    }

    static EmailStreamWriteResult WriteCanonicalDirect(EmailMessage? message, MimeEntity? entity, EmailWriteOptions options, EmailOutputSink output)
    {
        EmailContent? content = (entity ?? throw new InvalidOperationException()).HasDecodedContent() ? entity.GetDecodedContent() : entity.GetRawContent();
        bool decodedSource = entity.HasDecodedContent();
        EmailTransferEncodingKind selected = SelectEncoding(entity, content, decodedSource, options);
        if (selected < EmailTransferEncodingKind.SevenBit)
        {
            EmailWriteResult fallback = message == null ? new EmailWriter().WriteEntityWithEntityAndOptions(entity, options) : new EmailWriter().WriteMessageWithMessageAndOptions(message, options);
            return WriteFallback(fallback, output);
        }

        EmailCanonicalWriterWorker headerWriter = new EmailCanonicalWriterWorker(options);
        string transferEncoding = GetEncodingName(selected);
        byte[]? headers = message == null ? headerWriter.WriteDirectEntityHeaders(entity, transferEncoding) : headerWriter.WriteDirectMessageHeaders(message, entity, transferEncoding);
        EmailIssueList issues = headerWriter.GetIssues();
        long sourceByteCount = GetSourceByteCount(message, entity);
        if (headers == null)
        {
            return new EmailStreamWriteResult(options.GetMode(), issues, 0, sourceByteCount, 0, 0, 0, 0);
        }

        EmailCountingStream counter = new EmailCountingStream();
        EmailOutputSink preflight = EmailOutputSink.CreateWithCounter(counter);
        preflight.Write(headers);
        WriteEncoded(content, selected, preflight);
        EnsureTerminalCrlf(preflight);
        long outputByteCount = counter.GetByteCount();
        if (outputByteCount > options.GetMaximumOutputBytes())
        {
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.WritePolicyViolation, "Canonical streaming output would exceed the configured maximum output size of " + options.GetMaximumOutputBytes() + " bytes after observing " + outputByteCount + " bytes.", null, null, null, "No partial output was written."));
            return new EmailStreamWriteResult(options.GetMode(), issues, 0, sourceByteCount, 0, 0, 0, 0);
        }

        output.Write(headers);
        WriteEncoded(content, selected, output);
        EnsureTerminalCrlf(output);
        return new EmailStreamWriteResult(options.GetMode(), issues, outputByteCount, sourceByteCount, 0, outputByteCount, 0, 1);
    }

    static EmailTransferEncodingKind SelectEncoding(MimeEntity entity, EmailContent? content, bool decodedSource, EmailWriteOptions options)
    {
        if (!decodedSource && entity.HasTransferEncoding())
        {
            EmailTransferEncodingKind rawEncoding = entity.GetTransferEncodingKind();
            if (rawEncoding == EmailTransferEncodingKind.Base64 || rawEncoding == EmailTransferEncodingKind.QuotedPrintable)
            {
                return (EmailTransferEncodingKind)(-1);
            }
        }

        if (decodedSource && entity.HasTransferEncoding())
        {
            EmailTransferEncodingKind explicitEncoding = entity.GetTransferEncodingKind();
            if (explicitEncoding == EmailTransferEncodingKind.Base64 || explicitEncoding == EmailTransferEncodingKind.QuotedPrintable)
            {
                return explicitEncoding;
            }
        }

        EmailStreamingAnalysis analysis = Analyze(content);
        int maximumLineBytes = options.GetHeaderWriteOptions().GetMaximumLineLength();
        bool identitySafe = analysis.GetNullByteCount() == 0 && analysis.HasCanonicalLineBreaks() && analysis.GetMaximumLineByteCount() <= maximumLineBytes;
        if (analysis.GetHighBitByteCount() == 0 && identitySafe)
        {
            return EmailTransferEncodingKind.SevenBit;
        }

        if (options.GetTransportConstraint() >= EmailTransferEncodingConstraint.EightBit && identitySafe && IsTextualMediaType(entity))
        {
            return EmailTransferEncodingKind.EightBit;
        }

        if (options.GetTransportConstraint() == EmailTransferEncodingConstraint.Binary)
        {
            return EmailTransferEncodingKind.Binary;
        }

        if (IsTextualMediaType(entity) && analysis.IsMostlyText())
        {
            return EmailTransferEncodingKind.QuotedPrintable;
        }

        return EmailTransferEncodingKind.Base64;
    }

    static EmailStreamingAnalysis Analyze(EmailContent? content)
    {
        EmailStreamingAnalysis analysis = new EmailStreamingAnalysis();
        Stream? input = (content ?? throw new InvalidOperationException()).OpenRead();
        byte[] buffer = new byte[BufferBytes];
        while (true)
        {
            int read = (input ?? throw new InvalidOperationException()).Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            analysis.Feed(new ReadOnlySpan<byte>(buffer, 0, read));
        }

        if (content.GetKind() != EmailContentKind.Stream)
        {
            input.Close();
        }

        analysis.Complete();
        return analysis;
    }

    static void WriteEncoded(EmailContent? content, EmailTransferEncodingKind encoding, EmailOutputSink output)
    {
        Stream? input = (content ?? throw new InvalidOperationException()).OpenRead();
        byte[] buffer = new byte[BufferBytes];
        EmailBase64Encoder? base64 = encoding == EmailTransferEncodingKind.Base64 ? new EmailBase64Encoder(EmailBase64Constants.DefaultLineLength) : null;
        EmailQuotedPrintableEncoder? quotedPrintable = encoding == EmailTransferEncodingKind.QuotedPrintable ? new EmailQuotedPrintableEncoder(EmailQuotedPrintableConstants.DefaultLineLength, EmailQuotedPrintableNewlinePolicy.NormalizeToCrlf, false) : null;
        bool pendingCarriageReturn = false;
        while (true)
        {
            int read = (input ?? throw new InvalidOperationException()).Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            if (base64 != null)
            {
                output.Write(base64.Feed(new ReadOnlySpan<byte>(buffer, 0, read)));
            }
            else if (quotedPrintable != null)
            {
                output.Write(quotedPrintable.Feed(new ReadOnlySpan<byte>(buffer, 0, read)));
            }
            else if (encoding == EmailTransferEncodingKind.Binary)
            {
                output.WriteSegment(buffer, 0, read);
            }
            else
            {
                pendingCarriageReturn = WriteNormalized(buffer, read, pendingCarriageReturn, output);
            }
        }

        if (base64 != null)
        {
            output.Write(base64.Complete());
        }
        else if (quotedPrintable != null)
        {
            output.Write(quotedPrintable.Complete());
        }
        else if (pendingCarriageReturn)
        {
            output.Write(new byte[] { 13, 10 });
        }

        if (content.GetKind() != EmailContentKind.Stream)
        {
            input.Close();
        }
    }

    static bool WriteNormalized(byte[] buffer, int count, bool pendingCarriageReturn, EmailOutputSink output)
    {
        List<byte> normalized = new List<byte>();
        int index = 0;
        while (index < count)
        {
            byte value = buffer[index];
            if (pendingCarriageReturn)
            {
                pendingCarriageReturn = false;
                normalized.Add(13);
                normalized.Add(10);
                if (value == 10)
                {
                    index++;
                    continue;
                }
            }

            if (value == 13)
            {
                pendingCarriageReturn = true;
            }
            else if (value == 10)
            {
                normalized.Add(13);
                normalized.Add(10);
            }
            else
            {
                normalized.Add(value);
            }

            index++;
        }

        output.Write(normalized.ToArray());
        return pendingCarriageReturn;
    }

    static void EnsureTerminalCrlf(EmailOutputSink output)
    {
        if (output.EndsWithCrlf())
        {
            return;
        }

        output.Write(output.EndsWithCarriageReturn() ? new byte[] { 10 } : new byte[] { 13, 10 });
    }

    static EmailStreamWriteResult WriteFallback(EmailWriteResult result, EmailOutputSink output)
    {
        if (result.IsSuccess())
        {
            output.Write(result.GetBytes());
        }

        long outputBytes = result.IsSuccess() ? result.GetByteCount() : 0;
        long copiedBytes = result.IsSuccess() ? result.GetCopiedByteCount() : 0;
        long regeneratedBytes = result.IsSuccess() ? result.GetRegeneratedByteCount() : 0;
        int copiedRanges = result.IsSuccess() ? result.GetCopiedRangeCount() : 0;
        int regeneratedRanges = result.IsSuccess() ? result.GetRegeneratedRangeCount() : 0;
        return new EmailStreamWriteResult(result.GetMode(), result.GetIssues(), outputBytes, result.GetSourceByteCount(), copiedBytes, regeneratedBytes, copiedRanges, regeneratedRanges);
    }

    static bool CanStreamDirect(MimeEntity? entity)
    {
        if ((entity ?? throw new InvalidOperationException()).IsContainer() || entity.GetKind() == MimeEntityKind.Message || !entity.HasContent())
        {
            return false;
        }

        if (entity.HasTransferEncoding() && !entity.HasKnownTransferEncoding())
        {
            return false;
        }

        EmailContent? content = entity.HasDecodedContent() ? entity.GetDecodedContent() : entity.GetRawContent();
        return (content ?? throw new InvalidOperationException()).IsRepeatable();
    }

    static bool IsTextualMediaType(MimeEntity entity)
    {
        string type = (entity.GetMediaType() ?? throw new InvalidOperationException()).GetNormalizedTopLevelType();
        string subtype = (entity.GetMediaType() ?? throw new InvalidOperationException()).GetNormalizedSubtype();
        if (type == "text" || type == "message")
        {
            return true;
        }

        return subtype == "json" || subtype.EndsWith("+json", StringComparison.OrdinalIgnoreCase) || subtype == "xml" || subtype.EndsWith("+xml", StringComparison.OrdinalIgnoreCase) || subtype == "javascript" || subtype == "ecmascript" || subtype == "sql" || subtype == "graphql" || subtype == "x-www-form-urlencoded";
    }

    static string GetEncodingName(EmailTransferEncodingKind encoding)
    {
        if (encoding == EmailTransferEncodingKind.SevenBit)
        {
            return "7bit";
        }

        if (encoding == EmailTransferEncodingKind.EightBit)
        {
            return "8bit";
        }

        if (encoding == EmailTransferEncodingKind.Binary)
        {
            return "binary";
        }

        return encoding == EmailTransferEncodingKind.QuotedPrintable ? "quoted-printable" : "base64";
    }

    static long GetSourceByteCount(EmailMessage? message, MimeEntity entity)
    {
        if (message != null && message.HasSourceInfo())
        {
            return ((message.GetSourceInfo() ?? throw new InvalidOperationException()).GetMessageRange() ?? throw new InvalidOperationException()).GetLength();
        }

        return entity.HasSourceRange() ? (entity.GetSourceRange() ?? throw new InvalidOperationException()).GetLength() : 0;
    }
}
internal class EmailOutputSink
{
    Stream? stream;
    EmailCountingStream? counter;
    EmailHashingStream? hashing;
    SmtpDataWriter? smtpData;
    EmailCountingStream state;

    internal EmailOutputSink(Stream stream)
    {
        this.stream = stream;
        counter = null;
        hashing = null;
        smtpData = null;
        state = new EmailCountingStream();
    }

    internal static EmailOutputSink CreateWithStream(Stream stream)
    {
        return new EmailOutputSink(stream);
    }

    internal static EmailOutputSink CreateWithCounter(EmailCountingStream counter)
    {
        EmailOutputSink value = new EmailOutputSink(Stream.Null);
        value.stream = null;
        value.counter = counter;
        return value;
    }

    internal static EmailOutputSink CreateWithHashing(EmailHashingStream hashing)
    {
        EmailOutputSink value = new EmailOutputSink(Stream.Null);
        value.stream = null;
        value.hashing = hashing;
        return value;
    }

    internal static EmailOutputSink CreateWithSmtpData(SmtpDataWriter smtpData)
    {
        EmailOutputSink value = new EmailOutputSink(Stream.Null);
        value.stream = null;
        value.smtpData = smtpData;
        return value;
    }

    internal void Write(byte[] value)
    {
        WriteSegment(value, 0, value.Length);
    }

    internal void WriteSegment(byte[] value, int offset, int count)
    {
        if (stream != null)
        {
            stream.Write(value, offset, count);
        }
        else if (counter != null)
        {
            counter.WriteSegment(value, offset, count);
        }
        else if (hashing != null)
        {
            hashing.WriteSegment(value, offset, count);
        }
        else
        {
            (smtpData ?? throw new InvalidOperationException()).Feed(new ReadOnlySpan<byte>(value, offset, count));
        }

        state.WriteSegment(value, offset, count);
    }

    internal long GetByteCount()
    {
        return state.GetByteCount();
    }

    internal bool EndsWithCrlf()
    {
        return state.EndsWithCrlf();
    }

    internal bool EndsWithCarriageReturn()
    {
        return state.EndsWithCarriageReturn();
    }
}
internal class EmailStreamingAnalysis
{
    long sourceBytes;
    long textBytes;
    long highBitBytes;
    long nullBytes;
    long maximumLineBytes;
    long currentLineBytes;
    bool canonicalLineBreaks;
    bool pendingCarriageReturn;
    bool complete;

    internal EmailStreamingAnalysis()
    {
        sourceBytes = 0;
        textBytes = 0;
        highBitBytes = 0;
        nullBytes = 0;
        maximumLineBytes = 0;
        currentLineBytes = 0;
        canonicalLineBreaks = true;
        pendingCarriageReturn = false;
        complete = false;
    }

    internal void Feed(ReadOnlySpan<byte> input)
    {
        int index = 0;
        while (index < input.Length)
        {
            byte value = input[index];
            sourceBytes++;
            if (value >= 128)
            {
                highBitBytes++;
            }

            if (value == 0)
            {
                nullBytes++;
            }

            if (value == 9 || value == 10 || value == 13 || value >= 32 && value <= 126)
            {
                textBytes++;
            }

            if (pendingCarriageReturn)
            {
                pendingCarriageReturn = false;
                maximumLineBytes = Maximum(maximumLineBytes, currentLineBytes);
                currentLineBytes = 0;
                if (value == 10)
                {
                    index++;
                    continue;
                }

                canonicalLineBreaks = false;
            }

            if (value == 13)
            {
                pendingCarriageReturn = true;
            }
            else if (value == 10)
            {
                canonicalLineBreaks = false;
                maximumLineBytes = Maximum(maximumLineBytes, currentLineBytes);
                currentLineBytes = 0;
            }
            else
            {
                currentLineBytes++;
            }

            index++;
        }
    }

    internal void Complete()
    {
        if (pendingCarriageReturn)
        {
            canonicalLineBreaks = false;
        }

        maximumLineBytes = Maximum(maximumLineBytes, currentLineBytes);
        complete = true;
    }

    internal long GetHighBitByteCount()
    {
        EnsureComplete();
        return highBitBytes;
    }

    internal long GetNullByteCount()
    {
        EnsureComplete();
        return nullBytes;
    }

    internal long GetMaximumLineByteCount()
    {
        EnsureComplete();
        return maximumLineBytes;
    }

    internal bool HasCanonicalLineBreaks()
    {
        EnsureComplete();
        return canonicalLineBreaks;
    }

    internal bool IsMostlyText()
    {
        EnsureComplete();
        return sourceBytes == 0 || textBytes >= sourceBytes - sourceBytes / 5;
    }

    void EnsureComplete()
    {
        if (!complete)
        {
            throw new InvalidOperationException("The streaming encoding analysis is not complete.");
        }
    }

    static long Maximum(long left, long right)
    {
        return left >= right ? left : right;
    }
}
