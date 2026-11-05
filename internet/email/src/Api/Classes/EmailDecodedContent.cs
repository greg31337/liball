using System;
using System.IO;

namespace email;
/// <summary>
/// Provides operations for working with decoded content.
/// </summary>
/// <remarks>
/// Callers should observe the ownership and lifetime methods when the instance retains streams, temporary storage, or source buffers.
/// </remarks>
public class EmailDecodedContent : IEmailDecodedContentContract
{
    /// <summary>
    /// The default maximum bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaximumBytes = EmailDecodingLimitsConstants.DefaultMaxDecodedBytes;
    /// <summary>
    /// The default buffer bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultBufferBytes = 81920;
    internal static long CopyToImplementationWithContentAndOutput(EmailContent content, Stream output)
    {
        return CopyToImplementationWithContentAndOutputAndMaximumBytes(content, output, DefaultMaximumBytes);
    }

    /// <summary>
    /// Copies the to to a separate destination.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The computed count or size.</returns>
    public long CopyToWithContentAndOutput(EmailContent content, Stream output)
    {
        return EmailDecodedContent.CopyToImplementationWithContentAndOutput(content, output);
    }

    internal static long CopyToImplementationWithContentAndOutputAndMaximumBytes(EmailContent content, Stream output, long maximumBytes)
    {
        ValidateContent(content);
        ValidateOutput(output);
        ValidateMaximumBytes(maximumBytes);
        EmailDecodedContentSink sink = new EmailDecodedContentSink(output, null, maximumBytes, false);
        ProcessIdentity(content, sink);
        return sink.GetByteCount();
    }

    /// <summary>
    /// Copies the to to a separate destination.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    public long CopyToWithContentAndOutputAndMaximumBytes(EmailContent content, Stream output, long maximumBytes)
    {
        return EmailDecodedContent.CopyToImplementationWithContentAndOutputAndMaximumBytes(content, output, maximumBytes);
    }

    internal static long CopyToImplementationWithContentAndOutputAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(EmailContent? content, Stream output, long maximumBytes, string limitName, long reportedLimit, long reportedObserved)
    {
        ValidateContent(content);
        ValidateOutput(output);
        ValidateMaximumBytes(maximumBytes);
        EmailDecodedContentSink sink = EmailDecodedContentSink.CreateWithOutputAndHashingAndMaximumBytesAndStopAtMaximumAndLimitNameAndReportedLimitAndReportedObserved(output, null, maximumBytes, false, limitName, reportedLimit, reportedObserved);
        ProcessIdentity(content, sink);
        return sink.GetByteCount();
    }

    /// <summary>
    /// Copies the to to a separate destination.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <param name = "limitName">The limit name used by the operation.</param>
    /// <param name = "reportedLimit">The reported limit used by the operation.</param>
    /// <param name = "reportedObserved">The reported observed used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    public long CopyToWithContentAndOutputAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(EmailContent? content, Stream output, long maximumBytes, string limitName, long reportedLimit, long reportedObserved)
    {
        return EmailDecodedContent.CopyToImplementationWithContentAndOutputAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(content, output, maximumBytes, limitName, reportedLimit, reportedObserved);
    }

    internal static long CopyToImplementationWithEntityAndOutput(MimeEntity entity, Stream output)
    {
        return CopyToImplementationWithEntityAndOutputAndMaximumBytes(entity, output, DefaultMaximumBytes);
    }

    /// <summary>
    /// Copies the to to a separate destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The computed count or size.</returns>
    public long CopyToWithEntityAndOutput(MimeEntity entity, Stream output)
    {
        return EmailDecodedContent.CopyToImplementationWithEntityAndOutput(entity, output);
    }

    internal static long CopyToImplementationWithEntityAndOutputAndMaximumBytes(MimeEntity entity, Stream output, long maximumBytes)
    {
        ValidateEntity(entity);
        ValidateOutput(output);
        ValidateMaximumBytes(maximumBytes);
        EmailDecodedContentSink sink = new EmailDecodedContentSink(output, null, maximumBytes, false);
        ProcessEntity(entity, sink);
        return sink.GetByteCount();
    }

    /// <summary>
    /// Copies the to to a separate destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    public long CopyToWithEntityAndOutputAndMaximumBytes(MimeEntity entity, Stream output, long maximumBytes)
    {
        return EmailDecodedContent.CopyToImplementationWithEntityAndOutputAndMaximumBytes(entity, output, maximumBytes);
    }

