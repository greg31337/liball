using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Parses complete Internet messages or MIME entities from memory or streams.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailParser : IEmailParserContract
{
    internal static EmailParseResult ParseImplementationWithInputUsingValueArray(byte[] input)
    {
        return ParseMessageImplementationWithInputAndOptionsUsingValueArrayAndEmailParseOptions(input, new EmailParseOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailParseResult ParseWithInputUsingValueArray(byte[] input)
    {
        return EmailParser.ParseImplementationWithInputUsingValueArray(input);
    }

    internal static EmailParseResult ParseImplementationWithInputAndOptionsUsingValueArrayAndEmailParseOptions(byte[] input, EmailParseOptions options)
    {
        return ParseMessageImplementationWithInputAndOptionsUsingValueArrayAndEmailParseOptions(input, options);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailParseResult ParseWithInputAndOptionsUsingValueArrayAndEmailParseOptions(byte[] input, EmailParseOptions options)
    {
        return EmailParser.ParseImplementationWithInputAndOptionsUsingValueArrayAndEmailParseOptions(input, options);
    }

    internal static EmailParseResult ParseImplementationWithInputUsingReadOnlyMemory(ReadOnlyMemory<byte> input)
    {
        return ParseMessageImplementationWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(input, new EmailParseOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailParseResult ParseWithInputUsingReadOnlyMemory(ReadOnlyMemory<byte> input)
    {
        return EmailParser.ParseImplementationWithInputUsingReadOnlyMemory(input);
    }

    internal static EmailParseResult ParseImplementationWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(ReadOnlyMemory<byte> input, EmailParseOptions options)
    {
        return ParseMessageImplementationWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(input, options);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailParseResult ParseWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(ReadOnlyMemory<byte> input, EmailParseOptions options)
    {
        return EmailParser.ParseImplementationWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(input, options);
    }

    internal static EmailParseResult ParseImplementationWithInputUsingStream(Stream input)
    {
        return ParseMessageImplementationWithInputAndOptionsUsingStreamAndEmailParseOptions(input, new EmailParseOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailParseResult ParseWithInputUsingStream(Stream input)
    {
        return EmailParser.ParseImplementationWithInputUsingStream(input);
    }

    internal static EmailParseResult ParseImplementationWithInputAndOptionsUsingStreamAndEmailParseOptions(Stream input, EmailParseOptions options)
    {
        return ParseMessageImplementationWithInputAndOptionsUsingStreamAndEmailParseOptions(input, options);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailParseResult ParseWithInputAndOptionsUsingStreamAndEmailParseOptions(Stream input, EmailParseOptions options)
    {
        return EmailParser.ParseImplementationWithInputAndOptionsUsingStreamAndEmailParseOptions(input, options);
    }

    internal static EmailParseResult ParseMessageImplementationWithInputUsingValueArray(byte[] input)
    {
        return ParseMessageImplementationWithInputAndOptionsUsingValueArrayAndEmailParseOptions(input, new EmailParseOptions());
    }

    /// <summary>
    /// Parses the supplied message data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailParseResult ParseMessageWithInputUsingValueArray(byte[] input)
    {
        return EmailParser.ParseMessageImplementationWithInputUsingValueArray(input);
    }

    internal static EmailParseResult ParseMessageImplementationWithInputAndOptionsUsingValueArrayAndEmailParseOptions(byte[] input, EmailParseOptions? options)
    {
        if (input == null)
        {
            throw new ArgumentNullException("input");
        }

        EmailParseOptions snapshot = Snapshot(options);
        EnsureMessageLength(input.Length, snapshot);
        EmailContent sourceContent = new EmailContentOperations().CreateMemory(input, EmailContentOwnership.Borrowed, false);
        return ParseBuffered(input, input.Length, sourceContent, snapshot);
    }

    /// <summary>
    /// Parses the supplied message data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailParseResult ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(byte[] input, EmailParseOptions? options)
    {
        return EmailParser.ParseMessageImplementationWithInputAndOptionsUsingValueArrayAndEmailParseOptions(input, options);
    }

    internal static EmailParseResult ParseMessageImplementationWithInputUsingReadOnlyMemory(ReadOnlyMemory<byte> input)
    {
        return ParseMessageImplementationWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(input, new EmailParseOptions());
    }

    /// <summary>
    /// Parses the supplied message data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailParseResult ParseMessageWithInputUsingReadOnlyMemory(ReadOnlyMemory<byte> input)
    {
        return EmailParser.ParseMessageImplementationWithInputUsingReadOnlyMemory(input);
    }

    internal static EmailParseResult ParseMessageImplementationWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(ReadOnlyMemory<byte> input, EmailParseOptions? options)
    {
        EmailParseOptions snapshot = Snapshot(options);
        EnsureMessageLength(input.Length, snapshot);
        byte[] bytes = input.ToArray();
        EmailContent sourceContent = new EmailContentOperations().CreateMemory(bytes, EmailContentOwnership.Owned, false);
        return ParseBuffered(bytes, bytes.Length, sourceContent, snapshot);
    }

    /// <summary>
    /// Parses the supplied message data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailParseResult ParseMessageWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(ReadOnlyMemory<byte> input, EmailParseOptions? options)
    {
        return EmailParser.ParseMessageImplementationWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(input, options);
    }

    internal static EmailParseResult ParseMessageImplementationWithInputUsingStream(Stream input)
    {
        return ParseMessageImplementationWithInputAndOptionsUsingStreamAndEmailParseOptions(input, new EmailParseOptions());
    }

    /// <summary>
    /// Parses the supplied message data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailParseResult ParseMessageWithInputUsingStream(Stream input)
    {
        return EmailParser.ParseMessageImplementationWithInputUsingStream(input);
    }

    internal static EmailParseResult ParseMessageImplementationWithInputAndOptionsUsingStreamAndEmailParseOptions(Stream? input, EmailParseOptions? options)
    {
        if (input == null)
        {
            throw new ArgumentNullException("input");
        }

        if (!input.CanRead)
        {
            throw new ArgumentException("The input stream must be readable.", "input");
        }

        EmailParseOptions snapshot = Snapshot(options);
        long knownLength = -1;
        EmailContent? borrowedContent = null;
        if (input.CanSeek)
        {
            knownLength = input.Length - input.Position;
            if (knownLength < 0)
            {
                throw new ArgumentException("The stream position exceeds its length.", "input");
            }

            EnsureMessageLength(knownLength, snapshot);
            borrowedContent = new EmailContentOperations().CreateStream(input, EmailContentOwnership.Borrowed);
        }

        EmailParserStreamInput read = ReadStream(input, snapshot);
        EmailContent? sourceContent = borrowedContent;
        if (sourceContent == null)
        {
            sourceContent = new EmailContentOperations().CreateMemory(read.GetPreservedBytes(), EmailContentOwnership.Owned, false);
        }

        return ParseBuffered(read.GetHeaderBytes(), read.GetSourceByteCount(), sourceContent, snapshot);
    }

    /// <summary>
    /// Parses the supplied message data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailParseResult ParseMessageWithInputAndOptionsUsingStreamAndEmailParseOptions(Stream? input, EmailParseOptions? options)
    {
        return EmailParser.ParseMessageImplementationWithInputAndOptionsUsingStreamAndEmailParseOptions(input, options);
    }

    internal static EmailEntityParseResult ParseEntityImplementationWithInputUsingValueArray(byte[] input)
    {
        return new EmailMimeParser().ParseWithInputUsingValueArray(input);
    }

    /// <summary>
    /// Parses the supplied entity data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailEntityParseResult ParseEntityWithInputUsingValueArray(byte[] input)
    {
        return EmailParser.ParseEntityImplementationWithInputUsingValueArray(input);
    }

    internal static EmailEntityParseResult ParseEntityImplementationWithInputAndOptionsUsingValueArrayAndEmailParseOptions(byte[] input, EmailParseOptions options)
    {
        return new EmailMimeParser().ParseWithInputAndOptionsUsingValueArrayAndEmailParseOptions(input, options);
    }

    /// <summary>
    /// Parses the supplied entity data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailEntityParseResult ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(byte[] input, EmailParseOptions options)
    {
        return EmailParser.ParseEntityImplementationWithInputAndOptionsUsingValueArrayAndEmailParseOptions(input, options);
    }

    internal static EmailEntityParseResult ParseEntityImplementationWithInputUsingReadOnlyMemory(ReadOnlyMemory<byte> input)
    {
        return new EmailMimeParser().ParseWithInputUsingReadOnlyMemory(input);
    }

    /// <summary>
    /// Parses the supplied entity data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailEntityParseResult ParseEntityWithInputUsingReadOnlyMemory(ReadOnlyMemory<byte> input)
    {
        return EmailParser.ParseEntityImplementationWithInputUsingReadOnlyMemory(input);
    }

    internal static EmailEntityParseResult ParseEntityImplementationWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(ReadOnlyMemory<byte> input, EmailParseOptions options)
    {
        return new EmailMimeParser().ParseWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(input, options);
    }

    /// <summary>
    /// Parses the supplied entity data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailEntityParseResult ParseEntityWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(ReadOnlyMemory<byte> input, EmailParseOptions options)
    {
        return EmailParser.ParseEntityImplementationWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(input, options);
    }

    internal static EmailEntityParseResult ParseEntityImplementationWithInputUsingStream(Stream input)
    {
        return ParseEntityImplementationWithInputAndOptionsUsingStreamAndEmailParseOptions(input, new EmailParseOptions());
    }

    /// <summary>
    /// Parses the supplied entity data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailEntityParseResult ParseEntityWithInputUsingStream(Stream input)
    {
        return EmailParser.ParseEntityImplementationWithInputUsingStream(input);
    }

    internal static EmailEntityParseResult ParseEntityImplementationWithInputAndOptionsUsingStreamAndEmailParseOptions(Stream input, EmailParseOptions options)
    {
        EmailParseOptions entityOptions = Snapshot(options);
        entityOptions.SetPreserveSource(true);
        EmailParseResult messageResult = ParseMessageImplementationWithInputAndOptionsUsingStreamAndEmailParseOptions(input, entityOptions);
        MimeEntity? entity = null;
        if (messageResult.HasMessage() && (messageResult.GetMessage() ?? throw new InvalidOperationException()).HasSourceInfo() && ((messageResult.GetMessage() ?? throw new InvalidOperationException()).GetSourceInfo() ?? throw new InvalidOperationException()).HasBodyRange())
        {
            EmailMessage? message = messageResult.GetMessage();
            EmailSourceInfo? source = (message ?? throw new InvalidOperationException()).GetSourceInfo();
            entity = new EmailMimeParser().ParseBody(message.GetHeaders(), (source ?? throw new InvalidOperationException()).GetContent(), source.GetMessageRange(), source.GetBodyRange(), entityOptions);
        }

        return new EmailEntityParseResult(entity, messageResult.GetIssues(), messageResult.GetConsumedByteCount(), messageResult.GetSourceByteCount(), messageResult.IsComplete());
    }

    /// <summary>
    /// Parses the supplied entity data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailEntityParseResult ParseEntityWithInputAndOptionsUsingStreamAndEmailParseOptions(Stream input, EmailParseOptions options)
    {
        return EmailParser.ParseEntityImplementationWithInputAndOptionsUsingStreamAndEmailParseOptions(input, options);
    }

    static EmailParseOptions Snapshot(EmailParseOptions? options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return options.Clone();
    }

    static void EnsureMessageLength(long length, EmailParseOptions options)
    {
        long maximum = options.GetLimits().GetParsingLimits().GetMaxMessageBytes();
        if (length > maximum)
        {
            throw new EmailLimitException("MaxMessageBytes", maximum, length);
        }
    }

    static EmailParserStreamInput ReadStream(Stream input, EmailParseOptions options)
    {
        EmailParsingLimits limits = options.GetLimits().GetParsingLimits();
        long maximumMessageBytes = limits.GetMaxMessageBytes();
        long prefixLimit = limits.GetMaxHeaderBytes();
        if (prefixLimit < long.MaxValue)
        {
            prefixLimit++;
        }

        int bufferLength = options.GetStorageOptions().GetCopyBufferBytes();
        byte[] buffer = new byte[bufferLength];
        MemoryStream header = new MemoryStream();
        MemoryStream? preserved = !input.CanSeek ? new MemoryStream() : null;
        long total = 0;
        while (true)
        {
            int read = input.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            if (read > maximumMessageBytes - total)
            {
                throw new EmailLimitException("MaxMessageBytes", maximumMessageBytes, total + read);
            }

            long remainingPrefix = prefixLimit - header.Length;
            if (remainingPrefix > 0)
            {
                int prefixRead = remainingPrefix < read ? (int)remainingPrefix : read;
                header.Write(buffer, 0, prefixRead);
            }

            if (preserved != null)
            {
                preserved.Write(buffer, 0, read);
            }

            total += read;
        }

        byte[]? preservedBytes = preserved == null ? null : preserved.ToArray();
        return new EmailParserStreamInput(header.ToArray(), preservedBytes, total);
    }

    static EmailParseResult ParseBuffered(byte[] headerInput, long sourceByteCount, EmailContent sourceContent, EmailParseOptions options)
    {
        EmailHeaderBlockParserOptions headerOptions = new EmailHeaderBlockParserOptions();
        headerOptions.SetLimits(options.GetLimits().GetParsingLimits());
        headerOptions.SetMode(options.GetMode() == EmailParseMode.Strict ? EmailStructuredValueParseMode.Strict : EmailStructuredValueParseMode.Tolerant);
        headerOptions.SetCompatibilityOptions(options.GetCompatibilityOptions());
        EmailHeaderBlockParseResult headerResult = new EmailHeaderBlockParser().ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(headerInput, headerOptions, new EmailSourcePosition(0, 1, 1), true);
        bool complete = headerResult.IsComplete();
        EmailIssueList issues = headerResult.GetIssues();
        EmailMessage? message = null;
        EmailCompatibilityOptions compatibility = options.GetCompatibilityOptions();
        if (complete || compatibility.GetReturnPartialResults())
        {
            EmailSourceInfo? sourceInfo = CreateSourceInfo(options.GetPreserveSource() ? sourceContent : null, sourceByteCount, headerResult, complete);
            MimeEntity? body = null;
            if (complete)
            {
                long bodyStart = headerResult.GetBodyStartOffset();
                EmailRange bodyRange = new EmailRange(bodyStart, sourceByteCount - bodyStart);
                EmailHeaders contentHeaders = GetContentHeaders(headerResult.GetHeaders());
                EmailParseOptions bodyOptions = options.Clone();
                bodyOptions.SetExchangeCompatibilityContext(HasHeaderPrefix(headerResult.GetHeaders(), "X-MS-Exchange-"));
                bodyOptions.SetGmailCompatibilityContext(HasHeaderPrefix(headerResult.GetHeaders(), "X-Google-") || HasHeaderPrefix(headerResult.GetHeaders(), "X-Gm-"));
                EmailEntityParseResult bodyResult = new EmailMimeParser().ParseBodyResult(contentHeaders, sourceContent, bodyRange, bodyRange, bodyOptions);
                body = bodyResult.GetEntity();
                issues.AddRange(bodyResult.GetIssues());
                complete = bodyResult.IsComplete();
            }

            message = new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(headerResult.GetHeaders(), body, new EmailEnvelopeOperations().CreateWith(), sourceInfo, new EmailIssueList());
        }

        return new EmailParseResult(message, issues, sourceByteCount, sourceByteCount, complete);
    }

    static EmailSourceInfo? CreateSourceInfo(EmailContent? content, long sourceByteCount, EmailHeaderBlockParseResult headerResult, bool complete)
    {
        if (content == null)
        {
            return null;
        }

        EmailRange messageRange = new EmailRange(0, sourceByteCount);
        if (!complete)
        {
            EmailRange incompleteHeaderRange = new EmailRange(0, headerResult.GetConsumedByteCount());
            return new EmailSourceInfoOperations().CreateWithContentAndMessageRangeAndHeaderBlockRangeAndSeparatorRangeAndBodyRangeAndSourceNameAndComplete(content, messageRange, incompleteHeaderRange, null, null, null, false);
        }

        long bodyStart = headerResult.GetBodyStartOffset();
        int separatorLength = headerResult.HasSeparator() ? GetLineEndingLength(headerResult.GetSeparatorLineEnding()) : 0;
        EmailRange headerRange = new EmailRange(0, bodyStart - separatorLength);
        EmailRange? separatorRange = separatorLength == 0 ? null : new EmailRange(bodyStart - separatorLength, separatorLength);
        EmailRange bodyRange = new EmailRange(bodyStart, sourceByteCount - bodyStart);
        return new EmailSourceInfoOperations().CreateWithContentAndMessageRangeAndHeaderBlockRangeAndSeparatorRangeAndBodyRangeAndSourceNameAndComplete(content, messageRange, headerRange, separatorRange, bodyRange, null, true);
    }

    static int GetLineEndingLength(EmailLineEnding ending)
    {
        return ending == EmailLineEnding.CrLf ? 2 : 1;
    }

    static EmailHeaders GetContentHeaders(EmailHeaders headers)
    {
        List<EmailHeader> contentHeaders = new List<EmailHeader>();
        int index = 0;
        while (index < headers.GetCount())
        {
            EmailHeader header = headers.GetAt(index);
            if (header.GetName().StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
            {
                contentHeaders.Add(header.Clone());
            }

            index++;
        }

        return new EmailHeadersOperations().CreateWithPreservedHeaders(contentHeaders);
    }

    static bool HasHeaderPrefix(EmailHeaders headers, string prefix)
    {
        int index = 0;
        while (index < headers.GetCount())
        {
            if (headers.GetAt(index).GetName().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