    internal static long CopyToImplementationWithEntityAndOutputAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(MimeEntity entity, Stream output, long maximumBytes, string limitName, long reportedLimit, long reportedObserved)
    {
        ValidateEntity(entity);
        ValidateOutput(output);
        ValidateMaximumBytes(maximumBytes);
        EmailDecodedContentSink sink = EmailDecodedContentSink.CreateWithOutputAndHashingAndMaximumBytesAndStopAtMaximumAndLimitNameAndReportedLimitAndReportedObserved(output, null, maximumBytes, false, limitName, reportedLimit, reportedObserved);
        ProcessEntity(entity, sink);
        return sink.GetByteCount();
    }

    /// <summary>
    /// Copies the to to a separate destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <param name = "limitName">The limit name used by the operation.</param>
    /// <param name = "reportedLimit">The reported limit used by the operation.</param>
    /// <param name = "reportedObserved">The reported observed used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    public long CopyToWithEntityAndOutputAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(MimeEntity entity, Stream output, long maximumBytes, string limitName, long reportedLimit, long reportedObserved)
    {
        return EmailDecodedContent.CopyToImplementationWithEntityAndOutputAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(entity, output, maximumBytes, limitName, reportedLimit, reportedObserved);
    }

    internal static long SaveImplementationWithContentAndPath(EmailContent content, string path)
    {
        return SaveImplementationWithContentAndPathAndMaximumBytes(content, path, DefaultMaximumBytes);
    }

    /// <summary>
    /// Saves the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>The computed count or size.</returns>
    public long SaveWithContentAndPath(EmailContent content, string path)
    {
        return EmailDecodedContent.SaveImplementationWithContentAndPath(content, path);
    }

    internal static long SaveImplementationWithContentAndPathAndMaximumBytes(EmailContent content, string path, long maximumBytes)
    {
        ValidateContent(content);
        ValidateMaximumBytes(maximumBytes);
        string fullPath = ValidatePath(path);
        using (FileStream output = CreateOutputFile(fullPath))
        {
            return CopyToImplementationWithContentAndOutputAndMaximumBytes(content, output, maximumBytes);
        }
    }

    /// <summary>
    /// Saves the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    public long SaveWithContentAndPathAndMaximumBytes(EmailContent content, string path, long maximumBytes)
    {
        return EmailDecodedContent.SaveImplementationWithContentAndPathAndMaximumBytes(content, path, maximumBytes);
    }

    internal static long SaveImplementationWithEntityAndPath(MimeEntity entity, string path)
    {
        return SaveImplementationWithEntityAndPathAndMaximumBytes(entity, path, DefaultMaximumBytes);
    }

    /// <summary>
    /// Saves the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>The computed count or size.</returns>
    public long SaveWithEntityAndPath(MimeEntity entity, string path)
    {
        return EmailDecodedContent.SaveImplementationWithEntityAndPath(entity, path);
    }

    internal static long SaveImplementationWithEntityAndPathAndMaximumBytes(MimeEntity entity, string path, long maximumBytes)
    {
        ValidateEntity(entity);
        ValidateMaximumBytes(maximumBytes);
        string fullPath = ValidatePath(path);
        using (FileStream output = CreateOutputFile(fullPath))
        {
            return CopyToImplementationWithEntityAndOutputAndMaximumBytes(entity, output, maximumBytes);
        }
    }

    /// <summary>
    /// Saves the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    public long SaveWithEntityAndPathAndMaximumBytes(MimeEntity entity, string path, long maximumBytes)
    {
        return EmailDecodedContent.SaveImplementationWithEntityAndPathAndMaximumBytes(entity, path, maximumBytes);
    }

    internal static byte[] ReadPrefixImplementationWithContentAndMaximumPrefixBytes(EmailContent content, int maximumPrefixBytes)
    {
        ValidateContent(content);
        ValidatePrefixBytes(maximumPrefixBytes);
        using (MemoryStream output = new MemoryStream())
        {
            EmailDecodedContentSink sink = new EmailDecodedContentSink(output, null, maximumPrefixBytes, true);
            ProcessIdentity(content, sink);
            return output.ToArray();
        }
    }

    /// <summary>
    /// Reads the prefix from the input.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "maximumPrefixBytes">The source bytes or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[] ReadPrefixWithContentAndMaximumPrefixBytes(EmailContent content, int maximumPrefixBytes)
    {
        return EmailDecodedContent.ReadPrefixImplementationWithContentAndMaximumPrefixBytes(content, maximumPrefixBytes);
    }

    internal static byte[] ReadPrefixImplementationWithEntityAndMaximumPrefixBytes(MimeEntity entity, int maximumPrefixBytes)
    {
        ValidateEntity(entity);
        ValidatePrefixBytes(maximumPrefixBytes);
        using (MemoryStream output = new MemoryStream())
        {
            EmailDecodedContentSink sink = new EmailDecodedContentSink(output, null, maximumPrefixBytes, true);
            ProcessEntity(entity, sink);
            return output.ToArray();
        }
    }

    /// <summary>
    /// Reads the prefix from the input.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "maximumPrefixBytes">The source bytes or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[] ReadPrefixWithEntityAndMaximumPrefixBytes(MimeEntity entity, int maximumPrefixBytes)
    {
        return EmailDecodedContent.ReadPrefixImplementationWithEntityAndMaximumPrefixBytes(entity, maximumPrefixBytes);
    }

    internal static long MeasureLengthImplementationWithContent(EmailContent content)
    {
        return MeasureLengthImplementationWithContentAndMaximumBytes(content, DefaultMaximumBytes);
    }

    /// <summary>
    /// Measures the length.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    public long MeasureLengthWithContent(EmailContent content)
    {
        return EmailDecodedContent.MeasureLengthImplementationWithContent(content);
    }

    internal static long MeasureLengthImplementationWithContentAndMaximumBytes(EmailContent content, long maximumBytes)
    {
        ValidateContent(content);
        ValidateMaximumBytes(maximumBytes);
        EmailDecodedContentSink sink = new EmailDecodedContentSink(null, null, maximumBytes, false);
        ProcessIdentity(content, sink);
        return sink.GetByteCount();
    }

    /// <summary>
    /// Measures the length.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    public long MeasureLengthWithContentAndMaximumBytes(EmailContent content, long maximumBytes)
    {
        return EmailDecodedContent.MeasureLengthImplementationWithContentAndMaximumBytes(content, maximumBytes);
    }

    internal static long MeasureLengthImplementationWithContentAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(EmailContent content, long maximumBytes, string limitName, long reportedLimit, long reportedObserved)
    {
        ValidateContent(content);
        ValidateMaximumBytes(maximumBytes);
        EmailDecodedContentSink sink = EmailDecodedContentSink.CreateWithOutputAndHashingAndMaximumBytesAndStopAtMaximumAndLimitNameAndReportedLimitAndReportedObserved(null, null, maximumBytes, false, limitName, reportedLimit, reportedObserved);
        ProcessIdentity(content, sink);
        return sink.GetByteCount();
    }

    /// <summary>
    /// Measures the length.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <param name = "limitName">The limit name used by the operation.</param>
    /// <param name = "reportedLimit">The reported limit used by the operation.</param>
    /// <param name = "reportedObserved">The reported observed used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    public long MeasureLengthWithContentAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(EmailContent content, long maximumBytes, string limitName, long reportedLimit, long reportedObserved)
    {
        return EmailDecodedContent.MeasureLengthImplementationWithContentAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(content, maximumBytes, limitName, reportedLimit, reportedObserved);
    }

    internal static long MeasureLengthImplementationWithEntity(MimeEntity entity)
    {
        return MeasureLengthImplementationWithEntityAndMaximumBytes(entity, DefaultMaximumBytes);
    }

    /// <summary>
    /// Measures the length.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The computed count or size.</returns>
    public long MeasureLengthWithEntity(MimeEntity entity)
    {
        return EmailDecodedContent.MeasureLengthImplementationWithEntity(entity);
    }

    internal static long MeasureLengthImplementationWithEntityAndMaximumBytes(MimeEntity entity, long maximumBytes)
    {
        ValidateEntity(entity);
        ValidateMaximumBytes(maximumBytes);
        EmailDecodedContentSink sink = new EmailDecodedContentSink(null, null, maximumBytes, false);
        ProcessEntity(entity, sink);
        return sink.GetByteCount();
    }

    /// <summary>
    /// Measures the length.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    public long MeasureLengthWithEntityAndMaximumBytes(MimeEntity entity, long maximumBytes)
    {
        return EmailDecodedContent.MeasureLengthImplementationWithEntityAndMaximumBytes(entity, maximumBytes);
    }

    internal static long MeasureLengthImplementationWithEntityAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(MimeEntity entity, long maximumBytes, string limitName, long reportedLimit, long reportedObserved)
    {
        ValidateEntity(entity);
        ValidateMaximumBytes(maximumBytes);
        EmailDecodedContentSink sink = EmailDecodedContentSink.CreateWithOutputAndHashingAndMaximumBytesAndStopAtMaximumAndLimitNameAndReportedLimitAndReportedObserved(null, null, maximumBytes, false, limitName, reportedLimit, reportedObserved);
        ProcessEntity(entity, sink);
        return sink.GetByteCount();
    }

    /// <summary>
    /// Measures the length.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <param name = "limitName">The limit name used by the operation.</param>
    /// <param name = "reportedLimit">The reported limit used by the operation.</param>
    /// <param name = "reportedObserved">The reported observed used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    public long MeasureLengthWithEntityAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(MimeEntity entity, long maximumBytes, string limitName, long reportedLimit, long reportedObserved)
    {
        return EmailDecodedContent.MeasureLengthImplementationWithEntityAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(entity, maximumBytes, limitName, reportedLimit, reportedObserved);
    }

    internal static EmailHashResult ComputeHashImplementationWithContentAndAlgorithm(EmailContent content, EmailHashAlgorithm algorithm)
    {
        return ComputeHashImplementationWithContentAndAlgorithmAndMaximumBytes(content, algorithm, DefaultMaximumBytes);
    }

    /// <summary>
    /// Computes the hash.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <returns>The hash result produced by the operation.</returns>
    public EmailHashResult ComputeHashWithContentAndAlgorithm(EmailContent content, EmailHashAlgorithm algorithm)
    {
        return EmailDecodedContent.ComputeHashImplementationWithContentAndAlgorithm(content, algorithm);
    }

    internal static EmailHashResult ComputeHashImplementationWithContentAndAlgorithmAndMaximumBytes(EmailContent content, EmailHashAlgorithm algorithm, long maximumBytes)
    {
        ValidateContent(content);
        ValidateMaximumBytes(maximumBytes);
        EmailHashingStream hashing = new EmailHashingStream(algorithm);
        EmailDecodedContentSink sink = new EmailDecodedContentSink(null, hashing, maximumBytes, false);
        ProcessIdentity(content, sink);
        return hashing.Complete();
    }

    /// <summary>
    /// Computes the hash.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    public EmailHashResult ComputeHashWithContentAndAlgorithmAndMaximumBytes(EmailContent content, EmailHashAlgorithm algorithm, long maximumBytes)
    {
        return EmailDecodedContent.ComputeHashImplementationWithContentAndAlgorithmAndMaximumBytes(content, algorithm, maximumBytes);
    }

    internal static EmailHashResult ComputeHashImplementationWithEntityAndAlgorithm(MimeEntity entity, EmailHashAlgorithm algorithm)
    {
        return ComputeHashImplementationWithEntityAndAlgorithmAndMaximumBytes(entity, algorithm, DefaultMaximumBytes);
    }

    /// <summary>
    /// Computes the hash.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <returns>The hash result produced by the operation.</returns>
    public EmailHashResult ComputeHashWithEntityAndAlgorithm(MimeEntity entity, EmailHashAlgorithm algorithm)
    {
        return EmailDecodedContent.ComputeHashImplementationWithEntityAndAlgorithm(entity, algorithm);
    }

    internal static EmailHashResult ComputeHashImplementationWithEntityAndAlgorithmAndMaximumBytes(MimeEntity entity, EmailHashAlgorithm algorithm, long maximumBytes)
    {
        ValidateEntity(entity);
        ValidateMaximumBytes(maximumBytes);
        EmailHashingStream hashing = new EmailHashingStream(algorithm);
        EmailDecodedContentSink sink = new EmailDecodedContentSink(null, hashing, maximumBytes, false);
        ProcessEntity(entity, sink);
        return hashing.Complete();
    }

    /// <summary>
    /// Computes the hash.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    public EmailHashResult ComputeHashWithEntityAndAlgorithmAndMaximumBytes(MimeEntity entity, EmailHashAlgorithm algorithm, long maximumBytes)
    {
        return EmailDecodedContent.ComputeHashImplementationWithEntityAndAlgorithmAndMaximumBytes(entity, algorithm, maximumBytes);
    }

    static void ProcessEntity(MimeEntity entity, EmailDecodedContentSink sink)
    {
        if (entity.HasDecodedContent())
        {
            ProcessIdentity(entity.GetDecodedContent(), sink);
            return;
        }

        if (!entity.HasRawContent())
        {
            throw new InvalidOperationException("The entity has no content to decode.");
        }

        if (entity.HasTransferEncoding() && !entity.HasKnownTransferEncoding())
        {
            throw new InvalidOperationException("The entity content-transfer-encoding is not supported.");
        }

        EmailTransferEncodingKind kind = entity.GetEffectiveTransferEncodingKind();
        if (kind == EmailTransferEncodingKind.Base64 || kind == EmailTransferEncodingKind.QuotedPrintable)
        {
            ProcessEncoded(entity.GetRawContent(), kind, sink);
        }
        else
        {
            ProcessIdentity(entity.GetRawContent(), sink);
        }
    }

    static void ProcessIdentity(EmailContent? content, EmailDecodedContentSink sink)
    {
        if (!sink.StopsAtMaximum() && (content ?? throw new InvalidOperationException()).HasKnownLength() && content.GetLength() > sink.GetMaximumBytes())
        {
            throw sink.CreateLimitException(content.GetLength());
        }

        if (!sink.WantsMore())
        {
            return;
        }

        Stream? input = OpenInput(content);
        byte[] transfer = new byte[DefaultBufferBytes];
        long remaining = (content ?? throw new InvalidOperationException()).HasKnownLength() ? content.GetLength() : -1;
        bool finished = false;
        while (!finished && sink.WantsMore())
        {
            int requested = GetRequestedByteCount(transfer.Length, remaining, sink);
            if (requested == 0)
            {
                break;
            }

            int read = (input ?? throw new InvalidOperationException()).Read(transfer, 0, requested);
            if (read == 0)
            {
                if (remaining > 0)
                {
                    CloseInput(content, input);
                    throw new EndOfStreamException("The content source ended before its declared length.");
                }

                finished = true;
            }
            else
            {
                sink.Write(transfer, 0, read);
                if (remaining >= 0)
                {
                    remaining -= read;
                }
            }
        }

        CloseInput(content, input);
    }

    static void ProcessEncoded(EmailContent? content, EmailTransferEncodingKind kind, EmailDecodedContentSink sink)
    {
        if (!sink.WantsMore())
        {
            return;
        }

        long decoderMaximumBytes = GetDecoderMaximumBytes(sink);
        EmailBase64Decoder? base64 = kind == EmailTransferEncodingKind.Base64 ? new EmailBase64Decoder(EmailBase64DecodeMode.Tolerant, decoderMaximumBytes) : null;
        EmailQuotedPrintableDecoder? quotedPrintable = kind == EmailTransferEncodingKind.QuotedPrintable ? new EmailQuotedPrintableDecoder(EmailQuotedPrintableDecodeMode.Tolerant, decoderMaximumBytes) : null;
        Stream? input = OpenInput(content);
        byte[] transfer = new byte[DefaultBufferBytes];
        long remaining = (content ?? throw new InvalidOperationException()).HasKnownLength() ? content.GetLength() : -1;
        bool finished = false;
        while (!finished && sink.WantsMore())
        {
            int requested = GetEncodedRequestedByteCount(transfer.Length, remaining, sink);
            if (requested == 0)
            {
                break;
            }

            int read = (input ?? throw new InvalidOperationException()).Read(transfer, 0, requested);
            if (read == 0)
            {
                if (remaining > 0)
                {
                    CloseInput(content, input);
                    throw new EndOfStreamException("The encoded content source ended before its declared length.");
                }

                finished = true;
            }
            else
            {
                byte[] decoded = Decode(base64, quotedPrintable, transfer, read);
                sink.Write(decoded, 0, decoded.Length);
                if (remaining >= 0)
                {
                    remaining -= read;
                }

                if (HasFatalError(base64, quotedPrintable) && !sink.StopsAtMaximum())
                {
                    CloseInput(content, input);
                    throw sink.CreateLimitException(GetOverLimitObserved(sink.GetMaximumBytes()));
                }
            }
        }

        if (!sink.StopsAtMaximum() || sink.WantsMore())
        {
            byte[] tail = Complete(base64, quotedPrintable);
            sink.Write(tail, 0, tail.Length);
            if (HasFatalError(base64, quotedPrintable))
            {
                CloseInput(content, input);
                throw sink.CreateLimitException(GetOverLimitObserved(sink.GetMaximumBytes()));
            }
        }

        CloseInput(content, input);
    }

    static byte[] Decode(EmailBase64Decoder? base64, EmailQuotedPrintableDecoder? quotedPrintable, byte[] input, int count)
    {
        ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(input, 0, count);
        return base64 == null ? (quotedPrintable ?? throw new InvalidOperationException()).Feed(value) : base64.Feed(value);
    }

    static byte[] Complete(EmailBase64Decoder? base64, EmailQuotedPrintableDecoder? quotedPrintable)
    {
        return base64 == null ? (quotedPrintable ?? throw new InvalidOperationException()).Complete() : base64.Complete();
    }

    static bool HasFatalError(EmailBase64Decoder? base64, EmailQuotedPrintableDecoder? quotedPrintable)
    {
        return base64 == null ? (quotedPrintable ?? throw new InvalidOperationException()).HasFatalError() : base64.HasFatalError();
    }

    static Stream? OpenInput(EmailContent? content)
    {
        if ((content ?? throw new InvalidOperationException()).IsRepeatable() && content.HasKnownLength())
        {
            return content.OpenReadRange(0, content.GetLength());
        }

        return content.OpenRead();
    }

    static void CloseInput(EmailContent content, Stream? input)
    {
        if (!content.IsRangeReadStreamBorrowed())
        {
            (input ?? throw new InvalidOperationException()).Close();
        }
    }

    static int GetRequestedByteCount(int bufferBytes, long remaining, EmailDecodedContentSink sink)
    {
        int requested = remaining >= 0 && remaining < bufferBytes ? (int)remaining : bufferBytes;
        if (sink.StopsAtMaximum())
        {
            long wanted = sink.GetMaximumBytes() - sink.GetByteCount();
            if (wanted < requested)
            {
                requested = (int)wanted;
            }
        }

        return requested;
    }

    static int GetEncodedRequestedByteCount(int bufferBytes, long remaining, EmailDecodedContentSink sink)
    {
        int requested = remaining >= 0 && remaining < bufferBytes ? (int)remaining : bufferBytes;
        if (sink.StopsAtMaximum())
        {
            long wanted = sink.GetMaximumBytes() - sink.GetByteCount();
            long usefulInput = wanted > (int.MaxValue - 16) / 3 ? int.MaxValue : wanted * 3 + 16;
            if (usefulInput < requested)
            {
                requested = (int)usefulInput;
            }
        }

        return requested;
    }

    static long GetDecoderMaximumBytes(EmailDecodedContentSink sink)
    {
        if (!sink.StopsAtMaximum() || sink.GetMaximumBytes() > long.MaxValue - 3)
        {
            return sink.GetMaximumBytes();
        }

        return sink.GetMaximumBytes() + 3;
    }

    static long GetOverLimitObserved(long maximumBytes)
    {
        return maximumBytes == long.MaxValue ? long.MaxValue : maximumBytes + 1;
    }

    static FileStream CreateOutputFile(string path)
    {
        return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, DefaultBufferBytes, FileOptions.SequentialScan);
    }

    static string ValidatePath(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        if (path.Length == 0)
        {
            throw new ArgumentException("File paths cannot be empty.", "path");
        }

        return Path.GetFullPath(path);
    }

    static void ValidateContent(EmailContent? content)
    {
        if (content == null)
        {
            throw new ArgumentNullException("content");
        }

        if (content.IsClosed())
        {
            throw new ObjectDisposedException("content");
        }
    }

    static void ValidateEntity(MimeEntity entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException("entity");
        }

        if (!entity.HasContent())
        {
            throw new InvalidOperationException("The entity has no content to decode.");
        }

        if (!entity.HasDecodedContent() && entity.HasTransferEncoding() && !entity.HasKnownTransferEncoding())
        {
            throw new InvalidOperationException("The entity content-transfer-encoding is not supported.");
        }
    }

    static void ValidateOutput(Stream output)
    {
        if (output == null)
        {
            throw new ArgumentNullException("output");
        }

        if (!output.CanWrite)
        {
            throw new ArgumentException("The output stream must be writable.", "output");
        }
    }

    static void ValidatePrefixBytes(int maximumPrefixBytes)
    {
        if (maximumPrefixBytes < 0)
        {
            throw new ArgumentOutOfRangeException("maximumPrefixBytes");
        }
    }

    static void ValidateMaximumBytes(long maximumBytes)
    {
        if (maximumBytes < 0)
        {
            throw new ArgumentOutOfRangeException("maximumBytes");
        }
    }
}
